using System.Linq;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
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
    }
}
