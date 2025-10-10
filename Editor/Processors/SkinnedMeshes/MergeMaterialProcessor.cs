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
    internal class MergeMaterialProcessor : EditSkinnedMeshProcessor<MergeMaterial>
    {
        private static readonly int MainTexProp = Shader.PropertyToID("_MainTex");
        private static readonly int MainTexTransformProp = Shader.PropertyToID("_MainTexTransform");
        private static readonly int RectProp = Shader.PropertyToID("_Rect");

        private static Material? _helperMaterial;

        private static Material HelperMaterial =>
            _helperMaterial != null ? _helperMaterial : _helperMaterial = new Material(Assets.MergeTextureHelperV2);

        public MergeMaterialProcessor(MergeMaterial component) : base(component)
        {
        }

        public override EditSkinnedMeshProcessorOrder ProcessOrder => EditSkinnedMeshProcessorOrder.AfterRemoveMesh;

        class ValidatedMergeInfo
        {
            public readonly Material ReferenceMaterial;
            public readonly MaterialInformation ReferenceInformation;
            public readonly MergeMaterial.MergeInfo Settings;
            public readonly List<ValidatedMergeSource> Sources;

            public ValidatedMergeInfo(Material referenceMaterial, 
                MaterialInformation referenceInfomration,
                MergeMaterial.MergeInfo settings,
                List<ValidatedMergeSource> sources)
            {
                ReferenceMaterial = referenceMaterial;
                ReferenceInformation = referenceInfomration;
                Settings = settings;
                Sources = sources;
            }
        }

        class ValidatedMergeSource
        {
            public MergeMaterial.MergeSource Settings;
            public int SubMeshIndex;
            public List<TextureUsageInformation> Usages;
            public Dictionary<string, TextureUsageInformation> UsagesByName;

            public ValidatedMergeSource(MergeMaterial.MergeSource settings, int subMeshIndex, List<TextureUsageInformation> usages)
            {
                Settings = settings;
                SubMeshIndex = subMeshIndex;
                Usages = usages;
                UsagesByName = usages.ToDictionary(x => x.MaterialPropertyName);
            }
        }

        public override void Process(BuildContext context, MeshInfo2 target)
        {
            // check and remove duplicated materials in merge setting
            {
                var mergeInfoByMaterial = new Dictionary<ObjectReference, MergeMaterial.MergeInfo>();
                var duplicatedMaterials = new HashSet<ObjectReference>();

                foreach (var componentMerge in Component.merges)
                {
                    var newSources = new List<MergeMaterial.MergeSource>(componentMerge.source.Length);
                    foreach (var mergeSource in componentMerge.source)
                    {
                        if (mergeSource.material == null) continue;
                        var materialReference = ObjectRegistry.GetReference(mergeSource.material);
                        if (mergeInfoByMaterial.TryAdd(materialReference, componentMerge))
                            newSources.Add(mergeSource);
                        else
                            duplicatedMaterials.Add(materialReference);
                    }
                    componentMerge.source = newSources.ToArray();
                }

                if (duplicatedMaterials.Count > 0)
                    BuildLog.LogError("MergeMaterial:DuplicateMaterialInMergeSetting", duplicatedMaterials);
            }

            var submeshByMaterial = new Dictionary<ObjectReference, List<int>>();
            for (var i = 0; i < target.SubMeshes.Count; i++)
            {
                var subMesh = target.SubMeshes[i];
                foreach (var material in subMesh.SharedMaterials)
                {
                    var materialReference = ObjectRegistry.GetReference(material);
                    if (!submeshByMaterial.TryGetValue(materialReference, out var list))
                        submeshByMaterial.Add(materialReference, list = new List<int>());
                    list.Add(i);
                }
            }

            // bit 0..7 for uv0..uv7
            var transformUvs = new int[target.SubMeshes.Count];
            var slotsForMerge = new BitArray(target.SubMeshes.Count);
            var validatedSettings = new List<ValidatedMergeInfo>();

            foreach (var mergeInfo in Component.merges)
            {
                if (mergeInfo.source.Length == 0) continue;

                // TODO: add option to override material properties and shader
                Shader? shader = null;
                Material? referenceMaterial = null;
                MaterialInformation? referenceInfomration = null;
                bool isAutoReferenceMaterial = referenceMaterial == null;
                bool hasShaderError = false;
                var goodSources = new List<ValidatedMergeSource>();

                foreach (var mergeSource in mergeInfo.source)
                {
                    var materialReference = ObjectRegistry.GetReference(mergeSource.material);
                    if (!submeshByMaterial.TryGetValue(materialReference, out var subMeshIndices))
                    {
                        BuildLog.LogError("MergeMaterial:MaterialNotFoundInMesh", materialReference);
                        continue;
                    }

                    if (subMeshIndices.Count != 1)
                    {
                        BuildLog.LogError("MergeMaterial:MaterialUsedInMultipleSubMeshes",
                            materialReference);
                    }

                    var subMesh = target.SubMeshes[subMeshIndices[0]];
                    if (subMesh.SharedMaterials.Length != 1)
                    {
                        BuildLog.LogError("MergeMaterial:MultiMaterialSubMesh",
                            materialReference);
                    }

                    var material = subMesh.SharedMaterial!;
                    shader ??= material.shader;
                    referenceMaterial ??= material;
                    if (shader != material.shader)
                    {
                        BuildLog.LogError("MergeMaterial:DifferentShaderInMergeSetting",
                            string.Join(", ", mergeInfo.source.Select(s => s.material?.name ?? "null")),
                            mergeInfo.source.Select(x => x.material));
                        continue;
                    }

                    if (hasShaderError) continue;

                    // We don't reuse context.GetMaterialInformation(material)
                    // because we need to get non-animated material information
                    var matInfo = new MaterialInformation(material, new List<Renderer>() { Target }, null);
                    referenceInfomration ??= matInfo;
                    if (matInfo.DefaultResult is not { } shaderInformationResult)
                    {
                        BuildLog.LogError("MergeMaterial:UnsupportedShaderInMergeSetting", shader);
                        hasShaderError = true;
                        continue;
                    }

                    if (shaderInformationResult.TextureUsageInformationList is not { } textureUsageInformations)
                    {
                        BuildLog.LogError("MergeMaterial:UnsupportedShaderInMergeSetting", material);
                        hasShaderError = true;
                        continue;
                    }

                    var badProperties = textureUsageInformations.Where(usage => usage.UVChannel != UVChannel.NonMeshRelated && usage.UVMatrix == null)
                        .Select(x => x.MaterialPropertyName)
                        .ToList();

                    if (badProperties.Any())
                    {
                        BuildLog.LogError("MergeMaterial:UnsupportedMaterialSettings:UnknownUVTransform", string.Join(", ", badProperties), material);
                        continue;
                    }

                    goodSources.Add(new (mergeSource, subMeshIndices[0], textureUsageInformations));
                }

                if (!hasShaderError && referenceMaterial != null)
                {
                    // Check the textureUsage for referenceMaterial:
                    // - has texture usage for all textures available
                    // - has texture usage with identity matrix

                    Utils.Assert(referenceInfomration!.Material == referenceMaterial);

                    var allProperties = goodSources.SelectMany(x => x.Usages).Select(x => x.MaterialPropertyName).ToHashSet();
                    var referenceUsages = referenceInfomration.DefaultResult!.TextureUsageInformationList!;
                    if (!referenceUsages.Select(x => x.MaterialPropertyName).ToHashSet().SetEquals(allProperties))
                    {
                        // TODO: consider just ignore unused textures?
                        var nonUsedProperties = allProperties.Except(referenceUsages.Select(x => x.MaterialPropertyName)).ToList();
                        BuildLog.LogError("MergeMaterial:ReferenceMaterial:NotAllTexturesUsed", referenceMaterial, string.Join(", ", nonUsedProperties));
                        continue;
                    }

                    if (referenceUsages.Any(usage => usage.UVChannel != UVChannel.NonMeshRelated && usage.UVMatrix == null))
                    {
                        var badProperties = referenceUsages.Where(usage => usage.UVChannel != UVChannel.NonMeshRelated && usage.UVMatrix == null)
                            .Select(x => x.MaterialPropertyName)
                            .ToList();

                        // We don't have to create error if the reference material is automatically selected since it should
                        // already errored
                        if (!isAutoReferenceMaterial) BuildLog.LogError("MergeMaterial:ReferenceMaterial:UnknownUVTransform", string.Join(", ", badProperties), referenceMaterial);
                        continue;
                    }

                    // The settings LGTM! let's prepare for merge.

                    var uvFlags = referenceUsages.Aggregate(0, (f, i) => f | (i.UVChannel == UVChannel.NonMeshRelated ? 0 : 1 << ((int)i.UVChannel)));
                    foreach (var goodSource in goodSources) transformUvs[goodSource.SubMeshIndex] = uvFlags;
                    foreach (var goodSource in goodSources) slotsForMerge[goodSource.SubMeshIndex] = true;
                    validatedSettings.Add(new ValidatedMergeInfo(referenceMaterial, referenceInfomration, mergeInfo, goodSources));
                }
            }

            var users = new Dictionary<Vertex, int>();
            foreach (var v in target.Vertices) users[v] = 0;

            foreach (var targetSubMesh in target.SubMeshes)
            foreach (var v in targetSubMesh.Vertices.Distinct())
                users[v]++;

            // compute per-material data
            var targetRectForMaterial = new Rect[target.SubMeshes.Count];
            foreach (var componentMerge in validatedSettings)
            foreach (var mergeSource in componentMerge.Sources)
                targetRectForMaterial[mergeSource.SubMeshIndex] = mergeSource.Settings.targetRect;
            
            // map UVs`
            for (var subMeshI = 0; subMeshI < target.SubMeshes.Count; subMeshI++)
            {
                var transformChannels = transformUvs[subMeshI];
                if (transformChannels != 0)
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

                        for (var channel = 0; channel < 8; channel++)
                        {
                            if ((transformChannels & (1 << channel)) == 0) continue;
                            subMesh.Vertices[i].SetTexCoord(channel, MapUV(subMesh.Vertices[i].GetTexCoord(channel), targetRect));
                        }
                    }
                }
            }

            // merge submeshes
            target.FlattenMultiPassRendering("Merge Toon Lit");
            var copied = target.SubMeshes.Where((_, i) => !slotsForMerge[i]);
            var merged = validatedSettings.Select(x => new SubMesh(
                x.Sources.SelectMany(src => target.SubMeshes[src.SubMeshIndex].Triangles).ToList(),
                CreateMaterial(x, target, compress: true)));
            var subMeshes = copied.Concat(merged).ToList();
            target.SubMeshes.Clear();
            target.SubMeshes.AddRange(subMeshes);
        }

        private Vector2 MapUV(Vector2 vector2, Rect destSourceRect) =>
            new Vector2(vector2.x % 1, vector2.y % 1) * new Vector2(destSourceRect.width, destSourceRect.height) 
            + new Vector2(destSourceRect.x, destSourceRect.y);


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private static Material CreateMaterial(ValidatedMergeInfo mergeInfo, MeshInfo2 target, bool compress)
        {
            var mat = new Material(mergeInfo.ReferenceMaterial);
            mat.name = "AAO Merged Material";
            foreach (var information in mergeInfo.ReferenceInformation.DefaultResult!.TextureUsageInformationList!)
            {
                var texture = GenerateTexture(mergeInfo, target.SubMeshes, information.MaterialPropertyName, compress);
                texture.name = "AAO Merged Texture (for " + information.MaterialPropertyName + ")";
                mat.SetTexture(information.MaterialPropertyName, texture);
            }
            return mat;
        }

        private static TextureFormat BaseTextureFormat(MergeMaterial.MergedTextureFormat finalFormat)
        {
            switch (finalFormat)
            {
                case MergeMaterial.MergedTextureFormat.Alpha8:
                case MergeMaterial.MergedTextureFormat.ARGB4444:
                case MergeMaterial.MergedTextureFormat.RGB24:
                case MergeMaterial.MergedTextureFormat.RGBA32:
                case MergeMaterial.MergedTextureFormat.ARGB32:
                case MergeMaterial.MergedTextureFormat.RGB565:
                case MergeMaterial.MergedTextureFormat.R16:
                case MergeMaterial.MergedTextureFormat.RGBA4444:
                case MergeMaterial.MergedTextureFormat.BGRA32:
                case MergeMaterial.MergedTextureFormat.RG16:
                case MergeMaterial.MergedTextureFormat.R8:
                    return (TextureFormat) finalFormat;
                case MergeMaterial.MergedTextureFormat.BC4:
                    return TextureFormat.R8;
                case MergeMaterial.MergedTextureFormat.BC5:
                    return TextureFormat.RG16;
                case MergeMaterial.MergedTextureFormat.DXT1:
                    return TextureFormat.RGB24;
                case MergeMaterial.MergedTextureFormat.DXT5:
                case MergeMaterial.MergedTextureFormat.BC7:
                    return TextureFormat.RGBA32;
                case MergeMaterial.MergedTextureFormat.ASTC_4x4:
                case MergeMaterial.MergedTextureFormat.ASTC_5x5:
                case MergeMaterial.MergedTextureFormat.ASTC_6x6:
                case MergeMaterial.MergedTextureFormat.ASTC_8x8:
                case MergeMaterial.MergedTextureFormat.ASTC_10x10:
                case MergeMaterial.MergedTextureFormat.ASTC_12x12:
                    return TextureFormat.RGBA32;
                case MergeMaterial.MergedTextureFormat.Default:
                default:
                    throw new ArgumentOutOfRangeException(nameof(finalFormat), finalFormat, null);
            }
        }

        private static bool IsCompressedFormat(MergeMaterial.MergedTextureFormat finalFormat)
        {
            switch (finalFormat)
            {
                case MergeMaterial.MergedTextureFormat.Alpha8:
                case MergeMaterial.MergedTextureFormat.ARGB4444:
                case MergeMaterial.MergedTextureFormat.RGB24:
                case MergeMaterial.MergedTextureFormat.RGBA32:
                case MergeMaterial.MergedTextureFormat.ARGB32:
                case MergeMaterial.MergedTextureFormat.RGB565:
                case MergeMaterial.MergedTextureFormat.R16:
                case MergeMaterial.MergedTextureFormat.RGBA4444:
                case MergeMaterial.MergedTextureFormat.BGRA32:
                case MergeMaterial.MergedTextureFormat.RG16:
                case MergeMaterial.MergedTextureFormat.R8:
                    return false;
                case MergeMaterial.MergedTextureFormat.DXT1:
                case MergeMaterial.MergedTextureFormat.DXT5:
                case MergeMaterial.MergedTextureFormat.BC7:
                case MergeMaterial.MergedTextureFormat.BC4:
                case MergeMaterial.MergedTextureFormat.BC5:
                case MergeMaterial.MergedTextureFormat.ASTC_4x4:
                case MergeMaterial.MergedTextureFormat.ASTC_5x5:
                case MergeMaterial.MergedTextureFormat.ASTC_6x6:
                case MergeMaterial.MergedTextureFormat.ASTC_8x8:
                case MergeMaterial.MergedTextureFormat.ASTC_10x10:
                case MergeMaterial.MergedTextureFormat.ASTC_12x12:
                    return true;
                case MergeMaterial.MergedTextureFormat.Default:
                default:
                    throw new ArgumentOutOfRangeException(nameof(finalFormat), finalFormat, null);
            }
        }

        private static RenderTextureFormat GetRenderTarget(MergeMaterial.MergedTextureFormat finalFormat)
        {
            switch (finalFormat)
            {
                case MergeMaterial.MergedTextureFormat.ARGB4444:
                case MergeMaterial.MergedTextureFormat.RGBA4444:
                    return RenderTextureFormat.ARGB4444;
                // 8 bit for each channel
                case MergeMaterial.MergedTextureFormat.Alpha8:
                case MergeMaterial.MergedTextureFormat.RGB24:
                case MergeMaterial.MergedTextureFormat.RGBA32:
                case MergeMaterial.MergedTextureFormat.ARGB32:
                case MergeMaterial.MergedTextureFormat.BGRA32:
                case MergeMaterial.MergedTextureFormat.DXT1:
                case MergeMaterial.MergedTextureFormat.DXT5:
                case MergeMaterial.MergedTextureFormat.BC7:
                    return RenderTextureFormat.ARGB32;
                case MergeMaterial.MergedTextureFormat.RGB565:
                    return RenderTextureFormat.RGB565;
                case MergeMaterial.MergedTextureFormat.R16:
                    return RenderTextureFormat.R16;
                case MergeMaterial.MergedTextureFormat.RG16:
                case MergeMaterial.MergedTextureFormat.BC5:
                    return RenderTextureFormat.RG16;
                case MergeMaterial.MergedTextureFormat.R8:
                case MergeMaterial.MergedTextureFormat.BC4:
                    return RenderTextureFormat.R8;
                case MergeMaterial.MergedTextureFormat.ASTC_4x4:
                case MergeMaterial.MergedTextureFormat.ASTC_5x5:
                case MergeMaterial.MergedTextureFormat.ASTC_6x6:
                case MergeMaterial.MergedTextureFormat.ASTC_8x8:
                case MergeMaterial.MergedTextureFormat.ASTC_10x10:
                case MergeMaterial.MergedTextureFormat.ASTC_12x12:
                    return RenderTextureFormat.ARGB32;
                case MergeMaterial.MergedTextureFormat.Default:
                default:
                    throw new ArgumentOutOfRangeException(nameof(finalFormat), finalFormat, null);
            }
        }

        private static Texture GenerateTexture(
            ValidatedMergeInfo mergeInfo,
            List<SubMesh> subMeshes,
            string propertyName,
            bool compress
        )
        {
            var texWidth = mergeInfo.Settings.textureSize.x;
            var texHeight = mergeInfo.Settings.textureSize.y;
            var finalFormat = mergeInfo.Settings.mergedFormat;
            if (finalFormat == MergeMaterial.MergedTextureFormat.Default)
#if UNITY_ANDROID || UNITY_IOS
                finalFormat = MergeMaterial.MergedTextureFormat.ASTC_6x6;
#else
                finalFormat = MergeMaterial.MergedTextureFormat.DXT5;
#endif

            // use compatible format
            var targetFormat = GraphicsFormatUtility.GetGraphicsFormat(GetRenderTarget(finalFormat), RenderTextureReadWrite.Default);
            targetFormat = SystemInfo.GetCompatibleFormat(targetFormat, FormatUsage.Render);
            var target = new RenderTexture(texWidth, texHeight, 0, targetFormat);

#if true
            foreach (var source in mergeInfo.Sources)
            {
                if (!source.UsagesByName.TryGetValue(propertyName, out var usageInformation)) continue;
                if (usageInformation.UVChannel == UVChannel.NonMeshRelated) continue;
                var sourceMat = subMeshes[source.SubMeshIndex].SharedMaterial!; // selected material should not be null
                var sourceTex = sourceMat.GetTexture(propertyName);
                var sourceTexTransform = usageInformation.UVMatrix!.Value;
                HelperMaterial.SetTexture(MainTexProp, sourceTex);
                HelperMaterial.SetMatrix(MainTexTransformProp, new UnityEngine.Matrix4x4()
                {
                    m00 = sourceTexTransform.M00,
                    m01 = sourceTexTransform.M01,
                    m02 = sourceTexTransform.M02,
                    m10 = sourceTexTransform.M10,
                    m11 = sourceTexTransform.M11,
                    m12 = sourceTexTransform.M12,
                    m22 = 1,
                    m33 = 1,
                });
                HelperMaterial.SetVector(RectProp,
                    new Vector4(source.Settings.targetRect.x, source.Settings.targetRect.y, 
                        source.Settings.targetRect.width, source.Settings.targetRect.height));
                Graphics.Blit(sourceTex, target, HelperMaterial);
            }
#endif

            var texture = CopyFromRenderTarget(target, finalFormat);

            DestroyTracker.DestroyImmediate(target);

            if (compress && IsCompressedFormat(finalFormat))
                EditorUtility.CompressTexture(texture, (TextureFormat)finalFormat, TextureCompressionQuality.Normal);

            Utils.Assert(texture.format == (TextureFormat)finalFormat, 
                $"TextureFormat mismatch: expected {finalFormat} but was {texture.format}");

            return texture;
        }

        private static Texture2D CopyFromRenderTarget(RenderTexture source, MergeMaterial.MergedTextureFormat finalFormat)
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
            private readonly MergeMaterialProcessor _processor;

            public MeshInfoComputer(MergeMaterialProcessor processor, IMeshInfoComputer upstream) : base(upstream)
                => _processor = processor;

            public override Material?[] Materials(bool fast = true)
            {
                var upstream = base.Materials(fast);
                // TODO: IMPLEMENT THIS FUNCTION
                return upstream;
            }
        }
    }
}
