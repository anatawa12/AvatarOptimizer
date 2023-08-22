using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Anatawa12.AvatarOptimizer.ErrorReporting;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    class AnimatorParser
    {
        private readonly OptimizerSession _session;
        private readonly TraceAndOptimize _config;

        public AnimatorParser(OptimizerSession session, TraceAndOptimize config)
        {
            _session = session;
            _config = config;
        }

        public ImmutableModificationsContainer GatherAnimationModifications()
        {
            var modificationsContainer = new ModificationsContainer();
            modificationsContainer.MergeAsNewLayer(CollectAvatarRootAnimatorModifications(), alwaysAppliedLayer: true);

            foreach (var child in _session.GetRootComponent<Transform>().DirectChildrenEnumerable())
                WalkForAnimator(child, true, modificationsContainer);

            return modificationsContainer.ToImmutable();
        }

        private void WalkForAnimator(Transform transform, bool parentObjectAlwaysActive,
            ModificationsContainer modificationsContainer)
        {
            var gameObject = transform.gameObject;
            var objectAlwaysActive =
                parentObjectAlwaysActive &&
                modificationsContainer.IsAlwaysTrue(gameObject, "m_IsActive", gameObject.activeSelf);

            var animator = transform.GetComponent<Animator>();
            if (animator && animator.runtimeAnimatorController)
            {
                var runtimeController = animator.runtimeAnimatorController;
                IModificationsContainer parsed;

                parsed = ParseAnimatorController(gameObject, runtimeController);
                parsed = AddHumanoidModifications(parsed, animator);

                modificationsContainer.MergeAsNewLayer(parsed,
                    alwaysAppliedLayer: objectAlwaysActive &&
                                        modificationsContainer.IsAlwaysTrue(animator, "m_Enabled", animator.enabled));
            }

            foreach (var child in transform.DirectChildrenEnumerable())
                WalkForAnimator(child, parentObjectAlwaysActive: objectAlwaysActive, modificationsContainer);
        }


        private IModificationsContainer CollectAvatarRootAnimatorModifications()
        {
            var animator = _session.GetRootComponent<Animator>();
            var descriptor = _session.GetRootComponent<VRCAvatarDescriptor>();

            var modificationsContainer = new ModificationsContainer();

            if (animator)
                modificationsContainer = AddHumanoidModifications(modificationsContainer, animator).ToMutable();

            // process playable layers
            // see https://misskey.niri.la/notes/9ioemawdit
            // see https://creators.vrchat.com/avatars/playable-layers

            var playableWeightChanged = new AnimatorLayerMap<bool>();
            var animatorLayerWeightChanged = new AnimatorLayerMap<BitArray>()
            {
                [VRCAvatarDescriptor.AnimLayerType.Action] = new BitArray(1),
                [VRCAvatarDescriptor.AnimLayerType.FX] = new BitArray(1),
                [VRCAvatarDescriptor.AnimLayerType.Gesture] = new BitArray(1),
                [VRCAvatarDescriptor.AnimLayerType.Additive] = new BitArray(1),
            };
            var useDefaultLayers = !descriptor.customizeAnimationLayers;

            foreach (var layer in descriptor.baseAnimationLayers)
            {
                CollectWeightChangesInController(GetPlayableLayerController(layer, useDefaultLayers));
            }

            void CollectWeightChangesInController(RuntimeAnimatorController runtimeController)
            {
                BuildReport.ReportingObject(runtimeController, () =>
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

                                playableWeightChanged[layer] = true;
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

                                var array = animatorLayerWeightChanged[layer];
                                if (array.Length <= animatorLayerControl.layer)
                                    array.Length = animatorLayerControl.layer + 1;
                                array[animatorLayerControl.layer] = true;
                                break;
                            }
                        }
                    }
                }
            }

            var parsedLayers = new AnimatorLayerMap<IModificationsContainer>();

            foreach (var layer in descriptor.specialAnimationLayers.Concat(descriptor.baseAnimationLayers))
            {
                parsedLayers[layer.type] = ParseAnimatorController(descriptor.gameObject,
                    GetPlayableLayerController(layer, useDefaultLayers),
                    animatorLayerWeightChanged[layer.type]);
            }

            void MergeLayer(VRCAvatarDescriptor.AnimLayerType type, bool? alwaysApplied)
            {
                var alwaysAppliedLayer = alwaysApplied ?? !playableWeightChanged[type];
                modificationsContainer.MergeAsNewLayer(parsedLayers[type], alwaysAppliedLayer: alwaysAppliedLayer);
            }

            MergeLayer(VRCAvatarDescriptor.AnimLayerType.Base, true);
            // Station Sitting
            MergeLayer(VRCAvatarDescriptor.AnimLayerType.Sitting, false);
            MergeLayer(VRCAvatarDescriptor.AnimLayerType.Additive, null); // Idle
            MergeLayer(VRCAvatarDescriptor.AnimLayerType.Gesture, null);
            // Station Action
            MergeLayer(VRCAvatarDescriptor.AnimLayerType.Action, false);
            MergeLayer(VRCAvatarDescriptor.AnimLayerType.FX, true);

            // TPose and IKPose should only affect to Humanoid so skip here~

            switch (descriptor.lipSync)
            {
                // AvatarDescriptorから収集
                case VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape when descriptor.VisemeSkinnedMesh != null:
                {
                    var skinnedMeshRenderer = descriptor.VisemeSkinnedMesh;
                    var updater = modificationsContainer.ModifyComponent(skinnedMeshRenderer);
                    foreach (var blendShape in descriptor.VisemeBlendShapes)
                        updater.AddModification($"blendShape.{blendShape}", AnimationProperty.Variable());
                    break;
                }
                case VRC_AvatarDescriptor.LipSyncStyle.JawFlapBlendShape when descriptor.VisemeSkinnedMesh != null:
                {
                    var skinnedMeshRenderer = descriptor.VisemeSkinnedMesh;
                    var shape = descriptor.MouthOpenBlendShapeName;

                    modificationsContainer.ModifyComponent(skinnedMeshRenderer)
                        .AddModification($"blendShape.{shape}", AnimationProperty.Variable());
                    break;
                }
            }

            if (
                descriptor.customEyeLookSettings.eyelidType == VRCAvatarDescriptor.EyelidType.Blendshapes &&
                descriptor.customEyeLookSettings.eyelidsSkinnedMesh != null
            )
            {
                var skinnedMeshRenderer = descriptor.customEyeLookSettings.eyelidsSkinnedMesh;
                var mesh = skinnedMeshRenderer.sharedMesh;

                var updater = modificationsContainer.ModifyComponent(skinnedMeshRenderer);

                foreach (var blendShape in from index in descriptor.customEyeLookSettings.eyelidsBlendshapes
                         where 0 <= index && index < mesh.blendShapeCount
                         select mesh.GetBlendShapeName(index))
                    updater.AddModification($"blendShape.{blendShape}", AnimationProperty.Variable());
            }

            var bodySkinnedMesh = descriptor.transform.Find("Body")?.GetComponent<SkinnedMeshRenderer>();

            if (_config.mmdWorldCompatibility && bodySkinnedMesh)
            {
                var updater = modificationsContainer.ModifyComponent(bodySkinnedMesh);

                foreach (var shape in MmdBlendShapeNames)
                    updater.AddModification($"blendShape.{shape}", AnimationProperty.Variable());
            }

            return modificationsContainer;
        }

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

                var updater = mutable.ModifyComponent(transform);

                foreach (var key in new[]
                             { "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w" })
                    updater.AddModification(key, AnimationProperty.Variable());
            }

            return mutable;
        }

        private IModificationsContainer ParseAnimatorController(GameObject root, RuntimeAnimatorController controller,
            [CanBeNull] BitArray externallyWeightChanged = null)
        {
            return BuildReport.ReportingObject(controller, () =>
            {
                if (_config.advancedAnimatorParser)
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
        /// Fallback AnimatorController Parser but always assumed as partially applied 
        /// </summary>
        private IModificationsContainer FallbackParseAnimatorController(GameObject root, RuntimeAnimatorController controller)
        {
            return MergeContainersSideBySide(controller.animationClips.Select(clip => GetParsedAnimation(root, clip)));
        }

        private IModificationsContainer AdvancedParseAnimatorController(GameObject root, AnimatorController controller,
            IReadOnlyDictionary<AnimationClip, AnimationClip> mapping, [CanBeNull] BitArray externallyWeightChanged)
        {
            var layers = controller.layers;
            if (layers.Length == 0) return ImmutableModificationsContainer.Empty;

            var mergedController = new ModificationsContainer();

            for (var i = 0; i < layers.Length; i++)
            {
                var layer = layers[i];
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                var alwaysAppliedLayer = layer.defaultWeight != 1 && i != 0 &&
                                         (externallyWeightChanged == null || externallyWeightChanged[i]);
                var syncedLayer = layer.syncedLayerIndex;

                var animationClips = new HashSet<AnimationClip>();

                if (syncedLayer == -1)
                {
                    foreach (var state in CollectStates(layer.stateMachine))
                        CollectClipsInMotion(state.motion);

                }
                else
                {
                    foreach (var state in CollectStates(layers[syncedLayer].stateMachine))
                        CollectClipsInMotion(layer.GetOverrideMotion(state));
                }

                void CollectClipsInMotion(Motion motion)
                {
                    BuildReport.ReportingObject(motion, () =>
                    {
                        switch (motion)
                        {
                            case null:
                                animationClips.Add(null);
                                return;
                            case AnimationClip clip:
                                animationClips.Add(clip);
                                return;
                            case BlendTree blendTree:
                                foreach (var child in blendTree.children)
                                    CollectClipsInMotion(child.motion);
                                return;
                            default:
                                BuildReport.LogFatal("Unknown Motion Type: {0} in motion {1}",
                                    motion.GetType().Name, motion.name);
                                return;
                        }
                    });
                }

                AnimationClip MapClip(AnimationClip clip) => mapping.TryGetValue(clip, out var newClip) ? newClip : clip;

                mergedController.MergeAsNewLayer(
                    MergeContainersSideBySide(animationClips.Select(x => GetParsedAnimation(root, MapClip(x)))),
                    alwaysAppliedLayer);
            }

            return mergedController;
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

        private IModificationsContainer MergeContainersSideBySide<T>([ItemNotNull] IEnumerable<T> enumerable)
            where T : IModificationsContainer
        {
            using (var enumerator = enumerable.GetEnumerator())
            {
                if (!enumerator.MoveNext()) return ImmutableModificationsContainer.Empty;
                var first = enumerator.Current;
                if (!enumerator.MoveNext()) return first;

                // ReSharper disable once PossibleNullReferenceException // miss detections

                // merge all properties
                var merged = first.ToMutable();
                do merged.MergeAsSide(enumerator.Current);
                while (enumerator.MoveNext());

                return merged;
            }
        }

        private (AnimatorController, IReadOnlyDictionary<AnimationClip, AnimationClip>) GetControllerAndOverrides(
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
                var currentOverrides = overridesBuffer.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                foreach (var (original, mapped) in overrides)
                    if (currentOverrides.TryGetValue(mapped, out var newMapped))
                        overrides[original] = newMapped;

                foreach (var (original, mapped) in overridesBuffer)
                    if (!overrides.ContainsKey(original))
                        overrides.Add(original, mapped);
            }
        }

        private readonly Dictionary<(GameObject, AnimationClip), ImmutableModificationsContainer> _parsedAnimationCache =
            new Dictionary<(GameObject, AnimationClip), ImmutableModificationsContainer>();

        private ImmutableModificationsContainer GetParsedAnimation(GameObject root, [CanBeNull] AnimationClip clip)
        {
            if (clip == null) return ImmutableModificationsContainer.Empty;
            if (!_parsedAnimationCache.TryGetValue((root, clip), out var parsed))
                _parsedAnimationCache.Add((root, clip), parsed = ParseAnimation(root, clip));
            return parsed;
        }

        public static ImmutableModificationsContainer ParseAnimation(GameObject root, [NotNull] AnimationClip clip)
        {
            var modifications = new ModificationsContainer();

            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                var obj = AnimationUtility.GetAnimatedObject(root, binding);
                if (obj == null) continue;

                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                var currentPropertyMayNull = AnimationProperty.ParseProperty(curve);

                if (!(currentPropertyMayNull is AnimationProperty currentProperty)) continue;

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (curve[0].time != 0 || curve[curve.length - 1].time != clip.length)
                    currentProperty = currentProperty.PartiallyApplied();

                modifications.ModifyObjectUnsafe(obj)
                    .AddModification(binding.propertyName, currentProperty);
            }

            return modifications.ToImmutable();
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

        private class AnimatorLayerMap<T>
        {
            private T[] _values = new T[(int)(VRCAvatarDescriptor.AnimLayerType.IKPose + 1)];

            public static bool IsValid(VRCAvatarDescriptor.AnimLayerType type)
            {
                switch (type)
                {
                    case VRCAvatarDescriptor.AnimLayerType.Base:
                    case VRCAvatarDescriptor.AnimLayerType.Additive:
                    case VRCAvatarDescriptor.AnimLayerType.Gesture:
                    case VRCAvatarDescriptor.AnimLayerType.Action:
                    case VRCAvatarDescriptor.AnimLayerType.FX:
                    case VRCAvatarDescriptor.AnimLayerType.Sitting:
                    case VRCAvatarDescriptor.AnimLayerType.TPose:
                    case VRCAvatarDescriptor.AnimLayerType.IKPose:
                        return true;
                    case VRCAvatarDescriptor.AnimLayerType.Deprecated0:
                    default:
                        return false;
                }
            }

            public ref T this[VRCAvatarDescriptor.AnimLayerType type]
            {
                get
                {
                    if (!IsValid(type))
                        throw new ArgumentOutOfRangeException(nameof(type), type, null);

                    return ref _values[(int)type];
                }
            }
        }

        private static readonly int LayersCount = (int)(VRCAvatarDescriptor.AnimLayerType.IKPose + 1);

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
    }
}