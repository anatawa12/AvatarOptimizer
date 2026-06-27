using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.ndmf;
using nadena.dev.ndmf;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

#if AAO_VRCSDK3_AVATARS
using VRC.SDKBase.Validation.Performance;
using VRC.SDKBase.Validation.Performance.Stats;
#endif

namespace Anatawa12.AvatarOptimizer.Processors.OptimizationMetrics;

internal class CaptureOptimizationMetricsBefore : Pass<CaptureOptimizationMetricsBefore>
{
    public override string DisplayName => "Optimization Metrics: Capture Before";

    protected override void Execute(BuildContext context)
    {
        if (!context.GetState<AAOEnabled>().Enabled || !OptimizationMetricsSettings.EnableOptimizationMetrics) return;
        context.GetState<OptimizationMetricsState>().Before = OptimizationMetricsImpl.Capture(context.AvatarRootObject);
    }
}

internal class LogOptimizationMetricsAfter : Pass<LogOptimizationMetricsAfter>
{
    public override string DisplayName => "Optimization Metrics: Log Result";

    protected override void Execute(BuildContext context)
    {
        if (!context.GetState<AAOEnabled>().Enabled || !OptimizationMetricsSettings.EnableOptimizationMetrics) return;

        var state = context.GetState<OptimizationMetricsState>();

        var before = state.Before;
        if (before == null) return;

        var after = OptimizationMetricsImpl.Capture(context.AvatarRootObject);
        var lines = BuildDiffLines(before, after);
        if (lines.Count == 0) return;

        BuildLog.LogInfo("OptimizationMetrics:Results", string.Join("\n", lines));
    }

    private static List<string> BuildDiffLines(OptimizationMetricsSnapshot before, OptimizationMetricsSnapshot after)
    {
        var lines = new List<string>();
        foreach (var (key, value) in before.ResultsByKey)
        {
            // This should not happen.
            if (!after.ResultsByKey.TryGetValue(key, out var aStr)) continue;
            // Skip if both values are identical, as we only want to show differences.
            if (string.Equals(value, aStr, StringComparison.Ordinal)) continue;
            // We collect VRCTextureMegabytes for logging purposes, but excluded from report because it's not reliable.
            if (key.StartsWith("Hidden", StringComparison.Ordinal)) continue;

            var categoryName = AAOL10N.TryTr($"OptimizationMetrics:{key}") ?? key;
            lines.Add($"{categoryName}: {value} → {aStr}");
        }
        return lines;
    }
}

internal class OptimizationMetricsState
{
    public OptimizationMetricsSnapshot? Before { get; set; }
}

internal sealed class OptimizationMetricsSnapshot
{
    public Dictionary<string, string> ResultsByKey { get; } = new();
}

internal static class OptimizationMetricsImpl
{
    public static OptimizationMetricsSnapshot Capture(GameObject avatarRoot)
    {
        return MetricsSourceRegistry.Capture(avatarRoot);
    }

    interface IMetricsSource
    {
        int Priority { get; }
        IReadOnlyDictionary<string, string> Capture(GameObject avatarRoot);
    }

    static class MetricsSourceRegistry
    {
        private static IReadOnlyList<IMetricsSource>? _sources;
        public static IReadOnlyList<IMetricsSource> Sources => _sources ??= Build();

        private static IReadOnlyList<IMetricsSource> Build()
        {
            var list = new List<IMetricsSource>
            {
#if AAO_VRCSDK3_AVATARS
                new VrcMetricsSource(),
#endif
                new CustomMetricsSource()
            };
            return list.OrderBy(s => s.Priority).ToList();
        }

        public static OptimizationMetricsSnapshot Capture(GameObject avatarRoot)
        {
            var snapshot = new OptimizationMetricsSnapshot();
            foreach (var source in Sources)
            {
                var data = source.Capture(avatarRoot);
                foreach (var (key, value) in data)
                {
                    snapshot.ResultsByKey.TryAdd(key, value);
                }
            }
            if (Tracing.IsEnabled(TracingArea.OptimizationMetrics))
            {
                foreach (var (key, value) in snapshot.ResultsByKey)
                    Tracing.Trace(TracingArea.OptimizationMetrics, $"[{key}] = {value}");
            }
            return snapshot;
        }
    }

    class CustomMetricsSource : IMetricsSource
    {
        public int Priority => 0;

        static class CustomMetricKeys
        {
            public const string BlendShapeCount = "BlendShapeCount";
            public const string TextureMegabytes = "TextureMegabytes";
            public const string AnimatorLayerCount = "AnimatorLayerCount";
            public const string GameObjectCount = "GameObjectCount";
        }

        public IReadOnlyDictionary<string, string> Capture(GameObject avatarRoot)
        {
            return new Dictionary<string, string>
            {
                [CustomMetricKeys.BlendShapeCount] = avatarRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Where(smr => smr.sharedMesh != null)
                    .Sum(smr => smr.sharedMesh.blendShapeCount).ToString(),
                [CustomMetricKeys.TextureMegabytes] = ComputeTextureUsage(avatarRoot),
                [CustomMetricKeys.GameObjectCount] = avatarRoot.GetComponentsInChildren<Transform>(true).Length.ToString(),
                [CustomMetricKeys.AnimatorLayerCount] = CountAnimatorLayers(avatarRoot).ToString(),
            };
        }

        private string ComputeTextureUsage(GameObject avatarRoot)
        {
            var materials = new HashSet<Material>();
            {
                var sharedMaterials = new List<Material>();
                foreach (var renderer in avatarRoot.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.GetSharedMaterials(sharedMaterials);
                    materials.UnionWith(sharedMaterials);
                }
            }

            var textures = new HashSet<Texture>();
            foreach (var material in materials)
            {
                if (material == null) continue;
                Tracing.Trace(TracingArea.OptimizationMetrics, $"Collecting texture from {material}");
                var shader = material.shader;
                var propertyCount = shader.GetPropertyCount();
                for (int i = 0; i < propertyCount; i++)
                {
                    if (shader.GetPropertyType(i) == ShaderPropertyType.Texture)
                    {
                        var tex = material.GetTexture(shader.GetPropertyNameId(i));
                        if (tex != null)
                        {
                            Tracing.Trace(TracingArea.OptimizationMetrics, $"From Shader: Property {shader.GetPropertyName(i)}({i}): {tex}");
                            textures.Add(tex);
                        }
                    }
                }

                var names = new List<string>();
                material.GetTexturePropertyNames(names);
                foreach (var name in names)
                {
                    var tex = material.GetTexture(name);
                    if (tex != null)
                    {
                        Tracing.Trace(TracingArea.OptimizationMetrics, $"From Material: Property {name}: {tex}");
                        textures.Add(tex);
                    }
                }
            }

            long totalTextureUsageBytes = 0;
            foreach (var texture in textures)
            {
                var textureUsage = texture.graphicsFormat switch
                {
                    // Uncompressed 8-bit per component
                    GraphicsFormat.R8_SRGB or GraphicsFormat.R8_UNorm or GraphicsFormat.R8_SNorm or GraphicsFormat.R8_UInt or GraphicsFormat.R8_SInt
                        => TextureSizeUnblocked(texture, 1),
                    GraphicsFormat.R8G8_SRGB or GraphicsFormat.R8G8_UNorm or GraphicsFormat.R8G8_SNorm or GraphicsFormat.R8G8_UInt or GraphicsFormat.R8G8_SInt 
                        => TextureSizeUnblocked(texture, 2),
                    GraphicsFormat.R8G8B8_SRGB or GraphicsFormat.R8G8B8_UNorm or GraphicsFormat.R8G8B8_SNorm or GraphicsFormat.R8G8B8_UInt or GraphicsFormat.R8G8B8_SInt 
                        or GraphicsFormat.B8G8R8_SRGB or GraphicsFormat.B8G8R8_UNorm or GraphicsFormat.B8G8R8_SNorm or GraphicsFormat.B8G8R8_UInt or GraphicsFormat.B8G8R8_SInt 
                        => TextureSizeUnblocked(texture, 3),
                    GraphicsFormat.R8G8B8A8_SRGB or GraphicsFormat.R8G8B8A8_UNorm or GraphicsFormat.R8G8B8A8_SNorm or GraphicsFormat.R8G8B8A8_UInt or GraphicsFormat.R8G8B8A8_SInt 
                        or GraphicsFormat.B8G8R8A8_SRGB or GraphicsFormat.B8G8R8A8_UNorm or GraphicsFormat.B8G8R8A8_SNorm or GraphicsFormat.B8G8R8A8_UInt or GraphicsFormat.B8G8R8A8_SInt 
                        => TextureSizeUnblocked(texture, 4),
                    // Uncompressed 16-bit per component
                    GraphicsFormat.R16_UNorm or GraphicsFormat.R16_SNorm or GraphicsFormat.R16_UInt or GraphicsFormat.R16_SInt or GraphicsFormat.R16_SFloat
                        => TextureSizeUnblocked(texture, 2),
                    GraphicsFormat.R16G16_UNorm or GraphicsFormat.R16G16_SNorm or GraphicsFormat.R16G16_UInt or GraphicsFormat.R16G16_SInt or GraphicsFormat.R16G16_SFloat
                        => TextureSizeUnblocked(texture, 4),
                    GraphicsFormat.R16G16B16_UNorm or GraphicsFormat.R16G16B16_SNorm or GraphicsFormat.R16G16B16_UInt or GraphicsFormat.R16G16B16_SInt or GraphicsFormat.R16G16B16_SFloat
                        => TextureSizeUnblocked(texture, 6),
                    GraphicsFormat.R16G16B16A16_UNorm or GraphicsFormat.R16G16B16A16_SNorm or GraphicsFormat.R16G16B16A16_UInt or GraphicsFormat.R16G16B16A16_SInt or GraphicsFormat.R16G16B16A16_SFloat
                        => TextureSizeUnblocked(texture, 8),
                    // Uncompressed 32-bit per component
                    GraphicsFormat.R32_UInt or GraphicsFormat.R32_SInt or GraphicsFormat.R32_SFloat
                        => TextureSizeUnblocked(texture, 4),
                    GraphicsFormat.R32G32_UInt or GraphicsFormat.R32G32_SInt or GraphicsFormat.R32G32_SFloat
                        => TextureSizeUnblocked(texture, 8),
                    GraphicsFormat.R32G32B32_UInt or GraphicsFormat.R32G32B32_SInt or GraphicsFormat.R32G32B32_SFloat
                        => TextureSizeUnblocked(texture, 12),
                    GraphicsFormat.R32G32B32A32_UInt or GraphicsFormat.R32G32B32A32_SInt
                        or GraphicsFormat.R32G32B32A32_SFloat => TextureSizeUnblocked(texture, 16),
                    // packed 16bit, 4:4:4:4, 5:6:5, and 5:5:5:1
                    GraphicsFormat.R4G4B4A4_UNormPack16 or GraphicsFormat.B4G4R4A4_UNormPack16
                        or GraphicsFormat.R5G6B5_UNormPack16 or GraphicsFormat.B5G6R5_UNormPack16
                        or GraphicsFormat.R5G5B5A1_UNormPack16 or GraphicsFormat.B5G5R5A1_UNormPack16 or GraphicsFormat.A1R5G5B5_UNormPack16
                        => TextureSizeUnblocked(texture, 2),
                    // packed 32bit, 5:9:9:9, 10:11:11, 2:10:10:10
                    GraphicsFormat.E5B9G9R9_UFloatPack32
                        or GraphicsFormat.B10G11R11_UFloatPack32
                        or GraphicsFormat.A2B10G10R10_UNormPack32 or GraphicsFormat.A2B10G10R10_UIntPack32 or GraphicsFormat.A2B10G10R10_SIntPack32
                        or GraphicsFormat.A2R10G10B10_UNormPack32 or GraphicsFormat.A2R10G10B10_UIntPack32 or GraphicsFormat.A2R10G10B10_SIntPack32 or GraphicsFormat.A2R10G10B10_XRSRGBPack32 or GraphicsFormat.A2R10G10B10_XRUNormPack32
                        or GraphicsFormat.R10G10B10_XRSRGBPack32 or GraphicsFormat.R10G10B10_XRUNormPack32
                        => TextureSizeUnblocked(texture, 4),
                    // packed 64-bit? 4 * 10 bit
                    GraphicsFormat.A10R10G10B10_XRSRGBPack32 or GraphicsFormat.A10R10G10B10_XRUNormPack32
                        => TextureSizeUnblocked(texture, 4),

                    // basic 4x4-block based compression formats => 64 bit / 8 bytes, 4bpp
                    // DXT1: 4x4 pixels => 64 bit / 8 bytes (3 channels, no alpha)
                    GraphicsFormat.RGBA_DXT1_SRGB or GraphicsFormat.RGBA_DXT1_UNorm
                    // BC4: 4x4 pixels => 64 bit / 8 bytes (2 channels)
                        or GraphicsFormat.R_BC4_UNorm or GraphicsFormat.R_BC4_SNorm
                    // ETC: 4x4 => 64 bit / 8 bytes (3 channels)
                    // ETC2(RGB): 4x4 => 64 bit / 8 bytes (3 channels)
                        or GraphicsFormat.RGB_ETC_UNorm or GraphicsFormat.RGB_ETC2_SRGB or GraphicsFormat.RGB_ETC2_UNorm
                    // ETC2(RGBA1): 4x4 => 64 bit / 8 bytes (3 channels)
                        or GraphicsFormat.RGB_A1_ETC2_SRGB or GraphicsFormat.RGB_A1_ETC2_UNorm
                    // EAC: 4x4 => 64 bit / 8 bytes (1 channel)
                        or GraphicsFormat.R_EAC_UNorm or GraphicsFormat.R_EAC_SNorm
                        => TextureSize2dBlocked(texture, 4, 4, 8),

                    // basic 4x4-block based compression formats => 128 bit / 16 bytes, 8bpp
                    // DXT3: 4x4 pixels => 128 bit / 16 bytes (4 channels, with explicit alpha)
                    GraphicsFormat.RGBA_DXT3_SRGB or GraphicsFormat.RGBA_DXT3_UNorm
                    // DXT5: 4x4 pixels => 128 bit / 16 bytes (4 channels)
                        or GraphicsFormat.RGBA_DXT5_SRGB or GraphicsFormat.RGBA_DXT5_UNorm
                    // BC5: 4x4 pixels => 128 bit / 16 bytes (2 channels)
                        or GraphicsFormat.RG_BC5_UNorm or GraphicsFormat.RG_BC5_SNorm
                    // BC6H: 4x4 pixels => 128 bit / 16 bytes (3 channels, HDR, no alpha)
                        or GraphicsFormat.RGB_BC6H_UFloat or GraphicsFormat.RGB_BC6H_SFloat
                    // BC7: 4x4 pixels => 128 bit / 16 bytes (4 channels)
                        or GraphicsFormat.RGBA_BC7_SRGB or GraphicsFormat.RGBA_BC7_UNorm
                    // ETC2(RGBA): 4x4 => 128 bit / 16 bytes (4 channels)
                        or GraphicsFormat.RGBA_ETC2_SRGB or GraphicsFormat.RGBA_ETC2_UNorm
                    // EAC(2): 4x4 => 128 bit / 16 bytes (2 channels)
                        or GraphicsFormat.RG_EAC_UNorm or GraphicsFormat.RG_EAC_SNorm
                        => TextureSize2dBlocked(texture, 4, 4, 16),

                    // PVRTC 2BPP: 4x8 => 64 bit / 8 bytes with bilinear (4 channels, requires Power of Two size?)
                    GraphicsFormat.RGB_PVRTC_2Bpp_SRGB or GraphicsFormat.RGB_PVRTC_2Bpp_UNorm or GraphicsFormat.RGBA_PVRTC_2Bpp_SRGB or GraphicsFormat.RGBA_PVRTC_2Bpp_UNorm
                        => TextureSize2dBlocked(texture, 4, 8, 8),
                    // PVRTC 4BPP: 4x4 => 64 bit / 8 bytes with bilinear (4 channels, requires Power of Two size?)
                    GraphicsFormat.RGB_PVRTC_4Bpp_SRGB or GraphicsFormat.RGB_PVRTC_4Bpp_UNorm or GraphicsFormat.RGBA_PVRTC_4Bpp_SRGB or GraphicsFormat.RGBA_PVRTC_4Bpp_UNorm
                        => TextureSize2dBlocked(texture, 4, 4, 8),

                    // ASTC: kxk pixels => 128 bit / 16 bytes (4 channels, possibly HDR)
                    GraphicsFormat.RGBA_ASTC4X4_SRGB or GraphicsFormat.RGBA_ASTC4X4_UNorm or GraphicsFormat.RGBA_ASTC4X4_UFloat
                        => TextureSize2dBlocked(texture, 4, 4, 16),
                    GraphicsFormat.RGBA_ASTC5X5_SRGB or GraphicsFormat.RGBA_ASTC5X5_UNorm or GraphicsFormat.RGBA_ASTC5X5_UFloat
                        => TextureSize2dBlocked(texture, 5, 5, 16),
                    GraphicsFormat.RGBA_ASTC6X6_SRGB or GraphicsFormat.RGBA_ASTC6X6_UNorm or GraphicsFormat.RGBA_ASTC6X6_UFloat
                        => TextureSize2dBlocked(texture, 6, 6, 16),
                    GraphicsFormat.RGBA_ASTC8X8_SRGB or GraphicsFormat.RGBA_ASTC8X8_UNorm or GraphicsFormat.RGBA_ASTC8X8_UFloat
                        => TextureSize2dBlocked(texture, 8, 8, 16),
                    GraphicsFormat.RGBA_ASTC10X10_SRGB or GraphicsFormat.RGBA_ASTC10X10_UNorm or GraphicsFormat.RGBA_ASTC10X10_UFloat
                        => TextureSize2dBlocked(texture, 10, 10, 16),
                    GraphicsFormat.RGBA_ASTC12X12_SRGB or GraphicsFormat.RGBA_ASTC12X12_UNorm or GraphicsFormat.RGBA_ASTC12X12_UFloat
                        => TextureSize2dBlocked(texture, 12, 12, 16),

                    // YUV 4:2:2: 2x1 pixels => 32 bit / 4 bytes (3 channels)
                    GraphicsFormat.YUV2 => TextureSize2dBlocked(texture, 2, 1, 4),
                    // Depth buffer
                    GraphicsFormat.D16_UNorm => TextureSizeUnblocked(texture, 4),
                    GraphicsFormat.D24_UNorm => TextureSizeUnblocked(texture, 3),
                    GraphicsFormat.D24_UNorm_S8_UInt => TextureSizeUnblocked(texture, 4),
                    GraphicsFormat.D32_SFloat => TextureSizeUnblocked(texture, 4),
                    GraphicsFormat.D32_SFloat_S8_UInt => TextureSizeUnblocked(texture, 5), // possibly +24 bit unused => 8 bytes
                    GraphicsFormat.S8_UInt => TextureSizeUnblocked(texture, 1),
                    GraphicsFormat.D16_UNorm_S8_UInt => TextureSizeUnblocked(texture, 3),

                    _ => UnknownTextureSize(texture)
                };

                Tracing.Trace(TracingArea.OptimizationMetrics, $"Texture {texture} uses {textureUsage} bytes ({(float)textureUsage / (1024 * 1024):F2} MiB)");

                totalTextureUsageBytes += textureUsage;
            }

            return $"{(float)totalTextureUsageBytes / (1024 * 1024):F2} MiB";

            static long TextureSize2dBlocked(Texture texture, int blockWidth, int blockHeight, int blockBytes)
            {
                if (texture.dimension is not (TextureDimension.Tex2D or TextureDimension.Tex2DArray or TextureDimension.Cube or TextureDimension.CubeArray))
                    Debug.LogError(
                        $"Collecting texture from {texture}: Unexpected texture format for block compression: {texture.dimension}, {texture.graphicsFormat}");
                long single2DTextureSize = 0;
                var width = texture.width;
                var height = texture.height;
                for (var mipLevel = texture.mipmapCount - 1; mipLevel >= 0; mipLevel++)
                {
                    var levelWidth = Math.Max(width >> mipLevel, 1);
                    var levelHeight = Math.Max(height >> mipLevel, 1);
                    var xBlocks = (levelWidth + blockWidth - 1) / blockWidth;
                    var yBlocks = (levelHeight + blockHeight - 1) / blockHeight;
                    single2DTextureSize += (long)xBlocks * yBlocks * blockBytes;
                }

                var planes = 1;
                if (texture.dimension is TextureDimension.Cube or TextureDimension.CubeArray)
                    planes *= 6;
                if (texture is CubemapArray cubemapArray) planes *= cubemapArray.cubemapCount;
                else if (texture is Texture2DArray texture2DArray) planes *= texture2DArray.depth;
                else if (texture is RenderTexture renderTexture) planes *= renderTexture.volumeDepth;

                return single2DTextureSize * planes;
            }

            static long TextureSizeUnblocked(Texture texture, int bytesPerPixel)
            {
                static long Compute2DTexPixels(int width, int height, int mipmapCount)
                {
                    long totalPixels = 0;
                    for (var mipLevel = 0; mipLevel < mipmapCount; mipLevel++)
                        totalPixels += Math.Max(width >> mipLevel, 1) * Math.Max(height >> mipLevel, 1);
                    return totalPixels;
                }
                static long Compute3DTexPixels(int width, int height, int depth, int mipmapCount)
                {
                    long totalPixels = 0;
                    for (var mipLevel = 0; mipLevel < mipmapCount; mipLevel++)
                        totalPixels += Math.Max(width >> mipLevel, 1) * Math.Max(height >> mipLevel, 1) * Math.Max(depth >> mipLevel, 1);
                    return totalPixels;
                }

                static int UnsupportedDimWith3rdAxis(Texture texture)
                {
                    Debug.LogError($"Unsupported texture type for {texture.dimension}: {texture.GetType()}");
                    return 1;
                }
                
                static long ComputePixelsForUnknownDimention(Texture texture)
                {
                    Debug.LogError($"Unsupported texture type: {texture.graphicsFormat} ({texture.GetType()})");
                    return 0;
                }

                var totalPixels = texture.dimension switch
                {
                    TextureDimension.Tex2D => Compute2DTexPixels(texture.width, texture.height, texture.mipmapCount),
                    TextureDimension.Tex3D => Compute3DTexPixels(texture.width, texture.height, texture switch
                    {
                        Texture3D tex3D => tex3D.depth,
                        RenderTexture rt => rt.volumeDepth,
                        _ => UnsupportedDimWith3rdAxis(texture)
                    }, texture.mipmapCount),
                    TextureDimension.Cube => Compute2DTexPixels(texture.width, texture.height, texture.mipmapCount) * 6,
                    TextureDimension.Tex2DArray => Compute2DTexPixels(texture.width, texture.height, texture.mipmapCount)
                                                   * (texture switch
                                                   {
                                                       Texture2DArray array => array.depth,
                                                       RenderTexture rt => rt.volumeDepth,
                                                       _ => UnsupportedDimWith3rdAxis(texture)
                                                   }),
                    TextureDimension.CubeArray => Compute2DTexPixels(texture.width, texture.height, texture.mipmapCount) 
                                                  * 6 * (texture switch
                                                  {
                                                      CubemapArray array => array.cubemapCount,
                                                      RenderTexture rt => rt.volumeDepth,
                                                      _ => UnsupportedDimWith3rdAxis(texture)
                                                  }),
                    _ => ComputePixelsForUnknownDimention(texture)
                };

                return totalPixels * bytesPerPixel;
            }

            long UnknownTextureSize(Texture texture)
            {
                Debug.LogError($"Unsupported texture type. assuming 16 bpp: {texture.graphicsFormat} ({texture.GetType()})");
                return TextureSizeUnblocked(texture, 2);
            }
        }

        private int CountAnimatorLayers(GameObject avatarRoot)
        {
            List<RuntimeAnimatorController> controllers = new();

            var animator = avatarRoot.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
                controllers.Add(animator.runtimeAnimatorController);

#if AAO_VRCSDK3_AVATARS
            var descriptor = avatarRoot.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
            if (descriptor != null)
            {
                var layerControllers = VRCSDKUtils.GetAvatarLayerControllers(descriptor);
                foreach (var layer in AnimatorLayerMap.ValidLayerTypes)
                {
                    var c = layerControllers[layer];
                    if (c != null)
                        controllers.Add(c);
                }
            }
#endif

            return controllers.Sum(c => ACUtils.ComputeLayerCount(c));
        }
    }

#if AAO_VRCSDK3_AVATARS
    class VrcMetricsSource : IMetricsSource
    {
        public int Priority => 1000;

        private static Dictionary<AvatarPerformanceCategory, Func<AvatarPerformanceStats, string?>>? _computers;
        private static Dictionary<AvatarPerformanceCategory, Func<AvatarPerformanceStats, string?>> Computers => _computers ??= BuildComputers();

        public IReadOnlyDictionary<string, string> Capture(GameObject avatarRoot)
        {
            try
            {
                var stats = new AvatarPerformanceStats(false);
                AvatarPerformance.CalculatePerformanceStats(avatarRoot.name, avatarRoot, stats, false);
                var result = new Dictionary<string, string>();
                foreach (AvatarPerformanceCategory category in Enum.GetValues(typeof(AvatarPerformanceCategory)))
                {
                    if (!Computers.TryGetValue(category, out var computer)) continue;
                    try
                    {
                        var data = computer(stats);
                        if (data != null)
                        {
                            var name = category ==  AvatarPerformanceCategory.TextureMegabytes ? $"Hidden{category}" : category.ToString();
                            result[name] = data;
                        }
                    }
                    catch { }
                }
                return result;
            }
            catch { return new Dictionary<string, string>(); }
        }

        private static Dictionary<AvatarPerformanceCategory, Func<AvatarPerformanceStats, string?>> BuildComputers()
        {
            var dict = new Dictionary<AvatarPerformanceCategory, Func<AvatarPerformanceStats, string?>>();

            string? ToStringBytes(int? bytes) => bytes is { } i ? $"{i / 1024.0 / 1024.0:F2}MB" : null;
            string? ToStringMegabytes(float? mb) => mb is { } f ? $"{f:F2}MB" : null;
            string? ToStringCount(int? v) => v is { } ? v.ToString() : null;
            string? ToStringCountZero(int? v) => v is { } ? v.ToString() : "0";
            string? ToStringEnabled(bool? v) => v is { } b ? (b ? "Enabled" : "Disabled") : null;

            void Add(AvatarPerformanceCategory category, params Func<AvatarPerformanceStats, string?>[] computers)
            {
                dict[category] = s =>
                {
                    foreach (var c in computers)
                    {
                        try
                        {
                            var r = c(s);
                            if (r != null) return r;
                        }
                        catch
                        {
                            // ignored
                        }
                    }

                    return null;
                };
            }

            Add(AvatarPerformanceCategory.DownloadSize, s => ToStringBytes(s.downloadSizeBytes));
            Add(AvatarPerformanceCategory.UncompressedSize, s => ToStringBytes(s.uncompressedSizeBytes));
            Add(AvatarPerformanceCategory.PolyCount, s => ToStringCount(s.polyCount));
            Add(AvatarPerformanceCategory.AABB, s => s.aabb is { } b ? b.ToString() : null);
            Add(AvatarPerformanceCategory.SkinnedMeshCount, s => ToStringCount(s.skinnedMeshCount));
            Add(AvatarPerformanceCategory.MeshCount, s => ToStringCount(s.meshCount));
            Add(AvatarPerformanceCategory.MaterialCount, s => ToStringCount(s.materialCount));
            Add(AvatarPerformanceCategory.PhysBoneComponentCount, s => ToStringCountZero(s.physBone?.componentCount));
            Add(AvatarPerformanceCategory.PhysBoneTransformCount, s => ToStringCountZero(s.physBone?.transformCount));
            Add(AvatarPerformanceCategory.PhysBoneColliderCount, s => ToStringCountZero(s.physBone?.colliderCount));
            Add(AvatarPerformanceCategory.PhysBoneCollisionCheckCount, s => ToStringCountZero(s.physBone?.collisionCheckCount));
            Add(AvatarPerformanceCategory.ContactCount, s => ToStringCount(s.contactCount));
            Add(AvatarPerformanceCategory.AnimatorCount, s => ToStringCount(s.animatorCount));
            Add(AvatarPerformanceCategory.BoneCount, s => ToStringCount(s.boneCount));
            Add(AvatarPerformanceCategory.LightCount, s => ToStringCount(s.lightCount));
            Add(AvatarPerformanceCategory.ParticleSystemCount, s => ToStringCount(s.particleSystemCount));
            Add(AvatarPerformanceCategory.ParticleTotalCount, s => ToStringCount(s.particleTotalCount));
            Add(AvatarPerformanceCategory.ParticleMaxMeshPolyCount, s => ToStringCount(s.particleMaxMeshPolyCount));
            Add(AvatarPerformanceCategory.ParticleTrailsEnabled, s => ToStringEnabled(s.particleTrailsEnabled));
            Add(AvatarPerformanceCategory.ParticleCollisionEnabled, s => ToStringEnabled(s.particleCollisionEnabled));
            Add(AvatarPerformanceCategory.TrailRendererCount, s => ToStringCount(s.trailRendererCount));
            Add(AvatarPerformanceCategory.LineRendererCount, s => ToStringCount(s.lineRendererCount));
            Add(AvatarPerformanceCategory.ClothCount, s => ToStringCount(s.clothCount));
            Add(AvatarPerformanceCategory.ClothMaxVertices, s => ToStringCount(s.clothMaxVertices));
            Add(AvatarPerformanceCategory.PhysicsColliderCount, s => ToStringCount(s.physicsColliderCount));
            Add(AvatarPerformanceCategory.PhysicsRigidbodyCount, s => ToStringCount(s.physicsRigidbodyCount));
            Add(AvatarPerformanceCategory.AudioSourceCount, s => ToStringCount(s.audioSourceCount));
            Add(AvatarPerformanceCategory.TextureMegabytes, s => ToStringMegabytes(s.textureMegabytes));
            Add(AvatarPerformanceCategory.ConstraintsCount, s => ToStringCount(s.constraintsCount));
            Add(AvatarPerformanceCategory.ConstraintDepth, s => ToStringCount(s.constraintDepth));
#if AAO_VRCSDK3_AVATARS_VRC_RAYCAST
            Add(AvatarPerformanceCategory.RaycastCount, s => ToStringCount(s.raycastCount));
#endif
            return dict;
        }
    }
#endif
}
