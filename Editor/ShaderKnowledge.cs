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
            /// Registers UV Usage that are not considered by Avatar Optimizer.
            ///
            /// This will the UV Channel not affected by optimizations of Avatar Optimizer.
            /// </summary>
            /// <param name="uvChannel">The UVChannels that are used in the shader.</param>
            public abstract void RegisterOtherUVUsage(UsingUVChannels uvChannel);

            /// <summary>
            /// Registers Texture Usage and UV Usage that are considered by Avatar Optimizer.
            /// 
            /// The texture might go to the atlas / UV Packing if the UsingUVChannels is set and the UV Matrix is known
            /// </summary>
            /// <param name="textureMaterialPropertyName">The name of the texture property in the material.</param>
            /// <param name="samplerState">The information about the sampler state used for the specified texture.</param>
            /// <param name="uvChannels">The UVChannels that are used in the shader to determine the UV for the texture.</param>
            /// <param name="uvMatrix">The UV Transform Matrix for the texture. This includes textureName_ST scale offset. Null if the UV transfrom is not known.</param>
            /// <remarks>
            /// This section describes the current and planned implementation of UV Packing in the Avatar Optimizer about this function.
            /// 
            /// Currently, Avatar Optimizer does UV Packing if (non-exclusive):
            /// - Texture is reasonably used by small set of materials
            /// - UsingUVChannels is set to only one of UV Channels (per material)
            /// - UV Matrix is known and identity matrix
            /// 
            /// However, Avatar Optimizer will support more complex UV Packing in the future:
            /// - Support UV Matrix with scale is smaller and rotation is multiple of 90 degrees
            /// - multiple UV Channel texture
            /// </remarks>
            public abstract void RegisterTextureUVUsage(
                string textureMaterialPropertyName,
                SamplerStateInformation samplerState,
                UsingUVChannels uvChannels,
                UnityEngine.Matrix4x4? uvMatrix);
        }

        /// <summary>
        /// The information about the sampler state for the specified texture.
        ///
        /// You can combine multiple SamplerStateInformation for the texture with `|` operator.
        ///
        /// You can cast string to <c>SamplerStateInformation</c> to use the sampler state for
        /// the specified texture like <c>sampler_MainTex</c> by <c>(SamplerStateInformation)"_MainTex"</c>.
        ///
        /// If your shader is using hardcoded sampler state, you can use the predefined sampler state like
        /// <see cref="SamplerStateInformation.PointClampSampler"/> or <see cref="SamplerStateInformation.LinearRepeatSampler"/>.
        /// </summary>
        internal readonly struct SamplerStateInformation
        {
            private readonly string _textureName;
            private readonly bool _materialProperty;

            public SamplerStateInformation(string textureName)
            {
                _textureName = textureName;
                _materialProperty = true;
            }

            // construct builtin non-material property sampler state
            private SamplerStateInformation(string textureName, bool dummy)
            {
                _textureName = textureName;
                _materialProperty = false;
            }

            // I don't want to expose equals to public API so I made this internal function instead of overriding Equals
            internal static bool EQ(SamplerStateInformation left, SamplerStateInformation right)
            {
                if (left._materialProperty != right._materialProperty) return false;
                if (left._textureName != right._textureName) return false;
                return true;
            }

            public static readonly SamplerStateInformation Unknown = new("Unknown", false);
            public static readonly SamplerStateInformation PointClampSampler = new("PointClamp", false);
            public static readonly SamplerStateInformation PointRepeatSampler = new("PointRepeat", false);
            public static readonly SamplerStateInformation PointMirrorSampler = new("PointMirror", false);
            public static readonly SamplerStateInformation PointMirrorOnceSampler = new("PointMirrorOnce", false);
            public static readonly SamplerStateInformation LinearClampSampler = new("LinearClamp", false);
            public static readonly SamplerStateInformation LinearRepeatSampler = new("LinearRepeat", false);
            public static readonly SamplerStateInformation LinearMirrorSampler = new("LinearMirror", false);
            public static readonly SamplerStateInformation LinearMirrorOnceSampler = new("LinearMirrorOnce", false);
            public static readonly SamplerStateInformation TrilinearClampSampler = new("TrilinearClamp", false);
            public static readonly SamplerStateInformation TrilinearRepeatSampler = new("TrilinearRepeat", false);
            public static readonly SamplerStateInformation TrilinearMirrorSampler = new("TrilinearMirror", false);
            public static readonly SamplerStateInformation TrilinearMirrorOnceSampler = new("TrilinearMirrorOnce", false);

            public static implicit operator SamplerStateInformation(string textureName) => new(textureName);
            public static SamplerStateInformation operator|(SamplerStateInformation left, SamplerStateInformation right) =>
                Combine(left, right);

            private static SamplerStateInformation Combine(SamplerStateInformation left, SamplerStateInformation right)
            {
                // we may implement better logic in the future
                if (EQ(left, right)) return left;
                return Unknown;
            }
        }

        /// <summary>
        /// The flags to express which UV Channels might be used in the shader.
        ///
        /// Usage of the UV channels might be specified with some other APIs.
        /// </summary>
        [Flags]
        public enum UsingUVChannels
        {
            UV0 = 1,
            UV1 = 2,
            UV2 = 4,
            UV3 = 8,
            UV4 = 16,
            UV5 = 32,
            UV6 = 64,
            UV7 = 128,
            NonMesh = 256,
            Unknown = 0x7FFFFFFF,
        }

        /// <summary>
        /// Returns texture usage information for the material.
        /// </summary>
        /// <returns>null if the shader is not supported</returns>
        public static bool GetTextureUsageInformationForMaterial(TextureUsageInformationCallback information)
        {
            if (AssetDatabase.GetAssetPath(information.Shader).StartsWith("Packages/jp.lilxyzw.liltoon"))
            {
                // it looks liltoon!
                return GetTextureUsageInformationForMaterialLiltoon(information);
            }

            return false;
        }

        private static bool GetTextureUsageInformationForMaterialLiltoon(TextureUsageInformationCallback matInfo)
        {
            // This implementation is made for my Anon + Wahuku for testing this feature.
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

            matInfo.RegisterTextureUVUsage("_DitherTex", SamplerStateInformation.LinearRepeatSampler, UsingUVChannels.NonMesh, null); // dither UV is based on screen space

            // TODO: _MainTex with POM / PARALLAX (using LIL_SAMPLE_2D_POM)
            LIL_SAMPLE_2D_WithMat("_MainTex", "_MainTex", uvMain, uvMainMatrix); // main texture
            matInfo.RegisterTextureUVUsage("_MainGradationTex", SamplerStateInformation.LinearClampSampler, UsingUVChannels.NonMesh, null); // GradationMap UV is based on color
            LIL_SAMPLE_2D_WithMat("_MainColorAdjustMask", "_MainTex", uvMain, uvMainMatrix); // simple LIL_SAMPLE_2D

            if (matInfo.GetInteger("_UseMain2ndTex") != 0)
            {
                // caller of lilGetMain2nd will pass sampler for _MainTex as samp
                SamplerStateInformation samp = "_MainTex";

                UsingUVChannels uv2nd;
                switch (matInfo.GetInteger("_Main2ndTex_UVMode"))
                {
                    case 0: uv2nd = UsingUVChannels.UV0; break;
                    case 1: uv2nd = UsingUVChannels.UV1; break;
                    case 2: uv2nd = UsingUVChannels.UV2; break;
                    case 3: uv2nd = UsingUVChannels.UV3; break;
                    case 4: uv2nd = UsingUVChannels.NonMesh; break; // MatCap (normal-based UV)
                    default: uv2nd = UsingUVChannels.UV0 | UsingUVChannels.UV1 | UsingUVChannels.UV2 | UsingUVChannels.UV3; break;
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
                    case 0: uv3rd = UsingUVChannels.UV0; break;
                    case 1: uv3rd = UsingUVChannels.UV1; break;
                    case 2: uv3rd = UsingUVChannels.UV2; break;
                    case 3: uv3rd = UsingUVChannels.UV3; break;
                    case 4: uv3rd = UsingUVChannels.NonMesh; break; // MatCap (normal-based UV)
                    default: uv3rd = UsingUVChannels.UV0 | UsingUVChannels.UV1 | UsingUVChannels.UV2 | UsingUVChannels.UV3; break;
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
                    case 0: uvBump2nd = UsingUVChannels.UV0; break;
                    case 1: uvBump2nd = UsingUVChannels.UV1; break;
                    case 2: uvBump2nd = UsingUVChannels.UV2; break;
                    case 3: uvBump2nd = UsingUVChannels.UV3; break;
                    case null: uvBump2nd = UsingUVChannels.UV0 | UsingUVChannels.UV1 | UsingUVChannels.UV2 | UsingUVChannels.UV3; break;
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
                LIL_SAMPLE_2D_GRAD_WithMat("_ShadowStrengthMask", SamplerStateInformation.LinearRepeatSampler, uvMain, uvMainMatrix);
                LIL_SAMPLE_2D_GRAD_WithMat("_ShadowBorderMask", SamplerStateInformation.LinearRepeatSampler, uvMain, uvMainMatrix);
                LIL_SAMPLE_2D_GRAD_WithMat("_ShadowBlurMask", SamplerStateInformation.LinearRepeatSampler, uvMain, uvMainMatrix);
                // lilSampleLUT
                switch (matInfo.GetInteger("_ShadowColorType"))
                {
                    case 1:
                        LIL_SAMPLE_2D_WithMat("_ShadowColorTex", SamplerStateInformation.LinearClampSampler, UsingUVChannels.NonMesh, null);
                        LIL_SAMPLE_2D_WithMat("_Shadow2ndColorTex", SamplerStateInformation.LinearClampSampler, UsingUVChannels.NonMesh, null);
                        LIL_SAMPLE_2D_WithMat("_Shadow3rdColorTex", SamplerStateInformation.LinearClampSampler, UsingUVChannels.NonMesh, null);
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
                    case 1: emissionUV = UsingUVChannels.UV1; break;
                    case 2: emissionUV = UsingUVChannels.UV2; break;
                    case 3: emissionUV = UsingUVChannels.UV3; break;
                    case 4: emissionUV = UsingUVChannels.NonMesh; break; // uvRim; TODO: check
                    case null: emissionUV = UsingUVChannels.UV0 | UsingUVChannels.UV1 | UsingUVChannels.UV2 | UsingUVChannels.UV3 | UsingUVChannels.NonMesh; break;
                }

                var parallaxEnabled = matInfo.GetFloat("_EmissionParallaxDepth") != 0;

                LIL_GET_EMITEX("_EmissionMap", emissionUV, parallaxEnabled);

                // if LIL_FEATURE_ANIMATE_EMISSION_MASK_UV is enabled, UV0 is used and if not UVMain is used.
                var LIL_FEATURE_ANIMATE_EMISSION_MASK_UV = matInfo.GetVector("_EmissionBlendMask_ScrollRotate") != new Vector4(0, 0, 0, 0) || matInfo.GetVector("_Emission2ndBlendMask_ScrollRotate") != new Vector4(0, 0, 0, 0);

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
                    case 1: emission2ndUV = UsingUVChannels.UV1; break;
                    case 2: emission2ndUV = UsingUVChannels.UV2; break;
                    case 3: emission2ndUV = UsingUVChannels.UV3; break;
                    case 4: emission2ndUV = UsingUVChannels.NonMesh; break; // uvRim; TODO: check
                    case null: emission2ndUV = UsingUVChannels.UV0 | UsingUVChannels.UV1 | UsingUVChannels.UV2 | UsingUVChannels.UV3 | UsingUVChannels.NonMesh; break;
                }

                var parallaxEnabled = matInfo.GetFloat("_Emission2ndParallaxDepth") != 0;

                // actually LIL_GET_EMITEX is used but same as LIL_SAMPLE_2D_ST
                LIL_GET_EMITEX("_Emission2ndMap", emission2ndUV, parallaxEnabled);

                // if LIL_FEATURE_ANIMATE_EMISSION_MASK_UV is enabled, UV0 is used and if not UVMain is used. (weird)
                // https://github.com/lilxyzw/lilToon/blob/b96470d3dd9092b840052578048b2307fe6d8786/Assets/lilToon/Shader/Includes/lil_common_frag.hlsl#L1819-L1821
                var LIL_FEATURE_ANIMATE_EMISSION_MASK_UV = matInfo.GetVector("_EmissionBlendMask_ScrollRotate") != new Vector4(0, 0, 0, 0) || matInfo.GetVector("_Emission2ndBlendMask_ScrollRotate") != new Vector4(0, 0, 0, 0);

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
                    LIL_SAMPLE_1D("_Emission2ndGradTex", SamplerStateInformation.LinearRepeatSampler, UsingUVChannels.NonMesh);
                }
            }

            if (matInfo.GetInteger("_UseParallax") != 0)
            {
                matInfo.RegisterTextureUVUsage("_ParallaxMap", SamplerStateInformation.LinearRepeatSampler, UsingUVChannels.UV0, null);
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

            if (matInfo.GetInteger("_UseOutline") != 0) { // not on material side, on editor side toggle
                LIL_SAMPLE_2D_WithMat("_OutlineTex", "_OutlineTex", uvMain, uvMainMatrix);
                LIL_SAMPLE_2D_WithMat("_OutlineWidthMask", SamplerStateInformation.LinearRepeatSampler, uvMain, uvMainMatrix);
                // _OutlineVectorTex SamplerStateInformation.LinearRepeatSampler
                // UVs _OutlineVectorUVMode main,1,2,3
                
                switch (matInfo.GetInteger("_AudioLinkMask_UVMode"))
                {
                    case 0:
                        LIL_SAMPLE_2D_WithMat("_OutlineVectorTex", SamplerStateInformation.LinearRepeatSampler, uvMain, uvMainMatrix);
                        break;
                    case 1:
                        LIL_SAMPLE_2D("_OutlineVectorTex", SamplerStateInformation.LinearRepeatSampler, UsingUVChannels.UV1);
                        break;
                    case 2:
                        LIL_SAMPLE_2D("_OutlineVectorTex", SamplerStateInformation.LinearRepeatSampler, UsingUVChannels.UV2);
                        break;
                    case 3:
                        LIL_SAMPLE_2D("_OutlineVectorTex", SamplerStateInformation.LinearRepeatSampler, UsingUVChannels.UV3);
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

            void LIL_SAMPLE_2D_WithMat(string textureName, SamplerStateInformation samplerName, UsingUVChannels uvChannel, UnityEngine.Matrix4x4? matrix)
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

            void LIL_SAMPLE_2D_GRAD_WithMat(string textureName, SamplerStateInformation samplerName, UsingUVChannels uvChannel, UnityEngine.Matrix4x4? matrix)
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

            void LIL_SAMPLE_2D_ST_WithMat(string textureName, SamplerStateInformation samplerName, UsingUVChannels uvChannel, UnityEngine.Matrix4x4? matrix)
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
                LIL_SAMPLE_2D_WithMat(dissolveNoiseMask, samp, uv, STAndScrollRotateToMatrix(dissolveNoiseMaskST, dissolveNoiseMaskScrollRotate));
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
                STAndScrollRotateValueToMatrix(matInfo.GetVector(stPropertyName), matInfo.GetVector(scrollRotatePropertyName));

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
}
