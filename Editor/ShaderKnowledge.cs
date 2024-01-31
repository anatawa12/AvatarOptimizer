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

            return true;
        }
    }
}
