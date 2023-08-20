using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        public void GatherAnimationModifications()
        {
            foreach (var animator in _session.GetComponents<Animator>())
            {
                GatherAnimationModificationsInController(animator.gameObject, animator.runtimeAnimatorController);
                GatherHumanoidModifications(animator);
            }

            var descriptor = _session.GetRootComponent<VRCAvatarDescriptor>();

            if (descriptor)
            {
                foreach (var layer in descriptor.specialAnimationLayers)
                {
                    GatherAnimationModificationsInController(descriptor.gameObject, GetPlayableLayerController(layer));
                }

                if (descriptor.customizeAnimationLayers)
                {
                    foreach (var layer in descriptor.baseAnimationLayers)
                    {
                        GatherAnimationModificationsInController(descriptor.gameObject,
                            GetPlayableLayerController(layer));
                    }
                }

                switch (descriptor.lipSync)
                {
                    // AvatarDescriptorから収集
                    case VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape when descriptor.VisemeSkinnedMesh != null:
                    {
                        var skinnedMeshRenderer = descriptor.VisemeSkinnedMesh;
                        var updater = _modificationsContainer.ModifyComponent(skinnedMeshRenderer);
                        foreach (var blendShape in descriptor.VisemeBlendShapes)
                            updater.AddModification($"blendShape.{blendShape}", AnimationProperty.Variable());
                        break;
                    }
                    case VRC_AvatarDescriptor.LipSyncStyle.JawFlapBlendShape when descriptor.VisemeSkinnedMesh != null:
                    {
                        var skinnedMeshRenderer = descriptor.VisemeSkinnedMesh;
                        var shape = descriptor.MouthOpenBlendShapeName;

                        _modificationsContainer.ModifyComponent(skinnedMeshRenderer)
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

                    var updater = _modificationsContainer.ModifyComponent(skinnedMeshRenderer);

                    foreach (var blendShape in from index in descriptor.customEyeLookSettings.eyelidsBlendshapes
                             where 0 <= index && index < mesh.blendShapeCount
                             select mesh.GetBlendShapeName(index))
                        updater.AddModification($"blendShape.{blendShape}", AnimationProperty.Variable());
                }

                var bodySkinnedMesh = descriptor.transform.Find("Body")?.GetComponent<SkinnedMeshRenderer>();

                if (_config.mmdWorldCompatibility && bodySkinnedMesh)
                {
                    var updater = _modificationsContainer.ModifyComponent(bodySkinnedMesh);

                    foreach (var shape in MmdBlendShapeNames)
                        updater.AddModification($"blendShape.{shape}", AnimationProperty.Variable());
                }
            }
        }

        /// Mark rotations of humanoid bones as changeable variables
        private void GatherHumanoidModifications(Animator animator)
        {
            // if it's not humanoid, this pass doesn't matter
            if (!animator.isHuman) return;
            for (var bone = HumanBodyBones.Hips; bone < HumanBodyBones.LastBone; bone++)
            {
                var transform = animator.GetBoneTransform(bone);
                if (!transform) continue;

                var updater = _modificationsContainer.ModifyComponent(transform);

                foreach (var key in new[]
                             { "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w" })
                    updater.AddModification(key, AnimationProperty.Variable());
            }
        }

        private void GatherAnimationModificationsInController(GameObject root, RuntimeAnimatorController controller)
        {
            if (controller == null) return;
            FallbackGatherAnimationModificationsInController(root, controller);
        }

        /// <summary>
        /// Fallback AnimatorController Parser but always assumed as partially applied 
        /// </summary>
        private void FallbackGatherAnimationModificationsInController(GameObject root, RuntimeAnimatorController controller)
        {
            foreach (var clip in controller.animationClips)
            {
                _modificationsContainer.MergeAsNewLayer(GetParsedAnimation(root, clip), alwaysAppliedLayer: false);
            }
        }

        private readonly Dictionary<(GameObject, AnimationClip), ImmutableModificationsContainer> _parsedAnimationCache =
            new Dictionary<(GameObject, AnimationClip), ImmutableModificationsContainer>();

        private ImmutableModificationsContainer GetParsedAnimation(GameObject root, AnimationClip clip)
        {
            if (!_parsedAnimationCache.TryGetValue((root, clip), out var parsed))
                _parsedAnimationCache.Add((root, clip), parsed = ParseAnimation(root, clip));
            return parsed;
        }

        public static ImmutableModificationsContainer ParseAnimation(GameObject root, AnimationClip clip)
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

        private readonly ModificationsContainer _modificationsContainer = new ModificationsContainer();

        public IReadOnlyDictionary<Object, IReadOnlyDictionary<string, AnimationProperty>> ModifiedProperties =>
            _modificationsContainer.ModifiedProperties;

        public IReadOnlyDictionary<string, AnimationProperty> GetModifiedProperties(Component component) =>
            _modificationsContainer.GetModifiedProperties(component);

        private IReadOnlyDictionary<string, AnimationProperty> GetModifiedProperties(GameObject gameObject) =>
            _modificationsContainer.GetModifiedProperties(gameObject);

        private static RuntimeAnimatorController GetPlayableLayerController(VRCAvatarDescriptor.CustomAnimLayer layer)
        {
            if (!layer.isDefault && layer.animatorController)
            {
                return layer.animatorController;
            }

            var typeIndex = (int)layer.type;
            if (typeIndex < 0) return null;
            if (typeIndex >= DefaultLayers.Length) return null;
            ref var loader = ref DefaultLayers[typeIndex];
            if (!loader.IsValid) return null;
            var controller = loader.Value;
            if (controller == null)
                throw new InvalidOperationException($"default controller for {layer.type} not found");
            return controller;
        }

        private static readonly CachedGuidLoader<AnimatorController>[] DefaultLayers = CreateDefaultLayers();

        private static CachedGuidLoader<AnimatorController>[] CreateDefaultLayers()
        {
            var array = new CachedGuidLoader<AnimatorController>[(int)(VRCAvatarDescriptor.AnimLayerType.IKPose + 1)];
            // vrc_AvatarV3LocomotionLayer
            array[(int)VRCAvatarDescriptor.AnimLayerType.Base] = "4e4e1a372a526074884b7311d6fc686b";
            // vrc_AvatarV3IdleLayer
            array[(int)VRCAvatarDescriptor.AnimLayerType.Additive] = "573a1373059632b4d820876efe2d277f";
            // vrc_AvatarV3HandsLayer
            array[(int)VRCAvatarDescriptor.AnimLayerType.Gesture] = "404d228aeae421f4590305bc4cdaba16";
            // vrc_AvatarV3ActionLayer
            array[(int)VRCAvatarDescriptor.AnimLayerType.Action] = "3e479eeb9db24704a828bffb15406520";
            // vrc_AvatarV3FaceLayer
            array[(int)VRCAvatarDescriptor.AnimLayerType.FX] = "d40be620cf6c698439a2f0a5144919fe";
            // vrc_AvatarV3SittingLayer
            array[(int)VRCAvatarDescriptor.AnimLayerType.Sitting] = "1268460c14f873240981bf15aa88b21a";
            // vrc_AvatarV3UtilityTPose
            array[(int)VRCAvatarDescriptor.AnimLayerType.TPose] = "00121b5812372b74f9012473856d8acf";
            // vrc_AvatarV3UtilityIKPose
            array[(int)VRCAvatarDescriptor.AnimLayerType.IKPose] = "a9b90a833b3486e4b82834c9d1f7c4ee";
            return array;
        }

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

    interface IModificationsContainer
    {
        IReadOnlyDictionary<Object, IReadOnlyDictionary<string, AnimationProperty>> ModifiedProperties { get; }
    }

    readonly struct ImmutableModificationsContainer : IModificationsContainer
    {
        private readonly IReadOnlyDictionary<Object, IReadOnlyDictionary<string, AnimationProperty>> _modifiedProperties;

        public IReadOnlyDictionary<Object, IReadOnlyDictionary<string, AnimationProperty>> ModifiedProperties =>
            _modifiedProperties ?? Utils.EmptyDictionary<Object, IReadOnlyDictionary<string, AnimationProperty>>();
        public static ImmutableModificationsContainer Empty => default;

        public ImmutableModificationsContainer(ModificationsContainer from)
        {
            IReadOnlyDictionary<string, AnimationProperty> MapDictionary(IReadOnlyDictionary<string, AnimationProperty> dict) =>
                new ReadOnlyDictionary<string, AnimationProperty>(dict.ToDictionary(p1 => p1.Key, p1 => p1.Value));

            _modifiedProperties = new ReadOnlyDictionary<Object, IReadOnlyDictionary<string, AnimationProperty>>(from.ModifiedProperties
                .ToDictionary(p => p.Key, p => MapDictionary(p.Value)));
        }

        public ModificationsContainer ToMutable() => new ModificationsContainer(this);

        public IReadOnlyDictionary<string, AnimationProperty> GetModifiedProperties(Component component)
        {
            return ModifiedProperties.TryGetValue(component, out var value) ? value : Utils.EmptyDictionary<string, AnimationProperty>();
        }

        public IReadOnlyDictionary<string, AnimationProperty> GetModifiedProperties(GameObject component)
        {
            return ModifiedProperties.TryGetValue(component, out var value) ? value : Utils.EmptyDictionary<string, AnimationProperty>();
        }
    }

    class ModificationsContainer : IModificationsContainer
    {
        private readonly Dictionary<Object, Dictionary<string, AnimationProperty>> _modifiedProperties;
        
        private static readonly IReadOnlyDictionary<string, AnimationProperty> EmptyProperties =
            new ReadOnlyDictionary<string, AnimationProperty>(new Dictionary<string, AnimationProperty>());

        public IReadOnlyDictionary<Object, IReadOnlyDictionary<string, AnimationProperty>> ModifiedProperties { get; }

        public ModificationsContainer()
        {
            _modifiedProperties = new Dictionary<Object, Dictionary<string, AnimationProperty>>();
            ModifiedProperties = Utils.CastDic<IReadOnlyDictionary<string, AnimationProperty>>()
                .CastedDic(_modifiedProperties);
        }

        public ModificationsContainer(ImmutableModificationsContainer from)
        {
            Dictionary<string, AnimationProperty> MapDictionary(IReadOnlyDictionary<string, AnimationProperty> dict) =>
                dict.ToDictionary(p1 => p1.Key, p1 => p1.Value);

            _modifiedProperties = from.ModifiedProperties
                .ToDictionary(p => p.Key, p => MapDictionary(p.Value));

            ModifiedProperties = Utils.CastDic<IReadOnlyDictionary<string, AnimationProperty>>()
                .CastedDic(_modifiedProperties);
        }

        public IReadOnlyDictionary<string, AnimationProperty> GetModifiedProperties(Component component)
        {
            return _modifiedProperties.TryGetValue(component, out var value) ? value : Utils.EmptyDictionary<string, AnimationProperty>();
        }

        public IReadOnlyDictionary<string, AnimationProperty> GetModifiedProperties(GameObject component)
        {
            return _modifiedProperties.TryGetValue(component, out var value) ? value : Utils.EmptyDictionary<string, AnimationProperty>();
        }

        public ImmutableModificationsContainer ToImmutable() => new ImmutableModificationsContainer(this);

        #region Adding Modifications

        public ComponentAnimationUpdater ModifyComponent(Component component) =>
            ModifyObjectUnsafe(component);
        public ComponentAnimationUpdater ModifyGameObject(GameObject gameObject) =>
            ModifyObjectUnsafe(gameObject);

        public ComponentAnimationUpdater ModifyObjectUnsafe(Object obj)
        {
            if (!_modifiedProperties.TryGetValue(obj, out var properties))
                _modifiedProperties.Add(obj, properties = new Dictionary<string, AnimationProperty>());
            return new ComponentAnimationUpdater(properties);
        }

        public readonly struct ComponentAnimationUpdater
        {
            private readonly Dictionary<string, AnimationProperty> _properties;

            public ComponentAnimationUpdater(Dictionary<string, AnimationProperty> properties) => _properties = properties;

            public void AddModification(string propertyName, AnimationProperty propertyState)
            {
                if (_properties.TryGetValue(propertyName, out var property))
                    _properties[propertyName] = property.Merge(propertyState);
                else
                    _properties.Add(propertyName, propertyState);
            }
        }

        #endregion

        /// <summary>
        /// Merge the specified Animator as new layer applied after this layer
        /// </summary>
        public void MergeAsNewLayer<T>(T parsed, bool alwaysAppliedLayer) where T : IModificationsContainer
        {
            foreach (var (obj, properties) in parsed.ModifiedProperties)
            {
                var updater = ModifyObjectUnsafe(obj);

                foreach (var (propertyName, propertyState) in properties)
                    updater.AddModification(propertyName, alwaysAppliedLayer ? propertyState : propertyState.PartiallyApplied());
            }
        }
    }

    readonly struct AnimationProperty
    {
        public readonly PropertyState State;
        public readonly float ConstValue;

        private AnimationProperty(PropertyState state, float constValue) =>
            (State, ConstValue) = (state, constValue);

        public static AnimationProperty ConstAlways(float value) =>
            new AnimationProperty(PropertyState.ConstantAlways, value);

        public static AnimationProperty ConstPartially(float value) =>
            new AnimationProperty(PropertyState.ConstantAlways, value);

        public static AnimationProperty Variable() =>
            new AnimationProperty(PropertyState.Variable, float.NaN);

        public AnimationProperty Merge(AnimationProperty b)
        {
            if (State == PropertyState.Variable) return Variable();
            if (b.State == PropertyState.Variable) return Variable();

            // now they are constant.
            if (ConstValue.CompareTo(b.ConstValue) != 0) return Variable();

            var value = ConstValue;

            if (State == PropertyState.ConstantPartially) return ConstPartially(value);
            if (b.State == PropertyState.ConstantPartially) return ConstPartially(value);

            System.Diagnostics.Debug.Assert(State == PropertyState.ConstantAlways);
            System.Diagnostics.Debug.Assert(b.State == PropertyState.ConstantAlways);

            return this;
        }

        public static AnimationProperty? ParseProperty(AnimationCurve curve)
        {
            if (curve.keys.Length == 0) return null;
            if (curve.keys.Length == 1)
                return ConstAlways(curve.keys[0].value);

            float constValue = 0;
            foreach (var (preKey, postKey) in curve.keys.ZipWithNext())
            {
                var preWeighted = preKey.weightedMode == WeightedMode.Out || preKey.weightedMode == WeightedMode.Both;
                var postWeighted = postKey.weightedMode == WeightedMode.In || postKey.weightedMode == WeightedMode.Both;

                if (preKey.value.CompareTo(postKey.value) != 0) return Variable();
                constValue = preKey.value;
                // it's constant
                if (float.IsInfinity(preKey.outWeight) || float.IsInfinity(postKey.inTangent))
                    continue;
                if (preKey.outTangent == 0 && postKey.inTangent == 0)
                    continue;
                if (preWeighted && postWeighted && preKey.outWeight == 0 && postKey.inWeight == 0)
                    continue;
                return Variable();
            }

            return ConstAlways(constValue);
        }

        public AnimationProperty PartiallyApplied()
        {
            switch (State)
            {
                case PropertyState.ConstantAlways:
                    return ConstPartially(ConstValue);
                case PropertyState.ConstantPartially:
                case PropertyState.Variable:
                    return this;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public enum PropertyState
        {
            ConstantAlways,
            ConstantPartially,
            Variable,
        }
    }
}