using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    internal class MergeToonLitMaterialProcessor : EditSkinnedMeshProcessor<MergeToonLitMaterial>
    {
        private static readonly int MainTexProp = Shader.PropertyToID("_MainTex");
        private static readonly int MainTexStProp = Shader.PropertyToID("_MainTex_ST");
        private static readonly int RectProp = Shader.PropertyToID("_Rect");

        private static Material _helperMaterial;

        private static Material HelperMaterial =>
            _helperMaterial ? _helperMaterial : _helperMaterial = new Material(Utils.MergeTextureHelper);

        public MergeToonLitMaterialProcessor(MergeToonLitMaterial component) : base(component)
        {
        }

        public override int ProcessOrder => -10000;

        private struct Vertex
        {
            // ReSharper disable InconsistentNaming
            public readonly Vector3 position;
            public readonly Vector3 normals;
            public readonly Vector4 tangents;
            public readonly Vector2 uv; // UV is required
            public readonly Color32 colors32;
            public readonly NativeSlice<BoneWeight1> BoneWeights;
            public readonly SubMeshDescriptor[] SubMeshes;
            public readonly (string name, (Vector3 vertices, Vector3 normals, Vector3 tangents))[] BlendShapes;
            // ReSharper restore InconsistentNaming
        }


        public override void Process(OptimizerSession session)
        {
            // get source info for computing vertex mapping
            var srcMesh = new MeshInfo(Target);
            var mergingIndices = new BitArray(srcMesh.SubMeshes.Length);
            foreach (var mergeInfo in Component.merges)
            foreach (var source in mergeInfo.source)
                mergingIndices[source.materialIndex] = true;

            var boneWeightsPerVertex = new NativeSlice<BoneWeight1>[srcMesh.vertices.Length];
            {
                var weightBase = 0;
                for (var i = 0; i < srcMesh.BonesPerVertex.Length; i++)
                {
                    int bones = srcMesh.BonesPerVertex[i];
                    boneWeightsPerVertex[i] = srcMesh.AllBoneWeights.Slice(weightBase, bones);
                    weightBase += bones;
                }
            }

            var (destTriangles, destSubMeshes, destSourceVertexIndexMap, destSourceRects) =
                ComputeVertexMapping(mergingIndices, srcMesh);

            var destMesh = new MeshInfo(
                bounds: srcMesh.Bounds,
                trianglesCount: destTriangles.Length,
                vertexCount: destSourceVertexIndexMap.Length,
                uvCount: 1, // ToonLit will recognize one UV
                withColors: srcMesh.colors32 != null && srcMesh.colors32.Length == 0,
                subMeshCount: destSubMeshes.Length,
                bonesCount: srcMesh.bones.Length,
                blendShapes: srcMesh.BlendShapes.Select(x => (x.name, x.Item2.weight)).ToArray()
            );

            var weightsList = new List<BoneWeight1>();

            Array.Copy(destTriangles, destMesh.Triangles, destTriangles.Length);
            Array.Copy(destSubMeshes, destMesh.SubMeshes, destSubMeshes.Length);

            for (var i = 0; i < destSourceVertexIndexMap.Length; i++)
            {
                var sourceIndex = destSourceVertexIndexMap[i];

                destMesh.vertices[i] = srcMesh.vertices[sourceIndex];
                destMesh.normals[i] = srcMesh.normals[sourceIndex];
                destMesh.tangents[i] = srcMesh.tangents[sourceIndex];
                destMesh.uv[i] = MapUV(srcMesh.uv[sourceIndex], destSourceRects[i]);

                var boneWeights = boneWeightsPerVertex[sourceIndex];
                weightsList.AddRange(boneWeights);
                destMesh.BonesPerVertex[i] = (byte)boneWeights.Length;

                // ReSharper disable once PossibleNullReferenceException
                if (destMesh.colors32 != null)
                    destMesh.colors32[i] = srcMesh.colors32[sourceIndex];
                for (var j = 0; j < destMesh.BlendShapes.Length; j++)
                {
                    destMesh.BlendShapes[j].Item2.vertices[i] = srcMesh.BlendShapes[j].Item2.vertices[sourceIndex];
                    destMesh.BlendShapes[j].Item2.normals[i] = srcMesh.BlendShapes[j].Item2.normals[sourceIndex];
                    destMesh.BlendShapes[j].Item2.tangents[i] = srcMesh.BlendShapes[j].Item2.tangents[sourceIndex];
                }
            }

            destMesh.AllBoneWeights = new NativeArray<BoneWeight1>(weightsList.Count, Allocator.Temp,
                // will be initialized via copy
                NativeArrayOptions.UninitializedMemory);
            for (var i = 0; i < weightsList.Count; i++)
                destMesh.AllBoneWeights[i] = weightsList[i];

            // copy weight
            for (var j = 0; j < destMesh.BlendShapes.Length; j++)
                destMesh.BlendShapes[j].Item2.weight = srcMesh.BlendShapes[j].Item2.weight;

            Array.Copy(srcMesh.bindposes, destMesh.bindposes, destMesh.bindposes.Length);
            Array.Copy(srcMesh.bones, destMesh.bones, destMesh.bindposes.Length);

            var mesh = session.MayInstantiate(Target.sharedMesh);
            destMesh.WriteToMesh(mesh);
            Target.sharedMesh = mesh;

            Target.sharedMaterials = CreateMaterials(mergingIndices, Target.sharedMaterials, fast: false);
            foreach (var targetSharedMaterial in Target.sharedMaterials)
            {
                session.AddToAsset(targetSharedMaterial);
                session.AddToAsset(targetSharedMaterial.GetTexture(MainTexProp));
            }
        }

        private Vector2 MapUV(Vector2 vector2, Rect destSourceRect) =>
            vector2 * new Vector2(destSourceRect.width, destSourceRect.height) 
            + new Vector2(destSourceRect.x, destSourceRect.y);

        /// <summary>
        /// This method computes
        /// <list type="bullet">
        /// <item>dest -> source vertex index & rect mapping</item>
        /// <item>triangles & SubMeshDescriptors</item>
        /// </list>
        /// </summary>
        /// <param name="mergingIndices"></param>
        /// <param name="mesh"></param>
        (int[] destTriangles, SubMeshDescriptor[] destSubMeshes, int[] destSourceVertexIndexMap, Rect[] destSourceRects) 
            ComputeVertexMapping(BitArray mergingIndices, MeshInfo mesh)
        {
            var verticesOfUnchangedSubMeshes = mesh.SubMeshes
                .Where((_, i) => !mergingIndices[i])
                .SelectMany(x => Enumerable.Range(x.indexStart, x.indexCount).Select(i => mesh.Triangles[i]))
                .DistinctCountIntWithUpperLimit(mesh.vertices.Length);
            var verticesOfChangedSubMeshes = mesh.SubMeshes
                .Where((_, i) => mergingIndices[i])
                .Sum(x => Enumerable.Range(x.indexStart, x.indexCount).Select(i => mesh.Triangles[i])
                    .DistinctCountIntWithUpperLimit(mesh.vertices.Length));
            var totalVertexCount = verticesOfUnchangedSubMeshes + verticesOfChangedSubMeshes;

            var totalTriangleCount = mesh.SubMeshes.Sum(x => x.indexCount);
            var destTriangles = new int[totalTriangleCount];
            var totalSubMeshesCount = mergingIndices.CountFalse() + Component.merges.Length;
            var destSubMeshes = new SubMeshDescriptor[totalSubMeshesCount];

            var destSourceVertexIndexMap = new int[totalVertexCount];
            var destSourceRects = new Rect[totalVertexCount];
            
            // instance of int[] is shared but must be reset as you use
            var sourceDestVertexIndexMap = new int[mesh.vertices.Length];

            var nextVertexIndex = 0;
            var nextTriangleIndex = 0;
            var nextSubMeshIndex = 0;
            {
                // process unchanged sub-meshes

                // share vertices between sub-meshes because destSourceRect is same
                Utils.FillArray(sourceDestVertexIndexMap, -1);

                for (var i = 0; i < mesh.SubMeshes.Length; i++)
                {
                    if (mergingIndices[i]) continue;
                    var sourceSubMesh = mesh.SubMeshes[i];

                    var start = nextTriangleIndex;

                    AppendToDestTriangles(
                        sourceSubMesh: sourceSubMesh,
                        destSourceRect: new Rect(0, 0, 1, 1),
                        sourceTriangles: mesh.Triangles,
                        sourceDestVertexIndexMap: sourceDestVertexIndexMap,
                        destSourceVertexIndexMap: destSourceVertexIndexMap,
                        destSourceRects: destSourceRects,
                        nextTriangleIndex: ref nextTriangleIndex,
                        nextVertexIndex: ref nextVertexIndex,
                        destTriangles: destTriangles
                    );

                    destSubMeshes[nextSubMeshIndex++] = new SubMeshDescriptor(start, sourceSubMesh.indexCount);
                }

                Assert.AreEqual(mergingIndices.CountFalse(), nextSubMeshIndex);
                Assert.AreEqual(verticesOfUnchangedSubMeshes, nextVertexIndex);
            }

            // process merged
            foreach (var mergeInfo in Component.merges)
            {
                var indexStart = nextTriangleIndex;

                foreach (var source in mergeInfo.source)
                {
                    // do not share between source sub-meshes because destSourceRect can be changed.
                    Utils.FillArray(sourceDestVertexIndexMap, -1);
                    AppendToDestTriangles(
                        sourceSubMesh: mesh.SubMeshes[source.materialIndex],
                        destSourceRect: source.targetRect,
                        sourceTriangles: mesh.Triangles,
                        sourceDestVertexIndexMap: sourceDestVertexIndexMap,
                        destSourceVertexIndexMap: destSourceVertexIndexMap,
                        destSourceRects: destSourceRects,
                        nextTriangleIndex: ref nextTriangleIndex,
                        nextVertexIndex: ref nextVertexIndex,
                        destTriangles: destTriangles
                    );
                }

                destSubMeshes[nextSubMeshIndex++] = new SubMeshDescriptor(indexStart, nextTriangleIndex - indexStart);
            }

            // check all elements are filled
            Assert.AreEqual(totalSubMeshesCount, nextSubMeshIndex);
            Assert.AreEqual(totalVertexCount, nextVertexIndex);

            return (destTriangles, destSubMeshes, destSourceVertexIndexMap, destSourceRects);
        }

        void AppendToDestTriangles(
            in SubMeshDescriptor sourceSubMesh,
            Rect destSourceRect,
            // source info
            int[] sourceTriangles,
            // maps
            int[] sourceDestVertexIndexMap,
            int[] destSourceVertexIndexMap,
            Rect[] destSourceRects,
            // dest info
            ref int nextTriangleIndex,
            ref int nextVertexIndex,
            int[] destTriangles
        )
        {
            Assert.AreEqual(MeshTopology.Triangles, sourceSubMesh.topology);

            var start = nextTriangleIndex;

            for (var j = 0; j < sourceSubMesh.indexCount; j++)
            {
                var vertexIndex = sourceTriangles[sourceSubMesh.indexStart + j];
                var destVertexIndex = sourceDestVertexIndexMap[vertexIndex];
                if (destVertexIndex == -1)
                {
                    destVertexIndex = nextVertexIndex++;
                    sourceDestVertexIndexMap[vertexIndex] = destVertexIndex;
                    destSourceVertexIndexMap[destVertexIndex] = vertexIndex;
                    destSourceRects[destVertexIndex] = destSourceRect;
                }

                destTriangles[nextTriangleIndex++] = destVertexIndex;
            }

            Assert.AreEqual(start + sourceSubMesh.indexCount, nextTriangleIndex);
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// </summary>
        /// <returns>bitarray[i] is true if the materials[i] ill be merged to other material</returns>
        public BitArray ComputeMergingIndices()
        {
            var mergingIndices = new BitArray(Target.sharedMesh.subMeshCount);
            foreach (var mergeInfo in Component.merges)
            foreach (var source in mergeInfo.source)
                mergingIndices[source.materialIndex] = true;
            return mergingIndices;
        }

        private Material[] CreateMaterials(BitArray mergingIndices, Material[] upstream, bool fast)
        {
            var copied = upstream.Where((_, i) => !mergingIndices[i]);
            if (fast)
            {
                return copied.Concat(Component.merges.Select(x => new Material(Utils.ToonLitShader))).ToArray();
            }
            else
            {
                // slow mode: generate texture actually
                return copied.Concat(GenerateTextures(Component, upstream).Select(CreateMaterial)).ToArray();
            }
        }

        private static Material CreateMaterial(Texture texture)
        {
            var mat = new Material(Utils.ToonLitShader);
            mat.SetTexture(MainTexProp, texture);
            return mat;
        }

        public static Texture[] GenerateTextures(MergeToonLitMaterial config, Material[] materials)
        {
            return config.merges.Select(x => GenerateTexture(x, materials)).ToArray();
        }

        private static Texture GenerateTexture(MergeToonLitMaterial.MergeInfo mergeInfo, Material[] materials)
        {
            var texWidth = 1 << mergeInfo.textureSize.x;
            var texHeight = 1 << mergeInfo.textureSize.y;
            var texture = new Texture2D(texWidth, texHeight);
            var target = new RenderTexture(texWidth, texHeight, 0, RenderTextureFormat.ARGB32);

            foreach (var source in mergeInfo.source)
            {
                var sourceMat = materials[source.materialIndex];
                var sourceTex = sourceMat.GetTexture(MainTexProp);
                var sourceTexSt = sourceMat.GetVector(MainTexStProp);
                HelperMaterial.SetTexture(MainTexProp, sourceTex);
                HelperMaterial.SetVector(MainTexStProp, sourceTexSt);
                HelperMaterial.SetVector(RectProp,
                    new Vector4(source.targetRect.x, source.targetRect.y, source.targetRect.width,
                        source.targetRect.height));
                Graphics.Blit(sourceTex, target, HelperMaterial);
            }

            var prev = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                texture.Apply();
            }
            finally
            {
                RenderTexture.active = prev;
            }

            return texture;
        }

        public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => new MeshInfoComputer(this, upstream);

        class MeshInfoComputer : AbstractMeshInfoComputer
        {
            private readonly MergeToonLitMaterialProcessor _processor;

            public MeshInfoComputer(MergeToonLitMaterialProcessor processor, IMeshInfoComputer upstream) : base(upstream)
                => _processor = processor;

            public override Material[] Materials(bool fast = true) => 
                _processor.CreateMaterials(_processor.ComputeMergingIndices(), base.Materials(fast), fast);
        }
    }
}
