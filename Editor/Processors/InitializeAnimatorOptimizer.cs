using System;
using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

#if AAO_VRCSDK3_AVATARS
using VRC.SDKBase;
using VRC.SDK3.Avatars.Components;
#endif

namespace Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer
{
    // This pass prepares animator optimizer
    // This pass does the following things:
    // - Collects all AnimatorController objects and save to state
    // - If the RuntimeAnimatorController is AnimatorOverrideController, convert it to AnimatorController
    // Cloning the AnimatorController is moved to DuplicateAssets pass
    class InitializeAnimatorOptimizer : TraceAndOptimizePass<InitializeAnimatorOptimizer>
    {
        public override string DisplayName => "AnimOpt: Initialize";

        protected override void Execute(BuildContext context, TraceAndOptimizeState state)
        {
            if (!state.OptimizeAnimator) return;

            var animatorState = context.GetState<AnimatorOptimizerState>();

#if AAO_VRCSDK3_AVATARS
            // According to VRCSDK 3.5.0, default animation controllers doesn't have AnimatorLayerWeightControl so
            // we don't have to care about them.
            var changerBehaviours = new AnimatorLayerMap<HashSet<VRC_AnimatorLayerControl>>();
            {
                changerBehaviours[VRCAvatarDescriptor.AnimLayerType.Action] = new HashSet<VRC_AnimatorLayerControl>();
                changerBehaviours[VRCAvatarDescriptor.AnimLayerType.FX] = new HashSet<VRC_AnimatorLayerControl>();
                changerBehaviours[VRCAvatarDescriptor.AnimLayerType.Gesture] = new HashSet<VRC_AnimatorLayerControl>();
                changerBehaviours[VRCAvatarDescriptor.AnimLayerType.Additive] = new HashSet<VRC_AnimatorLayerControl>();
            }
#endif
            var clonedToController = new Dictionary<AnimatorController, AOAnimatorController>();

            foreach (var component in context.AvatarRootObject.GetComponents<Component>())
            {
                switch (component)
                {
                    case Animator animator:
                        ProcessController(animator.runtimeAnimatorController, component.gameObject);
                        break;
#if AAO_VRCSDK3_AVATARS
                    case VRCAvatarDescriptor avatarDescriptor:
                        foreach (ref var layer in avatarDescriptor.baseAnimationLayers.AsSpan())
                            ProcessController(layer.animatorController, component.gameObject);
                        foreach (ref var layer in avatarDescriptor.specialAnimationLayers.AsSpan())
                            ProcessController(layer.animatorController, component.gameObject);
#endif
                        break;
                    // do not run animator optimizer with unknown components
                    default:
                        continue;
                }
            }
            
            void ProcessController(RuntimeAnimatorController? runtimeController,
                GameObject rootGameObject)
            {
                if (runtimeController == null) return;
                var cloned = (AnimatorController)runtimeController;
                var wrapper = new AOAnimatorController(cloned, rootGameObject);
                animatorState.Add(wrapper);
                if (!clonedToController.TryAdd(cloned, wrapper)) return;

#if AAO_VRCSDK3_AVATARS
                foreach (var behaviour in ACUtils.StateMachineBehaviours(cloned))
                {
                    switch (behaviour)
                    {
                        case VRC_AnimatorLayerControl control:
                            if (control.playable.ToAnimLayerType() is VRCAvatarDescriptor.AnimLayerType l)
                                changerBehaviours[l].Add(control);
                            break;
                    }
                }
#endif
            }
            
#if AAO_VRCSDK3_AVATARS
            {
                var descriptor = context.AvatarDescriptor;
                if (descriptor && descriptor.customizeAnimationLayers)
                {
                    foreach (var playableLayer in descriptor.baseAnimationLayers)
                    {
                        if (playableLayer.isDefault || !playableLayer.animatorController ||
                            changerBehaviours[playableLayer.type] == null) continue;

                        var wrapper = clonedToController[(AnimatorController)playableLayer.animatorController];

                        foreach (var control in changerBehaviours[playableLayer.type])
                        {
                            if (control.layer < 0 || wrapper.layers.Length <= control.layer) continue;

                            var ourChange =
                                AnimatorWeightChanges.ForDurationAndWeight(control.blendDuration, control.goalWeight);

                            var layer = wrapper.layers[control.layer];

                            layer.WeightChange = layer.WeightChange.Merge(ourChange);
                            layer.LayerIndexUpdated += index => control.layer = index;
                        }

                        // process MMD world compatibility
                        if (playableLayer.type == VRCAvatarDescriptor.AnimLayerType.FX && state.MmdWorldCompatibility)
                        {
                            for (var i = 1; i <= 2; i++)
                            {
                                if (wrapper.layers.Length > i)
                                {
                                    wrapper.layers[i].MarkUnRemovable();
                                    wrapper.layers[i].WeightChange = wrapper.layers[i].WeightChange
                                        .Merge(AnimatorWeightChange.NonZeroOneChange);
                                }
                            }
                        }
                    }
                }
            }
#endif
        }
    }
}
