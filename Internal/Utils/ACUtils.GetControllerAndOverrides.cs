using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    static partial class ACUtils
    {
        public static (AnimatorController, IReadOnlyDictionary<AnimationClip, AnimationClip>) GetControllerAndOverrides(
            RuntimeAnimatorController runtimeController)
        {
            if (runtimeController == null) throw new ArgumentNullException(nameof(runtimeController));
            if (runtimeController is AnimatorController originalController)
                return (originalController, Utils.EmptyDictionary<AnimationClip, AnimationClip>());

            var overrides = new Dictionary<AnimationClip, AnimationClip>();
            var overridesBuffer = new List<KeyValuePair<AnimationClip, AnimationClip>>();

            for (;;)
            {
                if (runtimeController is AnimatorController controller)
                    return (controller, overrides);

                var overrideController = (AnimatorOverrideController)runtimeController;

                runtimeController = overrideController.runtimeAnimatorController;
                overrideController.GetOverrides(overridesBuffer);
                overridesBuffer.RemoveAll(x => !x.Value);

                var currentOverrides = overridesBuffer
                    .GroupBy(kvp => kvp.Value, kvp => kvp.Key)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var upperMappedFrom in overrides.Keys.ToArray())
                    if (currentOverrides.TryGetValue(upperMappedFrom, out var currentMappedFrom))
                        foreach (var mappedFrom in currentMappedFrom)
                            overrides[mappedFrom] = overrides[upperMappedFrom];

                foreach (var (original, mapped) in overridesBuffer)
                    overrides.TryAdd(original, mapped);
            }
        }
    }
}
