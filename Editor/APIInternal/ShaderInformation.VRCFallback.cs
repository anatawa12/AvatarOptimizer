using Anatawa12.AvatarOptimizer.API;

namespace Anatawa12.AvatarOptimizer.APIInternal;

// https://github.com/lilxyzw/lilAvatarUtils/tree/db12d5468b8fc03d5e9bc12a64c9d07686e5105a/Shaders

// Hidden
internal class VRCFallbackHiddenShaderInformation : ShaderInformation
{
    public override ShaderInformationKind SupportedInformationKind =>
        ShaderInformationKind.VertexIndexUsage | ShaderInformationKind.TextureAndUVUsage;

    // no usage
    public override void GetMaterialInformation(MaterialInformationCallback matInfo)
    {
    }
}

// Toon
internal class VRCFallbackToonShaderInformation : ShaderInformation
{
    public override ShaderInformationKind SupportedInformationKind =>
        ShaderInformationKind.VertexIndexUsage | ShaderInformationKind.TextureAndUVUsage;

    public override void GetMaterialInformation(MaterialInformationCallback matInfo)
    {
        var mainTexST = matInfo.GetVector("_MainTex_ST");
        Matrix2x3? mainTexSTMat = mainTexST is { } st ? Matrix2x3.NewScaleOffset(st) : null;
        matInfo.RegisterTextureUVUsage("_MainTex", "_MainTex", UsingUVChannels.UV0, mainTexSTMat);
    }
}

// ToonCutout
internal class VRCFallbackToonCutoutShaderInformation : ShaderInformation
{
    public override ShaderInformationKind SupportedInformationKind =>
        ShaderInformationKind.VertexIndexUsage | ShaderInformationKind.TextureAndUVUsage;

    public override void GetMaterialInformation(MaterialInformationCallback matInfo)
    {
        var mainTexST = matInfo.GetVector("_MainTex_ST");
        Matrix2x3? mainTexSTMat = mainTexST is { } st ? Matrix2x3.NewScaleOffset(st) : null;
        matInfo.RegisterTextureUVUsage("_MainTex", "_MainTex", UsingUVChannels.UV0, mainTexSTMat);
    }
}

// Unlit
internal class VRCFallbackUnlitShaderInformation : ShaderInformation
{
    public override ShaderInformationKind SupportedInformationKind =>
        ShaderInformationKind.VertexIndexUsage | ShaderInformationKind.TextureAndUVUsage;

    public override void GetMaterialInformation(MaterialInformationCallback matInfo)
    {
        var mainTexST = matInfo.GetVector("_MainTex_ST");
        Matrix2x3? mainTexSTMat = mainTexST is { } st ? Matrix2x3.NewScaleOffset(st) : null;
        matInfo.RegisterTextureUVUsage("_MainTex", "_MainTex", UsingUVChannels.UV0, mainTexSTMat);
    }
}

// UnlitCutout
internal class VRCFallbackUnlitCutoutShaderInformation : ShaderInformation
{
    public override ShaderInformationKind SupportedInformationKind =>
        ShaderInformationKind.VertexIndexUsage | ShaderInformationKind.TextureAndUVUsage;

    public override void GetMaterialInformation(MaterialInformationCallback matInfo)
    {
        var mainTexST = matInfo.GetVector("_MainTex_ST");
        Matrix2x3? mainTexSTMat = mainTexST is { } st ? Matrix2x3.NewScaleOffset(st) : null;
        matInfo.RegisterTextureUVUsage("_MainTex", "_MainTex", UsingUVChannels.UV0, mainTexSTMat);
    }
}

// UnlitTransparent
internal class VRCFallbackUnlitTransparentShaderInformation : ShaderInformation
{
    public override ShaderInformationKind SupportedInformationKind =>
        ShaderInformationKind.VertexIndexUsage | ShaderInformationKind.TextureAndUVUsage;

    public override void GetMaterialInformation(MaterialInformationCallback matInfo)
    {
        var mainTexST = matInfo.GetVector("_MainTex_ST");
        Matrix2x3? mainTexSTMat = mainTexST is { } st ? Matrix2x3.NewScaleOffset(st) : null;
        matInfo.RegisterTextureUVUsage("_MainTex", "_MainTex", UsingUVChannels.UV0, mainTexSTMat);
    }
}

// VertexLit
// ref https://github.com/TwoTailsGames/Unity-Built-in-Shaders/blob/master/DefaultResourcesExtra/Mobile/Mobile-VertexLit.shader
internal class VRCFallbackVertexLitShaderInformation : ShaderInformation
{
    public override ShaderInformationKind SupportedInformationKind =>
        ShaderInformationKind.VertexIndexUsage | ShaderInformationKind.TextureAndUVUsage;

    public override void GetMaterialInformation(MaterialInformationCallback matInfo)
    {
        var mainTexST = matInfo.GetVector("_MainTex_ST");
        Matrix2x3? mainTexSTMat = mainTexST is { } st ? Matrix2x3.NewScaleOffset(st) : null;
        matInfo.RegisterTextureUVUsage("_MainTex", "_MainTex", UsingUVChannels.UV0, mainTexSTMat);
    }
}

// Particle
// ref VRChat/Mobile/Particles/Multiply
internal class VRCFallbackParticleShaderInformation : ShaderInformation
{
    public override ShaderInformationKind SupportedInformationKind =>
        ShaderInformationKind.VertexIndexUsage | ShaderInformationKind.TextureAndUVUsage;

    public override void GetMaterialInformation(MaterialInformationCallback matInfo)
    {
        var mainTexST = matInfo.GetVector("_MainTex_ST");
        Matrix2x3? mainTexSTMat = mainTexST is { } st ? Matrix2x3.NewScaleOffset(st) : null;
        matInfo.RegisterTextureUVUsage("_MainTex", "_MainTex", UsingUVChannels.UV0, mainTexSTMat);
    }
}

// Sprite
// ref VRChat/Sprites/Default
internal class VRCFallbackSpriteShaderInformation : ShaderInformation
{
    public override ShaderInformationKind SupportedInformationKind =>
        ShaderInformationKind.VertexIndexUsage | ShaderInformationKind.TextureAndUVUsage;

    public override void GetMaterialInformation(MaterialInformationCallback matInfo)
    {
        // _MainTex and _AlphaTex
        // Do not apply _MainTex_ST to either
        matInfo.RegisterTextureUVUsage("_MainTex", "_MainTex", UsingUVChannels.UV0, null);
        matInfo.RegisterTextureUVUsage("_AlphaTex", "_AlphaTex", UsingUVChannels.UV0, null);
    }
}

// Matcap
// ref VRChat/Mobile/MatCap Lit
internal class VRCFallbackMatcapShaderInformation : ShaderInformation
{
    public override ShaderInformationKind SupportedInformationKind =>
        ShaderInformationKind.VertexIndexUsage | ShaderInformationKind.TextureAndUVUsage;

    public override void GetMaterialInformation(MaterialInformationCallback matInfo)
    {
        // _MainTex and _MatCap(NonMesh)
        var mainTexST = matInfo.GetVector("_MainTex_ST");
        Matrix2x3? mainTexSTMat = mainTexST is { } st ? Matrix2x3.NewScaleOffset(st) : null;
        matInfo.RegisterTextureUVUsage("_MainTex", "_MainTex", UsingUVChannels.UV0, mainTexSTMat);
        matInfo.RegisterTextureUVUsage("_MatCap", "_MatCap", UsingUVChannels.NonMesh, null);
    }
}