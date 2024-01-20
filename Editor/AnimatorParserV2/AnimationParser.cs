using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.AnimatorParsersV2
{
    class AnimationParser
    {
        internal NodeContainer ParseMotion(GameObject root, Motion motion,
            IReadOnlyDictionary<AnimationClip, AnimationClip> mapping)
        {
            using (ErrorReport.WithContextObject(motion))
                return ParseMotionInner(root, motion, mapping);
        }

        private NodeContainer ParseMotionInner(GameObject root, Motion motion,
            IReadOnlyDictionary<AnimationClip, AnimationClip> mapping)
        {
            switch (motion)
            {
                case null:
                    return new NodeContainer();
                case AnimationClip clip:
                    return GetParsedAnimation(root, mapping.TryGetValue(clip, out var newClip) ? newClip : clip);
                case BlendTree blendTree:
                    return ParseBlendTree(root, blendTree, mapping);
                default:
                    BuildLog.LogError("Unknown Motion Type: {0} in motion {1}", motion.GetType().Name, motion.name);
                    return new NodeContainer();
            }
        }

        private NodeContainer ParseBlendTree(GameObject root, BlendTree blendTree,
            IReadOnlyDictionary<AnimationClip, AnimationClip> mapping)
        {
            // for empty tree, return empty container
            if (blendTree.children.Length == 0)
                return new NodeContainer();

            // for single child tree, return child
            if (blendTree.children.Length == 1 && blendTree.blendType != BlendTreeType.Direct)
                return ParseMotionInner(root, blendTree.children[0].motion, mapping);

            var floats = new Dictionary<(ComponentOrGameObject, string), List<ImmutablePropModNode<float>>>();

            var children = blendTree.children;

            foreach (var container in children.Select(x => ParseMotionInner(root, x.motion, mapping)))
            {
                foreach (var (key, value) in container.FloatNodes)
                {
                    if (!floats.TryGetValue(key, out var list))
                        floats.Add(key, list = new List<ImmutablePropModNode<float>>());
                    list.Add(value);
                }
            }

            var nodes = new NodeContainer();

            foreach (var ((gameObject, prop), value) in floats)
            {
                nodes.Add(gameObject, prop, new BlendTreeNode<float>(value, blendTree.blendType, partial: value.Count != children.Length));
            }

            return nodes;
        }

        private readonly Dictionary<(GameObject, AnimationClip), NodeContainer> _parsedAnimationCache =
            new Dictionary<(GameObject, AnimationClip), NodeContainer>();

        internal NodeContainer GetParsedAnimation(GameObject root, [CanBeNull] AnimationClip clip)
        {
            if (clip == null) return new NodeContainer();
            if (!_parsedAnimationCache.TryGetValue((root, clip), out var parsed))
                _parsedAnimationCache.Add((root, clip), parsed = ParseAnimation(root, clip));
            return parsed;
        }

        public static NodeContainer ParseAnimation(GameObject root, [NotNull] AnimationClip clip)
        {
            var nodes = new NodeContainer();

            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                var obj = AnimationUtility.GetAnimatedObject(root, binding);
                if (obj == null) continue;
                var componentOrGameObject = obj is Component component ? (ComponentOrGameObject)component
                    : obj is GameObject gameObject ? (ComponentOrGameObject)gameObject
                    : throw new InvalidOperationException($"unexpected animated object: {obj} ({obj.GetType().Name}");

                var node = FloatAnimationCurveNode.Create(clip, binding);
                if (node == null) continue;
                nodes.Add(componentOrGameObject, binding.propertyName, node);
            }

            return nodes;
        }
    }
}
