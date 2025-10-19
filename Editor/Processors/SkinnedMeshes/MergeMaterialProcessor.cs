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
            public readonly Dictionary<string, MergeMaterial.TextureConfigOverride> Overrides;

            public ValidatedMergeInfo(Material referenceMaterial, 
                MaterialInformation referenceInfomration,
                MergeMaterial.MergeInfo settings,
                List<ValidatedMergeSource> sources, 
                Dictionary<string, MergeMaterial.TextureConfigOverride> overrides)
            {
                ReferenceMaterial = referenceMaterial;
                ReferenceInformation = referenceInfomration;
                Settings = settings;
                Sources = sources;
                Overrides = overrides;
            }
        }

        public class ValidatedMergeSource
        {
            public MergeMaterial.MergeSource Settings;
            public int SubMeshIndex;
            public Material Material;
            public List<TextureUsageInformation> Usages;
            public Dictionary<string, TextureUsageInformation> UsagesByName;

            public ValidatedMergeSource(MergeMaterial.MergeSource settings, Material material, int subMeshIndex,
                List<TextureUsageInformation> usages)
            {
                Settings = settings;
                Material = material;
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
            DoMergeMaterial1(target, validatedSettings);
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
                    BuildLog.LogError("MergeMaterial:error:DuplicateMaterialInMergeSetting", duplicatedMaterials);
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

            var validatedSettings = new List<ValidatedMergeInfo>();

            var materialMapping = new Dictionary<Material, (Material, int)>();
            foreach (var mergeInfo in component.merges)
            {
                if (mergeInfo.source.Length == 0) continue;

                foreach (var mergeSource in mergeInfo.source)
                {
                    var materialReference = ObjectRegistry.GetReference(mergeSource.material);
                    if (!submeshByMaterial.TryGetValue(materialReference, out var subMeshIndices))
                    {
                        BuildLog.LogError("MergeMaterial:error:MaterialNotFoundInMesh", materialReference);
                        continue;
                    }

                    if (subMeshIndices.Count != 1)
                    {
                        BuildLog.LogError("MergeMaterial:error:MaterialUsedInMultipleSubMeshes",
                            materialReference);
                        continue;
                    }

                    var subMesh = target.SubMeshes[subMeshIndices[0]];
                    if (subMesh.SharedMaterials.Length != 1)
                    {
                        BuildLog.LogError("MergeMaterial:error:MultiMaterialSubMesh",
                            materialReference);
                        continue;
                    }

                    var material = subMesh.SharedMaterial!;

                    materialMapping[mergeSource.material!] = (material, subMeshIndices[0]);
                }
                
                var validatedInfo = ValidateOneSetting(mergeInfo, m => materialMapping.TryGetValue(m, out var pair) ? pair : (null, -1), out var errors);

                foreach (var error in errors)
                {
                    switch (error)
                    {
                        case UnsupportedShaderInMergeSetting setting:
                            BuildLog.LogError("MergeMaterial:error:UnsupportedShaderInMergeSetting",
                                setting.ReferenceMaterial, setting.ReferenceMaterial.shader);
                            break;
                        case UnsupportedUVTransformInReferenceMaterial setting:
                            BuildLog.LogError("MergeMaterial:error:UnsupportedUVTransformInReferenceMaterial",
                                string.Join(", ", setting.BadProperties), setting.ReferenceMaterial);
                            break;
                        case UnknownUVTransform setting:
                            BuildLog.LogError("MergeMaterial:error:UnknownUVTransform",
                                string.Join(", ", setting.BadProperties), setting.Material);
                            break;
                        case DifferentShaderInMergeSetting:
                            BuildLog.LogError("MergeMaterial:error:DifferentShaderInMergeSetting",
                                mergeInfo.source.Select(x => x.material));
                            break;
                        case NotAllTexturesUsed settings:
                            BuildLog.LogError("MergeMaterial:error:ReferenceMaterial:NotAllTexturesUsed", 
                                string.Join(", ", settings.NonUsedProperties),
                                settings.ReferenceMaterial);
                            break;
                    }
                }

                if (validatedInfo != null)
                {
                    validatedSettings.Add(validatedInfo);
                    break;
                }
            }

            return validatedSettings;
        }

        public abstract class RootValidationError {}

        public class UnsupportedShaderInMergeSetting : RootValidationError
        {
            public readonly Material ReferenceMaterial;

            public UnsupportedShaderInMergeSetting(Material referenceMaterial)
            {
                ReferenceMaterial = referenceMaterial;
            }
        }

        public class UnsupportedUVTransformInReferenceMaterial : RootValidationError
        {
            public readonly List<string> BadProperties;
            public readonly Material ReferenceMaterial;

            public UnsupportedUVTransformInReferenceMaterial(List<string> badProperties, Material referenceMaterial)
            {
                BadProperties = badProperties;
                ReferenceMaterial = referenceMaterial;
            }
        }

        public class UnknownUVTransform : RootValidationError
        {
            public readonly List<string> BadProperties;
            public readonly Material Material;

            public UnknownUVTransform(List<string> badProperties, Material material)
            {
                BadProperties = badProperties;
                Material = material;
            }
        }

        public class DifferentShaderInMergeSetting : RootValidationError
        {
        }

        public class NotAllTexturesUsed : RootValidationError
        {
            public readonly List<string> NonUsedProperties;
            public readonly Material ReferenceMaterial;

            public NotAllTexturesUsed(List<string> nonUsedProperties, Material referenceMaterial)
            {
                NonUsedProperties = nonUsedProperties;
                ReferenceMaterial = referenceMaterial;
            }
        }

        public static ValidatedMergeInfo? ValidateOneSetting(MergeMaterial.MergeInfo mergeInfo, Func<Material, (Material?, int)> normalizeMaterial, out List<RootValidationError> validationErrors)
        {
            validationErrors = new List<RootValidationError>();
            {
                if (mergeInfo.source.Length == 0) return null;
                var mappedMaterials = mergeInfo.source.Select(x => x.material != null ? normalizeMaterial(x.material) : default).ToArray();

                var referenceMaterial = mergeInfo.referenceMaterial;
                if (referenceMaterial == null)
                    referenceMaterial = mappedMaterials.FirstOrDefault(x => x.Item1).Item1;

                if (referenceMaterial == null) return null; // All sources are null

                // We don't reuse context.GetMaterialInformation(material)
                // because we need to get non-animated material information
                var referenceInformation = new MaterialInformation(referenceMaterial, new List<Renderer>(), null);

                {
                    if (referenceInformation.DefaultResult is not { TextureUsageInformationList: not null })
                    {
                        validationErrors.Add(new UnsupportedShaderInMergeSetting(referenceMaterial));
                        return null;
                    }

                    var referenceUsages = referenceInformation.DefaultResult!.TextureUsageInformationList!;
                    var badProperties = referenceUsages.Where(usage =>
                            usage.UVChannel != UVChannel.NonMeshRelated && usage.UVMatrix != Matrix2x3.Identity)
                        .Select(x => x.MaterialPropertyName)
                        .ToList();
                    if (badProperties.Any())
                    {
                        validationErrors.Add(new UnsupportedUVTransformInReferenceMaterial(badProperties, referenceMaterial));
                        return null;
                    }
                }

                if (mappedMaterials.Any(x => x.Item1 != null && x.Item1.shader != referenceMaterial.shader))
                {
                    validationErrors.Add(new DifferentShaderInMergeSetting());
                    return null;
                }

                var goodSources = new List<ValidatedMergeSource>();
                for (var index = 0; index < mergeInfo.source.Length; index++)
                {
                    var mergeSource = mergeInfo.source[index];
                    var (material, subMeshIndex) = mappedMaterials[index];

                    if (material == null) continue; // skipped

                    // We don't reuse context.GetMaterialInformation(material)
                    // because we need to get non-animated material information
                    var matInfo = new MaterialInformation(material, new List<Renderer>(), null);

                    // We have checked that the shader is same as referenceMaterial.shader, and referenceMaterial has valid information.
                    var textureUsageInformations = matInfo.DefaultResult?.TextureUsageInformationList!;
                    Utils.Assert(textureUsageInformations != null,
                        $"textureUsageInformations != null: Unstable ShaderInformation: {material.shader}");

                    if (referenceMaterial != material)
                    {
                        var badProperties = textureUsageInformations.Where(usage =>
                                usage.UVChannel != UVChannel.NonMeshRelated && usage.UVMatrix == null)
                            .Select(x => x.MaterialPropertyName)
                            .ToList();

                        if (badProperties.Any())
                        {
                            validationErrors.Add(new UnknownUVTransform(badProperties, referenceMaterial));
                            continue;
                        }
                    }

                    goodSources.Add(new(mergeSource, material, subMeshIndex, textureUsageInformations));
                }

                var textureFormats = new Dictionary<string, MergeMaterial.TextureConfigOverride>();

                foreach (var @override in mergeInfo.textureConfigOverrides)
                {
                    if (@override.textureName != null)
                    {
                        textureFormats.TryAdd(@override.textureName, @override);
                    }
                }

                {
                    // Check the textureUsage for referenceMaterial:
                    // - has texture usage for all textures available
                    // - has texture usage with identity matrix
                    Utils.Assert(referenceInformation!.Material == referenceMaterial);

                    var allProperties = goodSources.SelectMany(x => x.Usages).Select(x => x.MaterialPropertyName)
                        .ToHashSet();
                    var referenceUsages = referenceInformation.DefaultResult!.TextureUsageInformationList!;
                    var nonUsedProperties =
                        allProperties.Except(referenceUsages.Select(x => x.MaterialPropertyName)).ToList();
                    if (nonUsedProperties.Any())
                    {
                        // TODO: consider just ignore unused textures?
                        validationErrors.Add(new NotAllTexturesUsed(nonUsedProperties, referenceMaterial));
                        return null;
                    }

                    // The settings LGTM! let's prepare for merge.

                    return new ValidatedMergeInfo(referenceMaterial, referenceInformation, mergeInfo, goodSources, textureFormats);
                }
            }
        }

        public static void DoMergeMaterial(MeshInfo2 target, List<ValidatedMergeInfo> validatedSettings)
        {
            var transformUvs = new int[target.SubMeshes.Count];

            foreach (var validatedMergeInfo in validatedSettings)
            {
                var referenceUsages = validatedMergeInfo.ReferenceInformation.DefaultResult!.TextureUsageInformationList!;
                var uvFlags = referenceUsages.Aggregate(0, (f, i) => f | (i.UVChannel == UVChannel.NonMeshRelated ? 0 : 1 << ((int)i.UVChannel)));
                foreach (var goodSource in validatedMergeInfo.Sources) transformUvs[goodSource.SubMeshIndex] = uvFlags;
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
        }

        public static void DoMergeMaterial1(MeshInfo2 target, List<ValidatedMergeInfo> validatedSettings)
        {
            var slotsForMerge = new BitArray(target.SubMeshes.Count);

            foreach (var validatedMergeInfo in validatedSettings)
            foreach (var goodSource in validatedMergeInfo.Sources) slotsForMerge[goodSource.SubMeshIndex] = true;

            // merge submeshes
            var copied = target.SubMeshes.Where((_, i) => !slotsForMerge[i]);
            var merged = validatedSettings.Select(x => new SubMesh(
                x.Sources.SelectMany(src => target.SubMeshes[src.SubMeshIndex].Triangles).ToList(),
                CreateMaterial(x, compress: true)));
            var subMeshes = copied.Concat(merged).ToList();
            target.SubMeshes.Clear();
            target.SubMeshes.AddRange(subMeshes);
        }

        private static Vector2 MapUV(Vector2 vector2, Rect destSourceRect) =>
            new Vector2(vector2.x % 1, vector2.y % 1) * new Vector2(destSourceRect.width, destSourceRect.height) 
            + new Vector2(destSourceRect.x, destSourceRect.y);


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static Texture CreateTexture(ValidatedMergeInfo mergeInfo, bool compress, string propertyName) =>
            GenerateTexture(MakeMergePlan(mergeInfo, propertyName), compress);

        public static Material CreateMaterial(ValidatedMergeInfo mergeInfo, bool compress)
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
                        source.Material.GetTexture(information.MaterialPropertyName) == null))
                    continue;

                var plan = MakeMergePlan(mergeInfo, information.MaterialPropertyName);
                if (!mergePlanAndTargetProperties.TryGetValue(plan, out var targetProperties))
                    mergePlanAndTargetProperties.Add(plan, targetProperties = new List<string>());
                targetProperties.Add(information.MaterialPropertyName);
            }

            foreach (var (mergePlan, properties) in mergePlanAndTargetProperties)
            {
                var texture = GenerateTexture(mergePlan, compress);
                if (texture != null) texture.name = "AAO Merged Texture (for " + string.Join(", ", properties) + ")";

                foreach (var property in properties)
                    mat.SetTexture(property, texture);
            }

            return mat;
        }

        private static TextureMergePlan MakeMergePlan(ValidatedMergeInfo mergeInfo, string propertyName)
        {
            HashSet<(Texture?, Matrix2x3, Rect)> sources = new();
            foreach (var source in mergeInfo.Sources)
            {
                if (!source.UsagesByName.TryGetValue(propertyName, out var usageInformation))
                    continue;
                var sourceTex = source.Material.GetTexture(propertyName);
                var sourceTexTransform = usageInformation.UVMatrix!.Value;

                sources.Add((sourceTex, sourceTexTransform, source.Settings.targetRect));
            }

            var finalFormat = mergeInfo.Settings.mergedFormat;
            var size = mergeInfo.Settings.textureSize;

            if (mergeInfo.Overrides.TryGetValue(propertyName, out var @override))
                (finalFormat, size) = (@override.formatOverride, @override.sizeOverride);

            if (finalFormat == MergeMaterial.MergedTextureFormat.Default)
#if UNITY_ANDROID || UNITY_IOS
                finalFormat = MergeMaterial.MergedTextureFormat.ASTC_6x6;
#else
                finalFormat = MergeMaterial.MergedTextureFormat.DXT5;
#endif

            return new TextureMergePlan(size.x, size.y,
                finalFormat, sources);
        }

        readonly struct TextureMergePlan : IEquatable<TextureMergePlan>
        {
            public readonly int Width;
            public readonly int Height;
            public readonly MergeMaterial.MergedTextureFormat Format;
            public readonly HashSet<(Texture?, Matrix2x3, Rect)> Sources;

            public TextureMergePlan(int width, int height, MergeMaterial.MergedTextureFormat format, HashSet<(Texture?, Matrix2x3, Rect)> sources)
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

        private static Texture GenerateTexture(TextureMergePlan mergePlan, bool compress)
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

            if (RenderTexture.active == target) RenderTexture.active = null;

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
