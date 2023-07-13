using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        private static Material _helperMaterial;

        private static Material HelperMaterial =>
            _helperMaterial ? _helperMaterial : _helperMaterial = new Material(Utils.MergeTextureHelper);

        public MergeToonLitMaterialProcessor(MergeToonLitMaterial component) : base(component)
        {
        }

        public override int ProcessOrder => -10000;

        public override void Process(OptimizerSession session, MeshInfo2 target, MeshInfo2Holder meshInfo2Holder)
        {
            // compute usages. AdditionalTemporal is usage count for now.
            // if #usages is not zero for merging triangles
            var users = new Dictionary<Vertex, int>();

            foreach (var v in target.Vertices) users[v] = 0;

            foreach (var targetSubMesh in target.SubMeshes)
            foreach (var v in targetSubMesh.Triangles.Distinct())
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
                    for (var i = 0; i < subMesh.Triangles.Count; i++)
                    {
                        if (vertexCache.TryGetValue(subMesh.Triangles[i], out var cached))
                        {
                            subMesh.Triangles[i] = cached;
                            continue;
                        }
                        if (users[subMesh.Triangles[i]] != 1)
                        {
                            // if there are multiple users for the vertex: duplicate it
                            var cloned = subMesh.Triangles[i].Clone();
                            target.Vertices.Add(cloned);

                            users[subMesh.Triangles[i]]--;

                            vertexCache[subMesh.Triangles[i]] = cloned;
                            subMesh.Triangles[i] = cloned;
                        }
                        else
                        {
                            vertexCache[subMesh.Triangles[i]] = subMesh.Triangles[i];
                        }

                        subMesh.Triangles[i].TexCoord0 = MapUV(subMesh.Triangles[i].TexCoord0, targetRect);
                    }
                }
            }

            // merge submeshes
            var copied = target.SubMeshes.Where((_, i) => !mergingIndices[i]);
            var materials = target.SubMeshes.Select(x => x.SharedMaterial).ToArray();
            var merged = Component.merges.Select(x => new SubMesh(
                x.source.SelectMany(src => target.SubMeshes[src.materialIndex].Triangles).ToList(),
                CreateMaterial(GenerateTexture(x, materials, !session.IsTest))));
            var subMeshes = copied.Concat(merged).ToList();
            target.SubMeshes.Clear();
            target.SubMeshes.AddRange(subMeshes);
            
            foreach (var subMesh in target.SubMeshes)
            {
                session.AddToAsset(subMesh.SharedMaterial);
                session.AddToAsset(subMesh.SharedMaterial.GetTexture(MainTexProp));
            }
        }

        private Vector2 MapUV(Vector2 vector2, Rect destSourceRect) =>
            vector2 * new Vector2(destSourceRect.width, destSourceRect.height) 
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
                return copied.Concat(GenerateTextures(Component, upstream, false).Select(CreateMaterial)).ToArray();
            }
        }

        private static Material CreateMaterial(Texture texture)
        {
            var mat = new Material(Utils.ToonLitShader);
            mat.SetTexture(MainTexProp, texture);
            return mat;
        }

        public static Texture[] GenerateTextures(MergeToonLitMaterial config, Material[] materials, bool compress)
        {
            return config.merges.Select(x => GenerateTexture(x, materials, compress)).ToArray();
        }

        private enum CompressionType
        {
            // writing to Texture2D finishes compression
            UseRaw,
            // we use Texture2d.Compress method to compress image
            UseCompressMethod,
            UseIspcAstc,
            // use PVRTexTool in unity installation
            UsePvrTexTool,
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
                case MergeToonLitMaterial.MergedTextureFormat.DXT1:
                    return TextureFormat.RGB24;
                case MergeToonLitMaterial.MergedTextureFormat.DXT5:
                    return TextureFormat.RGBA32;
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_4x4:
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_5x5:
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_6x6:
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_8x8:
                    return TextureFormat.RGBA32;
                case MergeToonLitMaterial.MergedTextureFormat.Default:
                default:
                    throw new ArgumentOutOfRangeException(nameof(finalFormat), finalFormat, null);
            }
        }

        private static CompressionType GetCompressionType(MergeToonLitMaterial.MergedTextureFormat finalFormat)
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
                    return CompressionType.UseRaw;
                case MergeToonLitMaterial.MergedTextureFormat.DXT1:
                case MergeToonLitMaterial.MergedTextureFormat.DXT5:
                    return CompressionType.UseCompressMethod;
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_4x4:
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_5x5:
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_6x6:
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_8x8:
                    return CompressionType.UseIspcAstc;
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
                    return RenderTextureFormat.ARGB32;
                case MergeToonLitMaterial.MergedTextureFormat.RGB565:
                    return RenderTextureFormat.RGB565;
                case MergeToonLitMaterial.MergedTextureFormat.R16:
                    return RenderTextureFormat.R16;
                case MergeToonLitMaterial.MergedTextureFormat.RG16:
                    return RenderTextureFormat.RG16;
                case MergeToonLitMaterial.MergedTextureFormat.R8:
                    return RenderTextureFormat.R8;
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_4x4:
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_5x5:
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_6x6:
                case MergeToonLitMaterial.MergedTextureFormat.ASTC_8x8:
                    return RenderTextureFormat.ARGB32;
                case MergeToonLitMaterial.MergedTextureFormat.Default:
                default:
                    throw new ArgumentOutOfRangeException(nameof(finalFormat), finalFormat, null);
            }
        }

        private static Texture GenerateTexture(
            MergeToonLitMaterial.MergeInfo mergeInfo,
            Material[] materials,
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

            var texture = CopyFromRenderTarget(target, finalFormat);

            Object.DestroyImmediate(target);

            switch (compress ? GetCompressionType(finalFormat) : CompressionType.UseRaw)
            {
                case CompressionType.UseRaw:
                    // Nothing to do.
                    break;
                case CompressionType.UseCompressMethod:
                    // DXT formats can be generated using Compress function
                    texture.Compress(true);
                    break;
                case CompressionType.UseIspcAstc:
                    int size;
                    switch (finalFormat)
                    {
                        case MergeToonLitMaterial.MergedTextureFormat.ASTC_4x4:
                            size = 4;
                            break;
                        case MergeToonLitMaterial.MergedTextureFormat.ASTC_5x5:
                            size = 5;
                            break;
                        case MergeToonLitMaterial.MergedTextureFormat.ASTC_6x6:
                            size = 6;
                            break;
                        case MergeToonLitMaterial.MergedTextureFormat.ASTC_8x8:
                            size = 8;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    texture = IspcTexCompressor.GenerateAstc(texture, size);
                    break;
                case CompressionType.UsePvrTexTool:
                    throw new NotImplementedException("PvrTexTool Invocation");
                default:
                    throw new ArgumentOutOfRangeException();
            }

            System.Diagnostics.Debug.Assert(texture.format == (TextureFormat)finalFormat, 
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

        private static Texture2D CompressWithPvrTexTool(Texture2D texture, MergeToonLitMaterial.MergedTextureFormat finalFormat)
        {
            throw new NotImplementedException();
        }

        public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => new MeshInfoComputer(this, upstream);

        class MeshInfoComputer : AbstractMeshInfoComputer
        {
            private readonly MergeToonLitMaterialProcessor _processor;

            public MeshInfoComputer(MergeToonLitMaterialProcessor processor, IMeshInfoComputer upstream) : base(upstream)
                => _processor = processor;

            public override Material[] Materials(bool fast = true)
            {
                var upstream = base.Materials(fast);
                return _processor.CreateMaterials(_processor.ComputeMergingIndices(upstream.Length), upstream, fast);
            }
        }
    }
}
