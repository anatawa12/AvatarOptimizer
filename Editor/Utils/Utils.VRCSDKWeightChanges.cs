#if AAO_VRCSDK3_AVATARS

using nadena.dev.ndmf;
using UnityEngine;
using VRC.SDKBase;

namespace Anatawa12.AvatarOptimizer
{
    partial class VRCSDKUtils
    {
        public static void CollectWeightChangesInController(RuntimeAnimatorController? runtimeController,
            AnimatorLayerMap<AnimatorWeightChange> playableWeightChanged,
            AnimatorLayerMap<AnimatorWeightChangesList> animatorLayerWeightChanged)
        {
            if (runtimeController == null) return;
            using (ErrorReport.WithContextObject(runtimeController))
            {
                foreach (var behaviour in ACUtils.StateMachineBehaviours(runtimeController))
                {
                    switch (behaviour)
                    {
                        case VRC_PlayableLayerControl playableLayerControl:
                            AddPlayableLayerChanges(playableLayerControl, playableWeightChanged);
                            break;
                        case VRC_AnimatorLayerControl animatorLayerControl:
                            AddAnimatorLayerChanges(animatorLayerControl, animatorLayerWeightChanged);
                            break;
                    }
                }
            }
        }

        private static void AddPlayableLayerChanges(VRC_PlayableLayerControl control, 
            AnimatorLayerMap<AnimatorWeightChange> playableWeightChanged)
        {
            if (control.layer.ToAnimLayerType() is not { } layer)
            {
                BuildLog.LogWarning(
                    "AnimatorParser:PlayableLayerControl:UnknownBlendablePlayableLayer",
                    $"{control.layer}",
                    control);
                return;
            }

            var current = AnimatorWeightChanges.ForDurationAndWeight(control.blendDuration,
                control.goalWeight);
            playableWeightChanged[layer] = playableWeightChanged[layer].Merge(current);
        }

        private static void AddAnimatorLayerChanges(VRC_AnimatorLayerControl control,
            AnimatorLayerMap<AnimatorWeightChangesList> animatorLayerWeightChanged)
        {
            if (control.playable.ToAnimLayerType() is not { } layer)
            {
                BuildLog.LogWarning(
                    "AnimatorParser:AnimatorLayerControl:UnknownBlendablePlayableLayer",
                    $"{control.layer}",
                    control);
                return;
            }

            var current = AnimatorWeightChanges.ForDurationAndWeight(control.blendDuration,
                control.goalWeight);
            var changesForPlayableLayer = animatorLayerWeightChanged[layer];
            changesForPlayableLayer[control.layer] =
                changesForPlayableLayer[control.layer].Merge(current);
        }
    }
}

#endif
