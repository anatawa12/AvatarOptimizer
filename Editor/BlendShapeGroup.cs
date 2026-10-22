using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer;

internal static class BlendShapeGroupExtensions
{
    public static void AddBlendShapeGroup(this BuildContext context, string blendShape, IEnumerable<SkinnedMeshRenderer> renderers)
    {
        context.GetState<BlendShapeGroupState>().BlendShapeGroup.Add(
            renderers
                .Select(renderer => context.GetAnimationComponent(renderer).GetProperty($"blendShape.{blendShape}"))
                .ToArray()
        );
    }
}

internal class BlendShapeGroupState
{
    public List<AnimationPropertyInfo<PropertyInfo>[]> BlendShapeGroup { get; } = new();
}
