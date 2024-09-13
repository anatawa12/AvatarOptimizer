using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    internal sealed class ShaderKnowledge
    {
        public static bool IsParameterAnimationAffected(Material material, MeshInfo2 meshInfo2, string propertyName)
        {
            // basic check only for now
            var shader = material.shader;
            if (shader.FindPropertyIndex(propertyName) == -1) return false;

            // TODO: add shader specific checks

            switch (propertyName)
            {
                // UV Discord, which is implemented by poiyomi and liltoon.
                case "_UDIMDiscardRow3_0":
                    return CheckAffectedUvDiscard(material, meshInfo2, 3, 0);
                case "_UDIMDiscardRow3_1":
                    return CheckAffectedUvDiscard(material, meshInfo2, 3, 1);
                case "_UDIMDiscardRow3_2":
                    return CheckAffectedUvDiscard(material, meshInfo2, 3, 2);
                case "_UDIMDiscardRow3_3":
                    return CheckAffectedUvDiscard(material, meshInfo2, 3, 3);
                case "_UDIMDiscardRow2_0":
                    return CheckAffectedUvDiscard(material, meshInfo2, 2, 0);
                case "_UDIMDiscardRow2_1":
                    return CheckAffectedUvDiscard(material, meshInfo2, 2, 1);
                case "_UDIMDiscardRow2_2":
                    return CheckAffectedUvDiscard(material, meshInfo2, 2, 2);
                case "_UDIMDiscardRow2_3":
                    return CheckAffectedUvDiscard(material, meshInfo2, 2, 3);
                case "_UDIMDiscardRow1_0":
                    return CheckAffectedUvDiscard(material, meshInfo2, 1, 0);
                case "_UDIMDiscardRow1_1":
                    return CheckAffectedUvDiscard(material, meshInfo2, 1, 1);
                case "_UDIMDiscardRow1_2":
                    return CheckAffectedUvDiscard(material, meshInfo2, 1, 2);
                case "_UDIMDiscardRow1_3":
                    return CheckAffectedUvDiscard(material, meshInfo2, 1, 3);
                case "_UDIMDiscardRow0_0":
                    return CheckAffectedUvDiscard(material, meshInfo2, 0, 0);
                case "_UDIMDiscardRow0_1":
                    return CheckAffectedUvDiscard(material, meshInfo2, 0, 1);
                case "_UDIMDiscardRow0_2":
                    return CheckAffectedUvDiscard(material, meshInfo2, 0, 2);
                case "_UDIMDiscardRow0_3":
                    return CheckAffectedUvDiscard(material, meshInfo2, 0, 3);
            }

            return true;
        }

        private static readonly int EnableUdimDiscardOptions = Shader.PropertyToID("_EnableUDIMDiscardOptions");
        private static readonly int UdimDiscardCompile = Shader.PropertyToID("_UDIMDiscardCompile");
        private static readonly int UdimDiscardUV = Shader.PropertyToID("_UDIMDiscardUV");

        private static bool CheckAffectedUvDiscard(Material material, MeshInfo2 meshInfo2, int row, int column)
        {
            // in poiyomi, "_EnableUDIMDiscardOptions" is used to toggle uv discard enabled or not.
            // in liltoon, "_UDIMDiscardCompile" is used to toggle uv discard enabled or not.
            // so, if both of them are disabled, we assume uv discard is not enabled.
            // liltoon _UDIMDiscardCompile can be animated by animation but I ignore it for now.
            if (material.GetFloat(EnableUdimDiscardOptions) == 0 && material.GetFloat(UdimDiscardCompile) == 0)
                return false;

            // "_UDIMDiscardMode" is uv discard mode. pixel or vertex. we don't care about it for now.

            // "_UDIMDiscardUV" is the property describes which uv channel is used for uv discard.
            // it also can be animated by animation but I ignore it for now.
            int texCoord;
            switch (material.GetFloat(UdimDiscardUV))
            {
                case 0: texCoord = 0; break;
                case 1: texCoord = 1; break;
                case 2: texCoord = 2; break;
                case 3: texCoord = 3; break;
                default: return true; // _UDIMDiscardUV is not valid value.
            }

            // the texcoord is not defined in the mesh, it is not affected.
            if (meshInfo2.GetTexCoordStatus(texCoord) == TexCoordStatus.NotDefined)
                return false;

            // if vertex is in the tile, it is affected.
            return meshInfo2.Vertices.AsParallel().Any(vertex =>
            {
                var uv = vertex.GetTexCoord(texCoord);
                // outside of 4x4 tile is not affected.
                if (uv.x < 0 || uv.x >= 4 || uv.y < 0 || uv.y >= 4) return false;
                var x = (int)Mathf.Floor(uv.x);
                var y = (int)Mathf.Floor(uv.y);
                return x == column && y == row;
            });
        }

        public abstract class TextureUsageInformationCallback
        {
            internal TextureUsageInformationCallback() { }

            public abstract Shader Shader { get; }

            /// <summary>
            /// Returns the integer value for the property in the material, or null if the property is not set or not found.
            /// </summary>
            /// <param name="propertyName">The name of the property in the material.</param>
            /// <param name="considerAnimation">Whether to consider the animation of the property. If this is true, this function will never </param>
            /// <returns>The integer value for the property in the material, which is same as <see cref="Material.GetInteger(string)"/>, or null if the property is not set or not found.</returns>
            public abstract int? GetInteger(string propertyName, bool considerAnimation = true);

            /// <summary>
            /// Returns the float value for the property in the material, or null if the property is not set or not found.
            /// </summary>
            /// <param name="propertyName">The name of the property in the material.</param>
            /// <param name="considerAnimation">Whether to consider the animation of the property. If this is true, this function will never </param>
            /// <returns>The integer value for the property in the material, which is same as <see cref="Material.GetFloat(string)"/>, or null if the property is not set or not found.</returns>
            public abstract float? GetFloat(string propertyName, bool considerAnimation = true);

            /// <summary>
            /// Returns the float value for the property in the material, or null if the property is not set or not found.
            /// </summary>
            /// <param name="propertyName">The name of the property in the material.</param>
            /// <param name="considerAnimation">Whether to consider the animation of the property. If this is true, this function will never </param>
            /// <returns>The integer value for the property in the material, which is same as <see cref="Material.GetVector(string)"/>, or null if the property is not set or not found.</returns>
            public abstract Vector4? GetVector(string propertyName, bool considerAnimation = true);

            /// <summary>
            /// Creates a new <see cref="TextureUsageInformation"/> instance.
            /// </summary>
            /// <param name="materialPropertyName">The name of the texture property in the material.</param>
            /// <param name="uvChannel">The UV channel for the texture.</param>
            /// <returns>A new <see cref="TextureUsageInformation"/> instance.</returns>
            public TextureUsageInformation CreateTextureUsageInformation(string materialPropertyName,
                UVChannel uvChannel) => new(materialPropertyName, uvChannel);

            /// <summary>
            /// Creates a new <see cref="TextureUsageInformation"/> instance for texture without ScaleOffset (_ST).
            /// </summary>
            /// <param name="materialPropertyName">The name of the texture property in the material.</param>
            /// <param name="uvChannel">The UV channel for the texture.</param>
            /// <returns>A new <see cref="TextureUsageInformation"/> instance.</returns>
            public TextureUsageInformation CreateTextureUsageInformationNoScaleOffset(string materialPropertyName,
                UVChannel uvChannel) => new(materialPropertyName, uvChannel);

            /// <summary>
            /// Creates a new <see cref="TextureUsageInformation"/> instance.
            /// </summary>
            /// <param name="materialPropertyName">The name of the texture property in the material.</param>
            /// <param name="uvChannel">The UV channel for the texture.</param>
            /// <param name="scaleOffsetProperty">The name of vector4 property for UV Scale and Offset</param>
            /// <returns>A new <see cref="TextureUsageInformation"/> instance.</returns>
            public TextureUsageInformation CreateTextureUsageInformation(string materialPropertyName,
                UVChannel uvChannel, string scaleOffsetProperty) => new(materialPropertyName, uvChannel);
        }

        public enum UVChannel
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
            Unknown = -1,
        }

        public class TextureUsageInformation
        {
            public string MaterialPropertyName { get; }
            public UVChannel UVChannel { get; }

            internal TextureUsageInformation(string materialPropertyName, UVChannel uvChannel)
            {
                MaterialPropertyName = materialPropertyName;
                UVChannel = uvChannel;
            }
        }

        // TODO: define return type
        /// <summary>
        /// Returns texture usage information for the material.
        /// </summary>
        /// <returns>null if the shader is not supported</returns>
        public static TextureUsageInformation[]? GetTextureUsageInformationForMaterial(TextureUsageInformationCallback information)
        {
            if (AssetDatabase.GetAssetPath(information.Shader).StartsWith("Packages/jp.lilxyzw.liltoon"))
            {
                // it looks liltoon!
                return GetTextureUsageInformationForMaterialLiltoon(information);
            }

            return null;
        }

        private static TextureUsageInformation[]? GetTextureUsageInformationForMaterialLiltoon(TextureUsageInformationCallback matInfo)
        {
            /*
             *
               * [NoScaleOffset] _DitherTex                  ("Dither", 2D) = "white" {}
               * [MainTexture]   _MainTex                    ("Texture", 2D) = "white" {}
               * [NoScaleOffset] _MainGradationTex           ("Gradation Map", 2D) = "white" {}
               * [NoScaleOffset] _MainColorAdjustMask        ("Adjust Mask", 2D) = "white" {}
               *                 _Main2ndTex                 ("Texture", 2D) = "white" {}
               * [NoScaleOffset] _Main2ndBlendMask           ("Mask", 2D) = "white" {}
               *                 _Main2ndDissolveMask        ("Dissolve Mask", 2D) = "white" {}
               *                 _Main2ndDissolveNoiseMask   ("Dissolve Noise Mask", 2D) = "gray" {}
               *                 _Main3rdTex                 ("Texture", 2D) = "white" {}
               * [NoScaleOffset] _Main3rdBlendMask           ("Mask", 2D) = "white" {}
               *                 _Main3rdDissolveMask        ("Dissolve Mask", 2D) = "white" {}
               *                 _Main3rdDissolveNoiseMask   ("Dissolve Noise Mask", 2D) = "gray" {}
               *                 _AlphaMask                  ("AlphaMask", 2D) = "white" {}
               * [Normal]        _BumpMap                    ("Normal Map", 2D) = "bump" {}
               * [Normal]        _Bump2ndMap                 ("Normal Map", 2D) = "bump" {}
               * [NoScaleOffset] _Bump2ndScaleMask           ("Mask", 2D) = "white" {}
               * [Normal]        _AnisotropyTangentMap       ("Tangent Map", 2D) = "bump" {}
               * [NoScaleOffset] _AnisotropyScaleMask        ("Scale Mask", 2D) = "white" {}
               *                 _AnisotropyShiftNoiseMask   ("sNoise", 2D) = "white" {}
               * [NoScaleOffset] _BacklightColorTex          ("Texture", 2D) = "white" {}
               * [NoScaleOffset] _ShadowStrengthMask         ("sStrength", 2D) = "white" {}
               * [NoScaleOffset] _ShadowBorderMask           ("sBorder", 2D) = "white" {}
               * [NoScaleOffset] _ShadowBlurMask             ("sBlur", 2D) = "white" {}
               * [NoScaleOffset] _ShadowColorTex             ("Shadow Color", 2D) = "black" {}
               * [NoScaleOffset] _Shadow2ndColorTex          ("2nd Color", 2D) = "black" {}
               * [NoScaleOffset] _Shadow3rdColorTex          ("3rd Color", 2D) = "black" {}
               * [NoScaleOffset] _RimShadeMask               ("Mask", 2D) = "white" {}
               * [NoScaleOffset] _SmoothnessTex              ("Smoothness", 2D) = "white" {}
               * [NoScaleOffset] _MetallicGlossMap           ("Metallic", 2D) = "white" {}
               * [NoScaleOffset] _ReflectionColorTex         ("sColor", 2D) = "white" {}
               *                 _MatCapTex                  ("Texture", 2D) = "white" {}
               * [NoScaleOffset] _MatCapBlendMask            ("Mask", 2D) = "white" {}
               * [Normal]        _MatCapBumpMap              ("Normal Map", 2D) = "bump" {}
               *                 _MatCap2ndTex               ("Texture", 2D) = "white" {}
               * [NoScaleOffset] _MatCap2ndBlendMask         ("Mask", 2D) = "white" {}
               * [Normal]        _MatCap2ndBumpMap           ("Normal Map", 2D) = "bump" {}
               * [NoScaleOffset] _RimColorTex                ("Texture", 2D) = "white" {}
               *                 _GlitterColorTex            ("Texture", 2D) = "white" {}
               *                 _GlitterShapeTex            ("Texture", 2D) = "white" {}
               *                 _EmissionMap                ("Texture", 2D) = "white" {}
               *                 _EmissionBlendMask          ("Mask", 2D) = "white" {}
               * [NoScaleOffset] _EmissionGradTex            ("Gradation Texture", 2D) = "white" {}
               *                 _Emission2ndMap             ("Texture", 2D) = "white" {}
               *                 _Emission2ndBlendMask       ("Mask", 2D) = "white" {}
               * [NoScaleOffset] _Emission2ndGradTex         ("Gradation Texture", 2D) = "white" {}
               * [NoScaleOffset] _ParallaxMap                ("Parallax Map", 2D) = "gray" {}
               *                 _AudioLinkMask              ("Mask", 2D) = "blue" {}
               * [NoScaleOffset] _AudioLinkLocalMap          ("Local Map", 2D) = "black" {}
               *                 _DissolveMask               ("Dissolve Mask", 2D) = "white" {}
               *                 _DissolveNoiseMask          ("Dissolve Noise Mask", 2D) = "gray" {}
               *                 _OutlineTex                 ("Texture", 2D) = "white" {}
               * [NoScaleOffset] _OutlineWidthMask           ("Width", 2D) = "white" {}
               * [NoScaleOffset][Normal] _OutlineVectorTex   ("Vector", 2D) = "bump" {}
               [HideInInspector]                               _BaseMap            ("Texture", 2D) = "white" {}
               [HideInInspector]                               _BaseColorMap       ("Texture", 2D) = "white" {}
             */

            // memo:
            // LIL_SAMPLE_2D just samples from specified texture, sampler and uv

            const string lil_sampler_linear_repeat = "builtin-sampler-Linear-Repeat";

            TextureUsageInformation LIL_SAMPLE_1D(string textureName, string samplerName, UVChannel uvChannel)
            {
                return matInfo.CreateTextureUsageInformationNoScaleOffset(textureName, uvChannel);
            }

            TextureUsageInformation LIL_SAMPLE_2D(string textureName, string samplerName, UVChannel uvChannel)
            {
                // might be _LOD: using SampleLevel
                return matInfo.CreateTextureUsageInformationNoScaleOffset(textureName, uvChannel);
            }

            TextureUsageInformation LIL_SAMPLE_2D_ST(string textureName, string samplerName, UVChannel uvChannel)
            {
                return matInfo.CreateTextureUsageInformation(textureName, uvChannel, $"{textureName}_ST");
            }

            TextureUsageInformation LIL_SAMPLE_2D_WithST(string textureName, string samplerName, UVChannel uvChannel, string st)
            {
                return matInfo.CreateTextureUsageInformation(textureName, uvChannel, st);
            }

            // This implementation is made for my Anon + Wahuku for testing this feature.
            // TODO: version check
            var information = new List<TextureUsageInformation>();

            var uvMain = UVChannel.UV0;
            var uvMainScaleOffset = "_MainTex_ST";

            TextureUsageInformation UVMain_LIL_SAMPLE_2D_ST(string textureName, string sampler)
            {
                // TODO: double ST support
                // TODO: recheck mainUV settings. includes (not limited to) ScrollRotate, Angle, Tilting
                return matInfo.CreateTextureUsageInformation(textureName, uvMain, uvMainScaleOffset);
            }

            TextureUsageInformation UVMain_LIL_SAMPLE_2D(string textureName, string sampler)
            {
                // TODO: recheck mainUV settings. includes (not limited to) ScrollRotate, Angle, Tilting
                return matInfo.CreateTextureUsageInformation(textureName, uvMain, uvMainScaleOffset);
            }

            TextureUsageInformation LIL_GET_SUBTEX(string textureName, UVChannel uvChannel)
            {
                var st = $"{textureName}_ST";

                // TODO: consider the following properties
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

                return matInfo.CreateTextureUsageInformation(textureName, uvChannel, st);
            }

            TextureUsageInformation LIL_GET_EMITEX(string textureName, UVChannel uvChannel)
            {
                var st = $"{textureName}_ST";

                // TODO: consider the following properties
                var scrollRotate = $"{textureName}_ScrollRotate";

                return matInfo.CreateTextureUsageInformation(textureName, uvChannel, st);
            }

            TextureUsageInformation LIL_GET_EMIMASK(string textureName, UVChannel uvChannel)
            {
                var st = $"{textureName}_ST";

                // sampler is sampler_MainTex
                // TODO: consider the following properties
                var scrollRotate = $"{textureName}_ScrollRotate";

                return matInfo.CreateTextureUsageInformation(textureName, uvChannel, st);
            }

            TextureUsageInformation UVMain_LIL_GET_EMIMASK(string textureName)
            {
                var st = $"{textureName}_ST";

                // sampler is sampler_MainTex
                // TODO: consider the following properties
                var scrollRotate = $"{textureName}_ScrollRotate";

                return matInfo.CreateTextureUsageInformation(textureName, uvMain, st);
            }

            void lilCalcDissolveWithOrWithoutNoise(
                // alpha,
                // dissolveAlpha,
                UVChannel uv, // ?
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
                string samp
            )
            {
                information.Add(LIL_SAMPLE_2D_WithST(dissolveMask, samp, uv, dissolveMaskST));
                information.Add(LIL_SAMPLE_2D_WithST(dissolveNoiseMask, samp, uv, dissolveNoiseMaskST));
                // TODO: dissolveNoiseMaskScrollRotate
            }

            information.Add(matInfo.CreateTextureUsageInformationNoScaleOffset("_DitherTex", UVChannel.NonMeshRelated)); // dither UV is based on screen space

            // TODO: _MainTex with POM / PARALLAX (using LIL_SAMPLE_2D_POM)
            information.Add(UVMain_LIL_SAMPLE_2D("_MainTex", "_MainTex")); // main texture
            information.Add(matInfo.CreateTextureUsageInformation("_MainGradationTex", UVChannel.NonMeshRelated)); // GradationMap UV is based on color
            information.Add(UVMain_LIL_SAMPLE_2D("_MainColorAdjustMask", "_MainTex")); // simple LIL_SAMPLE_2D

            if (matInfo.GetInteger("_UseMain2ndTex") != 0)
            {
                // caller of lilGetMain2nd will pass sampler for _MainTex as samp
                var samp = "_MainTex";

                UVChannel uv2nd;
                switch (matInfo.GetInteger("_Main2ndTex_UVMode"))
                {
                    case 0: uv2nd = UVChannel.UV0; break;
                    case 1: uv2nd = UVChannel.UV1; break;
                    case 2: uv2nd = UVChannel.UV2; break;
                    case 3: uv2nd = UVChannel.UV3; break;
                    case 4: uv2nd = UVChannel.NonMeshRelated; break; // MatCap (normal-based UV)
                    default: uv2nd = UVChannel.Unknown; break;
                }
                information.Add(LIL_GET_SUBTEX("_Main2ndTex", uv2nd));
                information.Add(UVMain_LIL_SAMPLE_2D("_Main2ndBlendMask", samp));
                lilCalcDissolveWithOrWithoutNoise(
                    UVChannel.UV0,
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

                UVChannel uv3rd;
                switch (matInfo.GetInteger("_Main2ndTex_UVMode"))
                {
                    case 0: uv3rd = UVChannel.UV0; break;
                    case 1: uv3rd = UVChannel.UV1; break;
                    case 2: uv3rd = UVChannel.UV2; break;
                    case 3: uv3rd = UVChannel.UV3; break;
                    case 4: uv3rd = UVChannel.NonMeshRelated; break; // MatCap (normal-based UV)
                    default: uv3rd = UVChannel.Unknown; break;
                }

                information.Add(LIL_GET_SUBTEX("_Main3rdTex", uv3rd));
                information.Add(UVMain_LIL_SAMPLE_2D("_Main3rdBlendMask", samp));
                lilCalcDissolveWithOrWithoutNoise(
                    UVChannel.UV0,
                    "_Main3rdDissolveMask",
                    "_Main3rdDissolveMask_ST",
                    "_Main3rdDissolveNoiseMask",
                    "_Main3rdDissolveNoiseMask_ST",
                    "_Main3rdDissolveNoiseMask_ScrollRotate",
                    samp
                );
            }

            information.Add(UVMain_LIL_SAMPLE_2D_ST("_AlphaMask", "_MainTex"));
            if (matInfo.GetInteger("_UseBumpMap") != 0)
            {
                information.Add(UVMain_LIL_SAMPLE_2D_ST("_BumpMap", "_MainTex"));
            }

            if (matInfo.GetInteger("_UseBump2ndMap") != 0)
            {
                var uvBump2nd = UVChannel.UV0;

                switch (matInfo.GetInteger("_Bump2ndMap_UVMode"))
                {
                    case 0: uvBump2nd = UVChannel.UV0; break;
                    case 1: uvBump2nd = UVChannel.UV1; break;
                    case 2: uvBump2nd = UVChannel.UV2; break;
                    case 3: uvBump2nd = UVChannel.UV3; break;
                    case null: uvBump2nd = UVChannel.Unknown; break;
                }

                information.Add(LIL_SAMPLE_2D_ST("_Bump2ndMap", lil_sampler_linear_repeat, uvBump2nd));
                information.Add(UVMain_LIL_SAMPLE_2D_ST("_Bump2ndScaleMask", "_MainTex")); // _Bump2ndScaleMask is defined as NoScaleOffset but sampled with LIL_SAMPLE_2D_ST?
            }

            if (matInfo.GetInteger("_UseAnisotropy") != 0)
            {
                information.Add(UVMain_LIL_SAMPLE_2D_ST("_AnisotropyTangentMap", "_MainTex"));
                information.Add(UVMain_LIL_SAMPLE_2D_ST("_AnisotropyScaleMask", "_MainTex"));

                // _AnisotropyShiftNoiseMask is used in another place but under _UseAnisotropy condition
                information.Add(UVMain_LIL_SAMPLE_2D_ST("_AnisotropyShiftNoiseMask", "_MainTex"));
            }

            if (matInfo.GetInteger("_UseBacklight") != 0)
            {
                var samp = "_MainTex";
                information.Add(UVMain_LIL_SAMPLE_2D_ST("_BacklightColorTex", samp));
            }

            if (matInfo.GetInteger("_UseShadow") != 0)
            {
                // TODO: Those sampling are GRAD 
                var samp = "_MainTex";
                information.Add(UVMain_LIL_SAMPLE_2D("_ShadowStrengthMask", lil_sampler_linear_repeat));
                information.Add(UVMain_LIL_SAMPLE_2D("_ShadowBorderMask", lil_sampler_linear_repeat));
                information.Add(UVMain_LIL_SAMPLE_2D("_ShadowBlurMask", lil_sampler_linear_repeat));
                // lilSampleLUT
                switch (matInfo.GetInteger("_ShadowColorType"))
                {
                    case 1:
                        information.Add(matInfo.CreateTextureUsageInformation("_ShadowColorTex", UVChannel.NonMeshRelated));
                        information.Add(matInfo.CreateTextureUsageInformation("_Shadow2ndColorTex", UVChannel.NonMeshRelated));
                        information.Add(matInfo.CreateTextureUsageInformation("_Shadow3rdColorTex", UVChannel.NonMeshRelated));
                        break;
                    case null:
                        information.Add(matInfo.CreateTextureUsageInformation("_ShadowColorTex", UVChannel.Unknown));
                        information.Add(matInfo.CreateTextureUsageInformation("_Shadow2ndColorTex", UVChannel.Unknown));
                        information.Add(matInfo.CreateTextureUsageInformation("_Shadow3rdColorTex", UVChannel.Unknown));
                        break;
                    default:
                        information.Add(UVMain_LIL_SAMPLE_2D("_ShadowColorTex", samp));
                        information.Add(UVMain_LIL_SAMPLE_2D("_Shadow2ndColorTex", samp));
                        information.Add(UVMain_LIL_SAMPLE_2D("_Shadow3rdColorTex", samp));
                        break;
                }
            }

            if (matInfo.GetInteger("_UseRimShade") != 0)
            {
                var samp = "_MainTex";

                information.Add(UVMain_LIL_SAMPLE_2D("_RimShadeMask", samp));
            }

            if (matInfo.GetInteger("_UseReflection") != 0)
            {
                // TODO: research
                var samp = "_MainTex"; // or lil_sampler_linear_repeat in lil_pass_foreward_reblur.hlsl

                information.Add(UVMain_LIL_SAMPLE_2D_ST("_SmoothnessTex", samp));
                information.Add(UVMain_LIL_SAMPLE_2D_ST("_MetallicGlossMap", samp));
                information.Add(UVMain_LIL_SAMPLE_2D_ST("_ReflectionColorTex", samp));
            }

            // Matcap
            if (matInfo.GetInteger("_UseMatCap") != 0)
            {
                var samp = "_MainTex"; // caller of lilGetMatCap

                information.Add(LIL_SAMPLE_2D("_MatCapTex", lil_sampler_linear_repeat, UVChannel.NonMeshRelated));
                information.Add(UVMain_LIL_SAMPLE_2D_ST("_MatCapBlendMask", samp));

                if (matInfo.GetInteger("_MatCapCustomNormal") != 0)
                {
                    information.Add(UVMain_LIL_SAMPLE_2D_ST("_MatCapBumpMap", samp));
                }
            }
            
            if (matInfo.GetInteger("_UseMatCap2nd") != 0)
            {
                var samp = "_MainTex"; // caller of lilGetMatCap

                information.Add(LIL_SAMPLE_2D("_MatCap2ndTex", lil_sampler_linear_repeat, UVChannel.NonMeshRelated));
                information.Add(UVMain_LIL_SAMPLE_2D_ST("_MatCap2ndBlendMask", samp));

                if (matInfo.GetInteger("_MatCap2ndCustomNormal") != 0)
                {
                    information.Add(UVMain_LIL_SAMPLE_2D_ST("_MatCap2ndBumpMap", samp));
                }
            }

            // rim light
            if (matInfo.GetInteger("_UseRim") != 0)
            {
                var samp = "_MainTex"; // caller of lilGetRim
                information.Add(UVMain_LIL_SAMPLE_2D_ST("_RimColorTex", samp));
            }

            if (matInfo.GetInteger("_UseGlitter") != 0)
            {
                var samp = "_MainTex"; // caller of lilGetGlitter

                information.Add(UVMain_LIL_SAMPLE_2D_ST("_GlitterColorTex", samp));
                // complex uv
                information.Add(matInfo.CreateTextureUsageInformation("_GlitterShapeTex", UVChannel.Unknown));
            }
            
            if (matInfo.GetInteger("_UseEmission") != 0)
            {
                var emissionUV = UVChannel.UV0;

                switch (matInfo.GetInteger("_EmissionMap_UVMode"))
                {
                    case 1: emissionUV = UVChannel.UV1; break;
                    case 2: emissionUV = UVChannel.UV2; break;
                    case 3: emissionUV = UVChannel.UV3; break;
                    case 4: emissionUV = UVChannel.NonMeshRelated; break; // uvRim; TODO: check
                    case null: emissionUV = UVChannel.Unknown; break;
                }

                UVChannel _EmissionMapParaTex;
                if (matInfo.GetFloat("_EmissionParallaxDepth") == 0)
                    _EmissionMapParaTex = emissionUV;
                else
                    _EmissionMapParaTex = UVChannel.Unknown; // hard to determine

                // actually LIL_GET_EMITEX is used but same as LIL_SAMPLE_2D_ST
                information.Add(LIL_GET_EMITEX("_EmissionMap", _EmissionMapParaTex));

                // if LIL_FEATURE_ANIMATE_EMISSION_MASK_UV is enabled, UV0 is used and if not UVMain is used.
                var LIL_FEATURE_ANIMATE_EMISSION_MASK_UV = matInfo.GetVector("_EmissionBlendMask_ScrollRotate") != new Vector4(0, 0, 0, 0) || matInfo.GetVector("_Emission2ndBlendMask_ScrollRotate") != new Vector4(0, 0, 0, 0);

                if (LIL_FEATURE_ANIMATE_EMISSION_MASK_UV)
                {
                    information.Add(LIL_GET_EMIMASK("_EmissionBlendMask", UVChannel.UV0));
                }
                else
                {
                    information.Add(UVMain_LIL_GET_EMIMASK("_EmissionBlendMask"));
                }

                if (matInfo.GetInteger("_EmissionUseGrad") != 0)
                {
                    information.Add(LIL_SAMPLE_1D("_EmissionGradTex", lil_sampler_linear_repeat, UVChannel.NonMeshRelated));
                }
            }

            if (matInfo.GetInteger("_Emission2ndMap") != 0)
            {
                var emission2ndUV = UVChannel.UV0;

                switch (matInfo.GetInteger("_Emission2ndMap_UVMode"))
                {
                    case 1: emission2ndUV = UVChannel.UV1; break;
                    case 2: emission2ndUV = UVChannel.UV2; break;
                    case 3: emission2ndUV = UVChannel.UV3; break;
                    case 4: emission2ndUV = UVChannel.NonMeshRelated; break; // uvRim; TODO: check
                    case null: emission2ndUV = UVChannel.Unknown; break;
                }

                UVChannel _Emission2ndMapParaTex;
                if (matInfo.GetFloat("_Emission2ndParallaxDepth") == 0)
                    _Emission2ndMapParaTex = emission2ndUV;
                else
                    _Emission2ndMapParaTex = UVChannel.Unknown; // hard to determine

                // actually LIL_GET_EMITEX is used but same as LIL_SAMPLE_2D_ST
                information.Add(LIL_GET_EMITEX("_Emission2ndMap", _Emission2ndMapParaTex));

                // if LIL_FEATURE_ANIMATE_EMISSION_MASK_UV is enabled, UV0 is used and if not UVMain is used. (weird)
                // https://github.com/lilxyzw/lilToon/blob/b96470d3dd9092b840052578048b2307fe6d8786/Assets/lilToon/Shader/Includes/lil_common_frag.hlsl#L1819-L1821
                var LIL_FEATURE_ANIMATE_EMISSION_MASK_UV = matInfo.GetVector("_EmissionBlendMask_ScrollRotate") != new Vector4(0, 0, 0, 0) || matInfo.GetVector("_Emission2ndBlendMask_ScrollRotate") != new Vector4(0, 0, 0, 0);

                if (LIL_FEATURE_ANIMATE_EMISSION_MASK_UV)
                {
                    information.Add(LIL_GET_EMIMASK("_Emission2ndBlendMask", UVChannel.UV0));
                }
                else
                {
                    information.Add(UVMain_LIL_GET_EMIMASK("_Emission2ndBlendMask"));
                }

                if (matInfo.GetInteger("_Emission2ndUseGrad") != 0)
                {
                    information.Add(LIL_SAMPLE_1D("_Emission2ndGradTex", lil_sampler_linear_repeat, UVChannel.NonMeshRelated));
                }
            }

            if (matInfo.GetInteger("_UseParallax") != 0)
            {
                information.Add(matInfo.CreateTextureUsageInformation("_ParallaxMap", UVChannel.Unknown));
            }

            if (matInfo.GetInteger("_UseAudioLink") != 0 && matInfo.GetInteger("_AudioLink2Vertex") != 0)
            {
                var _AudioLinkUVMode = matInfo.GetInteger("_AudioLinkUVMode");

                if (_AudioLinkUVMode is 3 or 4 or null)
                {
                    // TODO: _AudioLinkMask_ScrollRotate
                    switch (matInfo.GetInteger("_AudioLinkMask_UVMode"))
                    {
                        case 0:
                        default:
                            information.Add(UVMain_LIL_SAMPLE_2D_ST("_AudioLinkMask", "_AudioLinkMask"));
                            information.Add(UVMain_LIL_SAMPLE_2D_ST("_AudioLinkMask", lil_sampler_linear_repeat));
                            break;
                        case 1:
                            information.Add(LIL_SAMPLE_2D_ST("_AudioLinkMask", "_AudioLinkMask", UVChannel.UV1));
                            break;
                        case 2:
                            information.Add(LIL_SAMPLE_2D_ST("_AudioLinkMask", "_AudioLinkMask", UVChannel.UV2));
                            break;
                        case 3:
                            information.Add(LIL_SAMPLE_2D_ST("_AudioLinkMask", "_AudioLinkMask", UVChannel.UV3));
                            break;
                        case null:
                            information.Add(LIL_SAMPLE_2D_ST("_AudioLinkMask", "_AudioLinkMask", UVChannel.Unknown));
                            break;
                    }

                    information.Add(LIL_SAMPLE_2D("_AudioLinkMask", lil_sampler_linear_repeat, UVChannel.Unknown));
                }
            }

            if (matInfo.GetVector("_DissolveParams")?.x != 0)
            {
                lilCalcDissolveWithOrWithoutNoise(
                    //fd.col.a,
                    //dissolveAlpha,
                    UVChannel.UV0,
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

            if (matInfo.GetInteger("_UseOutline") != 0) { // not on material side, on editor side toggle
                information.Add(UVMain_LIL_SAMPLE_2D("_OutlineTex", "_OutlineTex"));
                information.Add(UVMain_LIL_SAMPLE_2D("_OutlineWidthMask", lil_sampler_linear_repeat));
                // _OutlineVectorTex lil_sampler_linear_repeat
                // UVs _OutlineVectorUVMode main,1,2,3
                
                switch (matInfo.GetInteger("_AudioLinkMask_UVMode"))
                {
                    case 0:
                        information.Add(UVMain_LIL_SAMPLE_2D("_OutlineVectorTex", lil_sampler_linear_repeat));
                        break;
                    case 1:
                        information.Add(LIL_SAMPLE_2D("_OutlineVectorTex", lil_sampler_linear_repeat, UVChannel.UV1));
                        break;
                    case 2:
                        information.Add(LIL_SAMPLE_2D("_OutlineVectorTex", lil_sampler_linear_repeat, UVChannel.UV2));
                        break;
                    case 3:
                        information.Add(LIL_SAMPLE_2D("_OutlineVectorTex", lil_sampler_linear_repeat, UVChannel.UV3));
                        break;
                    default:
                    case null:
                        information.Add(LIL_SAMPLE_2D("_OutlineVectorTex", lil_sampler_linear_repeat, UVChannel.Unknown));
                        break;
                }
            }

            // _BaseMap and _BaseColorMap are unused

            return information.ToArray();
        }
    }
}
