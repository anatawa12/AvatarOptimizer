using Anatawa12.AvatarOptimizer.API;

namespace Anatawa12.AvatarOptimizer.APIInternal;

internal class EmptyShaderInformation : ShaderInformation
{
    public override ShaderInformationKind SupportedInformationKind =>
        ShaderInformationKind.VertexIndexUsage | ShaderInformationKind.TextureAndUVUsage;

    public override void GetMaterialInformation(MaterialInformationCallback matInfo)
    {
    }
}

internal class MainTexOnlyShaderInformation : ShaderInformation
{
    private readonly string _mainTexName;

    public MainTexOnlyShaderInformation(string mainTexName = "_MainTex")
    {
        _mainTexName = mainTexName;
    }

    public override ShaderInformationKind SupportedInformationKind =>
        ShaderInformationKind.VertexIndexUsage | ShaderInformationKind.TextureAndUVUsage;

    public override void GetMaterialInformation(MaterialInformationCallback matInfo)
    {
        var mainTexST = matInfo.GetVector($"{_mainTexName}_ST");
        Matrix2x3? mainTexSTMat = mainTexST is { } st ? Matrix2x3.NewScaleOffset(st) : null;
        matInfo.RegisterTextureUVUsage(_mainTexName, _mainTexName, UsingUVChannels.UV0, mainTexSTMat);
    }
}