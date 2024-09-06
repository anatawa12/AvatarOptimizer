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

        public interface IMaterialPropertyAnimationProvider
        {
            bool IsAnimated(string propertyName);
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

            public TextureUsageInformation(string materialPropertyName, UVChannel uvChannel)
            {
                MaterialPropertyName = materialPropertyName;
                UVChannel = uvChannel;
            }
        }

        // TODO: define return type
        /// <summary>
        /// Returns texture usage information for the material.
        /// </summary>
        /// <param name="material"></param>
        /// <returns>null if the shader is not supported</returns>
        public static TextureUsageInformation[]? GetTextureUsageInformationForMaterial(Material material, IMaterialPropertyAnimationProvider animation)
        {
            if (AssetDatabase.GetAssetPath(material.shader).StartsWith("Packages/jp.lilxyzw.liltoon"))
            {
                // it looks liltoon!
                return GetTextureUsageInformationForMaterialLiltoon(material, animation);
            }

            return null;
        }

        private static TextureUsageInformation[]? GetTextureUsageInformationForMaterialLiltoon(Material material, IMaterialPropertyAnimationProvider animation)
        {
            // This implementation is made for my Anon + Wahuku for testing this feature.
            // TODO: version check
            var information = new List<TextureUsageInformation>();

            var uvMain = UVChannel.UV0;

            // TODO: UV Animation, angle (_MainTex_ScrollRotate) , Tilting, Offsets (_ST) for MainTex
            information.Add(new TextureUsageInformation("_DitherTex", UVChannel.NonMeshRelated));
            information.Add(new TextureUsageInformation("_MainTex", uvMain));
            information.Add(new TextureUsageInformation("_MainColorAdjustMask", uvMain));
            information.Add(new TextureUsageInformation("_MainGradationTex", uvMain));

            if (material.GetInt("_UseMain2ndTex") != 0 || animation.IsAnimated("_UseMain2ndTex"))
            {
                UVChannel main2ndUV;
                if (animation.IsAnimated("_Main2ndTex_UVMode"))
                {
                    main2ndUV = UVChannel.Unknown;
                }
                else 
                {
                    switch (material.GetInt("_Main2ndTex_UVMode"))
                    {
                        case 0: main2ndUV = UVChannel.UV0; break;
                        case 1: main2ndUV = UVChannel.UV1; break;
                        case 2: main2ndUV = UVChannel.UV2; break;
                        case 3: main2ndUV = UVChannel.UV3; break;
                        case 4: main2ndUV = UVChannel.NonMeshRelated; break;
                        default: main2ndUV = UVChannel.Unknown; break;
                    }
                }
                information.Add(new TextureUsageInformation("_Main2ndTex", main2ndUV));
                // TODO: UV Animation, angle (_MainTex2_ScrollRotate) , Tilting, Offsets (_ST) for MainTex2
                information.Add(new TextureUsageInformation("_Main2ndBlendMask", main2ndUV)); // NO ScaleOffset
                information.Add(new TextureUsageInformation("_Main2ndDissolveMask", main2ndUV));
                information.Add(new TextureUsageInformation("_Main2ndDissolveNoiseMask", main2ndUV));
                // TODO: isDecurl for MainTex2?
            }

            // TODO: Main3rd

            // Matcap
            if (material.GetInt("_UseMatCap") != 0 || animation.IsAnimated("_UseMatCap"))
            {
                information.Add(new TextureUsageInformation("_MatCapTex", UVChannel.NonMeshRelated));
                information.Add(new TextureUsageInformation("_MatCapBlendMask", UVChannel.UV0)); // No ScaleOffset
                // TODO: more properties like: _MatCapBumpMap
            }

            // TODO: Matcap2nd

            // rim light
            if (material.GetInt("_UseRim") != 0 || animation.IsAnimated("_UseRim"))
            {
                information.Add(new TextureUsageInformation("_RimColorTex", uvMain));
            }

            // outline
            if (material.GetInt("_UseOutline") != 0 || animation.IsAnimated("_UseOutline"))
            {
                information.Add(new TextureUsageInformation("_OutlineColorTex", uvMain));
                information.Add(new TextureUsageInformation("_OutlineWidthMask", uvMain)); // ??
            }

            // TODO: Many Properties
            return information.ToArray();
        }
    }
}
