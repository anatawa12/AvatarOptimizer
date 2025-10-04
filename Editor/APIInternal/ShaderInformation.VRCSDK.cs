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

[InitializeOnLoad]
class VRCSDKToonStandardShaderInformation : ShaderInformation
{
    private readonly bool withOutline;

    public VRCSDKToonStandardShaderInformation(bool withOutline)
    {
        this.withOutline = withOutline;
    }

    static VRCSDKToonStandardShaderInformation()
    {
        Register();
    }

    private static void Register()
    {
        ShaderInformationRegistry.RegisterShaderInformationWithGUID("e765db0afa7ecfc44ade2e4e2491f65a", new VRCSDKToonStandardShaderInformation(false));
        ShaderInformationRegistry.RegisterShaderInformationWithGUID("051a0ed2f2aedd741aa8186ae92f97e0", new VRCSDKToonStandardShaderInformation(true));
    }

    public override ShaderInformationKind SupportedInformationKind =>
        ShaderInformationKind.VertexIndexUsage | ShaderInformationKind.TextureAndUVUsage;
    public override void GetMaterialInformation(MaterialInformationCallback matInfo)
    {
        // TODO: way may have to add atlasing support later

        void Register(string name, UsingUVChannels uv)
        {
            var st = matInfo.GetVector(name + "_ST");
            Matrix2x3? stMat = st is { } st2 ? Matrix2x3.NewScaleOffset(st2) : null;
            matInfo.RegisterTextureUVUsage(name, name, uv, stMat);
        }

        Register("_MainTex", UsingUVChannels.UV0);
        Register("_Ramp", UsingUVChannels.NonMesh);

        if (matInfo.IsShaderKeywordEnabled("USE_NORMAL_MAPS") != false)
        {
            Register("_BumpMap", UsingUVChannels.UV0);
        }

        if (matInfo.IsShaderKeywordEnabled("USE_SPECULAR") != false)
        {
            Register("_MetallicMap", UsingUVChannels.UV0);
            Register("_GlossMap", UsingUVChannels.UV0);
        }

        if (matInfo.IsShaderKeywordEnabled("USE_MATCAP") != false)
        {
            Register("_Matcap", UsingUVChannels.NonMesh);
            Register("_MatcapMask", UsingUVChannels.UV0);
        }

        //if (matInfo.IsShaderKeywordEnabled("USE_EMISSION_MAP") != false) // Emission Map is always enabled since it's cheap
        {
            var uv = matInfo.GetFloat("_EmissionUV") switch
            {
                0 => UsingUVChannels.UV0,
                1 => UsingUVChannels.UV1,
                _ => UsingUVChannels.UV0 | UsingUVChannels.UV1,
            };
            Register("_EmissionMap", uv);
        }

        if (matInfo.IsShaderKeywordEnabled("USE_OCCLUSION_MAP") != false)
        {
            Register("_OcclusionMap", UsingUVChannels.UV0);
        }

        if (matInfo.IsShaderKeywordEnabled("USE_DETAIL_MAPS") != false)
        {
            var detailMapUV = matInfo.GetFloat("_DetailUV") switch
            {
                0 => UsingUVChannels.UV0,
                1 => UsingUVChannels.UV1,
                _ => UsingUVChannels.UV0 | UsingUVChannels.UV1,
            };

            Register("_DetailMask", UsingUVChannels.UV0); // always UV0 for mask
            Register("_DetailAlbedoMap", detailMapUV);
            if (matInfo.IsShaderKeywordEnabled("USE_NORMAL_MAPS") != false)
            {
                Register("_DetailNormalMap", detailMapUV);
            }
        }

        if (matInfo.IsShaderKeywordEnabled("USE_HUE_SHIFT") != false)
        {
            Register("_HueShiftMask", UsingUVChannels.UV0);
        }

        if (withOutline)
        {
            // note: this does not require the mipmap
            Register("_OutlineMask", UsingUVChannels.UV0);
        }
    }
}
