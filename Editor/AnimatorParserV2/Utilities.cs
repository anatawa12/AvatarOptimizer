using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor.Animations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.AnimatorParsersV2
{
    static class NodesMerger
    {
        public static ImmutableNodeContainer Merge<Merge>(IEnumerable<ImmutableNodeContainer> sources, Merge merger)
            where Merge : struct, IMergeProperty
        {
            var floats = new Dictionary<(ComponentOrGameObject, string), List<ImmutablePropModNode<float>>>();
            var sourceCount = 0;

            foreach (var container in sources)
            {
                sourceCount++;
                foreach (var (key, value) in container.FloatNodes)
                {
                    if (!floats.TryGetValue(key, out var list))
                        floats.Add(key, list = new List<ImmutablePropModNode<float>>());
                    list.Add(value);
                }
            }

            var nodes = new ImmutableNodeContainer();

            foreach (var ((target, prop), value) in floats)
                nodes.Add(target, prop, merger.MergeNode(value, sourceCount));

            return nodes;
        }

        [CanBeNull]
        [ContractAnnotation("controller: null => null")]
        [ContractAnnotation("controller: notnull => notnull")]
        public static ComponentNodeContainer AnimatorComponentFromController(Animator animator,
            [CanBeNull] AnimatorControllerNodeContainer controller)
        {
            if (controller == null) return null;

            var animatorNodeContainer = new ComponentNodeContainer();

            foreach (var ((target, prop), value) in controller.FloatNodes)
            {
                animatorNodeContainer.Add(target, prop,
                    new AnimatorPropModNode<float>(animator, new[] { new PlayableLayerNodeInfo<float>(value) }));
            }

            return animatorNodeContainer;
        }

        [NotNull]
        public static ComponentNodeContainer AnimationComponentFromAnimationClip(Animation animation,
            [NotNull] ImmutableNodeContainer animationClip)
        {
            if (animationClip == null) throw new ArgumentNullException(nameof(animationClip));
            var animatorNodeContainer = new ComponentNodeContainer();

            foreach (var ((target, prop), node) in animationClip.FloatNodes)
            {
                animatorNodeContainer.Add(target, prop, new AnimationComponentPropModNode<float>(animation, node));
            }

            return animatorNodeContainer;
        }

        public static ComponentNodeContainer ComponentFromPlayableLayers(Animator animator,
            IEnumerable<(AnimatorWeightState, AnimatorLayerBlendingMode, AnimatorControllerNodeContainer)>
                playableLayers)
        {
            Dictionary<(ComponentOrGameObject target, string prop), List<PlayableLayerNodeInfo<float>>> floatNodes =
                new Dictionary<(ComponentOrGameObject, string), List<PlayableLayerNodeInfo<float>>>();

            foreach (var (weight, mode, container) in playableLayers)
            {
                foreach (var (key, value) in container.FloatNodes)
                {
                    if (!floatNodes.TryGetValue(key, out var list))
                        floatNodes.Add(key, list = new List<PlayableLayerNodeInfo<float>>());
                    list.Add(new PlayableLayerNodeInfo<float>(weight, mode, value));
                }
            }

            var animatorNodeContainer = new ComponentNodeContainer();

            foreach (var ((target, prop), value) in floatNodes)
            {
                value.Reverse();
                animatorNodeContainer.Add(target, prop, new AnimatorPropModNode<float>(animator, value));
            }

            return animatorNodeContainer;
        }
    }
    
    interface IMergeProperty
    {
        ImmutablePropModNode<T> MergeNode<T>(List<ImmutablePropModNode<T>> nodes, int sourceCount);
    }
}