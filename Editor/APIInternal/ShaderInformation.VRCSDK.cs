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

        if (matInfo.IsShaderKeywordEnabled("USE_EMISSION_MAP") != false)
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

// VRChat/Sprites/Default
[InitializeOnLoad]
class VRCSDKSpriteDefaultShaderInformation : ShaderInformation
{
    static VRCSDKSpriteDefaultShaderInformation()
    {
        Register();
    }

    private static void Register()
    {
        var information = new VRCSDKSpriteDefaultShaderInformation();
        ShaderInformationRegistry.RegisterShaderInformationWithGUID("5f8fef09682fab74fb7a29d783391edb", information);
    }

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


// VRChat/Sprites/Diffuse
[InitializeOnLoad]
class VRCSDKSpriteDiffuseShaderInformation : ShaderInformation
{
    static VRCSDKSpriteDiffuseShaderInformation()
    {
        Register();
    }

    private static void Register()
    {
        var information = new VRCSDKSpriteDiffuseShaderInformation();
        ShaderInformationRegistry.RegisterShaderInformationWithGUID("9ae8ad653e1d98940bbc79866b9170f3", information);
    }

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

// VRChat/Mobile/MatCap Lit
[InitializeOnLoad]
class VRCSDKMatcapLitShaderInformation : ShaderInformation
{
    static VRCSDKMatcapLitShaderInformation()
    {
        Register();
    }

    private static void Register()
    {
        var information = new VRCSDKMatcapLitShaderInformation();
        ShaderInformationRegistry.RegisterShaderInformationWithGUID("3ad043b7f9839cb48a75a9238d433dec", information);
    }

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

// VRChat/Mobile/Particles/Additive
[InitializeOnLoad]
class VRCSDKParticleAdditiveShaderInformation : ShaderInformation
{
    static VRCSDKParticleAdditiveShaderInformation()
    {
        Register();
    }

    private static void Register()
    {
        var information = new VRCSDKParticleAdditiveShaderInformation();
        ShaderInformationRegistry.RegisterShaderInformationWithGUID("9200bec112b65ec4fbbbd33fa89c20f4", information);
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

// VRChat/Mobile/Particles/Alpha Blended
[InitializeOnLoad]
class VRCSDKParticleAlphaBlendedShaderInformation : ShaderInformation
{
    static VRCSDKParticleAlphaBlendedShaderInformation()
    {
        Register();
    }

    private static void Register()
    {
        var information = new VRCSDKParticleAlphaBlendedShaderInformation();
        ShaderInformationRegistry.RegisterShaderInformationWithGUID("8b39b95ac85682040beff730e0cfc77a", information);
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

// VRChat/Mobile/Particles/Multiply
[InitializeOnLoad]
class VRCSDKParticleMultiplyShaderInformation : ShaderInformation
{
    static VRCSDKParticleMultiplyShaderInformation()
    {
        Register();
    }

    private static void Register()
    {
        var information = new VRCSDKParticleMultiplyShaderInformation();
        ShaderInformationRegistry.RegisterShaderInformationWithGUID("d5b89f0c74ccf5049ba803c14a090378", information);
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
