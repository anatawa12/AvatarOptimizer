using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.API;
using Anatawa12.AvatarOptimizer.APIInternal;
using Anatawa12.AvatarOptimizer.ndmf;
using nadena.dev.ndmf;
using UnityEngine;
using UnityEngine.Rendering;

namespace Anatawa12.AvatarOptimizer.Processors;

internal class GatherShaderMaterialInformation : Pass<GatherShaderMaterialInformation>
{
    protected override void Execute(BuildContext context)
    {
        if (!context.GetState<AAOEnabled>().Enabled) return;

        var renderersByMaterial = new Dictionary<Material, List<Renderer>>();

        foreach (var renderer in context.GetComponents<Renderer>())
        {
            foreach (var material in context.GetAllPossibleMaterialFor(renderer))
            {
                if (material == null) continue;

                if (!renderersByMaterial.TryGetValue(material, out var list))
                    renderersByMaterial.Add(material, list = new List<Renderer>());

                list.Add(renderer);
            }
        }

        var informationByMaterial = context.GetState<MaterialInformationState>().MaterialInformationByMaterial;

        foreach (var (material, renderers) in renderersByMaterial)
        {
            informationByMaterial.Add(material, new MaterialInformation(material, renderers, context));
        }
    }
}

internal static class MaterialInformationStatics
{
    public static MaterialInformation? GetMaterialInformation(this BuildContext context, Material material)
    {
        if (context.GetState<MaterialInformationState>().MaterialInformationByMaterial.TryGetValue(material, out var information))
            return information;
        return null;
    }
}

internal class MaterialInformationState
{
    public readonly Dictionary<Material, MaterialInformation> MaterialInformationByMaterial = new();
}

internal class MaterialInformation
{
    public readonly Material Material;
    public readonly List<Renderer> UserRenderers;

    public readonly ShaderInformationResult? DefaultResult;
    public readonly ShaderInformationResult? FallbackResult;

    public MaterialInformation(Material material, List<Renderer> renderers, BuildContext? context)
    {
        Material = material;
        UserRenderers = renderers;

        // collect texture usage information

        if (ShaderInformationRegistry.GetShaderInformation(material.shader) is { } information)
        {
            DefaultResult = new ShaderInformationResult(information, material, renderers, context);
        }
		
		if (context != null && IsShaderFallbackSupported(context) && GetFallbackShaderInformation(material, context) is { } fallbackInformation)
		{
            FallbackResult = new ShaderInformationResult(fallbackInformation, material, renderers, context);
		}
	}

    public class ShaderInformationResult
    {
        public List<TextureUsageInformation>? TextureUsageInformationList { get; init; }
        public UsingUVChannels OtherUVUsage { get; init; }
        public bool UseVertexIndex { get; init; }

        public ShaderInformationResult(ShaderInformation information, Material material, List<Renderer> renderers, BuildContext? context)
        {
            var supportedKind = information.SupportedInformationKind;
            var provider = new MaterialInformationCallbackImpl(
                material,
                supportedKind,
                context == null ? null : renderers.Select(renderer => context.GetAnimationComponent(renderer)).ToList());
            information.GetMaterialInformation(provider);

            TextureUsageInformationList = provider.TextureUsageInformations;
            OtherUVUsage = provider.OtherUVUsage;
            UseVertexIndex = provider.UseVertexIndex;
        }
    }

    private bool IsShaderFallbackSupported(BuildContext context)
    {
        return context.PlatformProvider.QualifiedName == WellKnownPlatforms.VRChatAvatar30;
    }

	private ShaderInformation? GetFallbackShaderInformation(Material material, BuildContext context)
	{
		return context.PlatformProvider.QualifiedName switch
		{
			WellKnownPlatforms.VRChatAvatar30 => VRCFallbackShaderInformations.GetInformation(material),
			_ => throw new NotSupportedException($"Shader Fallback for {context.PlatformProvider.QualifiedName} is not supported."),
		};
	}

    class MaterialInformationCallbackImpl : MaterialInformationCallback
    {
        private readonly Material _material;
        private readonly List<AnimationComponentInfo<PropertyInfo>> _infos;
        private readonly List<TextureUsageInformation>? _textureUsageInformations;
        private readonly ShaderInformationKind _supportedKind;

        public bool UseVertexIndex { get; private set; }
        public List<TextureUsageInformation>? TextureUsageInformations => _textureUsageInformations;
        public UsingUVChannels OtherUVUsage { get; private set; }

        public MaterialInformationCallbackImpl(Material material, ShaderInformationKind supportedKind,
            List<AnimationComponentInfo<PropertyInfo>>? infos)
        {
            _material = material;
            _supportedKind = supportedKind;
            _infos = infos ?? new List<AnimationComponentInfo<PropertyInfo>>();

            if ((_supportedKind & ShaderInformationKind.TextureAndUVUsage) != 0)
            {
                _textureUsageInformations = new List<TextureUsageInformation>();
            }
        }

        public Shader Shader => _material.shader;

        private T? GetValue<T>(string propertyName, Func<string, T> computer, bool considerAnimation,
            string[]? subProperties = null) where T : struct
        {
            // animated; return null
            if (considerAnimation)
            {
                var animationProperty = $"material.{propertyName}";
                if (_infos.Any(x => x.GetFloatNode(animationProperty).ComponentNodes.Any()))
                    return null;
                foreach (var subProperty in subProperties ?? Array.Empty<string>())
                {
                    var subAnimationProperty = $"material.{propertyName}.{subProperty}";
                    if (_infos.Any(x => x.GetFloatNode(subAnimationProperty).ComponentNodes.Any()))
                        return null;
                }
            }

            return computer(propertyName);
        }

        public override int? GetInteger(string propertyName, bool considerAnimation = true) =>
            GetValue(propertyName, _material.SafeGetInteger, considerAnimation);

        public override int? GetInt(string propertyName, bool considerAnimation = true) =>
            GetValue(propertyName, _material.SafeGetInt, considerAnimation);

        private static readonly string[] VectorSubProperties = new[] { "r", "g", "b", "a", "x", "y", "z", "w" };

        public override float? GetFloat(string propertyName, bool considerAnimation = true) =>
            GetValue(propertyName, _material.SafeGetFloat, considerAnimation, VectorSubProperties);

        public override Vector4? GetVector(string propertyName, bool considerAnimation = true) =>
            GetValue(propertyName, _material.SafeGetVector, considerAnimation, VectorSubProperties);

        public override bool? IsShaderKeywordEnabled(string keywordName) => _material.IsKeywordEnabled(keywordName);

        public override void RegisterOtherUVUsage(UsingUVChannels uvChannel)
        {
            if ((_supportedKind & ShaderInformationKind.TextureAndUVUsage) == 0)
                throw new InvalidOperationException("RegisterOtherUVUsage is not registered as supported information");

            OtherUVUsage |= uvChannel;
        }

        public override void RegisterTextureUVUsage(
            string textureMaterialPropertyName,
            SamplerStateInformation samplerState,
            UsingUVChannels uvChannels,
            Matrix2x3? uvMatrix)
        {
            if ((_supportedKind & ShaderInformationKind.TextureAndUVUsage) == 0)
                throw new InvalidOperationException("RegisterOtherUVUsage is not registered as supported information");
            if (uvChannels == 0) throw new ArgumentOutOfRangeException(nameof(uvChannels), "No UV Source is specified");
            if (!_material.HasTexture(textureMaterialPropertyName)) return;

            const UsingUVChannels unsupportedUVChannels =
                ~(UsingUVChannels.NonMesh | UsingUVChannels.UV0 | UsingUVChannels.UV1 |
                  UsingUVChannels.UV2 | UsingUVChannels.UV3 | UsingUVChannels.UV4 |
                  UsingUVChannels.UV5 | UsingUVChannels.UV6 | UsingUVChannels.UV7);

            foreach (var (uvChannel, usage) in new[]
                     {
                         (UVChannel.NonMeshRelated, UsingUVChannels.NonMesh | unsupportedUVChannels),
                         (UVChannel.UV0, UsingUVChannels.UV0),
                         (UVChannel.UV1, UsingUVChannels.UV1),
                         (UVChannel.UV2, UsingUVChannels.UV2),
                         (UVChannel.UV3, UsingUVChannels.UV3),
                         (UVChannel.UV4, UsingUVChannels.UV4),
                         (UVChannel.UV5, UsingUVChannels.UV5),
                         (UVChannel.UV6, UsingUVChannels.UV6),
                         (UVChannel.UV7, UsingUVChannels.UV7),
                     })
            {
                if ((uvChannels & usage) == 0) continue;

                TextureWrapMode? wrapModeU, wrapModeV;

                if (samplerState.MaterialProperty)
                {
                    var texture = _material.GetTexture(textureMaterialPropertyName);
                    if (texture != null)
                    {
                        wrapModeU = texture.wrapModeU;
                        wrapModeV = texture.wrapModeV;
                    }
                    else
                    {
                        wrapModeU = null;
                        wrapModeV = null;
                    }
                }
                else
                {
                    wrapModeV = wrapModeU = samplerState.TextureName switch
                    {
                        "PointClamp" or "LinearClamp" or "TrilinearClamp" => TextureWrapMode.Clamp,
                        "PointRepeat" or "LinearRepeat" or "TrilinearRepeat" => TextureWrapMode.Repeat,
                        "PointMirror" or "LinearMirror" or "TrilinearMirror" => TextureWrapMode.Mirror,
                        "PointMirrorOnce" or "LinearMirrorOnce" or "TrilinearMirrorOnce" => TextureWrapMode.MirrorOnce,
                        "Unknown" or _ => null,
                    };
                }

                _textureUsageInformations?.Add(new TextureUsageInformation(textureMaterialPropertyName, uvChannel,
                    uvMatrix,
                    wrapModeU, wrapModeV));
            }
        }

        public override void RegisterVertexIndexUsage()
        {
            if ((_supportedKind & ShaderInformationKind.VertexIndexUsage) == 0)
                throw new InvalidOperationException("RegisterVertexIndexUsage is not registered as supported information");
            UseVertexIndex = true;
        }
    }

}

internal class TextureUsageInformation
{
    public string MaterialPropertyName { get; }
    public UVChannel UVChannel { get; }
    public Matrix2x3? UVMatrix { get; }
    public TextureWrapMode? WrapModeU { get; }
    public TextureWrapMode? WrapModeV { get; }

    internal TextureUsageInformation(string materialPropertyName, UVChannel uvChannel, Matrix2x3? uvMatrix, TextureWrapMode? wrapModeU, TextureWrapMode? wrapModeV)
    {
        MaterialPropertyName = materialPropertyName;
        UVChannel = uvChannel;
        UVMatrix = uvMatrix;
        WrapModeU = wrapModeU;
        WrapModeV = wrapModeV;
    }
}

enum UVChannel
{
    UV0 = 0,
    UV1 = 1,
    UV2 = 2,
    UV3 = 3,
    UV4 = 4,
    UV5 = 5,
    UV6 = 6,
    UV7 = 7,
    // For example, ScreenSpace (dither) or MatCap
    NonMeshRelated = 0x100 + 0,
}
