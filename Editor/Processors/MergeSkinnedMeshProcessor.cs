using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Anatawa12.Merger.Processors
{
    internal class MergeSkinnedMeshProcessor
    {
        public void Merge(MergerSession session)
        {
            foreach (var mergePhysBone in session.GetComponents<MergeSkinnedMesh>())
            {
                DoMerge(mergePhysBone, session);
            }
        }

        private void DoMerge(MergeSkinnedMesh merge, MergerSession session)
        {
            var trianglesTotalCount = merge.renderers.Sum(x => x.sharedMesh.triangles.Length);
            var boneTotalCount = merge.renderers.Sum(x => x.sharedMesh.bindposes.Length);
            var vertexTotalCount = merge.renderers.Sum(x => x.sharedMesh.vertexCount);
            var boneWeightsTotalCount = merge.renderers.Sum(x => x.sharedMesh.GetAllBoneWeights().Length);
            var (subMeshIndexMap, subMeshesTotalCount) = CreateSubMeshIndexMapping(merge);

            // bounds attributes
            var min = Vector3.positiveInfinity;
            var max = Vector3.negativeInfinity;
            // render bounds
            var renderMin = Vector3.positiveInfinity;
            var renderMax = Vector3.negativeInfinity;

            // vertex attributes
            Vector3[] vertices = new Vector3[vertexTotalCount];
            Vector3[] normals = null;
            Vector4[] tangents = null;
            Vector2[] uv = null;
            Vector2[] uv2 = null;
            Vector2[] uv3 = null;
            Vector2[] uv4 = null;
            Vector2[] uv5 = null;
            Vector2[] uv6 = null;
            Vector2[] uv7 = null;
            Vector2[] uv8 = null;
            //Color[] colors = null; // same as colors32
            Color32[] colors32 = null;
            var bonesPerVertex = new NativeArray<byte>(vertexTotalCount, Allocator.Temp);

            // bone attributes
            var bones = new Transform[boneTotalCount];
            var bindposes = new Matrix4x4[boneTotalCount];

            // others
            var boneWeights = new NativeArray<BoneWeight1>(boneWeightsTotalCount, Allocator.Temp);

            // blendShapes
            var blendShapeNames = new List<string>();
            var blendShapes = new Dictionary<string, (Vector3[] vertex, Vector3[] normal, Vector3[] tangent)>();

            // subMeshes
            var materials = new Material[subMeshesTotalCount];
            var subMeshInfos = new List<(int vertexBase, int[] triangles, SubMeshDescriptor submesh)>[subMeshesTotalCount];
            for (var i = 0; i < subMeshInfos.Length; i++)
                subMeshInfos[i] = new List<(int, int[], SubMeshDescriptor)>();

            var verticesBase = 0;
            var boneBase = 0;
            var boneWeightsBase = 0;
            var subMeshesBase = 0;

            // collect bones
            // ReSharper disable once LocalVariableHidesMember
            for (var rendererIndex = 0; rendererIndex < merge.renderers.Length; rendererIndex++)
            {
                var renderer = merge.renderers[rendererIndex];
                var mesh = renderer.sharedMesh;
                var vertexCount = mesh.vertexCount;

                var bounds = mesh.bounds;
                min = Vector3.Min(min, bounds.min);
                max = Vector3.Max(max, bounds.max);
                renderMin = Vector3.Min(renderMin, renderer.bounds.min);
                renderMax = Vector3.Max(renderMax, renderer.bounds.max);

                // vertex attributes
                Copy(verticesBase, vertexCount, vertexTotalCount, mesh.vertices, ref vertices);
                Copy(verticesBase, vertexCount, vertexTotalCount, mesh.normals, ref normals);
                Copy(verticesBase, vertexCount, vertexTotalCount, mesh.tangents, ref tangents);
                Copy(verticesBase, vertexCount, vertexTotalCount, mesh.uv, ref uv);
                Copy(verticesBase, vertexCount, vertexTotalCount, mesh.uv2, ref uv2);
                Copy(verticesBase, vertexCount, vertexTotalCount, mesh.uv3, ref uv3);
                Copy(verticesBase, vertexCount, vertexTotalCount, mesh.uv4, ref uv4);
                Copy(verticesBase, vertexCount, vertexTotalCount, mesh.uv5, ref uv5);
                Copy(verticesBase, vertexCount, vertexTotalCount, mesh.uv6, ref uv6);
                Copy(verticesBase, vertexCount, vertexTotalCount, mesh.uv7, ref uv7);
                Copy(verticesBase, vertexCount, vertexTotalCount, mesh.uv8, ref uv8);
                //Copy(verticesBase, vertexCount, vertexTotalCount, mesh.colors, ref colors);
                Copy(verticesBase, vertexCount, vertexTotalCount, mesh.colors32, ref colors32);
                Copy(verticesBase, vertexCount, vertexTotalCount, mesh.GetBonesPerVertex(), bonesPerVertex);

                // bone attributes
                var rendererBones = renderer.bones;
                var bindposesCount = mesh.bindposes.Length;
                Copy(boneBase, bindposesCount, boneTotalCount, rendererBones, ref bones);
                Copy(boneBase, bindposesCount, boneTotalCount, mesh.bindposes, ref bindposes);

                // other attributes
                var meshTriangles = mesh.triangles;

                var meshBoneWeights = mesh.GetAllBoneWeights();
                for (var i = 0; i < meshBoneWeights.Length; i++)
                {
                    var weight = meshBoneWeights[i];
                    boneWeights[boneWeightsBase + i] = new BoneWeight1()
                    {
                        weight = weight.weight,
                        boneIndex = weight.boneIndex + boneBase,
                    };
                }

                // blendShapes
                for (var i = 0; i < mesh.blendShapeCount; i++)
                {
                    Assert.AreEqual(1, mesh.GetBlendShapeFrameCount(i));
                    Assert.AreEqual(100.0f, mesh.GetBlendShapeFrameWeight(i, 0));
                    var deltaVertices = new Vector3[vertexCount];
                    var deltaNormals = new Vector3[vertexCount];
                    var deltaTangents = new Vector3[vertexCount];
                    mesh.GetBlendShapeFrameVertices(i, 0, deltaVertices, deltaNormals, deltaTangents);
                    var shapeName = mesh.GetBlendShapeName(i);
                    if (!blendShapes.TryGetValue(shapeName, out var tuple))
                    {
                        blendShapeNames.Add(shapeName);
                        tuple = default;
                    }

                    Copy(verticesBase, vertexCount, vertexTotalCount, deltaVertices, ref tuple.vertex);
                    Copy(verticesBase, vertexCount, vertexTotalCount, deltaNormals, ref tuple.normal);
                    Copy(verticesBase, vertexCount, vertexTotalCount, deltaTangents, ref tuple.tangent);
                    blendShapes[shapeName] = tuple;
                }

                // subMeshes
                for (var i = 0; i < mesh.subMeshCount; i++)
                    subMeshInfos[subMeshIndexMap[(rendererIndex, i)]]
                        .Add((verticesBase, meshTriangles, mesh.GetSubMesh(i)));

                var activeMaterialsCount = Math.Min(mesh.subMeshCount, renderer.materials.Length);
                for (var i = 0; i < activeMaterialsCount; i++)
                    materials[subMeshIndexMap[(rendererIndex, i)]] = renderer.materials[i];

                verticesBase += vertexCount;
                boneBase += bindposesCount;
                boneWeightsBase += meshBoneWeights.Length;
                subMeshesBase += mesh.subMeshCount;
            }

            // create triangles & subMeshes
            var triangles = new int[trianglesTotalCount];
            var subMeshes = new SubMeshDescriptor[subMeshesTotalCount];
            
            var trianglesIndex = 0;
            for (var i = 0; i < subMeshInfos.Length; i++)
            {
                var indexStart = trianglesIndex;
                foreach (var (vertexBase, sourceTriangles, subMesh) in subMeshInfos[i])
                {
                    Assert.AreEqual(MeshTopology.Triangles, subMesh.topology);
                    for (var j = 0; j < subMesh.indexCount; j++, trianglesIndex++)
                        triangles[trianglesIndex] = sourceTriangles[j + subMesh.indexStart] + vertexBase;
                }

                var length = trianglesIndex - indexStart;

                subMeshes[i] = new SubMeshDescriptor(indexStart, length);
            }

            // create mesh
            var newRenderer = merge.gameObject.GetOrAddComponent<SkinnedMeshRenderer>();
            var newMesh = new Mesh();
            var newBounds = new Bounds();
            newMesh.Clear();
            newMesh.ClearBlendShapes();
            newBounds.SetMinMax(min, max);
            newMesh.bounds = newBounds;
            newMesh.vertices = vertices;
            if (normals != null) newMesh.normals = normals;
            if (tangents != null) newMesh.tangents = tangents;
            if (uv != null) newMesh.uv = uv;
            if (uv2 != null) newMesh.uv2 = uv2;
            if (uv3 != null) newMesh.uv3 = uv3;
            if (uv4 != null) newMesh.uv4 = uv4;
            if (uv5 != null) newMesh.uv5 = uv5;
            if (uv6 != null) newMesh.uv6 = uv6;
            if (uv7 != null) newMesh.uv7 = uv7;
            if (uv8 != null) newMesh.uv8 = uv8;
            if (colors32 != null) newMesh.colors32 = colors32;
            newMesh.bindposes = bindposes;
            newMesh.triangles = triangles;
            newMesh.SetBoneWeights(bonesPerVertex, boneWeights);
            foreach (var blendShapeName in blendShapeNames)
            {
                var (vertex, normal, tangent) = blendShapes[blendShapeName];
                newMesh.AddBlendShapeFrame(blendShapeName, 100, vertex, normal, tangent);
            }

            newMesh.subMeshCount = subMeshes.Length;
            for (var i = 0; i < subMeshes.Length; i++)
                newMesh.SetSubMesh(i, subMeshes[i]);

            newRenderer.bones = bones;
            newRenderer.sharedMesh = newMesh;
            newRenderer.materials = materials;
            //newBounds.SetMinMax(renderMin, renderMax);
            //newRenderer.bounds = newBounds;

            foreach (var renderer in merge.renderers)
            {
                session.AddObjectMapping(renderer, newRenderer);
                session.Destroy(renderer);
            }
            session.Destroy(merge);
        }

        private (Dictionary<(int rendererIndex, int submeshIndex), int> mapping, int subMeshTotalCount) CreateSubMeshIndexMapping(MergeSkinnedMesh merge)
        {
            // initial mapping: 
            var result = merge.merges
                .SelectMany((x, i) => x.merges.Select(y => ((int)(y >> 32), (int)y, i)))
                .ToDictionary(x => (x.Item1, x.Item2), x => x.Item3);
            var nextIndex = merge.merges.Length;

            for (var i = 0; i < merge.renderers.Length; i++)
            {
                var renderer = merge.renderers[i];
                for (var j = 0; j < renderer.sharedMesh.subMeshCount; j++)
                {
                    if (!result.ContainsKey((i, j)))
                        result[(i, j)] = nextIndex++;
                }
            }

            return (result, nextIndex);
        }

        private static void Copy<T>(int baseIndex, int count, int totalLength, T[] src, ref T[] dest)
        {
            if (src == null || src.Length == 0) return;
            if (dest == null) dest = new T[totalLength];
            Array.Copy(src, 0, dest, baseIndex, count);
        }

        private static void Copy<T>(int baseIndex, int count, int totalLength, NativeArray<T> src, NativeArray<T> dest)
            where T : struct
        {
            if (src.Length == 0) return;
            NativeArray<T>.Copy(src, 0, dest, baseIndex, count);
        }
    }
}
