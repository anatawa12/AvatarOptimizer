using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.API;
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

        public class ValidatedMergeInfo
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

        public class ValidatedMergeSource
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
            CheckForDuplicatedMaterials(Component);
            var validatedSettings = ValidateMaterials(target, Component);
            DoMergeMaterial(target, validatedSettings);
        }

        public static void CheckForDuplicatedMaterials(MergeMaterial component)
        {
            // check and remove duplicated materials in merge setting
            {
                var mergeInfoByMaterial = new Dictionary<ObjectReference, MergeMaterial.MergeInfo>();
                var duplicatedMaterials = new HashSet<ObjectReference>();

                foreach (var componentMerge in component.merges)
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
        }

        public static List<ValidatedMergeInfo> ValidateMaterials(MeshInfo2 target, MergeMaterial component)
        {
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
            var validatedSettings = new List<ValidatedMergeInfo>();

            foreach (var mergeInfo in component.merges)
            {
                if (mergeInfo.source.Length == 0) continue;

                // TODO: add option to override material properties and shader
                Shader? shader = null;
                Material? referenceMaterial = mergeInfo.referenceMaterial;
                MaterialInformation? referenceInfomration = null;
                bool isAutoReferenceMaterial = referenceMaterial == null;
                bool hasShaderError = false;
                var goodSources = new List<ValidatedMergeSource>();

                if (referenceMaterial != null) 
                {
                    shader = referenceMaterial.shader;
                    
                    // We don't reuse context.GetMaterialInformation(material)
                    // because we need to get non-animated material information
                    var matInfo = new MaterialInformation(referenceMaterial, new List<Renderer>() { target.SourceRenderer }, null);

                    if (matInfo.DefaultResult is not { TextureUsageInformationList: not null })
                    {
                        BuildLog.LogError("MergeMaterial:UnsupportedShaderInMergeSetting", referenceMaterial, shader);
                        hasShaderError = true;
                        continue;
                    }

                    referenceInfomration = matInfo;
                }

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
                    var matInfo = new MaterialInformation(material, new List<Renderer>() { target.SourceRenderer }, null);
                    referenceInfomration ??= matInfo;
                    if (matInfo.DefaultResult is not { TextureUsageInformationList: {} textureUsageInformations })
                    {
                        BuildLog.LogError("MergeMaterial:UnsupportedShaderInMergeSetting", material, shader);
                        hasShaderError = true;
                        continue;
                    }

                    if (referenceMaterial != material)
                    {
                        var badProperties = textureUsageInformations.Where(usage =>
                                usage.UVChannel != UVChannel.NonMeshRelated && usage.UVMatrix == null)
                            .Select(x => x.MaterialPropertyName)
                            .ToList();

                        if (badProperties.Any())
                        {
                            BuildLog.LogError("MergeMaterial:UnsupportedMaterialSettings:UnknownUVTransform",
                                string.Join(", ", badProperties), material);
                            continue;
                        }
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
                    var nonUsedProperties = allProperties.Except(referenceUsages.Select(x => x.MaterialPropertyName)).ToList();
                    if (nonUsedProperties.Any())
                    {
                        // TODO: consider just ignore unused textures?
                        BuildLog.LogError("MergeMaterial:ReferenceMaterial:NotAllTexturesUsed", referenceMaterial, string.Join(", ", nonUsedProperties));
                        continue;
                    }

                    var badProperties = referenceUsages.Where(usage => usage.UVChannel != UVChannel.NonMeshRelated && usage.UVMatrix != Matrix2x3.Identity)
                        .Select(x => x.MaterialPropertyName)
                        .ToList();
                    if (badProperties.Any())
                    {

                        // We don't have to create error if the reference material is automatically selected since it should
                        // already errored
                        if (!isAutoReferenceMaterial) BuildLog.LogError("MergeMaterial:ReferenceMaterial:UnknownUVTransform", string.Join(", ", badProperties), referenceMaterial);
                        continue;
                    }

                    // The settings LGTM! let's prepare for merge.

                    validatedSettings.Add(new ValidatedMergeInfo(referenceMaterial, referenceInfomration, mergeInfo, goodSources));
                }
            }

            return validatedSettings;
        }

        public static void DoMergeMaterial(MeshInfo2 target, List<ValidatedMergeInfo> validatedSettings)
        {
            var transformUvs = new int[target.SubMeshes.Count];
            var slotsForMerge = new BitArray(target.SubMeshes.Count);

            foreach (var validatedMergeInfo in validatedSettings)
            {
                var referenceUsages = validatedMergeInfo.ReferenceInformation.DefaultResult!.TextureUsageInformationList!;
                var uvFlags = referenceUsages.Aggregate(0, (f, i) => f | (i.UVChannel == UVChannel.NonMeshRelated ? 0 : 1 << ((int)i.UVChannel)));
                foreach (var goodSource in validatedMergeInfo.Sources) transformUvs[goodSource.SubMeshIndex] = uvFlags;
                foreach (var goodSource in validatedMergeInfo.Sources) slotsForMerge[goodSource.SubMeshIndex] = true;
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

        private static Vector2 MapUV(Vector2 vector2, Rect destSourceRect) =>
            new Vector2(vector2.x % 1, vector2.y % 1) * new Vector2(destSourceRect.width, destSourceRect.height) 
            + new Vector2(destSourceRect.x, destSourceRect.y);


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private static Material CreateMaterial(ValidatedMergeInfo mergeInfo, MeshInfo2 target, bool compress)
        {
            var mat = new Material(mergeInfo.ReferenceMaterial);
            mat.name = "AAO Merged Material";

            var mergePlanAndTargetProperties = new Dictionary<TextureMergePlan, List<string>>();

            foreach (var information in mergeInfo.ReferenceInformation.DefaultResult!.TextureUsageInformationList!)
            {
                if (information.UVChannel == UVChannel.NonMeshRelated) continue;
                // For mask textures, it's likely to have no texture.
                // In such case, we just return null.
                if (mergeInfo.Sources.All(source =>
                        !source.UsagesByName.ContainsKey(information.MaterialPropertyName) ||
                        target.SubMeshes[source.SubMeshIndex].SharedMaterial!.GetTexture(information.MaterialPropertyName) == null))
                    continue;
                
                HashSet<(Texture, Matrix2x3, Rect)> sources = new();
                foreach (var source in mergeInfo.Sources)
                {
                    if (!source.UsagesByName.TryGetValue(information.MaterialPropertyName, out var usageInformation)) continue;
                    var sourceMat = target.SubMeshes[source.SubMeshIndex].SharedMaterial!;
                    var sourceTex = sourceMat.GetTexture(information.MaterialPropertyName);
                    var sourceTexTransform = usageInformation.UVMatrix!.Value;

                    sources.Add((sourceTex, sourceTexTransform, source.Settings.targetRect));
                }

                var finalFormat = mergeInfo.Settings.mergedFormat;
                if (finalFormat == MergeMaterial.MergedTextureFormat.Default)
#if UNITY_ANDROID || UNITY_IOS
                finalFormat = MergeMaterial.MergedTextureFormat.ASTC_6x6;
#else
                    finalFormat = MergeMaterial.MergedTextureFormat.DXT5;
#endif

                var plan = new TextureMergePlan(mergeInfo.Settings.textureSize.x, mergeInfo.Settings.textureSize.y, finalFormat, sources);
                if (!mergePlanAndTargetProperties.TryGetValue(plan, out var targetProperties))
                    mergePlanAndTargetProperties.Add(plan, targetProperties = new List<string>());
                targetProperties.Add(information.MaterialPropertyName);
            }

            foreach (var (mergePlan, properties) in mergePlanAndTargetProperties)
            {
                var texture = GenerateTexture(mergePlan, compress: true);
                if (texture != null) texture.name = "AAO Merged Texture (for " + string.Join(", ", properties) + ")";

                foreach (var property in properties)
                    mat.SetTexture(property, texture);
            }

            return mat;
        }

        readonly struct TextureMergePlan : IEquatable<TextureMergePlan>
        {
            public readonly int Width;
            public readonly int Height;
            public readonly MergeMaterial.MergedTextureFormat Format;
            public readonly HashSet<(Texture, Matrix2x3, Rect)> Sources;

            public TextureMergePlan(int width, int height, MergeMaterial.MergedTextureFormat format, HashSet<(Texture, Matrix2x3, Rect)> sources)
            {
                Width = width;
                Height = height;
                Format = format;
                Sources = sources;
            }

            public bool Equals(TextureMergePlan other) =>
                Width == other.Width &&
                Height == other.Height &&
                Format == other.Format &&
                Sources.SetEquals(other.Sources);

            public override bool Equals(object? obj) => obj is TextureMergePlan other && Equals(other);
            public override int GetHashCode() => Sources.GetSetHashCode();
            public static bool operator ==(TextureMergePlan left, TextureMergePlan right) => left.Equals(right);
            public static bool operator !=(TextureMergePlan left, TextureMergePlan right) => !left.Equals(right);
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

        private static Texture? GenerateTexture(TextureMergePlan mergePlan, bool compress)
        {
            var texWidth = mergePlan.Width;
            var texHeight = mergePlan.Height;
            var finalFormat = mergePlan.Format;
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

            foreach (var (sourceTex, texTransform, targetRect) in mergePlan.Sources)
            {
                HelperMaterial.SetTexture(MainTexProp, sourceTex);
                HelperMaterial.SetMatrix(MainTexTransformProp, new UnityEngine.Matrix4x4()
                {
                    m00 = texTransform.M00,
                    m01 = texTransform.M01,
                    m02 = texTransform.M02,
                    m10 = texTransform.M10,
                    m11 = texTransform.M11,
                    m12 = texTransform.M12,
                    m22 = 1,
                    m33 = 1,
                });
                HelperMaterial.SetVector(RectProp,
                    new Vector4(targetRect.x, targetRect.y, 
                        targetRect.width, targetRect.height));
                Graphics.Blit(sourceTex, target, HelperMaterial);
            }

            var texture = CopyFromRenderTarget(target, finalFormat);

            DestroyTracker.DestroyImmediate(target);

            if (compress && IsCompressedFormat(finalFormat))
                EditorUtility.CompressTexture(texture, (TextureFormat)finalFormat, TextureCompressionQuality.Normal);

            if (compress)
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
