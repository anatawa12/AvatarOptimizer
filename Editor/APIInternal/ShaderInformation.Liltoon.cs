using System;
using System.Reflection;
using Anatawa12.AvatarOptimizer.API;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.APIInternal;

[InitializeOnLoad]
internal class LiltoonShaderInformation : ShaderInformation
{
    internal override bool IsInternalInformation => true;

    static LiltoonShaderInformation()
    {
        Register();
    }

    private static void Register()
    {
        // get current version value
        if (Utils.GetTypeFromName("lilToon.lilConstants") is not {} lilConstantsType) return;
        if (lilConstantsType.GetField("currentVersionValue", BindingFlags.Public | BindingFlags.Static) is not {} currentVersionValueField) return;
        if (currentVersionValueField.GetValue(null) is not int versionValue) return;

        // check if version is supported version
        if (versionValue > supportedLiltoon) return;

        var information = new LiltoonShaderInformation();
        foreach (var guid in guids)
        {
            ShaderInformationRegistry.RegisterShaderInformationWithGUID(guid, information);
        }
    }

    private static int supportedLiltoon = 43;

    private static string[] guids =
    {
        "544c75f56e9af8048b29a6ace5f52091", // lts_fur_cutout.shader
        "7ec9f85eb7ee04943adfe19c2ba5901f", // lts_furonly_cutout.shader
        "00795bf598b44dc4e9bd363348e77085", // lts_fakeshadow.shader
        "3eef4aee6ba0de047b0d40409ea2891c", // lts_tess.shader
        "61b4f98a5d78b4a4a9d89180fac793fc", // ltspass_opaque.shader
        "df12117ecd77c31469c224178886498e", // lts.shader
        "dce3f3e248acc7b4daeda00daf616b4d", // lts_ref.shader
        "14006db8206fb304aa86110d57626d40", // ltspass_tess_cutout.shader
        "bdf24b2e925ce8a4fb0e903889a52e0e", // ltspass_tess_transparent.shader
        "fba17785d6b2c594ab6c0303c834da65", // lts_oo.shader
        "62df797f407281640a224388953448cc", // ltsl_twotrans_o.shader
        "afa1a194f5a2fd243bda3a17bca1b36e", // lts_tess_trans.shader
        "b269573b9937b8340b3e9e191a3ba5a8", // lts_onetrans.shader
        "165365ab7100a044ca85fc8c33548a62", // lts_trans.shader
        "9294844b15dca184d914a632279b24e1", // ltsmulti.shader
        "59b5e58e88aae8a4ca42d1a7253e2fb2", // ltspass_lite_opaque.shader
        "583a88005abb81a4ebbce757b4851a0d", // ltsl_o.shader
        "82226adb1a0b8c4418f574cfdcf523da", // ltsl_twotrans.shader
        "7171688840c632447b22ec14e2bdef7e", // lts_onetrans_o.shader
        "c6d605ee23b18fc46903f38c67db701f", // lts_tess_o.shader
        "33aad051c4a3a844a8f9330addb86a97", // lts_furonly.shader
        "0e3ece1bd59542743bccadb21f68318e", // ltsl_trans.shader
        "381af8ba8e1740a41b9768ccfb0416c2", // ltsl.shader
        "d7af54cdd86902d41b8c240e06b93009", // ltsmulti_ref.shader
        "8cf5267d397b04846856f6d3d9561da0", // ltsl_cutout_o.shader
        "90f83c35b0769a748abba5d0880f36d5", // lts_tess_onetrans.shader
        "3c79b10c7e0b2784aaa4c2f8dd17d55e", // lts_trans_o.shader
        "85d6126cae43b6847aff4b13f4adb8ec", // lts_cutout.shader
        "3b3957e6c393b114bab6f835b4ed8f5d", // lts_cutout_oo.shader
        "d28e4b78ba8368e49a44f86c0291df58", // ltsl_overlay.shader
        "2683fad669f20ec49b8e9656954a33a8", // ltspass_transparent.shader
        "7e61dbad981ad4f43a03722155db1c6a", // lts_tess_twotrans_o.shader
        "67ed0252d63362a4ab23707a720508b7", // lts_tess_onetrans_o.shader
        "7e398ea50f9b70045b1774e05b46a39f", // lts_tess_twotrans.shader
        "7a7ac427f85673a45a3e4190fc10bc28", // ltspass_tess_opaque.shader
        "9b0c2630b12933248922527d4507cfa9", // lts_tess_trans_o.shader
        "33e950d038b8dfd4f824f3985c2abfb7", // lts_overlay_one.shader
        "efa77a80ca0344749b4f19fdd5891cbe", // lts_o.shader
        "8a6ef0489c3ffbf46812460af3d52bb0", // ltspass_lite_cutout.shader
        "f8d9dfac6dbfaaf4c9c3aaf4bd8c955f", // lts_furonly_two.shader
        "3fb94a39b2685ee4d9817dcaf6542d99", // lts_ref_blur.shader
        "69f861c14129e724096c0955f8079012", // ltsmulti_gem.shader
        "3b4aa19949601f046a20ca8bdaee929f", // lts_cutout_o.shader
        "0c762f24b85918a49812fc5690619178", // lts_trans_oo.shader
        "ad219df2a46e841488aee6a013e84e36", // ltspass_cutout.shader
        "55706696b2bdb5d4d8541b89e17085c8", // lts_fur.shader
        "94274b8ef5d3af842b9427384cba3a8f", // lts_overlay.shader
        "fd68f52288a6b0243bf6c217bf0930ea", // ltspass_proponly.shader
        "9cf054060007d784394b8b0bb703e441", // lts_twotrans_o.shader
        "6a77405f7dfdc1447af58854c7f43f39", // lts_twotrans.shader
        "bbfffd5515b843c41a85067191cbf687", // lts_tess_cutout.shader
        "dc9ded9f9d6f16c4e92cbb8f4269ae31", // ltsl_overlay_one.shader
        "2bde4bd29a2a70a4d9cf98772a6717ac", // ltspass_dummy.shader
        "34c2907eba944ed45a43970e0c11bcfd", // ltsl_onetrans.shader
        "a8d94439709469942bc7dcc9156ba110", // lts_gem.shader
        "701268c07d37f5441b25b2cb99fae4b3", // ltsl_onetrans_o.shader
        "51b2dee0ab07bd84d8147601ff89e511", // ltsmulti_o.shader
        "1c12a37046f07ac4486881deaf0187ea", // ltsl_trans_o.shader
        "b957dce3d03ff5445ac989f8de643c7f", // ltsl_cutout.shader
        "5ba517885727277409feada18effa4a6", // lts_tess_cutout_o.shader
        "8773c83ab40fff24b800f74360819a6c", // ltspass_lite_transparent.shader
        "54bc8b41278802d4a81b27fe402994e2", // lts_fur_two.shader
        "1e50f1bc4d1b0e34cbf16b82589f6407", // ltsmulti_fur.shader
        "f96a89829ccb1e54b85214550519a8d6", // ltspass_baker.shader
    };

    public override bool GetTextureUsageInformationForMaterial(TextureUsageInformationCallback matInfo)
    {
        // TODO: version check

        var uvMain = UsingUVChannels.UV0;
        var uvMainScaleOffset = "_MainTex_ST";
        UnityEngine.Matrix4x4? uvMainMatrix = ComputeUVMainMatrix();

        UnityEngine.Matrix4x4? ComputeUVMainMatrix()
        {
            // _ShiftBackfaceUV
            if (matInfo.GetFloat("_ShiftBackfaceUV") != 0) return null; // changed depends on face
            return STAndScrollRotateToMatrix("_MainTex_ST", "_MainTex_ScrollRotate");
        }

        matInfo.RegisterTextureUVUsage("_DitherTex", SamplerStateInformation.LinearRepeatSampler,
            UsingUVChannels.NonMesh, null); // dither UV is based on screen space

        // TODO: _MainTex with POM / PARALLAX (using LIL_SAMPLE_2D_POM)
        LIL_SAMPLE_2D_WithMat("_MainTex", "_MainTex", uvMain, uvMainMatrix); // main texture
        matInfo.RegisterTextureUVUsage("_MainGradationTex", SamplerStateInformation.LinearClampSampler,
            UsingUVChannels.NonMesh, null); // GradationMap UV is based on color
        LIL_SAMPLE_2D_WithMat("_MainColorAdjustMask", "_MainTex", uvMain, uvMainMatrix); // simple LIL_SAMPLE_2D

        if (matInfo.GetInteger("_UseMain2ndTex") != 0)
        {
            // caller of lilGetMain2nd will pass sampler for _MainTex as samp
            SamplerStateInformation samp = "_MainTex";

            UsingUVChannels uv2nd;
            switch (matInfo.GetInteger("_Main2ndTex_UVMode"))
            {
                case 0:
                    uv2nd = UsingUVChannels.UV0;
                    break;
                case 1:
                    uv2nd = UsingUVChannels.UV1;
                    break;
                case 2:
                    uv2nd = UsingUVChannels.UV2;
                    break;
                case 3:
                    uv2nd = UsingUVChannels.UV3;
                    break;
                case 4:
                    uv2nd = UsingUVChannels.NonMesh;
                    break; // MatCap (normal-based UV)
                default:
                    uv2nd = UsingUVChannels.UV0 | UsingUVChannels.UV1 | UsingUVChannels.UV2 | UsingUVChannels.UV3;
                    break;
            }

            LIL_GET_SUBTEX("_Main2ndTex", uv2nd);
            LIL_SAMPLE_2D_WithMat("_Main2ndBlendMask", samp, uvMain, uvMainMatrix);
            lilCalcDissolveWithOrWithoutNoise(
                UsingUVChannels.UV0,
                "_Main2ndDissolveMask",
                "_Main2ndDissolveMask_ST",
                "_Main2ndDissolveNoiseMask",
                "_Main2ndDissolveNoiseMask_ST",
                "_Main2ndDissolveNoiseMask_ScrollRotate",
                samp
            );
        }

        if (matInfo.GetInteger("_UseMain3rdTex") != 0)
        {
            // caller of lilGetMain3rd will pass sampler for _MainTex as samp
            var samp = "_MainTex";

            UsingUVChannels uv3rd;
            switch (matInfo.GetInteger("_Main2ndTex_UVMode"))
            {
                case 0:
                    uv3rd = UsingUVChannels.UV0;
                    break;
                case 1:
                    uv3rd = UsingUVChannels.UV1;
                    break;
                case 2:
                    uv3rd = UsingUVChannels.UV2;
                    break;
                case 3:
                    uv3rd = UsingUVChannels.UV3;
                    break;
                case 4:
                    uv3rd = UsingUVChannels.NonMesh;
                    break; // MatCap (normal-based UV)
                default:
                    uv3rd = UsingUVChannels.UV0 | UsingUVChannels.UV1 | UsingUVChannels.UV2 | UsingUVChannels.UV3;
                    break;
            }

            LIL_GET_SUBTEX("_Main3rdTex", uv3rd);
            LIL_SAMPLE_2D_WithMat("_Main3rdBlendMask", samp, uvMain, uvMainMatrix);
            lilCalcDissolveWithOrWithoutNoise(
                UsingUVChannels.UV0,
                "_Main3rdDissolveMask",
                "_Main3rdDissolveMask_ST",
                "_Main3rdDissolveNoiseMask",
                "_Main3rdDissolveNoiseMask_ST",
                "_Main3rdDissolveNoiseMask_ScrollRotate",
                samp
            );
        }

        LIL_SAMPLE_2D_ST_WithMat("_AlphaMask", "_MainTex", uvMain, uvMainMatrix);
        if (matInfo.GetInteger("_UseBumpMap") != 0)
        {
            LIL_SAMPLE_2D_ST_WithMat("_BumpMap", "_MainTex", uvMain, uvMainMatrix);
        }

        if (matInfo.GetInteger("_UseBump2ndMap") != 0)
        {
            var uvBump2nd = UsingUVChannels.UV0;

            switch (matInfo.GetInteger("_Bump2ndMap_UVMode"))
            {
                case 0:
                    uvBump2nd = UsingUVChannels.UV0;
                    break;
                case 1:
                    uvBump2nd = UsingUVChannels.UV1;
                    break;
                case 2:
                    uvBump2nd = UsingUVChannels.UV2;
                    break;
                case 3:
                    uvBump2nd = UsingUVChannels.UV3;
                    break;
                case null:
                    uvBump2nd = UsingUVChannels.UV0 | UsingUVChannels.UV1 | UsingUVChannels.UV2 | UsingUVChannels.UV3;
                    break;
            }

            LIL_SAMPLE_2D_ST("_Bump2ndMap", SamplerStateInformation.LinearRepeatSampler, uvBump2nd);
            LIL_SAMPLE_2D_ST_WithMat("_Bump2ndScaleMask", "_MainTex", uvMain, uvMainMatrix);

            // Note: _Bump2ndScaleMask is defined as NoScaleOffset but sampled with LIL_SAMPLE_2D_ST?
        }

        if (matInfo.GetInteger("_UseAnisotropy") != 0)
        {
            LIL_SAMPLE_2D_ST_WithMat("_AnisotropyTangentMap", "_MainTex", uvMain, uvMainMatrix);
            LIL_SAMPLE_2D_ST_WithMat("_AnisotropyScaleMask", "_MainTex", uvMain, uvMainMatrix);

            // _AnisotropyShiftNoiseMask is used in another place but under _UseAnisotropy condition
            LIL_SAMPLE_2D_ST_WithMat("_AnisotropyShiftNoiseMask", "_MainTex", uvMain, uvMainMatrix);
        }

        if (matInfo.GetInteger("_UseBacklight") != 0)
        {
            var samp = "_MainTex";
            LIL_SAMPLE_2D_ST_WithMat("_BacklightColorTex", samp, uvMain, uvMainMatrix);
        }

        if (matInfo.GetInteger("_UseShadow") != 0)
        {
            SamplerStateInformation samp = "_MainTex";
            LIL_SAMPLE_2D_GRAD_WithMat("_ShadowStrengthMask", SamplerStateInformation.LinearRepeatSampler, uvMain,
                uvMainMatrix);
            LIL_SAMPLE_2D_GRAD_WithMat("_ShadowBorderMask", SamplerStateInformation.LinearRepeatSampler, uvMain,
                uvMainMatrix);
            LIL_SAMPLE_2D_GRAD_WithMat("_ShadowBlurMask", SamplerStateInformation.LinearRepeatSampler, uvMain,
                uvMainMatrix);
            // lilSampleLUT
            switch (matInfo.GetInteger("_ShadowColorType"))
            {
                case 1:
                    LIL_SAMPLE_2D_WithMat("_ShadowColorTex", SamplerStateInformation.LinearClampSampler,
                        UsingUVChannels.NonMesh, null);
                    LIL_SAMPLE_2D_WithMat("_Shadow2ndColorTex", SamplerStateInformation.LinearClampSampler,
                        UsingUVChannels.NonMesh, null);
                    LIL_SAMPLE_2D_WithMat("_Shadow3rdColorTex", SamplerStateInformation.LinearClampSampler,
                        UsingUVChannels.NonMesh, null);
                    break;
                case null:
                    var sampler = samp | SamplerStateInformation.LinearClampSampler;
                    LIL_SAMPLE_2D_WithMat("_ShadowColorTex", sampler, UsingUVChannels.NonMesh | uvMain, null);
                    LIL_SAMPLE_2D_WithMat("_Shadow2ndColorTex", sampler, UsingUVChannels.NonMesh | uvMain, null);
                    LIL_SAMPLE_2D_WithMat("_Shadow3rdColorTex", sampler, UsingUVChannels.NonMesh | uvMain, null);
                    break;
                default:
                    LIL_SAMPLE_2D_WithMat("_ShadowColorTex", samp, uvMain, uvMainMatrix);
                    LIL_SAMPLE_2D_WithMat("_Shadow2ndColorTex", samp, uvMain, uvMainMatrix);
                    LIL_SAMPLE_2D_WithMat("_Shadow3rdColorTex", samp, uvMain, uvMainMatrix);
                    break;
            }
        }

        if (matInfo.GetInteger("_UseRimShade") != 0)
        {
            var samp = "_MainTex";

            LIL_SAMPLE_2D_WithMat("_RimShadeMask", samp, uvMain, uvMainMatrix);
        }

        if (matInfo.GetInteger("_UseReflection") != 0)
        {
            // TODO: research
            var samp = "_MainTex"; // or SamplerStateInformation.LinearRepeatSampler in lil_pass_foreward_reblur.hlsl

            LIL_SAMPLE_2D_ST_WithMat("_SmoothnessTex", samp, uvMain, uvMainMatrix);
            LIL_SAMPLE_2D_ST_WithMat("_MetallicGlossMap", samp, uvMain, uvMainMatrix);
            LIL_SAMPLE_2D_ST_WithMat("_ReflectionColorTex", samp, uvMain, uvMainMatrix);
        }

        // Matcap
        if (matInfo.GetInteger("_UseMatCap") != 0)
        {
            var samp = "_MainTex"; // caller of lilGetMatCap

            LIL_SAMPLE_2D("_MatCapTex", SamplerStateInformation.LinearRepeatSampler, UsingUVChannels.NonMesh);
            LIL_SAMPLE_2D_ST_WithMat("_MatCapBlendMask", samp, uvMain, uvMainMatrix);

            if (matInfo.GetInteger("_MatCapCustomNormal") != 0)
            {
                LIL_SAMPLE_2D_ST_WithMat("_MatCapBumpMap", samp, uvMain, uvMainMatrix);
            }
        }

        if (matInfo.GetInteger("_UseMatCap2nd") != 0)
        {
            var samp = "_MainTex"; // caller of lilGetMatCap

            LIL_SAMPLE_2D("_MatCap2ndTex", SamplerStateInformation.LinearRepeatSampler, UsingUVChannels.NonMesh);
            LIL_SAMPLE_2D_ST_WithMat("_MatCap2ndBlendMask", samp, uvMain, uvMainMatrix);

            if (matInfo.GetInteger("_MatCap2ndCustomNormal") != 0)
            {
                LIL_SAMPLE_2D_ST_WithMat("_MatCap2ndBumpMap", samp, uvMain, uvMainMatrix);
            }
        }

        // rim light
        if (matInfo.GetInteger("_UseRim") != 0)
        {
            var samp = "_MainTex"; // caller of lilGetRim
            LIL_SAMPLE_2D_ST_WithMat("_RimColorTex", samp, uvMain, uvMainMatrix);
        }

        if (matInfo.GetInteger("_UseGlitter") != 0)
        {
            var samp = "_MainTex"; // caller of lilGetGlitter

            LIL_SAMPLE_2D_ST_WithMat("_GlitterColorTex", samp, uvMain, uvMainMatrix);
            if (matInfo.GetInteger("_GlitterApplyShape") != 0)
            {
                // complex uv
                LIL_SAMPLE_2D_GRAD("_GlitterShapeTex", SamplerStateInformation.LinearClampSampler,
                    UsingUVChannels.NonMesh);
            }
        }

        if (matInfo.GetInteger("_UseEmission") != 0)
        {
            UsingUVChannels emissionUV = UsingUVChannels.UV0;

            switch (matInfo.GetInteger("_EmissionMap_UVMode"))
            {
                case 1:
                    emissionUV = UsingUVChannels.UV1;
                    break;
                case 2:
                    emissionUV = UsingUVChannels.UV2;
                    break;
                case 3:
                    emissionUV = UsingUVChannels.UV3;
                    break;
                case 4:
                    emissionUV = UsingUVChannels.NonMesh;
                    break; // uvRim; TODO: check
                case null:
                    emissionUV = UsingUVChannels.UV0 | UsingUVChannels.UV1 | UsingUVChannels.UV2 | UsingUVChannels.UV3 |
                                 UsingUVChannels.NonMesh;
                    break;
            }

            var parallaxEnabled = matInfo.GetFloat("_EmissionParallaxDepth") != 0;

            LIL_GET_EMITEX("_EmissionMap", emissionUV, parallaxEnabled);

            // if LIL_FEATURE_ANIMATE_EMISSION_MASK_UV is enabled, UV0 is used and if not UVMain is used.
            var LIL_FEATURE_ANIMATE_EMISSION_MASK_UV =
                matInfo.GetVector("_EmissionBlendMask_ScrollRotate") != new Vector4(0, 0, 0, 0) ||
                matInfo.GetVector("_Emission2ndBlendMask_ScrollRotate") != new Vector4(0, 0, 0, 0);

            if (LIL_FEATURE_ANIMATE_EMISSION_MASK_UV)
            {
                LIL_GET_EMIMASK("_EmissionBlendMask", UsingUVChannels.UV0);
            }
            else
            {
                LIL_GET_EMIMASK_WithMat("_EmissionBlendMask", uvMain, uvMainMatrix);
            }

            if (matInfo.GetInteger("_EmissionUseGrad") != 0)
            {
                LIL_SAMPLE_1D("_EmissionGradTex", SamplerStateInformation.LinearRepeatSampler, UsingUVChannels.NonMesh);
            }
        }

        if (matInfo.GetInteger("_Emission2ndMap") != 0)
        {
            UsingUVChannels emission2ndUV = UsingUVChannels.UV0;

            switch (matInfo.GetInteger("_Emission2ndMap_UVMode"))
            {
                case 1:
                    emission2ndUV = UsingUVChannels.UV1;
                    break;
                case 2:
                    emission2ndUV = UsingUVChannels.UV2;
                    break;
                case 3:
                    emission2ndUV = UsingUVChannels.UV3;
                    break;
                case 4:
                    emission2ndUV = UsingUVChannels.NonMesh;
                    break; // uvRim; TODO: check
                case null:
                    emission2ndUV = UsingUVChannels.UV0 | UsingUVChannels.UV1 | UsingUVChannels.UV2 |
                                    UsingUVChannels.UV3 | UsingUVChannels.NonMesh;
                    break;
            }

            var parallaxEnabled = matInfo.GetFloat("_Emission2ndParallaxDepth") != 0;

            // actually LIL_GET_EMITEX is used but same as LIL_SAMPLE_2D_ST
            LIL_GET_EMITEX("_Emission2ndMap", emission2ndUV, parallaxEnabled);

            // if LIL_FEATURE_ANIMATE_EMISSION_MASK_UV is enabled, UV0 is used and if not UVMain is used. (weird)
            // https://github.com/lilxyzw/lilToon/blob/b96470d3dd9092b840052578048b2307fe6d8786/Assets/lilToon/Shader/Includes/lil_common_frag.hlsl#L1819-L1821
            var LIL_FEATURE_ANIMATE_EMISSION_MASK_UV =
                matInfo.GetVector("_EmissionBlendMask_ScrollRotate") != new Vector4(0, 0, 0, 0) ||
                matInfo.GetVector("_Emission2ndBlendMask_ScrollRotate") != new Vector4(0, 0, 0, 0);

            if (LIL_FEATURE_ANIMATE_EMISSION_MASK_UV)
            {
                LIL_GET_EMIMASK("_Emission2ndBlendMask", UsingUVChannels.UV0);
            }
            else
            {
                LIL_GET_EMIMASK_WithMat("_Emission2ndBlendMask", uvMain, uvMainMatrix);
            }

            if (matInfo.GetInteger("_Emission2ndUseGrad") != 0)
            {
                LIL_SAMPLE_1D("_Emission2ndGradTex", SamplerStateInformation.LinearRepeatSampler,
                    UsingUVChannels.NonMesh);
            }
        }

        if (matInfo.GetInteger("_UseParallax") != 0)
        {
            matInfo.RegisterTextureUVUsage("_ParallaxMap", SamplerStateInformation.LinearRepeatSampler,
                UsingUVChannels.UV0, null);
        }

        if (matInfo.GetInteger("_UseAudioLink") != 0 && matInfo.GetInteger("_AudioLink2Vertex") != 0)
        {
            var _AudioLinkUVMode = matInfo.GetInteger("_AudioLinkUVMode");

            if (_AudioLinkUVMode is 3 or 4 or null)
            {
                // TODO: _AudioLinkMask_ScrollRotate
                var sampler = "_AudioLinkMask" | SamplerStateInformation.LinearRepeatSampler;
                switch (matInfo.GetInteger("_AudioLinkMask_UVMode"))
                {
                    case 0:
                    default:
                        LIL_SAMPLE_2D_ST_WithMat("_AudioLinkMask", sampler, uvMain, uvMainMatrix);
                        break;
                    case 1:
                        LIL_SAMPLE_2D_ST("_AudioLinkMask", sampler, UsingUVChannels.UV1);
                        break;
                    case 2:
                        LIL_SAMPLE_2D_ST("_AudioLinkMask", sampler, UsingUVChannels.UV2);
                        break;
                    case 3:
                        LIL_SAMPLE_2D_ST("_AudioLinkMask", sampler, UsingUVChannels.UV3);
                        break;
                    case null:
                        LIL_SAMPLE_2D_ST_WithMat("_AudioLinkMask", sampler,
                            uvMain | UsingUVChannels.UV1 | UsingUVChannels.UV2 | UsingUVChannels.UV3,
                            Combine(uvMainMatrix, Matrix4x4.identity));
                        break;
                }
            }
        }

        if (matInfo.GetVector("_DissolveParams")?.x != 0)
        {
            lilCalcDissolveWithOrWithoutNoise(
                //fd.col.a,
                //dissolveAlpha,
                UsingUVChannels.UV0,
                //fd.positionOS,
                //_DissolveParams,
                //_DissolvePos,
                "_DissolveMask",
                "_DissolveMask_ST",
                //_DissolveMaskEnabled,
                "_DissolveNoiseMask",
                "_DissolveNoiseMask_ST",
                "_DissolveNoiseMask_ScrollRotate",
                //_DissolveNoiseStrength
                "_MainTex"
            );
        }

        if (matInfo.GetInteger("_UseOutline") != 0)
        {
            // not on material side, on editor side toggle
            LIL_SAMPLE_2D_WithMat("_OutlineTex", "_OutlineTex", uvMain, uvMainMatrix);
            LIL_SAMPLE_2D_WithMat("_OutlineWidthMask", SamplerStateInformation.LinearRepeatSampler, uvMain,
                uvMainMatrix);
            // _OutlineVectorTex SamplerStateInformation.LinearRepeatSampler
            // UVs _OutlineVectorUVMode main,1,2,3

            switch (matInfo.GetInteger("_AudioLinkMask_UVMode"))
            {
                case 0:
                    LIL_SAMPLE_2D_WithMat("_OutlineVectorTex", SamplerStateInformation.LinearRepeatSampler, uvMain,
                        uvMainMatrix);
                    break;
                case 1:
                    LIL_SAMPLE_2D("_OutlineVectorTex", SamplerStateInformation.LinearRepeatSampler,
                        UsingUVChannels.UV1);
                    break;
                case 2:
                    LIL_SAMPLE_2D("_OutlineVectorTex", SamplerStateInformation.LinearRepeatSampler,
                        UsingUVChannels.UV2);
                    break;
                case 3:
                    LIL_SAMPLE_2D("_OutlineVectorTex", SamplerStateInformation.LinearRepeatSampler,
                        UsingUVChannels.UV3);
                    break;
                default:
                case null:
                    matInfo.RegisterTextureUVUsage(
                        "_OutlineVectorTex",
                        SamplerStateInformation.LinearRepeatSampler,
                        UsingUVChannels.UV0 | UsingUVChannels.UV1 | UsingUVChannels.UV2 | UsingUVChannels.UV3,
                        Combine(uvMainMatrix, UnityEngine.Matrix4x4.identity)
                    );
                    break;
            }
        }

        // _BaseMap and _BaseColorMap are unused

        return true;

        void LIL_SAMPLE_1D(string textureName, SamplerStateInformation samplerName, UsingUVChannels uvChannel)
        {
            matInfo.RegisterTextureUVUsage(
                textureName,
                samplerName,
                uvChannel,
                UnityEngine.Matrix4x4.identity
            );
        }

        void LIL_SAMPLE_2D(string textureName, SamplerStateInformation samplerName, UsingUVChannels uvChannel)
        {
            // might be _LOD: using SampleLevel
            matInfo.RegisterTextureUVUsage(
                textureName,
                samplerName,
                uvChannel,
                UnityEngine.Matrix4x4.identity
            );
        }

        void LIL_SAMPLE_2D_WithMat(string textureName, SamplerStateInformation samplerName, UsingUVChannels uvChannel,
            UnityEngine.Matrix4x4? matrix)
        {
            // might be _LOD: using SampleLevel
            matInfo.RegisterTextureUVUsage(
                textureName,
                samplerName,
                uvChannel,
                matrix
            );
        }

        void LIL_SAMPLE_2D_GRAD(string textureName, SamplerStateInformation samplerName, UsingUVChannels uvChannel)
        {
            // additional parameter for SampleGrad does not affect UV location much
            LIL_SAMPLE_2D(textureName, samplerName, uvChannel);
        }

        void LIL_SAMPLE_2D_GRAD_WithMat(string textureName, SamplerStateInformation samplerName,
            UsingUVChannels uvChannel, UnityEngine.Matrix4x4? matrix)
        {
            // additional parameter for SampleGrad does not affect UV location much
            LIL_SAMPLE_2D_WithMat(textureName, samplerName, uvChannel, matrix);
        }

        void LIL_SAMPLE_2D_ST(string textureName, SamplerStateInformation samplerName, UsingUVChannels uvChannel)
        {
            matInfo.RegisterTextureUVUsage(
                textureName,
                samplerName,
                uvChannel,
                STToMatrix($"{textureName}_ST")
            );
        }

        void LIL_SAMPLE_2D_ST_WithMat(string textureName, SamplerStateInformation samplerName,
            UsingUVChannels uvChannel, UnityEngine.Matrix4x4? matrix)
        {
            matInfo.RegisterTextureUVUsage(
                textureName,
                samplerName,
                uvChannel,
                Multiply(STToMatrix($"{textureName}_ST"), matrix)
            );
        }

        void LIL_GET_SUBTEX(string textureName, UsingUVChannels uvChannel)
        {
            // lilGetSubTex

            // TODO: consider the following properties
            var st = $"{textureName}_ST";
            var scrollRotate = $"{textureName}_ScrollRotate";
            var angle = $"{textureName}Angle";
            var isDecal = $"{textureName}IsDecal";
            var isLeftOnly = $"{textureName}IsLeftOnly";
            var isRightOnly = $"{textureName}IsRightOnly";
            var shouldCopy = $"{textureName}ShouldCopy";
            var shouldFlipMirror = $"{textureName}ShouldFlipMirror";
            var shouldFlipCopy = $"{textureName}ShouldFlipCopy";
            var isMSDF = $"{textureName}IsMSDF";
            var decalAnimation = $"{textureName}DecalAnimation";
            var decalSubParam = $"{textureName}DecalSubParam";
            // fd.nv?
            // fd.isRightHand?


            Matrix4x4? ComputeMatrix()
            {
                var stValueOpt = matInfo.GetVector(st);
                var rotateValueOpt = matInfo.GetVector(scrollRotate);
                var angleValueOpt = matInfo.GetFloat(angle);
                //var isDecalValueOpt = matInfo.GetFloat(isDecal);
                var isLeftOnlyValueOpt = matInfo.GetFloat(isLeftOnly);
                var isRightOnlyValueOpt = matInfo.GetFloat(isRightOnly);
                var shouldCopyValueOpt = matInfo.GetFloat(shouldCopy);
                var shouldFlipMirrorValueOpt = matInfo.GetFloat(shouldFlipMirror);
                var shouldFlipCopyValueOpt = matInfo.GetFloat(shouldFlipCopy);
                //var isMSDFValueOpt = matInfo.GetFloat(isMSDF);
                var decalAnimationValueOpt = matInfo.GetVector(decalAnimation);
                // var decalSubParamValueOpt = matInfo.GetVector(decalSubParam);

                if (stValueOpt is not { } stValue) return null;
                if (rotateValueOpt is not { } rotateValue) return null;
                if (angleValueOpt is not { } angleValue) return null;

                rotateValue.z = angleValue;

                if (STAndScrollRotateValueToMatrix(stValue, rotateValue) is not { } matrix) return null;

                // shouldCopy is true => x = abs(x - 0.5) + 0.5
                if (shouldCopyValueOpt != 0) return null;
                // shouldFlipCopy is true => flips
                if (shouldFlipCopyValueOpt != 0) return null;
                // shouldFlipMirror is true => flips
                if (shouldFlipMirrorValueOpt != 0) return null;

                // isDecal is true => decal
                if (isLeftOnlyValueOpt != 0) return null;
                if (isRightOnlyValueOpt != 0) return null;

                // rotation is performed in STAndScrollRotateValueToMatrix

                if (decalAnimationValueOpt != new Vector4(1.0f, 1.0f, 1.0f, 30.0f)) return null;

                return matrix;
            }

            matInfo.RegisterTextureUVUsage(textureName, textureName, uvChannel, ComputeMatrix());
        }

        void LIL_GET_EMITEX(string textureName, UsingUVChannels uvChannel, bool parallaxEnabled)
        {
            LIL_SAMPLE_2D_WithMat(textureName, textureName, uvChannel,
                parallaxEnabled ? null : STAndScrollRotateToMatrix($"{textureName}_ST", $"{textureName}_ScrollRotate"));
        }

        void LIL_GET_EMIMASK_WithMat(string textureName, UsingUVChannels uvChannel, UnityEngine.Matrix4x4? matrix)
        {
            LIL_SAMPLE_2D_WithMat(textureName, "_MainTex", uvChannel,
                Multiply(matrix, STAndScrollRotateToMatrix($"{textureName}_ST", $"{textureName}_ScrollRotate")));
        }

        void LIL_GET_EMIMASK(string textureName, UsingUVChannels uvChannel)
        {
            LIL_SAMPLE_2D_WithMat(textureName, "_MainTex", uvChannel,
                STAndScrollRotateToMatrix($"{textureName}_ST", $"{textureName}_ScrollRotate"));
        }

        void lilCalcDissolveWithOrWithoutNoise(
            // alpha,
            // dissolveAlpha,
            UsingUVChannels uv, // ?
            // positionOS,
            // dissolveParams,
            // dissolvePos,
            string dissolveMask,
            string dissolveMaskST,
            //  dissolveMaskEnabled
            string dissolveNoiseMask,
            string dissolveNoiseMaskST,
            string dissolveNoiseMaskScrollRotate,
            // dissolveNoiseStrength,
            SamplerStateInformation samp
        )
        {
            LIL_SAMPLE_2D_WithMat(dissolveMask, samp, uv, STToMatrix(dissolveMaskST));
            LIL_SAMPLE_2D_WithMat(dissolveNoiseMask, samp, uv,
                STAndScrollRotateToMatrix(dissolveNoiseMaskST, dissolveNoiseMaskScrollRotate));
        }

        // lilCalcUV
        Matrix4x4? STToMatrix(string stPropertyName) => STValueToMatrix(matInfo.GetVector(stPropertyName));

        Matrix4x4? STValueToMatrix(Vector4? stIn)
        {
            if (stIn is not { } st) return null;

            var matrix = Matrix4x4.identity;
            matrix.m00 = st.x;
            matrix.m11 = st.y;
            matrix.m03 = st.z;
            matrix.m13 = st.w;

            return matrix;
        }

        // lilCalcUV
        Matrix4x4? STAndScrollRotateToMatrix(string stPropertyName, string scrollRotatePropertyName) =>
            STAndScrollRotateValueToMatrix(matInfo.GetVector(stPropertyName),
                matInfo.GetVector(scrollRotatePropertyName));

        Matrix4x4? STAndScrollRotateValueToMatrix(Vector4? stValueIn, Vector4? scrollRotateIn)
        {
            if (STValueToMatrix(stValueIn) is not { } stMatrix) return null;
            if (scrollRotateIn is not { } scrollRotate) return stMatrix;

            float staticAngle = scrollRotate.z;
            float scrollAngleSpeed = scrollRotate.w;
            Vector2 scrollSpeed = new(scrollRotate.x, scrollRotate.y);

            if (scrollSpeed != Vector2.zero || scrollAngleSpeed != 0) return null;

            if (staticAngle == 0) return stMatrix;

            var result = stMatrix;

            result *= Matrix4x4.TRS(new Vector3(-0.5f, -0.5f), Quaternion.identity, Vector3.one);
            result *= Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, staticAngle), Vector3.one);
            result *= Matrix4x4.TRS(new Vector3(0.5f, 0.5f), Quaternion.identity, Vector3.one);

            return result;
        }

        static Matrix4x4? Combine(Matrix4x4? a, Matrix4x4? b)
        {
            if (a == null) return b;
            if (b == null) return a;
            if (a == b) return a;
            return null;
        }

        Matrix4x4? Multiply(Matrix4x4? a, Matrix4x4? b)
        {
            if (a == null || b == null) return null;
            return a.Value * b.Value;
        }
    }
}
