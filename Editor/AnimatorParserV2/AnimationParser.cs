using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Profiling;
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
                ImmutableNodeContainer, ImmutablePropModNode<FloatValueInfo>, ImmutablePropModNode<ObjectValueInfo>,
                BlendTreeElement<FloatValueInfo>, BlendTreeElement<ObjectValueInfo>,
                ImmutableNodeContainer, ImmutableNodeContainer, ImmutablePropModNode<FloatValueInfo>, 
                ImmutablePropModNode<ObjectValueInfo>,
                BlendTreeMergeProperty
            >(children
                        // As far as I tested, Unity Editor just ignores null motion in BlendTree.
                        // See https://github.com/anatawa12/AvatarOptimizer/discussions/1489#discussioncomment-14211785
                    .Where(x => x.motion != null)
                    .Select(x => ParseMotionInner(root, x.motion, mapping)),
                new BlendTreeMergeProperty(blendTree.blendType));
        }

        internal readonly struct BlendTreeMergeProperty : 
            IMergeProperty1<
                ImmutableNodeContainer, ImmutablePropModNode<FloatValueInfo>, ImmutablePropModNode<ObjectValueInfo>,
                BlendTreeElement<FloatValueInfo>, BlendTreeElement<ObjectValueInfo>,
                ImmutableNodeContainer, ImmutableNodeContainer, ImmutablePropModNode<FloatValueInfo>,
                ImmutablePropModNode<ObjectValueInfo>
            >
        {
            private readonly BlendTreeType _blendType;

            public BlendTreeMergeProperty(BlendTreeType blendType)
            {
                _blendType = blendType;
            }

            public ImmutableNodeContainer CreateContainer() => new();
            public ImmutableNodeContainer GetContainer(ImmutableNodeContainer source) => source;

            public BlendTreeElement<FloatValueInfo> GetIntermediate(ImmutableNodeContainer source,
                ImmutablePropModNode<FloatValueInfo> node, int index) => new BlendTreeElement<FloatValueInfo>(index, node);

            public BlendTreeElement<ObjectValueInfo> GetIntermediate(ImmutableNodeContainer source,
                ImmutablePropModNode<ObjectValueInfo> node, int index) => new BlendTreeElement<ObjectValueInfo>(index, node);

            public ImmutablePropModNode<FloatValueInfo> MergeNode(List<BlendTreeElement<FloatValueInfo>> nodes, int sourceCount) =>
                new BlendTreeNode<FloatValueInfo>(nodes, _blendType, partial: nodes.Count != sourceCount);

            public ImmutablePropModNode<ObjectValueInfo> MergeNode(List<BlendTreeElement<ObjectValueInfo>> nodes, int sourceCount) =>
                new BlendTreeNode<ObjectValueInfo>(nodes, _blendType, partial: nodes.Count != sourceCount);
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
            Profiler.BeginSample("ParseAnimation");
            var nodes = new ImmutableNodeContainer();

            AnimationClip? additiveReferenceClip;
            // in seconds
            float additiveReferenceFrame;

            {
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                if (settings.hasAdditiveReferencePose)
                {
                    additiveReferenceClip = settings.additiveReferencePoseClip;
                    additiveReferenceFrame = settings.additiveReferencePoseTime;
                }
                else
                {
                    additiveReferenceClip = null;
                    additiveReferenceFrame = 0;
                }
            }

            Profiler.BeginSample("ParseAnimation.GetCurveBindings");
            var floatBindings = AnimationUtility.GetCurveBindings(clip);
            Profiler.EndSample();
            Profiler.BeginSample("AnimationParser.ProcessFloatNodes");
            foreach (var binding in floatBindings)
            {
                var obj = Utils.GetAnimatedObject(root, binding);
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
                nodes.Set(componentOrGameObject, propertyName, node);
            }
            Profiler.EndSample();

            Profiler.BeginSample("ParseAnimation.GetObjectReferenceCurveBindings");
            var objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            Profiler.EndSample();
            Profiler.BeginSample("ProcessObjectNodes");
            foreach (var binding in objectBindings)
            {
                var obj = Utils.GetAnimatedObject(root, binding);
                if (obj == null) continue;
                var componentOrGameObject = obj is Component component ? (ComponentOrGameObject)component
                    : obj is GameObject gameObject ? (ComponentOrGameObject)gameObject
                    : throw new InvalidOperationException($"unexpected animated object: {obj} ({obj.GetType().Name}");

                var node = ObjectAnimationCurveNode.Create(clip, binding);
                if (node == null) continue;
                nodes.Set(componentOrGameObject, binding.propertyName, node);
            }
            Profiler.EndSample();

            Profiler.EndSample();
            return nodes;
        }
    }

}
