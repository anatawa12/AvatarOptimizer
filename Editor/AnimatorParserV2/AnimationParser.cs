using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.AnimatorParsersV2
{
    class AnimationParser
    {
        internal ImmutableNodeContainer ParseMotion(GameObject root, Motion? motion,
            IReadOnlyDictionary<AnimationClip, AnimationClip> mapping)
        {
            using (ErrorReport.WithContextObject(motion))
                return ParseMotionInner(root, motion, mapping);
        }

        private ImmutableNodeContainer ParseMotionInner(GameObject root, Motion? motion,
            IReadOnlyDictionary<AnimationClip, AnimationClip> mapping)
        {
            switch (motion)
            {
                case null:
                    return new ImmutableNodeContainer();
                case AnimationClip clip:
                    return GetParsedAnimation(root, mapping.TryGetValue(clip, out var newClip) ? newClip : clip);
                case BlendTree blendTree:
                    return ParseBlendTree(root, blendTree, mapping);
                default:
                    BuildLog.LogError("Unknown Motion Type: {0} in motion {1}", motion.GetType().Name, motion.name);
                    return new ImmutableNodeContainer();
            }
        }

        private ImmutableNodeContainer ParseBlendTree(GameObject root, BlendTree blendTree,
            IReadOnlyDictionary<AnimationClip, AnimationClip> mapping)
        {
            // for empty tree, return empty container
            if (blendTree.children.Length == 0)
                return new ImmutableNodeContainer();

            // for single child tree, return child
            if (blendTree.children.Length == 1 && blendTree.blendType != BlendTreeType.Direct)
                return ParseMotionInner(root, blendTree.children[0].motion, mapping);

            var children = blendTree.children;

            return NodesMerger.Merge<
                ImmutableNodeContainer, ImmutablePropModNode<float>, ImmutablePropModNode<Object>,
                BlendTreeElement<float>, BlendTreeElement<Object>,
                ImmutableNodeContainer, ImmutableNodeContainer, ImmutablePropModNode<float>, 
                ImmutablePropModNode<Object>,
                BlendTreeMergeProperty
            >(children.Select(x => ParseMotionInner(root, x.motion, mapping)),
                new BlendTreeMergeProperty(blendTree.blendType));
        }

        internal readonly struct BlendTreeMergeProperty : 
            IMergeProperty1<
                ImmutableNodeContainer, ImmutablePropModNode<float>, ImmutablePropModNode<Object>,
                BlendTreeElement<float>, BlendTreeElement<Object>,
                ImmutableNodeContainer, ImmutableNodeContainer, ImmutablePropModNode<float>,
                ImmutablePropModNode<Object>
            >
        {
            private readonly BlendTreeType _blendType;

            public BlendTreeMergeProperty(BlendTreeType blendType)
            {
                _blendType = blendType;
            }

            public ImmutableNodeContainer CreateContainer() => new();
            public ImmutableNodeContainer GetContainer(ImmutableNodeContainer source) => source;

            public BlendTreeElement<float> GetIntermediate(ImmutableNodeContainer source,
                ImmutablePropModNode<float> node, int index) => new BlendTreeElement<float>(index, node);

            public BlendTreeElement<Object> GetIntermediate(ImmutableNodeContainer source,
                ImmutablePropModNode<Object> node, int index) => new BlendTreeElement<Object>(index, node);

            public ImmutablePropModNode<float> MergeNode(List<BlendTreeElement<float>> nodes, int sourceCount) =>
                new BlendTreeNode<float>(nodes, _blendType, partial: nodes.Count != sourceCount);

            public ImmutablePropModNode<Object> MergeNode(List<BlendTreeElement<Object>> nodes, int sourceCount) =>
                new BlendTreeNode<Object>(nodes, _blendType, partial: nodes.Count != sourceCount);
        }

        private readonly Dictionary<(GameObject, AnimationClip), ImmutableNodeContainer> _parsedAnimationCache = new();

        internal ImmutableNodeContainer GetParsedAnimation(GameObject root, AnimationClip? clip)
        {
            if (clip == null) return new ImmutableNodeContainer();
            if (!_parsedAnimationCache.TryGetValue((root, clip), out var parsed))
                _parsedAnimationCache.Add((root, clip), parsed = ParseAnimation(root, clip));
            return parsed;
        }

        public static ImmutableNodeContainer ParseAnimation(GameObject root, AnimationClip clip)
        {
            var nodes = new ImmutableNodeContainer();

            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                var obj = AnimationUtility.GetAnimatedObject(root, binding);
                if (obj == null) continue;
                var componentOrGameObject = obj is Component component ? (ComponentOrGameObject)component
                    : obj is GameObject gameObject ? (ComponentOrGameObject)gameObject
                    : throw new InvalidOperationException($"unexpected animated object: {obj} ({obj.GetType().Name}");

                var propertyName = binding.propertyName;
                // For Animator component, to toggle `m_Enabled` as a property of `Behavior`, 
                //  we have to use `Behavior.m_Enabled` instead if `Animator.m_Enabled` so we store as different property name.
                if (binding.type == typeof(Behaviour) && propertyName == "m_Enabled")
                    propertyName = Props.EnabledFor(obj);

                var node = FloatAnimationCurveNode.Create(clip, binding);
                if (node == null) continue;
                nodes.Set(componentOrGameObject, propertyName, node);
            }

            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                var obj = AnimationUtility.GetAnimatedObject(root, binding);
                if (obj == null) continue;
                var componentOrGameObject = obj is Component component ? (ComponentOrGameObject)component
                    : obj is GameObject gameObject ? (ComponentOrGameObject)gameObject
                    : throw new InvalidOperationException($"unexpected animated object: {obj} ({obj.GetType().Name}");

                var node = ObjectAnimationCurveNode.Create(clip, binding);
                if (node == null) continue;
                nodes.Set(componentOrGameObject, binding.propertyName, node);
            }

            return nodes;
        }
    }

}
