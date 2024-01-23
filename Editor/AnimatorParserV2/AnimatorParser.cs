using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.API;
using Anatawa12.AvatarOptimizer.APIInternal;
using JetBrains.Annotations;
using nadena.dev.ndmf;
using UnityEditor.Animations;
using UnityEngine;

#if AAO_VRCSDK3_AVATARS
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
#endif

namespace Anatawa12.AvatarOptimizer.AnimatorParsersV2
{
    class AnimatorParser
    {
        private bool mmdWorldCompatibility;
        private AnimationParser _animationParser = new AnimationParser();

        public AnimatorParser(bool mmdWorldCompatibility)
        {
            this.mmdWorldCompatibility = mmdWorldCompatibility;
        }

        public RootPropModNodeContainer GatherAnimationModifications(BuildContext context)
        {
            var rootNode = new RootPropModNodeContainer();

            CollectAvatarRootAnimatorModifications(context, rootNode);

            foreach (var child in context.AvatarRootTransform.DirectChildrenEnumerable())
                WalkForAnimator(child, true, rootNode);

            OtherMutateComponents(rootNode, context);

            return rootNode;
        }

        private void WalkForAnimator(Transform transform, bool parentObjectAlwaysActive,
            RootPropModNodeContainer rootNode)
        {
            var gameObject = transform.gameObject;
            bool objectAlwaysActive;
            switch (rootNode.GetConstantValue(gameObject, "m_IsActive", gameObject.activeSelf))
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
                ParseAnimationOrAnimator(animator, objectAlwaysActive, rootNode,
                    () => AddHumanoidModifications(
                        NodesMerger.AnimatorComponentFromController(animator,
                            ParseAnimatorController(gameObject, animator.runtimeAnimatorController)), animator));
            }

            var animation = transform.GetComponent<Animation>();
            if (animation && animation.playAutomatically && animation.clip)
            {
                // We can animate `Animate Physics`, `Play Automatically` and `Enabled` of Animation component.
                // However, animating `Play Automatically` will have no effect (initial state will be used)
                // so if `Play Automatically` is disabled, we can do nothing with Animation component.
                // That's why we ignore Animation component if playAutomatically is false.

                ParseAnimationOrAnimator(animation, objectAlwaysActive, rootNode,
                    () => NodesMerger.AnimationComponentFromAnimationClip(animation,
                        _animationParser.GetParsedAnimation(gameObject, animation.clip)));
            }

            foreach (var child in transform.DirectChildrenEnumerable())
                WalkForAnimator(child, parentObjectAlwaysActive: objectAlwaysActive, rootNode);
        }

        private void ParseAnimationOrAnimator(
            Behaviour animator,
            bool objectAlwaysActive,
            RootPropModNodeContainer modifications,
            Func<ComponentNodeContainer> parseComponent)
        {
            bool alwaysApplied;
            switch (modifications.GetConstantValue(animator, "m_Enabled", animator.enabled))
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
                parsed.FloatNodes.TryGetValue((animator, "m_Enabled"), out var node);
                switch (node.AsConstantValue(animator.enabled))
                {
                    case null:
                    case false:
                        alwaysApplied = false;
                        break;
                    case true:
                        break;
                }
            }

            modifications.Add(parsed, alwaysApplied);
        }

        #region OtherComponents

        private class Collector : ComponentMutationsCollector
        {
            private readonly RootPropModNodeContainer _modifications;

            public Collector(RootPropModNodeContainer modifications) => _modifications = modifications;
            public Component Modifier { get; set; }


            public override void ModifyProperties(Component component, IEnumerable<string> properties)
            {
                foreach (var prop in properties)
                    _modifications.Add(component, prop, new VariableComponentPropModNode<float>(Modifier), true);
            }
        }

        /// <summary>
        /// Collect modifications by non-animation changes. For example, constraints, PhysBones, and else 
        /// </summary>
        private static void OtherMutateComponents(RootPropModNodeContainer mod, BuildContext context)
        {
            var collector = new Collector(mod);
            foreach (var component in context.GetComponents<Component>())
            {
                using (ErrorReport.WithContextObject(component))
                {
                    collector.Modifier = component;
                    if (ComponentInfoRegistry.TryGetInformation(component.GetType(), out var info))
                        info.CollectMutationsInternal(component, collector);

                }
            }
        }

        #endregion

        #region Avatar Root Animator

        private void CollectAvatarRootAnimatorModifications(BuildContext session,
            RootPropModNodeContainer modifications)
        {

            var animator = session.AvatarRootObject.GetComponent<Animator>();
            if (animator)
                modifications.Add(AddHumanoidModifications(null, animator), true);
            
#if AAO_VRCSDK3_AVATARS
            var descriptor = session.AvatarRootObject.GetComponent<VRCAvatarDescriptor>();
            if (descriptor)
                CollectAvatarDescriptorModifications(modifications, descriptor);
#endif
            
#if AAO_VRM0
            var blendShapeProxy = session.AvatarRootObject.GetComponent<VRM.VRMBlendShapeProxy>();
            if (blendShapeProxy)
                modifications.Add(CollectBlendShapeProxyModifications(session, blendShapeProxy), true);
#endif
            
#if AAO_VRM1
            var vrm10Instance = session.AvatarRootObject.GetComponent<UniVRM10.Vrm10Instance>();
            if (vrm10Instance)
                modifications.Add(CollectVrm10InstanceModifications(session, vrm10Instance), true);
#endif
        }
        
#if AAO_VRCSDK3_AVATARS
        private void CollectAvatarDescriptorModifications(RootPropModNodeContainer modifications, VRCAvatarDescriptor descriptor)
        {
            // process playable layers
            // see https://misskey.niri.la/notes/9ioemawdit
            // see https://creators.vrchat.com/avatars/playable-layers

            var animator = descriptor.GetComponent<Animator>();
            if (animator == null)
            {
                BuildLog.LogError("AnimatorParser:AnimatorNotFoundOnAvatarRoot", descriptor.gameObject);
                return;
            }

            var playableWeightChanged = new AnimatorLayerMap<ParserAnimatorWeightState>();
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
                fxLayer[1] = Merge(fxLayer[1], ParserAnimatorWeightState.EitherZeroOrOne);
                fxLayer[2] = Merge(fxLayer[2], ParserAnimatorWeightState.EitherZeroOrOne);
            }

            var controllers = new AnimatorLayerMap<RuntimeAnimatorController>();

            foreach (var layer in descriptor.specialAnimationLayers.Concat(descriptor.baseAnimationLayers))
            {
                controllers[layer.type] = GetPlayableLayerController(layer, useDefaultLayers);
            }

            var playableLayers =
                new List<(AnimatorWeightState, AnimatorLayerBlendingMode, AnimatorControllerNodeContainer)>();

            void MergeLayer(
                VRCAvatarDescriptor.AnimLayerType type, 
                bool alwaysApplied, 
                float defaultWeight,
                AnimatorLayerBlendingMode mode = AnimatorLayerBlendingMode.Override
            )
            {
                AnimatorWeightState? weightState;
                if (alwaysApplied)
                    weightState = AnimatorWeightState.AlwaysOne;
                else
                    weightState = GetWeightState(defaultWeight, playableWeightChanged[type]);

                if (weightState == null) return;
                

                var parsedLayer =
                    ParseAnimatorController(descriptor.gameObject, controllers[type], animatorLayerWeightChanged[type]);
                playableLayers.Add((weightState.Value, mode, parsedLayer));
            }

            var isHumanoid = animator != null && animator.isHuman;
            MergeLayer(VRCAvatarDescriptor.AnimLayerType.Base, true, 1);
            // Station Sitting here
            MergeLayer(VRCAvatarDescriptor.AnimLayerType.Sitting, false, 1);
            if (isHumanoid) MergeLayer(VRCAvatarDescriptor.AnimLayerType.Additive, false, 1, AnimatorLayerBlendingMode.Additive); // A.K.A. Idle
            if (isHumanoid) MergeLayer(VRCAvatarDescriptor.AnimLayerType.Gesture, false, 1);
            // Station Action here
            MergeLayer(VRCAvatarDescriptor.AnimLayerType.Action, false, 0);
            MergeLayer(VRCAvatarDescriptor.AnimLayerType.FX, false, 1);

            modifications.Add(NodesMerger.ComponentFromPlayableLayers(animator, playableLayers), true);

            // TPose and IKPose should only affect to Humanoid so skip here~

            var bodySkinnedMesh = descriptor.transform.Find("Body")?.GetComponent<SkinnedMeshRenderer>();

            if (mmdWorldCompatibility && bodySkinnedMesh)
            {
                foreach (var shape in MmdBlendShapeNames)
                    modifications.Add(bodySkinnedMesh, $"blendShape.{shape}",
                        new VariableComponentPropModNode<float>(descriptor), true);
            }
        }

        private void CollectWeightChangesInController(RuntimeAnimatorController runtimeController,
            AnimatorLayerMap<ParserAnimatorWeightState> playableWeightChanged,
            AnimatorLayerMap<AnimatorLayerWeightMap<int>> animatorLayerWeightChanged)
        {
            using (ErrorReport.WithContextObject(runtimeController))
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
            }

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
                                    BuildLog.LogWarning("AnimatorParser:PlayableLayerControl:UnknownBlendablePlayableLayer",
                                            $"{playableLayerControl.layer}",
                                            stateMachineBehaviour);
                                    continue;
                            }

                            var current = AnimatorLayerWeightStates.WeightStateFor(playableLayerControl.blendDuration,
                                playableLayerControl.goalWeight);
                            playableWeightChanged[layer] = Merge(playableWeightChanged[layer], current);
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
                                    BuildLog.LogWarning("AnimatorParser:AnimatorLayerControl:UnknownBlendablePlayableLayer",
                                            $"{animatorLayerControl.layer}",
                                            stateMachineBehaviour);
                                    continue;
                            }

                            var current = AnimatorLayerWeightStates.WeightStateFor(animatorLayerControl.blendDuration,
                                animatorLayerControl.goalWeight);
                            animatorLayerWeightChanged[layer][animatorLayerControl.layer] =
                                Merge(animatorLayerWeightChanged[layer][animatorLayerControl.layer], current);
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
#endif
        
#if AAO_VRM0
        private ComponentNodeContainer CollectBlendShapeProxyModifications(BuildContext context, VRM.VRMBlendShapeProxy vrmBlendShapeProxy)
        {
            var nodes = new ComponentNodeContainer();

            var bindings = vrmBlendShapeProxy.BlendShapeAvatar.Clips.SelectMany(clip => clip.Values);
            foreach (var binding in bindings)
            {
                var skinnedMeshRenderer =
                    context.AvatarRootTransform.Find(binding.RelativePath).GetComponent<SkinnedMeshRenderer>();
                var blendShapePropName = 
                    $"blendShape.{skinnedMeshRenderer.sharedMesh.GetBlendShapeName(binding.Index)}";
                nodes.Add(skinnedMeshRenderer, blendShapePropName, new VariableComponentPropModNode<float>(vrmBlendShapeProxy));
            }
            
            // Currently, MaterialValueBindings are guaranteed to not change (MaterialName, in particular)
            // unless MergeToonLitMaterial is used, which breaks material animations anyway.
            // Gather material modifications here once we start tracking material changes...

            return nodes;
        }
#endif

#if AAO_VRM1
        private ComponentNodeContainer CollectVrm10InstanceModifications(BuildContext context, UniVRM10.Vrm10Instance vrm10Instance)
        {
            var nodes = new ComponentNodeContainer();

            var bindings = vrm10Instance.Vrm.Expression.Clips.SelectMany(clip => clip.Clip.MorphTargetBindings);
            foreach (var binding in bindings)
            {
                var skinnedMeshRenderer = 
                    context.AvatarRootTransform.Find(binding.RelativePath).GetComponent<SkinnedMeshRenderer>();
                var blendShapePropName = 
                    $"blendShape.{skinnedMeshRenderer.sharedMesh.GetBlendShapeName(binding.Index)}";
                nodes.Add(skinnedMeshRenderer, blendShapePropName, new VariableComponentPropModNode<float>(vrm10Instance));
            }

            // Currently, MaterialValueBindings are guaranteed to not change (MaterialName, in particular)
            // unless MergeToonLitMaterial is used, which breaks material animations anyway.
            // Gather material modifications here once we start tracking material changes...

            return nodes;
        }
#endif
        
        #endregion

        #region Animator

        /// Mark rotations of humanoid bones as changeable variables
        [CanBeNull]
        private ComponentNodeContainer AddHumanoidModifications([CanBeNull] ComponentNodeContainer container, Animator animator)
        {
            // if it's not humanoid, this pass doesn't matter
            if (!animator.isHuman) return container;

            if (container == null) container = new ComponentNodeContainer();

            for (var bone = HumanBodyBones.Hips; bone < HumanBodyBones.LastBone; bone++)
            {
                var transform = animator.GetBoneTransform(bone);
                if (!transform) continue;

                foreach (var prop in TransformRotationAnimationKeys)
                    // overriding
                    container.Set(transform, prop, new HumanoidAnimatorPropModNode(animator));
            }

            return container;
        }

        [CanBeNull]
        public AnimatorControllerNodeContainer ParseAnimatorController(GameObject root, RuntimeAnimatorController controller,
            [CanBeNull] AnimatorLayerWeightMap<int> externallyWeightChanged = null)
        {
            using (ErrorReport.WithContextObject(controller))
            {
                var (animatorController, mapping) = GetControllerAndOverrides(controller);
                return AdvancedParseAnimatorController(root, animatorController, mapping,
                    externallyWeightChanged);
            }
        }

        [CanBeNull]
        internal AnimatorControllerNodeContainer AdvancedParseAnimatorController(GameObject root,
            AnimatorController controller,
            IReadOnlyDictionary<AnimationClip, AnimationClip> mapping,
            [CanBeNull] AnimatorLayerWeightMap<int> externallyWeightChanged)
        {
            var layers = controller.layers;
            return NodesMerger.AnimatorControllerFromAnimatorLayers(controller.layers.Select((layer, i) =>
            {
                AnimatorWeightState weightState;
                if (i == 0)
                {
                    weightState = AnimatorWeightState.AlwaysOne;
                }
                else
                {
                    var external = externallyWeightChanged?.Get(i) ?? ParserAnimatorWeightState.NotChanged;

                    if (!(GetWeightState(layers[i].defaultWeight, external) is AnimatorWeightState parsed))
                        return (default, default, null); // skip weight zero layer

                    weightState = parsed;
                }

                var parsedLayer = ParseAnimatorControllerLayer(root, controller, mapping, i);

                return (weightState, layer.blendingMode, parsedLayer);
            }));
        }

        public ImmutableNodeContainer ParseAnimatorControllerLayer(
            GameObject root,
            AnimatorController controller,
            IReadOnlyDictionary<AnimationClip, AnimationClip> mapping,
            int layerIndex)
        {
            var layer = controller.layers[layerIndex];

            var syncedLayer = layer.syncedLayerIndex;

            IEnumerable<ImmutableNodeContainer> parsedMotions;

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

            return NodesMerger.Merge(parsedMotions, default(LayerMerger));
        }

        struct LayerMerger : IMergeProperty
        {
            public ImmutablePropModNode<T> MergeNode<T>(List<ImmutablePropModNode<T>> nodes, int sourceCount) =>
                new AnimatorLayerPropModNode<T>(nodes, nodes.Count != sourceCount);
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

        internal class AnimatorLayerWeightMap<TKey>
        {
            private Dictionary<TKey, ParserAnimatorWeightState> _backed =
                new Dictionary<TKey, ParserAnimatorWeightState>();

            public ParserAnimatorWeightState this[TKey key]
            {
                get
                {
                    _backed.TryGetValue(key, out var state);
                    return state;
                }
                set => _backed[key] = value;
            }

            public ParserAnimatorWeightState Get(TKey key) => this[key];
        }

        public enum ParserAnimatorWeightState
        {
            NotChanged,
            AlwaysZero,
            AlwaysOne,
            EitherZeroOrOne,
            Variable
        }

        AnimatorWeightState? GetWeightState(float weight, ParserAnimatorWeightState external)
        {
            bool isOneWeight;
            
            if (weight == 0) isOneWeight = false;
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            else if (weight == 1) isOneWeight = true;
            else return AnimatorWeightState.Variable;
            
            switch (external)
            {
                case ParserAnimatorWeightState.NotChanged:
                    if (!isOneWeight) return null; // skip weight zero layer
                    return AnimatorWeightState.AlwaysOne;

                case ParserAnimatorWeightState.AlwaysZero:
                    if (!isOneWeight) return null; // skip weight zero layer
                    return AnimatorWeightState.EitherZeroOrOne;

                case ParserAnimatorWeightState.AlwaysOne:
                    return isOneWeight
                        ? AnimatorWeightState.AlwaysOne
                        : AnimatorWeightState.EitherZeroOrOne;

                case ParserAnimatorWeightState.EitherZeroOrOne:
                    return AnimatorWeightState.EitherZeroOrOne;
                case ParserAnimatorWeightState.Variable:
                    return AnimatorWeightState.Variable;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        static ParserAnimatorWeightState Merge(ParserAnimatorWeightState a, ParserAnimatorWeightState b)
        {
            // 25 pattern
            if (a == b) return a;

            if (a == ParserAnimatorWeightState.NotChanged) return b;
            if (b == ParserAnimatorWeightState.NotChanged) return a;

            if (a == ParserAnimatorWeightState.Variable) return ParserAnimatorWeightState.Variable;
            if (b == ParserAnimatorWeightState.Variable) return ParserAnimatorWeightState.Variable;

            if (a == ParserAnimatorWeightState.AlwaysOne && b == ParserAnimatorWeightState.AlwaysZero)
                return ParserAnimatorWeightState.EitherZeroOrOne;
            if (b == ParserAnimatorWeightState.AlwaysOne && a == ParserAnimatorWeightState.AlwaysZero)
                return ParserAnimatorWeightState.EitherZeroOrOne;

            if (a == ParserAnimatorWeightState.EitherZeroOrOne && b == ParserAnimatorWeightState.AlwaysZero)
                return ParserAnimatorWeightState.EitherZeroOrOne;
            if (b == ParserAnimatorWeightState.EitherZeroOrOne && a == ParserAnimatorWeightState.AlwaysZero)
                return ParserAnimatorWeightState.EitherZeroOrOne;

            if (a == ParserAnimatorWeightState.EitherZeroOrOne && b == ParserAnimatorWeightState.AlwaysOne)
                return ParserAnimatorWeightState.EitherZeroOrOne;
            if (b == ParserAnimatorWeightState.EitherZeroOrOne && a == ParserAnimatorWeightState.AlwaysOne)
                return ParserAnimatorWeightState.EitherZeroOrOne;

            throw new ArgumentOutOfRangeException();
        }

    static class AnimatorLayerWeightStates
    {
        public static ParserAnimatorWeightState WeightStateFor(float duration, float weight) =>
            duration != 0 ? ParserAnimatorWeightState.Variable : WeightStateFor(weight);

        public static ParserAnimatorWeightState WeightStateFor(float weight)
        {
            switch (weight)
            {
                case 0:
                    return ParserAnimatorWeightState.AlwaysZero;
                case 1:
                    return ParserAnimatorWeightState.AlwaysOne;
                default:
                    return ParserAnimatorWeightState.Variable;
            }
        }
        
        public static AnimatorWeightState ForAlwaysApplied(bool alwaysApplied) =>
            alwaysApplied ? AnimatorWeightState.AlwaysOne : AnimatorWeightState.EitherZeroOrOne;
    }
        #endregion

        #region Constants

        private static readonly string[] TransformRotationAnimationKeys =
            { "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w" };

        private static readonly string[] TransformPositionAnimationKeys =
            { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z" };

        private static readonly string[] TransformScaleAnimationKeys =
            { "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z" };

#if AAO_VRCSDK3_AVATARS
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
#endif

        private static readonly string[] MmdBlendShapeNames = new [] {
            // New EN by Yi MMD World
            //  https://docs.google.com/spreadsheets/d/1mfE8s48pUfjP_rBIPN90_nNkAIBUNcqwIxAdVzPBJ-Q/edit?usp=sharing
            // Old EN by Xoriu
            //  https://booth.pm/ja/items/3341221
            //  https://images-wixmp-ed30a86b8c4ca887773594c2.wixmp.com/i/0b7b5e4b-c62e-41f7-8ced-1f3e58c4f5bf/d5nbmvp-5779f5ac-d476-426c-8ee6-2111eff8e76c.png
            // Old EN, New EN, JA,

            // ===== Mouth =====
            "a",            "Ah",               "あ",
            "i",            "Ch",               "い",
            "u",            "U",                "う",
            "e",            "E",                "え",
            "o",            "Oh",               "お",
            "Niyari",       "Grin",             "にやり",
            "Mouse_2",      "∧",                "∧",
            "Wa",           "Wa",               "ワ",
            "Omega",        "ω",                "ω",
            "Mouse_1",      "▲",                "▲",
            "MouseUP",      "Mouth Horn Raise", "口角上げ",
            "MouseDW",      "Mouth Horn Lower", "口角下げ",
            "MouseWD",      "Mouth Side Widen", "口横広げ", 
            "n",            null,               "ん",
            "Niyari2",      null,               "にやり２",
            // by Xoriu only
            "a 2",          null,               "あ２",
            "□",            null,               "□",
            "ω□",           null,               "ω□",
            "Smile",        null,               "にっこり",
            "Pero",         null,               "ぺろっ",
            "Bero-tehe",    null,               "てへぺろ",
            "Bero-tehe2",   null,               "てへぺろ２",

            // ===== Eyes =====
            "Blink",        "Blink",            "まばたき",
            "Smile",        "Blink Happy",      "笑い",
            "> <",          "Close><",          "はぅ",
            "EyeSmall",     "Pupil",            "瞳小",
            "Wink-c",       "Wink 2 Right",     "ｳｨﾝｸ２右",
            "Wink-b",       "Wink 2",           "ウィンク２",
            "Wink",         "Wink",             "ウィンク",
            "Wink-a",       "Wink Right",       "ウィンク右",
            "Howawa",       "Calm",             "なごみ",
            "Jito-eye",     "Stare",            "じと目",
            "Ha!!!",        "Surprised",        "びっくり",
            "Kiri-eye",     "Slant",            "ｷﾘｯ",
            "EyeHeart",     "Heart",            "はぁと",
            "EyeStar",      "Star Eye",         "星目",
            "EyeFunky",     null,               "恐ろしい子！",
            // by Xoriu only
            "O O",          null,               "はちゅ目",
            "EyeSmall-v",   null,               "瞳縦潰れ",
            "EyeUnderli",   null,               "光下",
            "EyHi-Off",     null,               "ハイライト消",
            "EyeRef-off",   null,               "映り込み消",

            // ===== Eyebrow =====
            "Smily",        "Cheerful",         "にこり",
            "Up",           "Upper",            "上",
            "Down",         "Lower",            "下",
            "Serious",      "Serious",          "真面目",
            "Trouble",      "Sadness",          "困る",
            "Get angry",    "Anger",            "怒り",
            null,           "Front",            "前",
            
            // ===== Eyes + Eyebrow Feeling =====
            // by Xoriu only
            "Joy",          null,               "喜び",
            "Wao!?",        null,               "わぉ!?",
            "Howawa ω",     null,               "なごみω",
            "Wail",         null,               "悲しむ",
            "Hostility",    null,               "敵意",

            // ===== Other ======
            null,           "Blush",            "照れ",
            "ToothAnon",    null,               "歯無し下",
            "ToothBnon",    null,               "歯無し上",
            null,           null,               "涙",

            // others

            // https://gist.github.com/lilxyzw/80608d9b16bf3458c61dec6b090805c5
            "しいたけ",

            // https://site.nicovideo.jp/ch/userblomaga_thanks/archive/ar1471249
            "なぬ！",
            "はんっ！",
            "えー",
            "睨み",
            "睨む",
            "白目",
            "瞳大",
            "頬染め",
            "青ざめ",
        }.Where(x => x != null).Distinct().ToArray();

        #endregion
    }
}
