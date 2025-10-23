using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    internal class MergeToonLitMaterialProcessor : EditSkinnedMeshProcessor<MergeToonLitMaterial>
    {
        private static readonly int MainTexProp = Shader.PropertyToID("_MainTex");
        private static readonly int MainTexStProp = Shader.PropertyToID("_MainTex_ST");
        private static readonly int RectProp = Shader.PropertyToID("_Rect");

        private static Material? _helperMaterial;

        private static Material HelperMaterial =>
            _helperMaterial != null ? _helperMaterial : _helperMaterial = new Material(Assets.MergeTextureHelper);

        public MergeToonLitMaterialProcessor(MergeToonLitMaterial component) : base(component)
        {
        }

        public override EditSkinnedMeshProcessorOrder ProcessOrder => EditSkinnedMeshProcessorOrder.AfterRemoveMesh;

        public override void Process(BuildContext context, MeshInfo2 target)
        {
            // compute usages. AdditionalTemporal is usage count for now.
            // if #usages is not zero for merging triangles
            var users = new Dictionary<Vertex, int>();

            foreach (var v in target.Vertices) users[v] = 0;

            foreach (var targetSubMesh in target.SubMeshes)
            foreach (var v in targetSubMesh.Vertices.Distinct())
                users[v]++;

            // compute per-material data
            var mergingIndices = ComputeMergingIndices(target.SubMeshes.Count);
            var targetRectForMaterial = new Rect[target.SubMeshes.Count];
            foreach (var componentMerge in Component.merges)
            foreach (var mergeSource in componentMerge.source)
                targetRectForMaterial[mergeSource.materialIndex] = mergeSource.targetRect;

            // map UVs
            for (var subMeshI = 0; subMeshI < target.SubMeshes.Count; subMeshI++)
            {
                if (mergingIndices[subMeshI])
                {
                    // the material is for merge.
                    var subMesh = target.SubMeshes[subMeshI];
                    var targetRect = targetRectForMaterial[subMeshI];
                    var vertexCache = new Dictionary<Vertex, Vertex>();
                    for (var i = 0; i < subMesh.Vertices.Count; i++)
                    {
                        if (vertexCache.TryGetValue(subMesh.Vertices[i], out var cached))
                        {
                            subMesh.Vertices[i] = cached;
                            continue;
                        }
                        if (users[subMesh.Vertices[i]] != 1)
                        {
                            // if there are multiple users for the vertex: duplicate it
                            var cloned = subMesh.Vertices[i].Clone();
                            target.VerticesMutable.Add(cloned);

                            users[subMesh.Vertices[i]]--;

                            vertexCache[subMesh.Vertices[i]] = cloned;
                            subMesh.Vertices[i] = cloned;
                        }
                        else
                        {
                            vertexCache[subMesh.Vertices[i]] = subMesh.Vertices[i];
                        }

                        subMesh.Vertices[i].TexCoord0 = MapUV(subMesh.Vertices[i].TexCoord0, targetRect);
                    }
                }
            }

            // merge submeshes
            target.FlattenMultiPassRendering("Merge Toon Lit");
            var copied = target.SubMeshes.Where((_, i) => !mergingIndices[i]);
            var materials = target.SubMeshes.Select(x => x.SharedMaterial).ToArray();
            var merged = Component.merges.Select(x => new SubMesh(
                x.source.SelectMany(src => target.SubMeshes[src.materialIndex].Triangles).ToList(),
                CreateMaterial(GenerateTexture(x, materials, true))));
            var subMeshes = copied.Concat(merged).ToList();
            target.SubMeshes.Clear();
            target.SubMeshes.AddRange(subMeshes);
        }

        private Vector2 MapUV(Vector2 vector2, Rect destSourceRect) =>
            new Vector2(vector2.x % 1, vector2.y % 1) * new Vector2(destSourceRect.width, destSourceRect.height) 
            + new Vector2(destSourceRect.x, destSourceRect.y);


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// </summary>
        /// <returns>bitarray[i] is true if the materials[i] ill be merged to other material</returns>
        public BitArray ComputeMergingIndices(int subMeshCount)
        {
            var mergingIndices = new BitArray(subMeshCount);
            foreach (var mergeInfo in Component.merges)
            foreach (var source in mergeInfo.source)
                mergingIndices[source.materialIndex] = true;
            return mergingIndices;
        }

        private Material?[] CreateMaterials(BitArray mergingIndices, Material?[] upstream, bool fast)
        {
            var copied = upstream.Where((_, i) => !mergingIndices[i]);
            if (fast)
            {
                return copied.Concat(Component.merges.Select(x => new Material(Assets.ToonLitShader))).ToArray();
            }
            else
            {
                // slow mode: generate texture actually
                return copied.Concat(GenerateTextures(Component, upstream, false).Select(CreateMaterial)).ToArray();
            }
        }

        private static Material CreateMaterial(Texture texture)
        {
            var mat = new Material(Assets.ToonLitShader);
            mat.SetTexture(MainTexProp, texture);
            return mat;
        }

        public static Texture[] GenerateTextures(MergeToonLitMaterial config, Material?[] materials, bool compress)
        {
            return config.merges.Select(x => GenerateTexture(x, materials, compress)).ToArray();
        }

        private static TextureFormat BaseTextureFormat(MergeToonLitMaterial.MergedTextureFormat finalFormat)
        {
            switch (finalFormat)
            {
                case MergeToonLitMaterial.MergedTextureFormat.Alpha8:
                case MergeToonLitMaterial.MergedTextureFormat.ARGB4444:
                case MergeToonLitMaterial.MergedTextureFormat.RGB24:
                case MergeToonLitMaterial.MergedTextureFormat.RGBA32:
                case MergeToonLitMaterial.MergedTextureFormat.ARGB32:
                case MergeToonLitMaterial.MergedTextureFormat.RGB565:
                case MergeToonLitMaterial.MergedTextureFormat.R16:
                case MergeToonLitMaterial.MergedTextureFormat.RGBA4444:
                case MergeToonLitMaterial.MergedTextureFormat.BGRA32:
                case MergeToonLitMaterial.MergedTextureFormat.RG16:
                case MergeToonLitMaterial.MergedTextureFormat.R8:
                    return (TextureFormat) finalFormat;
                case MergeToonLitMaterial.MergedTextureFormat.BC4:
                    return TextureFormat.R8;
                case MergeToonLitMaterial.MergedTextureFormat.BC5:
                    return TextureFormat.RG16;
                case MergeToonLitMaterial.MergedTextureFormat.DXT1:
                    return TextureFormat.RGB24;
                case MergeToonLitMaterial.MergedTextureFormat.DXT5:
                case MergeToonLitMaterial.MergedTextureFormat.BC7:
                    return TextureFormat.RGBA32;
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_4x4:
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_5x5:
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_6x6:
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_8x8:
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_10x10:
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_12x12:
                    return TextureFormat.RGBA32;
                case MergeToonLitMaterial.MergedTextureFormat.Default:
                default:
                    throw new ArgumentOutOfRangeException(nameof(finalFormat), finalFormat, null);
            }
        }

        private static bool IsCompressedFormat(MergeToonLitMaterial.MergedTextureFormat finalFormat)
        {
            switch (finalFormat)
            {
                case MergeToonLitMaterial.MergedTextureFormat.Alpha8:
                case MergeToonLitMaterial.MergedTextureFormat.ARGB4444:
                case MergeToonLitMaterial.MergedTextureFormat.RGB24:
                case MergeToonLitMaterial.MergedTextureFormat.RGBA32:
                case MergeToonLitMaterial.MergedTextureFormat.ARGB32:
                case MergeToonLitMaterial.MergedTextureFormat.RGB565:
                case MergeToonLitMaterial.MergedTextureFormat.R16:
                case MergeToonLitMaterial.MergedTextureFormat.RGBA4444:
                case MergeToonLitMaterial.MergedTextureFormat.BGRA32:
                case MergeToonLitMaterial.MergedTextureFormat.RG16:
                case MergeToonLitMaterial.MergedTextureFormat.R8:
                    return false;
                case MergeToonLitMaterial.MergedTextureFormat.DXT1:
                case MergeToonLitMaterial.MergedTextureFormat.DXT5:
                case MergeToonLitMaterial.MergedTextureFormat.BC7:
                case MergeToonLitMaterial.MergedTextureFormat.BC4:
                case MergeToonLitMaterial.MergedTextureFormat.BC5:
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_4x4:
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_5x5:
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_6x6:
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_8x8:
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_10x10:
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_12x12:
                    return true;
                case MergeToonLitMaterial.MergedTextureFormat.Default:
                default:
                    throw new ArgumentOutOfRangeException(nameof(finalFormat), finalFormat, null);
            }
        }

        private static RenderTextureFormat GetRenderTarget(MergeToonLitMaterial.MergedTextureFormat finalFormat)
        {
            switch (finalFormat)
            {
                case MergeToonLitMaterial.MergedTextureFormat.ARGB4444:
                case MergeToonLitMaterial.MergedTextureFormat.RGBA4444:
                    return RenderTextureFormat.ARGB4444;
                // 8 bit for each channel
                case MergeToonLitMaterial.MergedTextureFormat.Alpha8:
                case MergeToonLitMaterial.MergedTextureFormat.RGB24:
                case MergeToonLitMaterial.MergedTextureFormat.RGBA32:
                case MergeToonLitMaterial.MergedTextureFormat.ARGB32:
                case MergeToonLitMaterial.MergedTextureFormat.BGRA32:
                case MergeToonLitMaterial.MergedTextureFormat.DXT1:
                case MergeToonLitMaterial.MergedTextureFormat.DXT5:
                case MergeToonLitMaterial.MergedTextureFormat.BC7:
                    return RenderTextureFormat.ARGB32;
                case MergeToonLitMaterial.MergedTextureFormat.RGB565:
                    return RenderTextureFormat.RGB565;
                case MergeToonLitMaterial.MergedTextureFormat.R16:
                    return RenderTextureFormat.R16;
                case MergeToonLitMaterial.MergedTextureFormat.RG16:
                case MergeToonLitMaterial.MergedTextureFormat.BC5:
                    return RenderTextureFormat.RG16;
                case MergeToonLitMaterial.MergedTextureFormat.R8:
                case MergeToonLitMaterial.MergedTextureFormat.BC4:
                    return RenderTextureFormat.R8;
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_4x4:
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_5x5:
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_6x6:
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_8x8:
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_10x10:
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_12x12:
                    return RenderTextureFormat.ARGB32;
                case MergeToonLitMaterial.MergedTextureFormat.Default:
                default:
                    throw new ArgumentOutOfRangeException(nameof(finalFormat), finalFormat, null);
            }
        }

        private static Texture GenerateTexture(
            MergeToonLitMaterial.MergeInfo mergeInfo,
            Material?[] materials,
            bool compress
        )
        {
            var texWidth = mergeInfo.textureSize.x;
            var texHeight = mergeInfo.textureSize.y;
            var finalFormat = mergeInfo.mergedFormat;
            if (finalFormat == MergeToonLitMaterial.MergedTextureFormat.Default)
#if UNITY_ANDROID || UNITY_IOS
                finalFormat = MergeToonLitMaterial.MergedTextureFormat.ASTC_6x6;
#else
                finalFormat = MergeToonLitMaterial.MergedTextureFormat.DXT5;
#endif

            // use compatible format
            var targetFormat = GraphicsFormatUtility.GetGraphicsFormat(GetRenderTarget(finalFormat), RenderTextureReadWrite.Default);
            targetFormat = SystemInfo.GetCompatibleFormat(targetFormat, FormatUsage.Render);
            var target = new RenderTexture(texWidth, texHeight, 0, targetFormat);

            foreach (var source in mergeInfo.source)
            {
                var sourceMat = materials[source.materialIndex]!; // selected material should not be null
                var sourceTex = sourceMat.GetTexture(MainTexProp);
                var sourceTexSt = sourceMat.GetVector(MainTexStProp);
                HelperMaterial.SetTexture(MainTexProp, sourceTex);
                HelperMaterial.SetVector(MainTexStProp, sourceTexSt);
                HelperMaterial.SetVector(RectProp,
                    new Vector4(source.targetRect.x, source.targetRect.y, source.targetRect.width,
                        source.targetRect.height));
                Graphics.Blit(sourceTex, target, HelperMaterial);
            }

            var texture = CopyFromRenderTarget(target, finalFormat);

            DestroyTracker.DestroyImmediate(target);

            if (compress && IsCompressedFormat(finalFormat))
                EditorUtility.CompressTexture(texture, (TextureFormat)finalFormat, TextureCompressionQuality.Normal);

            Utils.Assert(!compress || texture.format == (TextureFormat)finalFormat, 
                $"TextureFormat mismatch: expected {finalFormat} but was {texture.format}");

            return texture;
        }

        private static Texture2D CopyFromRenderTarget(RenderTexture source, MergeToonLitMaterial.MergedTextureFormat finalFormat)
        {
            var prev = RenderTexture.active;
            var texture = new Texture2D(source.width, source.height, BaseTextureFormat(finalFormat), true);
            
            try
            {
                RenderTexture.active = source;
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

            public override Material?[] Materials(bool fast = true)
            {
                var upstream = base.Materials(fast);
                return _processor.CreateMaterials(_processor.ComputeMergingIndices(upstream.Length), upstream, fast);
            }
        }
    }
}
