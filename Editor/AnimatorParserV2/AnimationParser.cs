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
                ImmutableNodeContainer, ImmutablePropModNode<ValueInfo<float>>, ImmutablePropModNode<ValueInfo<Object>>,
                BlendTreeElement<ValueInfo<float>>, BlendTreeElement<ValueInfo<Object>>,
                ImmutableNodeContainer, ImmutableNodeContainer, ImmutablePropModNode<ValueInfo<float>>, 
                ImmutablePropModNode<ValueInfo<Object>>,
                BlendTreeMergeProperty
            >(children.Select(x => ParseMotionInner(root, x.motion, mapping)),
                new BlendTreeMergeProperty(blendTree.blendType));
        }

        internal readonly struct BlendTreeMergeProperty : 
            IMergeProperty1<
                ImmutableNodeContainer, ImmutablePropModNode<ValueInfo<float>>, ImmutablePropModNode<ValueInfo<Object>>,
                BlendTreeElement<ValueInfo<float>>, BlendTreeElement<ValueInfo<Object>>,
                ImmutableNodeContainer, ImmutableNodeContainer, ImmutablePropModNode<ValueInfo<float>>,
                ImmutablePropModNode<ValueInfo<Object>>
            >
        {
            private readonly BlendTreeType _blendType;

            public BlendTreeMergeProperty(BlendTreeType blendType)
            {
                _blendType = blendType;
            }

            public ImmutableNodeContainer CreateContainer() => new();
            public ImmutableNodeContainer GetContainer(ImmutableNodeContainer source) => source;

            public BlendTreeElement<ValueInfo<float>> GetIntermediate(ImmutableNodeContainer source,
                ImmutablePropModNode<ValueInfo<float>> node, int index) => new BlendTreeElement<ValueInfo<float>>(index, node);

            public BlendTreeElement<ValueInfo<Object>> GetIntermediate(ImmutableNodeContainer source,
                ImmutablePropModNode<ValueInfo<Object>> node, int index) => new BlendTreeElement<ValueInfo<Object>>(index, node);

            public ImmutablePropModNode<ValueInfo<float>> MergeNode(List<BlendTreeElement<ValueInfo<float>>> nodes, int sourceCount) =>
                new BlendTreeNode<ValueInfo<float>>(nodes, _blendType, partial: nodes.Count != sourceCount);

            public ImmutablePropModNode<ValueInfo<Object>> MergeNode(List<BlendTreeElement<ValueInfo<Object>>> nodes, int sourceCount) =>
                new BlendTreeNode<ValueInfo<Object>>(nodes, _blendType, partial: nodes.Count != sourceCount);
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

            AnimationClip? additiveReferenceClip;
            // in seconds
            float additiveReferenceFrame;

            using (var serialized = new SerializedObject(clip))
            {
                if (serialized.FindProperty("m_AnimationClipSettings.m_HasAdditiveReferencePose").boolValue)
                {
                    additiveReferenceClip = (AnimationClip?)serialized
                        .FindProperty("m_AnimationClipSettings.m_AdditiveReferencePoseClip").objectReferenceValue;
                    additiveReferenceFrame = serialized
                        .FindProperty("m_AnimationClipSettings.m_AdditiveReferencePoseTime").floatValue;
                }
                else
                {
                    additiveReferenceClip = null;
                    additiveReferenceFrame = 0;
                }
            }

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

                var node = FloatAnimationCurveNode.Create(clip, binding, additiveReferenceClip, additiveReferenceFrame);
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
