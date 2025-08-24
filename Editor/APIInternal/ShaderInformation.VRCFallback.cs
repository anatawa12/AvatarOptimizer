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
