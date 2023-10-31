using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static Anatawa12.AvatarOptimizer.ErrorReporting.BuildReport;

namespace Anatawa12.AvatarOptimizer.AnimatorParsers
{
    class AnimationParser
    {
        internal IModificationsContainer ParseMotion(GameObject root, Motion motion,
            IReadOnlyDictionary<AnimationClip, AnimationClip> mapping) =>
            ReportingObject(motion, () => ParseMotionInner(root, motion, mapping));

        private IModificationsContainer ParseMotionInner(GameObject root, Motion motion,
            IReadOnlyDictionary<AnimationClip, AnimationClip> mapping)
        {
            switch (motion)
            {
                case null:
                    return ImmutableModificationsContainer.Empty;
                case AnimationClip clip:
                    return GetParsedAnimation(root, mapping.TryGetValue(clip, out var newClip) ? newClip : clip);
                case BlendTree blendTree:
                    return ParseBlendTree(root, blendTree, mapping);
                default:
                    LogFatal("Unknown Motion Type: {0} in motion {1}", motion.GetType().Name, motion.name);
                    return ImmutableModificationsContainer.Empty;
            }
        }

        private IModificationsContainer ParseBlendTree(GameObject root, BlendTree blendTree,
            IReadOnlyDictionary<AnimationClip, AnimationClip> mapping)
        {
            switch (blendTree.blendType)
            {
                case BlendTreeType.Simple1D:
                case BlendTreeType.SimpleDirectional2D:
                case BlendTreeType.FreeformDirectional2D:
                case BlendTreeType.FreeformCartesian2D:
                    // in those blend blend total blend is always 1 so
                    // if all animation sets same value, the result will also be same value.
                    return blendTree.children.Select(x => ParseMotionInner(root, x.motion, mapping)).MergeContainersSideBySide();
                case BlendTreeType.Direct:
                    // in direct blend tree, total blend can be not zero so all properties are Variable.
                    var merged = blendTree.children.Select(x => ParseMotionInner(root, x.motion, mapping))
                        .MergeContainersSideBySide()
                        .ToMutable();

                    merged.MakeAllVariable();
                    return merged;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private readonly Dictionary<(GameObject, AnimationClip), ImmutableModificationsContainer> _parsedAnimationCache =
            new Dictionary<(GameObject, AnimationClip), ImmutableModificationsContainer>();

        internal ImmutableModificationsContainer GetParsedAnimation(GameObject root, [CanBeNull] AnimationClip clip)
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
                var componentOrGameObject = obj is Component component ? (ComponentOrGameObject)component
                    : obj is GameObject gameObject ? (ComponentOrGameObject)gameObject
                    : throw new InvalidOperationException($"unexpected animated object: {obj} ({obj.GetType().Name}");

                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                var currentPropertyMayNull = AnimationProperty.ParseProperty(curve, clip);

                if (!(currentPropertyMayNull is AnimationProperty currentProperty)) continue;

                modifications.ModifyObject(componentOrGameObject)
                    .AddModificationAsNewLayer(binding.propertyName, currentProperty);
            }

            return modifications.ToImmutable();
        }
    }
}
