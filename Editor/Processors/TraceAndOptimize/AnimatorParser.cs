using System;
using System.Collections.Generic;
using System.Linq;
using static Anatawa12.AvatarOptimizer.ErrorReporting.BuildReport;
using JetBrains.Annotations;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    class AnimatorParser
    {
        private bool mmdWorldCompatibility;
        private bool advancedAnimatorParser;
        private AnimationParser _animationParser = new AnimationParser();

        public AnimatorParser(bool mmdWorldCompatibility, bool advancedAnimatorParser)
        {
            this.mmdWorldCompatibility = mmdWorldCompatibility;
            this.advancedAnimatorParser = advancedAnimatorParser;
        }

        public AnimatorParser(TraceAndOptimize config)
        {
            mmdWorldCompatibility = config.mmdWorldCompatibility;
            advancedAnimatorParser = config.advancedAnimatorParser;
        }

        public ImmutableModificationsContainer GatherAnimationModifications(OptimizerSession session)
        {
            var modificationsContainer = new ModificationsContainer();
            modificationsContainer.MergeAsNewLayer(CollectAvatarRootAnimatorModifications(session), 
                weightState: AnimatorWeightState.AlwaysOne);

            foreach (var child in session.GetRootComponent<Transform>().DirectChildrenEnumerable())
                WalkForAnimator(child, true, modificationsContainer);

            OtherMutateComponents(modificationsContainer, session);

            return modificationsContainer.ToImmutable();
        }

        private void WalkForAnimator(Transform transform, bool parentObjectAlwaysActive,
            ModificationsContainer modificationsContainer)
        {
            var gameObject = transform.gameObject;
            bool objectAlwaysActive;
            switch (modificationsContainer.GetConstantValue(gameObject, "m_IsActive", gameObject.activeSelf))
            {
                case null:
                    objectAlwaysActive = false;
                    break;
                default: // true
                    objectAlwaysActive = parentObjectAlwaysActive;
                    break;
                case false:
                    // this object is disabled so animator(s) in this component will never activated.
                    return;
            }

            var animator = transform.GetComponent<Animator>();
            if (animator && animator.runtimeAnimatorController)
            {
                ParseAnimationOrAnimator(animator, objectAlwaysActive, modificationsContainer,
                    () => AddHumanoidModifications(
                        ParseAnimatorController(gameObject, animator.runtimeAnimatorController), animator));
            }

            var animation = transform.GetComponent<Animation>();
            if (animation && animation.playAutomatically && animation.clip)
            {
                // We can animate `Animate Physics`, `Play Automatically` and `Enabled` of Animation component.
                // However, animating `Play Automatically` will have no effect (initial state will be used)
                // so if `Play Automatically` is disabled, we can do nothing with Animation component.
                // That's why we ignore Animation component if playAutomatically is false.

                ParseAnimationOrAnimator(animation, objectAlwaysActive, modificationsContainer,
                    () => _animationParser.GetParsedAnimation(gameObject, animation.clip));
            }

            foreach (var child in transform.DirectChildrenEnumerable())
                WalkForAnimator(child, parentObjectAlwaysActive: objectAlwaysActive, modificationsContainer);
        }

        private void ParseAnimationOrAnimator(
            Behaviour animator,
            bool objectAlwaysActive,
            ModificationsContainer modificationsContainer,
            Func<IModificationsContainer> parseComponent)
        {
            bool alwaysApplied;
            switch (modificationsContainer.GetConstantValue(animator, "m_Enabled", animator.enabled))
            {
                case null:
                    alwaysApplied = false;
                    break;
                default: // true
                    alwaysApplied = objectAlwaysActive;
                    break;
                case false:
                    // this component is disabled so animator(s) in this component will never activated.
                    return;
            }

            var parsed = parseComponent();

            if (alwaysApplied)
            {
                switch (parsed.GetConstantValue(animator, "m_Enabled", animator.enabled))
                {
                    case null:
                    case false:
                        alwaysApplied = false;
                        break;
                    case true:
                        break;
                }
            }

            modificationsContainer.MergeAsNewLayer(parsed,
                weightState: AnimatorLayerWeightStates.ForAlwaysApplied(alwaysApplied));
        }

        #region OtherComponents

        /// <summary>
        /// Collect modifications by non-animation changes. For example, constraints, PhysBones, and else 
        /// </summary>
        private void OtherMutateComponents(ModificationsContainer mod, OptimizerSession session)
        {
            ReportingObjects(session.GetComponents<Component>(), component =>
            {
                switch (component)
                {
                    case VRCPhysBoneBase pb:
                        foreach (var transform in pb.GetAffectedTransforms())
                        {
                            var updater = mod.ModifyObject(transform);
                            foreach (var prop in TransformPositionAnimationKeys.Concat(TransformRotationAnimationKeys))
                                updater.AddModificationAsNewLayer(prop, AnimationProperty.Variable());
                        }

                        break;
                    case Rigidbody _:
                    case ParentConstraint _:
                    {
                        var updater = mod.ModifyObject(component.transform);
                        foreach (var prop in TransformPositionAnimationKeys.Concat(TransformRotationAnimationKeys))
                            updater.AddModificationAsNewLayer(prop, AnimationProperty.Variable());
                        break;
                    }
                    case AimConstraint _:
                    case LookAtConstraint _:
                    case RotationConstraint _:
                    {
                        var updater = mod.ModifyObject(component.transform);
                        foreach (var prop in TransformRotationAnimationKeys)
                            updater.AddModificationAsNewLayer(prop, AnimationProperty.Variable());
                        break;
                    }
                    case PositionConstraint _:
                    {
                        var updater = mod.ModifyObject(component.transform);
                        foreach (var prop in TransformPositionAnimationKeys)
                            updater.AddModificationAsNewLayer(prop, AnimationProperty.Variable());
                        break;
                    }
                    case ScaleConstraint _:
                    {
                        var updater = mod.ModifyObject(component.transform);
                        foreach (var prop in TransformScaleAnimationKeys)
                            updater.AddModificationAsNewLayer(prop, AnimationProperty.Variable());
                        break;
                    }
                    case RemoveMeshByBlendShape removeMesh:
                    {
                        var blendShapes = removeMesh.RemovingShapeKeys;
                        {
                            var updater = mod.ModifyObject(removeMesh.GetComponent<SkinnedMeshRenderer>());
                            foreach (var blendShape in blendShapes)
                                updater.AddModificationAsNewLayer($"blendShape.{blendShape}",
                                    AnimationProperty.Variable());
                        }

                        DeriveMergeSkinnedMeshProperties(removeMesh.GetComponent<MergeSkinnedMesh>());

                        void DeriveMergeSkinnedMeshProperties(MergeSkinnedMesh mergeSkinnedMesh)
                        {
                            if (mergeSkinnedMesh == null) return;

                            foreach (var renderer in mergeSkinnedMesh.renderersSet.GetAsSet())
                            {
                                var updater = mod.ModifyObject(renderer);
                                foreach (var blendShape in blendShapes)
                                    updater.AddModificationAsNewLayer($"blendShape.{blendShape}",
                                        AnimationProperty.Variable());

                                DeriveMergeSkinnedMeshProperties(renderer.GetComponent<MergeSkinnedMesh>());
                            }
                        }

                        break;
                    }
                    default:
                    {
                        if (DynamicBone.TryCast(component, out var dynamicBone))
                        {
                            // DynamicBone : similar to PhysBone
                            foreach (var transform in dynamicBone.GetAffectedTransforms())
                            {
                                var updater = mod.ModifyObject(transform);
                                foreach (var prop in TransformRotationAnimationKeys)
                                    updater.AddModificationAsNewLayer(prop, AnimationProperty.Variable());
                            }
                        }
                        break;
                    }
                    // TODO: FinalIK
                }
            });
        }

        #endregion

        #region AvatarDescriptor

        private IModificationsContainer CollectAvatarRootAnimatorModifications(OptimizerSession session)
        {
            var animator = session.GetRootComponent<Animator>();
            var descriptor = session.GetRootComponent<VRCAvatarDescriptor>();

            var modificationsContainer = new ModificationsContainer();

            if (animator)
                modificationsContainer = AddHumanoidModifications(modificationsContainer, animator).ToMutable();

            // process playable layers
            // see https://misskey.niri.la/notes/9ioemawdit
            // see https://creators.vrchat.com/avatars/playable-layers

            var playableWeightChanged = new AnimatorLayerMap<AnimatorWeightState>();
            var animatorLayerWeightChanged = new AnimatorLayerMap<AnimatorLayerWeightMap<int>>
            {
                [VRCAvatarDescriptor.AnimLayerType.Action] = new AnimatorLayerWeightMap<int>(),
                [VRCAvatarDescriptor.AnimLayerType.FX] = new AnimatorLayerWeightMap<int>(),
                [VRCAvatarDescriptor.AnimLayerType.Gesture] = new AnimatorLayerWeightMap<int>(),
                [VRCAvatarDescriptor.AnimLayerType.Additive] = new AnimatorLayerWeightMap<int>(),
            };
            var useDefaultLayers = !descriptor.customizeAnimationLayers;

            foreach (var layer in descriptor.baseAnimationLayers)
                CollectWeightChangesInController(GetPlayableLayerController(layer, useDefaultLayers),
                    playableWeightChanged, animatorLayerWeightChanged);

            if (mmdWorldCompatibility)
            {
                var fxLayer = animatorLayerWeightChanged[VRCAvatarDescriptor.AnimLayerType.FX];
                fxLayer[1] = fxLayer[1].Merge(AnimatorWeightState.EitherZeroOrOne);
                fxLayer[2] = fxLayer[2].Merge(AnimatorWeightState.EitherZeroOrOne);
            }

            var controllers = new AnimatorLayerMap<RuntimeAnimatorController>();

            foreach (var layer in descriptor.specialAnimationLayers.Concat(descriptor.baseAnimationLayers))
            {
                controllers[layer.type] = GetPlayableLayerController(layer, useDefaultLayers);
            }

            void MergeLayer(VRCAvatarDescriptor.AnimLayerType type, bool alwaysApplied, float defaultWeight)
            {
                AnimatorWeightState weightState;
                if (alwaysApplied)
                    weightState = AnimatorWeightState.AlwaysOne;
                else
                    weightState = AnimatorLayerWeightStates.WeightStateFor(defaultWeight)
                        .Merge(playableWeightChanged[type]);

                if (weightState == AnimatorWeightState.AlwaysZero) return;


                var parsedLayer = ParseAnimatorController(descriptor.gameObject,
                    controllers[type], animatorLayerWeightChanged[type]);

                modificationsContainer.MergeAsNewLayer(parsedLayer, weightState);
            }

            MergeLayer(VRCAvatarDescriptor.AnimLayerType.Base, true, 1);
            // Station Sitting
            MergeLayer(VRCAvatarDescriptor.AnimLayerType.Sitting, false, 1);
            MergeLayer(VRCAvatarDescriptor.AnimLayerType.Additive, false, 1); // Idle
            MergeLayer(VRCAvatarDescriptor.AnimLayerType.Gesture, false, 1);
            // Station Action
            MergeLayer(VRCAvatarDescriptor.AnimLayerType.Action, false, 0);
            MergeLayer(VRCAvatarDescriptor.AnimLayerType.FX, false, 1);

            // TPose and IKPose should only affect to Humanoid so skip here~

            switch (descriptor.lipSync)
            {
                // AvatarDescriptorから収集
                case VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape when descriptor.VisemeSkinnedMesh != null:
                {
                    var skinnedMeshRenderer = descriptor.VisemeSkinnedMesh;
                    var updater = modificationsContainer.ModifyObject(skinnedMeshRenderer);
                    foreach (var blendShape in descriptor.VisemeBlendShapes)
                        updater.AddModificationAsNewLayer($"blendShape.{blendShape}", AnimationProperty.Variable());
                    break;
                }
                case VRC_AvatarDescriptor.LipSyncStyle.JawFlapBlendShape when descriptor.VisemeSkinnedMesh != null:
                {
                    var skinnedMeshRenderer = descriptor.VisemeSkinnedMesh;
                    var shape = descriptor.MouthOpenBlendShapeName;

                    modificationsContainer.ModifyObject(skinnedMeshRenderer)
                        .AddModificationAsNewLayer($"blendShape.{shape}", AnimationProperty.Variable());
                    break;
                }
            }

            if (descriptor.enableEyeLook)
            {
                var leftEye = descriptor.customEyeLookSettings.leftEye;
                var rightEye = descriptor.customEyeLookSettings.rightEye;
                if (leftEye)
                {
                    var updater = modificationsContainer.ModifyObject(rightEye);
                    foreach (var prop in TransformRotationAnimationKeys)
                        updater.AddModificationAsNewLayer(prop, AnimationProperty.Variable());
                }


                if (rightEye)
                {
                    var updater = modificationsContainer.ModifyObject(rightEye);
                    foreach (var prop in TransformRotationAnimationKeys)
                        updater.AddModificationAsNewLayer(prop, AnimationProperty.Variable());
                }

                switch (descriptor.customEyeLookSettings.eyelidType)
                {
                    case VRCAvatarDescriptor.EyelidType.None:
                        break;
                    case VRCAvatarDescriptor.EyelidType.Bones:
                    {
                        foreach (var eyelids in new[]
                                 {
                                     descriptor.customEyeLookSettings.lowerLeftEyelid,
                                     descriptor.customEyeLookSettings.upperLeftEyelid,
                                     descriptor.customEyeLookSettings.lowerRightEyelid,
                                     descriptor.customEyeLookSettings.upperRightEyelid,
                                 })
                        {
                            var updater = modificationsContainer.ModifyObject(eyelids);
                            foreach (var prop in TransformRotationAnimationKeys)
                                updater.AddModificationAsNewLayer(prop, AnimationProperty.Variable());
                        }
                    }
                        break;
                    case VRCAvatarDescriptor.EyelidType.Blendshapes
                        when descriptor.customEyeLookSettings.eyelidsSkinnedMesh != null:
                    {
                        var skinnedMeshRenderer = descriptor.customEyeLookSettings.eyelidsSkinnedMesh;
                        var mesh = skinnedMeshRenderer.sharedMesh;

                        var updater = modificationsContainer.ModifyObject(skinnedMeshRenderer);

                        foreach (var blendShape in from index in descriptor.customEyeLookSettings.eyelidsBlendshapes
                                 where 0 <= index && index < mesh.blendShapeCount
                                 select mesh.GetBlendShapeName(index))
                            updater.AddModificationAsNewLayer($"blendShape.{blendShape}", AnimationProperty.Variable());
                    }
                        break;
                }
            }

            var bodySkinnedMesh = descriptor.transform.Find("Body")?.GetComponent<SkinnedMeshRenderer>();

            if (mmdWorldCompatibility && bodySkinnedMesh)
            {
                var updater = modificationsContainer.ModifyObject(bodySkinnedMesh);

                foreach (var shape in MmdBlendShapeNames)
                    updater.AddModificationAsNewLayer($"blendShape.{shape}", AnimationProperty.Variable());
            }

            return modificationsContainer;
        }

        private void CollectWeightChangesInController(RuntimeAnimatorController runtimeController,
            AnimatorLayerMap<AnimatorWeightState> playableWeightChanged,
            AnimatorLayerMap<AnimatorLayerWeightMap<int>> animatorLayerWeightChanged)
        {
            ReportingObject(runtimeController, () =>
            {
                var (controller, _) = GetControllerAndOverrides(runtimeController);

                foreach (var layer in controller.layers)
                {
                    if (layer.syncedLayerIndex == -1)
                        foreach (var state in CollectStates(layer.stateMachine))
                            CollectWeightChangesInBehaviors(state.behaviours);
                    else
                        foreach (var state in CollectStates(controller.layers[layer.syncedLayerIndex]
                                     .stateMachine))
                            CollectWeightChangesInBehaviors(layer.GetOverrideBehaviours(state));
                }
            });

            return;

            void CollectWeightChangesInBehaviors(StateMachineBehaviour[] stateBehaviours)
            {
                foreach (var stateMachineBehaviour in stateBehaviours)
                {
                    switch (stateMachineBehaviour)
                    {
                        case VRC_PlayableLayerControl playableLayerControl:
                        {
                            VRCAvatarDescriptor.AnimLayerType layer;
                            switch (playableLayerControl.layer)
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
                                    throw new ArgumentOutOfRangeException();
                            }

                            var current = AnimatorLayerWeightStates.WeightStateFor(playableLayerControl.blendDuration,
                                playableLayerControl.goalWeight);
                            playableWeightChanged[layer] = playableWeightChanged[layer].Merge(current);
                        }
                            break;
                        case VRC_AnimatorLayerControl animatorLayerControl:
                        {
                            VRCAvatarDescriptor.AnimLayerType layer;
                            switch (animatorLayerControl.playable)
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
                                    throw new ArgumentOutOfRangeException();
                            }

                            var current = AnimatorLayerWeightStates.WeightStateFor(animatorLayerControl.blendDuration,
                                animatorLayerControl.goalWeight);
                            animatorLayerWeightChanged[layer][animatorLayerControl.layer] =
                                animatorLayerWeightChanged[layer][animatorLayerControl.layer].Merge(current);
                            break;
                        }
                    }
                }
            }
        }

        private static RuntimeAnimatorController GetPlayableLayerController(VRCAvatarDescriptor.CustomAnimLayer layer,
            bool useDefault = false)
        {
            if (!useDefault && !layer.isDefault && layer.animatorController)
            {
                return layer.animatorController;
            }

            if (!AnimatorLayerMap<object>.IsValid(layer.type)) return null;
            ref var loader = ref DefaultLayers[layer.type];
            var controller = loader.Value;
            if (controller == null)
                throw new InvalidOperationException($"default controller for {layer.type} not found");
            return controller;
        }
        
        #endregion

        #region Animator

        /// Mark rotations of humanoid bones as changeable variables
        private IModificationsContainer AddHumanoidModifications(IModificationsContainer container, Animator animator)
        {
            // if it's not humanoid, this pass doesn't matter
            if (!animator.isHuman) return container;

            var mutable = container.ToMutable();
            for (var bone = HumanBodyBones.Hips; bone < HumanBodyBones.LastBone; bone++)
            {
                var transform = animator.GetBoneTransform(bone);
                if (!transform) continue;

                var updater = mutable.ModifyObject(transform);

                foreach (var key in TransformRotationAnimationKeys)
                    updater.AddModificationAsNewLayer(key, AnimationProperty.Variable());
            }

            return mutable;
        }

        public IModificationsContainer ParseAnimatorController(GameObject root, RuntimeAnimatorController controller,
            [CanBeNull] AnimatorLayerWeightMap<int> externallyWeightChanged = null)
        {
            return ReportingObject(controller, () =>
            {
                if (advancedAnimatorParser)
                {
                    var (animatorController, mapping) = GetControllerAndOverrides(controller);
                    return AdvancedParseAnimatorController(root, animatorController, mapping,
                        externallyWeightChanged);
                }
                else
                {
                    return FallbackParseAnimatorController(root, controller);
                }
            });
        }

        /// <summary>
        /// Fallback AnimatorController Parser but always assumed as partially applied.
        /// This process assumes everything is applied as non-additive state motion.
        /// This parsing MAY not correct with direct blendtree or additive layer
        /// but it's extremely rare case so ignoring such case.
        /// </summary>
        private IModificationsContainer FallbackParseAnimatorController(GameObject root, RuntimeAnimatorController controller)
        {
            return controller.animationClips.Select(clip => _animationParser.GetParsedAnimation(root, clip)).MergeContainersSideBySide();
        }

        internal IModificationsContainer AdvancedParseAnimatorController(GameObject root, AnimatorController controller,
            IReadOnlyDictionary<AnimationClip, AnimationClip> mapping,
            [CanBeNull] AnimatorLayerWeightMap<int> externallyWeightChanged)
        {
            var layers = controller.layers;
            if (layers.Length == 0) return ImmutableModificationsContainer.Empty;

            var mergedController = new ModificationsContainer();

            for (var i = 0; i < layers.Length; i++)
            {
                var weightState = i == 0
                    ? AnimatorWeightState.AlwaysOne
                    : AnimatorLayerWeightStates.WeightStateFor(layers[i].defaultWeight)
                        .Merge(externallyWeightChanged?.Get(i) ?? AnimatorWeightState.NotChanged);

                if (weightState == AnimatorWeightState.AlwaysZero) continue;

                var parsedLayer = ParseAnimatorControllerLayer(root, controller, mapping, i);


                switch (layers[i].blendingMode)
                {
                    case AnimatorLayerBlendingMode.Override:
                        mergedController.MergeAsNewLayer(parsedLayer, weightState);
                        break;
                    case AnimatorLayerBlendingMode.Additive:
                        mergedController.MergeAsNewAdditiveLayer(parsedLayer, weightState);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return mergedController;
        }

        internal IModificationsContainer ParseAnimatorControllerLayer(
            GameObject root,
            AnimatorController controller,
            IReadOnlyDictionary<AnimationClip, AnimationClip> mapping,
            int layerIndex)
        {
            var layer = controller.layers[layerIndex];

            var syncedLayer = layer.syncedLayerIndex;

            IEnumerable<IModificationsContainer> parsedMotions;

            if (syncedLayer == -1)
            {
                parsedMotions = CollectStates(layer.stateMachine)
                    .Select(state => _animationParser.ParseMotion(root, state.motion, mapping));
            }
            else
            {
                parsedMotions = CollectStates(controller.layers[syncedLayer].stateMachine)
                    .Select(state => _animationParser.ParseMotion(root, layer.GetOverrideMotion(state), mapping));
            }

            return parsedMotions.MergeContainersSideBySide();
        }

        private IEnumerable<AnimatorState> CollectStates(AnimatorStateMachine stateMachineIn)
        {
            var queue = new Queue<AnimatorStateMachine>();
            queue.Enqueue(stateMachineIn);

            while (queue.Count != 0)
            {
                var stateMachine = queue.Dequeue();
                foreach (var state in stateMachine.states)
                    yield return state.state;

                foreach (var childStateMachine in stateMachine.stateMachines)
                    queue.Enqueue(childStateMachine.stateMachine);
            }
        }

        public (AnimatorController, IReadOnlyDictionary<AnimationClip, AnimationClip>) GetControllerAndOverrides(
            RuntimeAnimatorController runtimeController)
        {
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
                    if (!overrides.ContainsKey(original))
                        overrides.Add(original, mapped);
            }
        }

        #endregion

        #region Constants

        private static readonly string[] TransformRotationAnimationKeys =
            { "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w" };

        private static readonly string[] TransformPositionAnimationKeys =
            { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z" };

        private static readonly string[] TransformScaleAnimationKeys =
            { "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z" };

        private static readonly AnimatorLayerMap<CachedGuidLoader<AnimatorController>> DefaultLayers =
            new AnimatorLayerMap<CachedGuidLoader<AnimatorController>>
            {
                // vrc_AvatarV3LocomotionLayer
                [VRCAvatarDescriptor.AnimLayerType.Base] = "4e4e1a372a526074884b7311d6fc686b",
                // vrc_AvatarV3IdleLayer
                [VRCAvatarDescriptor.AnimLayerType.Additive] = "573a1373059632b4d820876efe2d277f",
                // vrc_AvatarV3HandsLayer
                [VRCAvatarDescriptor.AnimLayerType.Gesture] = "404d228aeae421f4590305bc4cdaba16",
                // vrc_AvatarV3ActionLayer
                [VRCAvatarDescriptor.AnimLayerType.Action] = "3e479eeb9db24704a828bffb15406520",
                // vrc_AvatarV3FaceLayer
                [VRCAvatarDescriptor.AnimLayerType.FX] = "d40be620cf6c698439a2f0a5144919fe",
                // vrc_AvatarV3SittingLayer
                [VRCAvatarDescriptor.AnimLayerType.Sitting] = "1268460c14f873240981bf15aa88b21a",
                // vrc_AvatarV3UtilityTPose
                [VRCAvatarDescriptor.AnimLayerType.TPose] = "00121b5812372b74f9012473856d8acf",
                // vrc_AvatarV3UtilityIKPose
                [VRCAvatarDescriptor.AnimLayerType.IKPose] = "a9b90a833b3486e4b82834c9d1f7c4ee"
            };

        private static readonly string[] MmdBlendShapeNames = {
            // https://booth.pm/ja/items/3341221
            // https://images-wixmp-ed30a86b8c4ca887773594c2.wixmp.com/i/0b7b5e4b-c62e-41f7-8ced-1f3e58c4f5bf/d5nbmvp-5779f5ac-d476-426c-8ee6-2111eff8e76c.png
            "まばたき",
            "笑い",
            "ウィンク",
            "ウィンク右",
            "ウィンク２",
            "ｳｨﾝｸ２右",
            "なごみ",
            "はぅ",
            "びっくり",
            "じと目",
            "ｷﾘｯ",
            "はちゅ目",

            "星目",
            "はぁと",
            "瞳小",
            "瞳縦潰れ",
            "光下",
            "恐ろしい子！",
            "ハイライト消",
            "映り込み消",
            "喜び",
            "わぉ!?",
            "なごみω",
            "悲しむ",
            "敵意",

            "あ",
            "い",
            "う",
            "え",
            "お",
            "あ２",
            "ん",
            "▲",
            "∧",
            "□",
            "ワ",
            "ω",

            "ω□",
            "にやり",
            "にやり２",
            "にっこり",
            "ぺろっ",
            "てへぺろ",
            "てへぺろ２",
            "口角上げ",
            "口角下げ",
            "口横広げ",
            "歯無し上",
            "歯無し下",

            "真面目",
            "困る",
            "にこり",
            "怒り",
            "下",
            "上",

            // english
            "Blink",
            "Smile",
            "Wink",
            "Wink-a",
            "Wink-b",
            "Wink-c",
            "Howawa",
            "> <",
            "Ha!!!",
            "Jito-eye",
            "Kiri-eye",
            "O O",

            "EyeStar",
            "EyeHeart",
            "EyeSmall",
            "EyeSmall-v",
            "EyeUnderli",
            "EyeFunky",
            "EyHi-Off",
            "EyeRef-off",
            "Joy",
            "Wao!?",
            "Howawa ω",
            "Wail",
            "Hostility",

            "a",
            "i",
            "u",
            "e",
            "o",
            "a 2",
            "n",
            "Mouse_1",
            "Mouse_2",
            //"□",
            "Wa",
            "Omega",

            // "ω□",
            "Niyari",
            "Niyari2",
            "Smile",
            "Pero",
            "Bero-tehe",
            "Bero-tehe2",
            "MouseUP",
            "MouseDW",
            "MouseWD",
            "ToothAnon",
            "ToothBnon",

            "Serious",
            "Trouble",
            "Smily",
            "Get angry",
            "Up",
            "Down",

            // https://gist.github.com/lilxyzw/80608d9b16bf3458c61dec6b090805c5
            "しいたけ",
            "照れ",
            "涙",
        };

        #endregion
    }
}
