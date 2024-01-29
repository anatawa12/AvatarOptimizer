#if AAO_VRCSDK3_AVATARS

using nadena.dev.ndmf;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace Anatawa12.AvatarOptimizer
{
    static partial class ACUtils
    {
        public static void CollectWeightChangesInController(RuntimeAnimatorController runtimeController,
            AnimatorLayerMap<AnimatorWeightChange> playableWeightChanged,
            AnimatorLayerMap<AnimatorWeightChangesList> animatorLayerWeightChanged)
        {
            if (runtimeController == null) return;
            using (ErrorReport.WithContextObject(runtimeController))
            {
                foreach (var behaviour in StateMachineBehaviours(runtimeController))
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

        private static void AddPlayableLayerChanges(VRC_PlayableLayerControl control, AnimatorLayerMap<AnimatorWeightChange> playableWeightChanged)
        {
            VRCAvatarDescriptor.AnimLayerType layer;
            switch (control.layer)
            {
                case VRC_PlayableLayerControl.BlendableLayer.Action:
                    layer = VRCAvatarDescriptor.AnimLayerType.Action;
                    break;
                case VRC_PlayableLayerControl.BlendableLayer.FX:
                    layer = VRCAvatarDescriptor.AnimLayerType.FX;
                    break;
                case VRC_PlayableLayerControl.BlendableLayer.Gesture:
                    layer = VRCAvatarDescriptor.AnimLayerType.Gesture;
                    break;
                case VRC_PlayableLayerControl.BlendableLayer.Additive:
                    layer = VRCAvatarDescriptor.AnimLayerType.Additive;
                    break;
                default:
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
        
        private static void AddAnimatorLayerChanges(VRC_AnimatorLayerControl control, AnimatorLayerMap<AnimatorWeightChangesList> animatorLayerWeightChanged)
        {
            VRCAvatarDescriptor.AnimLayerType layer;
            switch (control.playable)
            {
                case VRC_AnimatorLayerControl.BlendableLayer.Action:
                    layer = VRCAvatarDescriptor.AnimLayerType.Action;
                    break;
                case VRC_AnimatorLayerControl.BlendableLayer.FX:
                    layer = VRCAvatarDescriptor.AnimLayerType.FX;
                    break;
                case VRC_AnimatorLayerControl.BlendableLayer.Gesture:
                    layer = VRCAvatarDescriptor.AnimLayerType.Gesture;
                    break;
                case VRC_AnimatorLayerControl.BlendableLayer.Additive:
                    layer = VRCAvatarDescriptor.AnimLayerType.Additive;
                    break;
                default:
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
