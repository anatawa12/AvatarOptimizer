using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Anatawa12.AvatarOptimizer.API;
using Anatawa12.AvatarOptimizer.APIInternal;
using nadena.dev.ndmf;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;
using System.Runtime.InteropServices;


#if AAO_VRCSDK3_AVATARS
using VRC.SDK3.Avatars.Components;
#endif

namespace Anatawa12.AvatarOptimizer.AnimatorParsersV2
{
    class AnimatorParser
    {
        private readonly bool _mmdWorldCompatibility;
        private readonly AnimationParser _animationParser = new AnimationParser();

        public AnimatorParser(bool mmdWorldCompatibility)
        {
            _mmdWorldCompatibility = mmdWorldCompatibility;
        }

        public RootPropModNodeContainer GatherAnimationModifications(BuildContext context)
        {
            Profiler.BeginSample("GatherAnimationModifications");
            var rootNode = new RootPropModNodeContainer();

            CollectAvatarRootAnimatorModifications(context, rootNode);

            foreach (var child in context.AvatarRootTransform.DirectChildrenEnumerable())
                WalkForAnimator(child, true, rootNode);

            OtherMutateComponents(rootNode, context);
            Profiler.EndSample();

            return rootNode;
        }

        private void WalkForAnimator(Transform transform, bool parentObjectAlwaysActive,
            RootPropModNodeContainer rootNode)
        {
            var gameObject = transform.gameObject;
            bool objectAlwaysActive;
            switch (rootNode.GetConstantValue(gameObject, Props.IsActive, gameObject.activeSelf))
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

            if (transform.TryGetComponent<Animator>(out var animator) && animator.runtimeAnimatorController)
            {
                ParseAnimationOrAnimator(animator, objectAlwaysActive, rootNode,
                    () => AddHumanoidModifications(
                        NodesMerger.AnimatorComponentFromController(animator,
                            ParseAnimatorController(gameObject, animator.runtimeAnimatorController)), animator));
            }

            if (transform.TryGetComponent<Animation>(out var animation) && animation.playAutomatically && animation.clip)
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
            switch (modifications.GetConstantValue(animator, Props.EnabledFor(animator), animator.enabled))
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
                parsed.FloatNodes.TryGetValue((animator, Props.EnabledFor(animator)), out var node);
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
            public Component? Modifier { get; set; }


            public override void ModifyProperties(Component component, IEnumerable<string> properties)
            {
                foreach (var prop in properties)
                {
                    _modifications.Add(component, prop, new VariableComponentPropModNode(Modifier!), ApplyState.Always);
                }
            }
        }

        /// <summary>
        /// Collect modifications by non-animation changes. For example, constraints, PhysBones, and else 
        /// </summary>
        private static void OtherMutateComponents(RootPropModNodeContainer mod, BuildContext context)
        {
            Profiler.BeginSample("OtherMutateComponents");
            var collector = new Collector(mod);
            foreach (var component in context.GetComponents<Component>())
            {
                using (ErrorReport.WithContextObject(component))
                {
                    collector.Modifier = component;
                    if (ComponentInfoRegistry.TryGetInformation(component.GetType(), out var info))
                    {
                        Profiler.BeginSample($"CollectMutationsInternal ({component.GetType()})");
                        info.CollectMutationsInternal(component, collector);
                        Profiler.EndSample();
                    }
                }
            }
            Profiler.EndSample();
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
        private void CollectAvatarDescriptorModifications(RootPropModNodeContainer modifications,
            VRCAvatarDescriptor descriptor)
        {
            // process playable layers
            // see https://misskey.niri.la/notes/9ioemawdit
            // see https://creators.vrchat.com/avatars/playable-layers

            if (!descriptor.TryGetComponent<Animator>(out var animator))
            {
                BuildLog.LogError("AnimatorParser:AnimatorNotFoundOnAvatarRoot", descriptor.gameObject);
                return;
            }

            var useDefaultLayers = !descriptor.customizeAnimationLayers;

            // load controllers
            var controllers = new AnimatorLayerMap<RuntimeAnimatorController>();

            // load default layers to not cause error
            foreach (var layer in stackalloc[]
                     {
                         VRCAvatarDescriptor.AnimLayerType.Base,
                         VRCAvatarDescriptor.AnimLayerType.Additive,
                         VRCAvatarDescriptor.AnimLayerType.Gesture,
                         VRCAvatarDescriptor.AnimLayerType.Action,
                         VRCAvatarDescriptor.AnimLayerType.FX,
                         VRCAvatarDescriptor.AnimLayerType.Sitting,
                         VRCAvatarDescriptor.AnimLayerType.TPose,
                         VRCAvatarDescriptor.AnimLayerType.IKPose,
                     })
            {
                ref var loader = ref DefaultLayers[layer];
                var controller = loader.Value;
                if (controller == null)
                    throw new InvalidOperationException($"default controller for {layer} not found");
                controllers[layer] = controller;
            }

            foreach (var layer in descriptor.specialAnimationLayers.Concat(descriptor.baseAnimationLayers))
                controllers[layer.type] = GetPlayableLayerController(layer, useDefaultLayers)!;

            // parse weight changes
            var animatorLayerWeightChanged = new AnimatorLayerMap<AnimatorWeightChangesList>();
            foreach (var layer in new[]
                     {
                         VRCAvatarDescriptor.AnimLayerType.Action,
                         VRCAvatarDescriptor.AnimLayerType.FX,
                         VRCAvatarDescriptor.AnimLayerType.Gesture,
                         VRCAvatarDescriptor.AnimLayerType.Additive,
                     })
                animatorLayerWeightChanged[layer] =
                    new AnimatorWeightChangesList(controllers[layer]?.ComputeLayerCount() ?? 0);
            var playableWeightChanged = new AnimatorLayerMap<AnimatorWeightChange>();
            foreach (var layer in descriptor.baseAnimationLayers)
                VRCSDKUtils.CollectWeightChangesInController(controllers[layer.type],
                    playableWeightChanged, animatorLayerWeightChanged);
            // layer control can be executed on the other animators than avatar root
            // https://github.com/anatawa12/AvatarOptimizer/issues/824
            foreach (var childAnimator in descriptor.transform.GetComponentsInChildren<Animator>())
                VRCSDKUtils.CollectWeightChangesInController(childAnimator.runtimeAnimatorController,
                    playableWeightChanged, animatorLayerWeightChanged);

            if (_mmdWorldCompatibility)
            {
                var fxLayer = animatorLayerWeightChanged[VRCAvatarDescriptor.AnimLayerType.FX];
                fxLayer[1] = fxLayer[1].Merge(AnimatorWeightChange.EitherZeroOrOne);
                fxLayer[2] = fxLayer[2].Merge(AnimatorWeightChange.EitherZeroOrOne);
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
                AnimatorWeightState weightState;
                weightState = alwaysApplied
                    ? AnimatorWeightState.AlwaysOne
                    : GetWeightState(defaultWeight, playableWeightChanged[type]);

                var parsedLayer =
                    ParseAnimatorController(descriptor.gameObject, controllers[type], animatorLayerWeightChanged[type]);
                playableLayers.Add((weightState, mode, parsedLayer));
            }

            var isHumanoid = animator != null && animator.isHuman;
            MergeLayer(VRCAvatarDescriptor.AnimLayerType.Base, true, 1);
            // Station Sitting here
            MergeLayer(VRCAvatarDescriptor.AnimLayerType.Sitting, false, 1);
            if (isHumanoid)
                MergeLayer(VRCAvatarDescriptor.AnimLayerType.Additive, false, 1,
                    AnimatorLayerBlendingMode.Additive); // A.K.A. Idle
            if (isHumanoid) MergeLayer(VRCAvatarDescriptor.AnimLayerType.Gesture, false, 1);
            // Station Action here
            MergeLayer(VRCAvatarDescriptor.AnimLayerType.Action, false, 0);
            MergeLayer(VRCAvatarDescriptor.AnimLayerType.FX, false, 1);

            if (animator != null)
                modifications.Add(NodesMerger.ComponentFromPlayableLayers(animator, playableLayers), true);

            // TPose and IKPose should only affect to Humanoid so skip here~

            if (_mmdWorldCompatibility && descriptor.transform.Find("Body") is { } body && body.TryGetComponent<SkinnedMeshRenderer>(out var bodySkinnedMesh))
            {
                foreach (var shape in MmdBlendShapeNames)
                    modifications.Add(bodySkinnedMesh, $"blendShape.{shape}",
                        new VariableComponentPropModNode(descriptor), ApplyState.Always);
            }
        }

        private static RuntimeAnimatorController? GetPlayableLayerController(VRCAvatarDescriptor.CustomAnimLayer layer,
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
        private static ComponentNodeContainer CollectBlendShapeProxyModifications(BuildContext context,
            VRM.VRMBlendShapeProxy vrmBlendShapeProxy)
        {
            var nodes = new ComponentNodeContainer();

            var bindings = vrmBlendShapeProxy.BlendShapeAvatar.Clips.SelectMany(clip => clip.Values);
            foreach (var binding in bindings.Select(binding => (binding.RelativePath, binding.Index)).Distinct())
            {
                var skinnedMeshRenderer =
                    context.AvatarRootTransform.Find(binding.RelativePath).GetComponent<SkinnedMeshRenderer>();
                var blendShapePropName =
                    $"blendShape.{skinnedMeshRenderer.sharedMesh.GetBlendShapeName(binding.Index)}";
                nodes.Add(skinnedMeshRenderer, blendShapePropName,
                    new VariableComponentPropModNode(vrmBlendShapeProxy));
            }

            // Currently, MaterialValueBindings are guaranteed to not change (MaterialName, in particular)
            // unless MergeToonLitMaterial is used, which breaks material animations anyway.
            // Gather material modifications here once we start tracking material changes...

            return nodes;
        }
#endif

#if AAO_VRM1
        private static ComponentNodeContainer CollectVrm10InstanceModifications( BuildContext context,
            UniVRM10.Vrm10Instance vrm10Instance)
        {
            var nodes = new ComponentNodeContainer();

            var bindings = vrm10Instance.Vrm.Expression.Clips.SelectMany(clip => clip.Clip.MorphTargetBindings);
            foreach (var binding in bindings.Select(binding => (binding.RelativePath, binding.Index)).Distinct())
            {
                var skinnedMeshRenderer =
                    context.AvatarRootTransform.Find(binding.RelativePath).GetComponent<SkinnedMeshRenderer>();
                var blendShapePropName =
                    $"blendShape.{skinnedMeshRenderer.sharedMesh.GetBlendShapeName(binding.Index)}";
                nodes.Add(skinnedMeshRenderer, blendShapePropName,
                    new VariableComponentPropModNode(vrm10Instance));
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
        [return:NotNullIfNotNull("container")]
        private ComponentNodeContainer? AddHumanoidModifications(ComponentNodeContainer? container, Animator animator)
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

        public AnimatorControllerNodeContainer ParseAnimatorController(
            GameObject root,
            RuntimeAnimatorController controller,
            AnimatorWeightChangesList? externallyWeightChanged = null)
        {
            using (ErrorReport.WithContextObject(controller))
            {
                var (animatorController, mapping) = ACUtils.GetControllerAndOverrides(controller);
                return AdvancedParseAnimatorController(root, animatorController, mapping, externallyWeightChanged);
            }
        }

        internal AnimatorControllerNodeContainer AdvancedParseAnimatorController(
            GameObject root,
            AnimatorController controller,
            IReadOnlyDictionary<AnimationClip, AnimationClip> mapping,
            AnimatorWeightChangesList? externallyWeightChanged)
        {
            Profiler.BeginSample("AdvancedParseAnimatorController");
            var layers = controller.layers;
            var result = NodesMerger.AnimatorControllerFromAnimatorLayers(controller.layers.Select((layer, i) =>
            {
                AnimatorWeightState weightState;
                if (i == 0)
                {
                    weightState = AnimatorWeightState.AlwaysOne;
                }
                else
                {
                    var external = externallyWeightChanged?.Get(i) ?? AnimatorWeightChange.NotChanged;

                    weightState = GetWeightState(layers[i].defaultWeight, external);
                }

                var parsedLayer = ParseAnimatorControllerLayer(root, controller, mapping, i);

                return (weightState, layer.blendingMode, parsedLayer);
            }));
            Profiler.EndSample();
            return result;
        }

        public AnimatorLayerNodeContainer ParseAnimatorControllerLayer(
            GameObject root,
            AnimatorController controller,
            IReadOnlyDictionary<AnimationClip, AnimationClip> mapping,
            int layerIndex)
        {
            var layer = controller.layers[layerIndex];

            var syncedLayer = layer.syncedLayerIndex;

            IEnumerable<(AnimatorState, ImmutableNodeContainer)> parsedMotions;

            if (syncedLayer == -1)
            {
                parsedMotions = ACUtils.AllStates(layer.stateMachine)
                    .Select(state => (state, _animationParser.ParseMotion(root, state.motion, mapping)));
            }
            else
            {
                parsedMotions = ACUtils.AllStates(controller.layers[syncedLayer].stateMachine)
                    .Select(state =>
                        (state, _animationParser.ParseMotion(root, layer.GetOverrideMotion(state), mapping)));
            }

            return NodesMerger.Merge<
                AnimatorLayerNodeContainer, AnimatorLayerPropModNode<FloatValueInfo>, AnimatorLayerPropModNode<ObjectValueInfo>,
                AnimatorStatePropModNode<FloatValueInfo>, AnimatorStatePropModNode<ObjectValueInfo>,
                (AnimatorState, ImmutableNodeContainer),
                ImmutableNodeContainer, ImmutablePropModNode<FloatValueInfo>, ImmutablePropModNode<ObjectValueInfo>,
                LayerMerger
            >(parsedMotions, default);
        }

        struct LayerMerger : IMergeProperty1<
            AnimatorLayerNodeContainer, AnimatorLayerPropModNode<FloatValueInfo>, AnimatorLayerPropModNode<ObjectValueInfo>,
            AnimatorStatePropModNode<FloatValueInfo>, AnimatorStatePropModNode<ObjectValueInfo>,
            (AnimatorState, ImmutableNodeContainer),
            ImmutableNodeContainer, ImmutablePropModNode<FloatValueInfo>, ImmutablePropModNode<ObjectValueInfo>
        >
        {
            public AnimatorLayerNodeContainer CreateContainer() => new AnimatorLayerNodeContainer();
            public ImmutableNodeContainer GetContainer((AnimatorState, ImmutableNodeContainer) source) => source.Item2;

            public AnimatorStatePropModNode<FloatValueInfo> GetIntermediate((AnimatorState, ImmutableNodeContainer) source,
                ImmutablePropModNode<FloatValueInfo> node, int index) =>
                new AnimatorStatePropModNode<FloatValueInfo>(node, source.Item1);

            public AnimatorStatePropModNode<ObjectValueInfo> GetIntermediate((AnimatorState, ImmutableNodeContainer) source,
                ImmutablePropModNode<ObjectValueInfo> node, int index) =>
                new AnimatorStatePropModNode<ObjectValueInfo>(node, source.Item1);

            public AnimatorLayerPropModNode<FloatValueInfo>
                MergeNode(List<AnimatorStatePropModNode<FloatValueInfo>> nodes, int sourceCount) =>
                new AnimatorLayerPropModNode<FloatValueInfo>(nodes, nodes.Count != sourceCount ? ApplyState.Partially : ApplyState.Always);

            public AnimatorLayerPropModNode<ObjectValueInfo>
                MergeNode(List<AnimatorStatePropModNode<ObjectValueInfo>> nodes, int sourceCount) =>
                new AnimatorLayerPropModNode<ObjectValueInfo>(nodes, nodes.Count != sourceCount ? ApplyState.Partially : ApplyState.Always);
        }

        AnimatorWeightState GetWeightState(float weight, AnimatorWeightChange external)
        {
            bool isOneWeight;

            if (weight == 0) isOneWeight = false;
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            else if (weight == 1) isOneWeight = true;
            else return AnimatorWeightState.Variable;

            switch (external)
            {
                case AnimatorWeightChange.NotChanged:
                    return isOneWeight ? AnimatorWeightState.AlwaysOne : AnimatorWeightState.AlwaysZero;

                case AnimatorWeightChange.AlwaysZero:
                    return isOneWeight ? AnimatorWeightState.EitherZeroOrOne : AnimatorWeightState.AlwaysZero;

                case AnimatorWeightChange.AlwaysOne:
                    return isOneWeight
                        ? AnimatorWeightState.AlwaysOne
                        : AnimatorWeightState.EitherZeroOrOne;

                case AnimatorWeightChange.EitherZeroOrOne:
                    return AnimatorWeightState.EitherZeroOrOne;
                case AnimatorWeightChange.Variable:
                    return AnimatorWeightState.Variable;
                default:
                    throw new ArgumentOutOfRangeException();
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

        // @formatter:off
        private static readonly string[] MmdBlendShapeNames = new[]
        {
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
        }.Where(x => x != null).Distinct().ToArray()!; // removed null with Where
        // @formatter:on

        #endregion
    }
}
