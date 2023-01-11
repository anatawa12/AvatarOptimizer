using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class MergeSkinnedMeshProcessor
    {
        public void Process(OptimizerSession session)
        {
            var proceed = new HashSet<MergeSkinnedMesh>();
            foreach (var mergePhysBone in session.GetComponents<MergeSkinnedMesh>())
            {
                DoMerge(mergePhysBone, session, proceed);
            }
        }

        private class MeshInfo
        {
            public Bounds Bounds;
            public readonly int[] Triangles;
            // ReSharper disable InconsistentNaming
            public readonly Vector3[] vertices;
            public readonly Vector3[] normals;
            public readonly Vector4[] tangents;
            public readonly Vector2[] uv;
            public readonly Vector2[] uv2;
            public readonly Vector2[] uv3;
            public readonly Vector2[] uv4;
            public readonly Vector2[] uv5;
            public readonly Vector2[] uv6;
            public readonly Vector2[] uv7;
            public readonly Vector2[] uv8;
            public readonly Color32[] colors32;
            public readonly NativeArray<byte> BonesPerVertex;
            public readonly NativeArray<BoneWeight1> AllBoneWeights;
            public readonly SubMeshDescriptor[] SubMeshes;
            public readonly (string name, (Vector3[] vertices, Vector3[] normals, Vector3[] tangents, float weight))[]
                BlendShapes;
            public readonly Material[] SharedMaterials;
            public readonly Matrix4x4[] bindposes;
            public readonly Transform[] bones;
            // ReSharper restore InconsistentNaming

            public MeshInfo(SkinnedMeshRenderer renderer)
            {
                var mesh = renderer.sharedMesh;
                Bounds = mesh.bounds;
                Triangles = mesh.triangles;
                vertices = mesh.vertices;
                normals = mesh.normals;
                tangents = mesh.tangents;
                uv = mesh.uv;
                uv2 = mesh.uv2;
                uv3 = mesh.uv3;
                uv4 = mesh.uv4;
                uv5 = mesh.uv5;
                uv6 = mesh.uv6;
                uv7 = mesh.uv7;
                uv8 = mesh.uv8;
                colors32 = mesh.colors32;

                BonesPerVertex = mesh.GetBonesPerVertex();
                AllBoneWeights = mesh.GetAllBoneWeights();

                BlendShapes = new (string, (Vector3[], Vector3[], Vector3[], float))[mesh.blendShapeCount];
                for (var i = 0; i < mesh.blendShapeCount; i++)
                {
                    Assert.AreEqual(1, mesh.GetBlendShapeFrameCount(i));
                    Assert.AreEqual(100.0f, mesh.GetBlendShapeFrameWeight(i, 0));
                    var deltaVertices = new Vector3[vertices.Length];
                    var deltaNormals = new Vector3[vertices.Length];
                    var deltaTangents = new Vector3[vertices.Length];
                    mesh.GetBlendShapeFrameVertices(i, 0, deltaVertices, deltaNormals, deltaTangents);
                    var shapeName = mesh.GetBlendShapeName(i);
                    var weight = renderer.GetBlendShapeWeight(i);
                    BlendShapes[i] = (shapeName, (deltaVertices, deltaNormals, deltaTangents, weight));
                }

                SubMeshes = new SubMeshDescriptor[mesh.subMeshCount];
                for (var i = 0; i < SubMeshes.Length; i++)
                    SubMeshes[i] = mesh.GetSubMesh(i);

                var sourceMaterials = renderer.sharedMaterials;
                SharedMaterials = new Material[mesh.subMeshCount];
                Array.Copy(sourceMaterials, SharedMaterials, Math.Min(sourceMaterials.Length, SharedMaterials.Length));

                bindposes = mesh.bindposes;

                var bones = renderer.bones;
                this.bones = new Transform[bindposes.Length];
                Array.Copy(bones, this.bones, Math.Min(bones.Length, this.bones.Length));
            }

            public MeshInfo(MeshRenderer renderer)
            {
                var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                Bounds = mesh.bounds;
                Triangles = mesh.triangles;
                vertices = mesh.vertices;
                normals = mesh.normals;
                tangents = mesh.tangents;
                uv = mesh.uv;
                uv2 = mesh.uv2;
                uv3 = mesh.uv3;
                uv4 = mesh.uv4;
                uv5 = mesh.uv5;
                uv6 = mesh.uv6;
                uv7 = mesh.uv7;
                uv8 = mesh.uv8;
                colors32 = mesh.colors32;
                BonesPerVertex = new NativeArray<byte>(vertices.Length, Allocator.Temp);
                AllBoneWeights = new NativeArray<BoneWeight1>(vertices.Length, Allocator.Temp);
                for (var i = 0; i < AllBoneWeights.Length; i++)
                {
                    BonesPerVertex[i] = 1;
                    AllBoneWeights[i] = new BoneWeight1 { weight = 1f, boneIndex = 0 };
                }

                BlendShapes = Array.Empty<(string, (Vector3[], Vector3[], Vector3[], float))>();

                SubMeshes = new SubMeshDescriptor[mesh.subMeshCount];
                for (var i = 0; i < SubMeshes.Length; i++)
                    SubMeshes[i] = mesh.GetSubMesh(i);

                var sourceMaterials = renderer.sharedMaterials;
                SharedMaterials = new Material[mesh.subMeshCount];
                Array.Copy(sourceMaterials, SharedMaterials, Math.Min(sourceMaterials.Length, SharedMaterials.Length));

                bindposes = new [] { Matrix4x4.identity };
                bones = new [] { renderer.transform };
            }
        }

        private void DoMerge(MergeSkinnedMesh merge, OptimizerSession session, ISet<MergeSkinnedMesh> proceed)
        {
            if (proceed.Contains(merge)) return;
            proceed.Add(merge);
            foreach (var skinnedMeshRenderer in merge.renderers)
            {
                var depends = skinnedMeshRenderer.GetComponent<MergeSkinnedMesh>();
                if (proceed.Contains(depends)) continue;
                DoMerge(merge, session, proceed);
            }

            var white = new Color32(0xff, 0xff, 0xff, 0xff);
            var meshInfos = merge.renderers.Select(x => new MeshInfo(x))
                .Concat(merge.staticRenderers.Select(x => new MeshInfo(x)))
                .ToArray();
            var trianglesTotalCount = meshInfos.Sum(x => x.Triangles.Length);
            var vertexTotalCount = meshInfos.Sum(x => x.vertices.Length);
            var boneWeightsTotalCount = meshInfos.Sum(x => x.AllBoneWeights.Length);
            var (subMeshIndexMap, subMeshesTotalCount) = CreateSubMeshIndexMapping(merge.merges, meshInfos);
            var (bindPoseIndexMap, bindPoseTotalCount) = CreateBindPoseIndexMapping(meshInfos);

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
            var bones = new Transform[bindPoseTotalCount];
            var bindposes = new Matrix4x4[bindPoseTotalCount];

            // others
            var boneWeights = new NativeArray<BoneWeight1>(boneWeightsTotalCount, Allocator.Temp);

            // blendShapes
            var blendShapeNames = new List<string>();
            var blendShapes =
                new Dictionary<string, (Vector3[] vertex, Vector3[] normal, Vector3[] tangent, float weight)>();

            // subMeshes
            var sharedMaterials = new Material[subMeshesTotalCount];
            var subMeshInfos = new List<(int vertexBase, int[] triangles, SubMeshDescriptor submesh)>[subMeshesTotalCount];
            for (var i = 0; i < subMeshInfos.Length; i++)
                subMeshInfos[i] = new List<(int, int[], SubMeshDescriptor)>();

            var verticesBase = 0;
            var boneWeightsBase = 0;

            // collect bones
            // ReSharper disable once LocalVariableHidesMember
            for (var rendererIndex = 0; rendererIndex < meshInfos.Length; rendererIndex++)
            {
                var mesh = meshInfos[rendererIndex];
                var vertexCount = mesh.vertices.Length;

                var bounds = mesh.Bounds;
                min = Vector3.Min(min, bounds.min);
                max = Vector3.Max(max, bounds.max);
                renderMin = Vector3.Min(renderMin, mesh.Bounds.min);
                renderMax = Vector3.Max(renderMax, mesh.Bounds.max);

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
                Copy(verticesBase, vertexCount, vertexTotalCount, mesh.colors32, ref colors32, white);
                Copy(verticesBase, vertexCount, vertexTotalCount, mesh.BonesPerVertex, bonesPerVertex);

                // bone attributes
                var bindPoseIndices = bindPoseIndexMap[rendererIndex];
                var rendererBones = mesh.bones;
                var bindposesCount = mesh.bindposes.Length;
                for (var i = 0; i < rendererBones.Length; i++)
                    if (bindPoseIndices[i] != -1)
                        bones[bindPoseIndices[i]] = rendererBones[i];
                for (var i = 0; i < bindposesCount; i++)
                    if (bindPoseIndices[i] != -1)
                        bindposes[bindPoseIndices[i]] = mesh.bindposes[i];

                // other attributes
                var meshTriangles = mesh.Triangles;

                var meshBoneWeights = mesh.AllBoneWeights;
                for (var i = 0; i < meshBoneWeights.Length; i++)
                {
                    var weight = meshBoneWeights[i];
                    boneWeights[boneWeightsBase + i] = new BoneWeight1()
                    {
                        weight = weight.weight,
                        boneIndex = bindPoseIndices[weight.boneIndex],
                    };
                }

                // blendShapes
                foreach (var (shapeName, (deltaVertices, deltaNormals, deltaTangents, shapeWeight)) in mesh.BlendShapes)
                {
                    if (!blendShapes.TryGetValue(shapeName, out var tuple))
                    {
                        blendShapeNames.Add(shapeName);
                        tuple = default;
                        tuple.weight = shapeWeight;
                    }

                    Copy(verticesBase, vertexCount, vertexTotalCount, deltaVertices, ref tuple.vertex);
                    Copy(verticesBase, vertexCount, vertexTotalCount, deltaNormals, ref tuple.normal);
                    Copy(verticesBase, vertexCount, vertexTotalCount, deltaTangents, ref tuple.tangent);
                    blendShapes[shapeName] = tuple;
                }

                // subMeshes
                for (var i = 0; i < mesh.SubMeshes.Length; i++)
                    subMeshInfos[subMeshIndexMap[rendererIndex][i]]
                        .Add((verticesBase, meshTriangles, mesh.SubMeshes[i]));

                for (var i = 0; i < mesh.SharedMaterials.Length; i++)
                    sharedMaterials[subMeshIndexMap[rendererIndex][i]] = mesh.SharedMaterials[i];

                verticesBase += vertexCount;
                boneWeightsBase += meshBoneWeights.Length;
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
            var newMesh = session.AddToAsset(new Mesh());
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
                var (vertex, normal, tangent, _) = blendShapes[blendShapeName];
                newMesh.AddBlendShapeFrame(blendShapeName, 100, vertex, normal, tangent);
            }

            newMesh.subMeshCount = subMeshes.Length;
            for (var i = 0; i < subMeshes.Length; i++)
                newMesh.SetSubMesh(i, subMeshes[i]);

            newRenderer.bones = bones;
            newRenderer.sharedMesh = newMesh;
            newRenderer.sharedMaterials = sharedMaterials;
            //newBounds.SetMinMax(renderMin, renderMax);
            //newRenderer.bounds = newBounds;

            for (var i = 0; i < blendShapeNames.Count; i++)
                newRenderer.SetBlendShapeWeight(i, blendShapes[blendShapeNames[i]].weight);

            session.Destroy(merge);

            foreach (var renderer in merge.renderers)
            {
                session.AddObjectMapping(renderer, newRenderer);
                session.Destroy(renderer);
            }

            foreach (var renderer in merge.staticRenderers)
            {
                session.Destroy(renderer.GetComponent<MeshFilter>());
                session.Destroy(renderer);
            }
        }

        private (int[][] mapping, int subMeshTotalCount)
            CreateSubMeshIndexMapping(MergeSkinnedMesh.MergeConfig[] merges, MeshInfo[] infos)
        {
            var result = new int[infos.Length][];

            // initialize with -1
            for (var i = 0; i < infos.Length; i++)
            {
                result[i] = new int[infos[i].SubMeshes.Length];
                for (var j = 0; j < result[i].Length; j++)
                    result[i][j] = -1;
            }

            for (var i = 0; i < merges.Length; i++)
                foreach (var pair in merges[i].merges)
                    result[(int)(pair >> 32)][(int)pair] = i;

            var nextIndex = merges.Length;

            foreach (var t in result)
                for (var j = 0; j < t.Length; j++)
                    if (t[j] == -1)
                        t[j] = nextIndex++;

            return (result, nextIndex);
        }

        private (int[][] mapping, int subMeshTotalCount) CreateBindPoseIndexMapping(MeshInfo[] infos)
        {
            var mapping = new Dictionary<(Transform, Matrix4x4), int>();
            var indicesArray = new int[infos.Length][];
            var nextIndex = 0;
            for (var i = 0; i < infos.Length; i++)
            {
                var sharedMesh = infos[i];
                var usedBones = new BitArray(sharedMesh.bindposes.Length);
                foreach (var weight in sharedMesh.AllBoneWeights)
                    usedBones[weight.boneIndex] = true;

                var indices = indicesArray[i] = new int[sharedMesh.bindposes.Length];
                for (var bindPoseIndex = 0; bindPoseIndex < sharedMesh.bindposes.Length; bindPoseIndex++)
                {
                    if (!usedBones[bindPoseIndex])
                    {
                        indices[bindPoseIndex] = -1;
                    }
                    else
                    {
                        var key = (sharedMesh.bones[bindPoseIndex], sharedMesh.bindposes[bindPoseIndex]);
                        indices[bindPoseIndex] =
                            mapping.TryGetValue(key, out var index) ? index : mapping[key] = nextIndex++;
                    }
                }
            }

            return (indicesArray, nextIndex);
        }

        private static void Copy<T>(int baseIndex, int count, int totalLength, T[] src, ref T[] dest, T init = default)
        {
            if (src == null || src.Length == 0) return;
            if (dest == null)
            {
                dest = new T[totalLength];
                for (var i = 0; i < dest.Length; i++)
                    dest[i] = init;
            }
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
