using System;
using System.Collections.Generic;
using System.Linq;
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
                if (!ValidateController(cloned)) return; // If transition types are invalid, skip processing
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
            
            bool ValidateController(AnimatorController animatorController)
            {
                if (animatorController.parameters.Select(x => x.name).Distinct().Count() != animatorController.parameters.Length)
                {
                    Debug.LogError(
                        $"Animator Controller '{animatorController.name}' has parameters with duplicate names. Animator Optimizer requires unique parameter names to function correctly. Skipping optimization for this controller.",
                        context.AvatarRootObject);
                    return false;
                }

                var parameterTypes = animatorController.parameters.ToDictionary(p => p.name, p => p.type);

                // check types are valid types
                foreach (var (name, type) in parameterTypes)
                {
                    if (type is not (AnimatorControllerParameterType.Float or
                        AnimatorControllerParameterType.Int or
                        AnimatorControllerParameterType.Bool or
                        AnimatorControllerParameterType.Trigger))
                    {
                        Debug.LogError(
                            $"Animator Controller '{animatorController.name}' has a parameter '{name}' with an unsupported type '{type}'. Supported types are Float, Int, Bool, and Trigger. Skipping optimization for this controller.",
                            context.AvatarRootObject);
                        return false;
                    }
                }

                foreach (var layer in animatorController.layers)
                {
                    if (layer.syncedLayerIndex >= 0) continue; // skip synced layers
                    foreach (var transition in ACUtils.AllTransitions(layer.stateMachine))
                    {
                        foreach (var condition in transition.conditions)
                        {
                            if (!parameterTypes.TryGetValue(condition.parameter, out var paramType))
                            {
                                Debug.LogError(
                                    $"Animator Controller '{animatorController.name}' has a transition condition that references a non-existent parameter (hash: {condition.parameter}). Skipping optimization for this controller.",
                                    context.AvatarRootObject);
                                return false;
                            }

                            bool isValid = paramType switch
                            {
                                AnimatorControllerParameterType.Float => condition.IsValidForFloat(),
                                AnimatorControllerParameterType.Int => condition.IsValidForInt(),
                                AnimatorControllerParameterType.Bool => condition.IsValidForBool(),
                                AnimatorControllerParameterType.Trigger => condition.IsValidForTrigger(),
                                _ => throw new ArgumentOutOfRangeException()
                            };

                            if (!isValid)
                            {
                                Debug.LogError(
                                    $"Animator Controller '{animatorController.name}' has a transition condition with an invalid parameter type for the condition mode '{condition.mode}' on parameter '{condition.parameter}'. Skipping optimization for this controller.",
                                    context.AvatarRootObject);
                                return false;
                            }
                        }
                    }
                }

                return true;
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
