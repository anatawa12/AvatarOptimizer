using Anatawa12.AvatarOptimizer.API;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.APIInternal;

// Unity builtin "Standard" shader
// https://github.com/TwoTailsGames/Unity-Built-in-Shaders/blob/master/DefaultResourcesExtra/Standard.shader
[InitializeOnLoad]
class StandardShaderInformation : ShaderInformation
{
    static StandardShaderInformation()
    {
        Register();
    }

    private static void Register()
    {
        var information = new StandardShaderInformation();
        if (!GlobalObjectId.TryParse("GlobalObjectId_V1-4-0000000000000000f000000000000000-46-0", out var id)) return;
        var shader = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as Shader;
        if (shader == null) return;
        ShaderInformationRegistry.RegisterShaderInformation(shader, information);
    }

    public override ShaderInformationKind SupportedInformationKind =>
        ShaderInformationKind.VertexIndexUsage | ShaderInformationKind.TextureAndUVUsage;
    public override void GetMaterialInformation(MaterialInformationCallback matInfo)
    {
        var mainTexST = matInfo.GetVector("_MainTex_ST");
        Matrix2x3? mainTexSTMat = mainTexST is { } st ? Matrix2x3.NewScaleOffset(st) : null;
        var mainTexSTMatParallex = mainTexSTMat;
        if (matInfo.IsShaderKeywordEnabled("_PARALLAXMAP") != false)
            mainTexSTMat = null;

        matInfo.RegisterTextureUVUsage("_ParallaxMap", "_ParallaxMap", UsingUVChannels.UV0, mainTexSTMatParallex);

        matInfo.RegisterTextureUVUsage("_MainTex", "_MainTex", UsingUVChannels.UV0, mainTexSTMat);
        matInfo.RegisterTextureUVUsage("_MetallicGlossMap", "_MetallicGlossMap", UsingUVChannels.UV0, mainTexSTMat);
        matInfo.RegisterTextureUVUsage("_BumpMap", "_BumpMap", UsingUVChannels.UV0, mainTexSTMat);
        matInfo.RegisterTextureUVUsage("_OcclusionMap", "_OcclusionMap", UsingUVChannels.UV0, mainTexSTMat);
        matInfo.RegisterTextureUVUsage("_EmissionMap", "_EmissionMap", UsingUVChannels.UV0, mainTexSTMat);
        matInfo.RegisterTextureUVUsage("_DetailMask", "_DetailMask", UsingUVChannels.UV0, mainTexSTMat);

        var detailMapST = matInfo.GetVector("_DetailAlbedoMap_ST");
        Matrix2x3? detailMapSTMat = detailMapST is { } st2 ? Matrix2x3.NewScaleOffset(st2) : null;

        if (matInfo.IsShaderKeywordEnabled("_PARALLAXMAP") != false)
            detailMapSTMat = null;

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

// Unity builtin "Mobile/Vertex Lit" shader
// https://github.com/TwoTailsGames/Unity-Built-in-Shaders/blob/master/DefaultResourcesExtra/Mobile/Mobile-VertexLit.shader
[InitializeOnLoad]
class MobileVertexLitShaderInformation : ShaderInformation
{
    static MobileVertexLitShaderInformation()
    {
        Register();
    }

    private static void Register()
    {
        var information = new MobileVertexLitShaderInformation();
        if (!GlobalObjectId.TryParse("GlobalObjectId_V1-4-0000000000000000f000000000000000-10701-0", out var id)) return;
        var shader = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as Shader;
        if (shader == null) return;
        ShaderInformationRegistry.RegisterShaderInformation(shader, information);
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
