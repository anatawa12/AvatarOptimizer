using Anatawa12.AvatarOptimizer.API;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.APIInternal;

// VRChat SDK Mobile Shaders

[InitializeOnLoad]
class VRCSDKStandardLiteShaderInformation : ShaderInformation
{
    static VRCSDKStandardLiteShaderInformation()
    {
        Register();
    }

    private static void Register()
    {
        var information = new VRCSDKStandardLiteShaderInformation();
        ShaderInformationRegistry.RegisterShaderInformationWithGUID("0b7113dea2069fc4e8943843eff19f70", information);
    }

    public override ShaderInformationKind SupportedInformationKind =>
        ShaderInformationKind.VertexIndexUsage | ShaderInformationKind.TextureAndUVUsage;
    public override void GetMaterialInformation(MaterialInformationCallback matInfo)
    {
        var mainTexST = matInfo.GetVector("_MainTex_ST");
        Matrix2x3? mainTexSTMat = mainTexST is { } st ? Matrix2x3.NewScaleOffset(st) : null;

        matInfo.RegisterTextureUVUsage("_MetallicGlossMap", "_MetallicGlossMap", UsingUVChannels.UV0, mainTexSTMat);
        matInfo.RegisterTextureUVUsage("_MainTex", "_MainTex", UsingUVChannels.UV0, mainTexSTMat);
        matInfo.RegisterTextureUVUsage("_BumpMap", "_BumpMap", UsingUVChannels.UV0, mainTexSTMat);
        matInfo.RegisterTextureUVUsage("_OcclusionMap", "_OcclusionMap", UsingUVChannels.UV0, mainTexSTMat);
        matInfo.RegisterTextureUVUsage("_EmissionMap", "_EmissionMap", UsingUVChannels.UV0, mainTexSTMat);
        matInfo.RegisterTextureUVUsage("_DetailMask", "_DetailMask", UsingUVChannels.UV0, mainTexSTMat);

        var detailMapST = matInfo.GetVector("_DetailAlbedoMap_ST");
        Matrix2x3? detailMapSTMat = detailMapST is { } st2 ? Matrix2x3.NewScaleOffset(st2) : null;
        matInfo.RegisterTextureUVUsage("_DetailAlbedoMap", "_DetailAlbedoMap", UsingUVChannels.UV0, mainTexSTMat);

        var detailMapUV = matInfo.GetFloat("_UVSec") switch
        {
            null => UsingUVChannels.UV0 | UsingUVChannels.UV1,
            0 => UsingUVChannels.UV0,
            _ => UsingUVChannels.UV1,
        };

        matInfo.RegisterTextureUVUsage("_DetailAlbedoMap", "_DetailAlbedoMap", detailMapUV, detailMapSTMat);
        matInfo.RegisterTextureUVUsage("_DetailNormalMap", "_DetailNormalMap", detailMapUV, detailMapSTMat);
    }
}

[InitializeOnLoad]
class VRCSDKToonLitShaderInformation : ShaderInformation
{
    static VRCSDKToonLitShaderInformation()
    {
        Register();
    }

    private static void Register()
    {
        var information = new VRCSDKToonLitShaderInformation();
        ShaderInformationRegistry.RegisterShaderInformationWithGUID("affc81f3d164d734d8f13053effb1c5c", information);
    }

    public override ShaderInformationKind SupportedInformationKind =>
        ShaderInformationKind.VertexIndexUsage | ShaderInformationKind.TextureAndUVUsage;
    public override void GetMaterialInformation(MaterialInformationCallback matInfo)
    {
        var mainTexST = matInfo.GetVector("_MainTex_ST");
        Matrix2x3? mainTexSTMat = mainTexST is { } st ? Matrix2x3.NewScaleOffset(st) : null;
        matInfo.RegisterTextureUVUsage("_MainTex", "_MainTex", UsingUVChannels.UV0, mainTexSTMat);
    }
}
