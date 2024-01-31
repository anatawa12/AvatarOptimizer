using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.AnimatorParsersV2
{
    static partial class NodesMerger
    {
        public static TResultContainer Merge<
            TResultContainer,
            TResultFloatNode,
            TResultObjectNode,

            TIntermediateFloat,
            TIntermediateObject,

            TSource,
            TSourceContainer,
            TSourceFloatNode,
            TSourceObjectNode,

            TMerger
        >(IEnumerable<TSource> sources, TMerger merger)
            where TResultContainer : NodeContainerBase<TResultFloatNode, TResultObjectNode>
            where TResultFloatNode : PropModNode<float>
            where TResultObjectNode : PropModNode<Object>

            where TSourceContainer : INodeContainer<TSourceFloatNode, TSourceObjectNode>

            where TMerger : struct, IMergeProperty1<
                TResultContainer, TResultFloatNode, TResultObjectNode,
                TIntermediateFloat, TIntermediateObject,
                TSource, TSourceContainer, TSourceFloatNode, TSourceObjectNode
            >
        {
            var floats = new Dictionary<(ComponentOrGameObject, string), List<TIntermediateFloat>>();
            var objects = new Dictionary<(ComponentOrGameObject, string), List<TIntermediateObject>>();
            var index = 0;

            foreach (var source in sources)
            {
                var container = merger.GetContainer(source);
                index++;
                foreach (var (key, node) in container.FloatNodes)
                {
                    if (!floats.TryGetValue(key, out var list))
                        floats.Add(key, list = new List<TIntermediateFloat>());
                    list.Add(merger.GetIntermediate(source, node, index));
                }

                foreach (var (key, node) in container.ObjectNodes)
                {
                    if (!objects.TryGetValue(key, out var list))
                        objects.Add(key, list = new List<TIntermediateObject>());
                    list.Add(merger.GetIntermediate(source, node, index));
                }
            }

            var sourceCount = index;

            var nodes = merger.CreateContainer();

            foreach (var ((target, prop), value) in floats)
                nodes.Add(target, prop, merger.MergeNode(value, sourceCount));

            foreach (var ((target, prop), value) in objects)
                nodes.Add(target, prop, merger.MergeNode(value, sourceCount));

            return nodes;
        }
    }

    interface IMergeProperty1 <
        TResultContainer,
        TResultFloatNode,
        TResultObjectNode,

        TIntermediateFloat,
        TIntermediateObject,

        TSource,
        TSourceContainer,
        TSourceFloatNode,
        TSourceObjectNode
    >
    {
        TResultContainer CreateContainer();

        TSourceContainer GetContainer(TSource source);

        TIntermediateFloat GetIntermediate(TSource source, TSourceFloatNode node, int index);
        TIntermediateObject GetIntermediate(TSource source, TSourceObjectNode node, int index);

        TResultFloatNode MergeNode(List<TIntermediateFloat> nodes, int sourceCount);
        TResultObjectNode MergeNode(List<TIntermediateObject> nodes, int sourceCount);
    }

    static partial class NodesMerger 
    {
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
                    new AnimatorPropModNode<float>(animator, new[] { new PlayableLayerNodeInfo<float>(value, 0) }));
            }

            foreach (var ((target, prop), value) in controller.ObjectNodes)
            {
                animatorNodeContainer.Add(target, prop,
                    new AnimatorPropModNode<Object>(animator, new[] { new PlayableLayerNodeInfo<Object>(value, 0) }));
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

            foreach (var ((target, prop), value) in animationClip.ObjectNodes)
            {
                animatorNodeContainer.Add(target, prop, new AnimationComponentPropModNode<Object>(animation, value));
            }

            return animatorNodeContainer;
        }

        public static ComponentNodeContainer ComponentFromPlayableLayers(Animator animator,
            IEnumerable<(AnimatorWeightState, AnimatorLayerBlendingMode, AnimatorControllerNodeContainer)>
                playableLayers)
        {
            Dictionary<(ComponentOrGameObject target, string prop), List<PlayableLayerNodeInfo<float>>> floatNodes =
                new Dictionary<(ComponentOrGameObject, string), List<PlayableLayerNodeInfo<float>>>();
            Dictionary<(ComponentOrGameObject target, string prop), List<PlayableLayerNodeInfo<Object>>> objectNodes =
                new Dictionary<(ComponentOrGameObject, string), List<PlayableLayerNodeInfo<Object>>>();

            var layerIndex = 0;
            foreach (var (weight, mode, container) in playableLayers)
            {
                foreach (var (key, value) in container.FloatNodes)
                {
                    if (!floatNodes.TryGetValue(key, out var list))
                        floatNodes.Add(key, list = new List<PlayableLayerNodeInfo<float>>());
                    list.Add(new PlayableLayerNodeInfo<float>(weight, mode, value, layerIndex));
                }
                foreach (var (key, value) in container.ObjectNodes)
                {
                    if (!objectNodes.TryGetValue(key, out var list))
                        objectNodes.Add(key, list = new List<PlayableLayerNodeInfo<Object>>());
                    list.Add(new PlayableLayerNodeInfo<Object>(weight, mode, value, layerIndex));
                }

                layerIndex++;
            }

            var animatorNodeContainer = new ComponentNodeContainer();

            foreach (var ((target, prop), value) in floatNodes)
            {
                value.Reverse();
                animatorNodeContainer.Add(target, prop, new AnimatorPropModNode<float>(animator, value));
            }
            foreach (var ((target, prop), value) in objectNodes)
            {
                value.Reverse();
                animatorNodeContainer.Add(target, prop, new AnimatorPropModNode<Object>(animator, value));
            }

            return animatorNodeContainer;
        }

        internal static AnimatorControllerNodeContainer AnimatorControllerFromAnimatorLayers(
            IEnumerable<(AnimatorWeightState, AnimatorLayerBlendingMode, AnimatorLayerNodeContainer)> layers)
        {
            Dictionary<(ComponentOrGameObject target, string prop), List<AnimatorLayerNodeInfo<float>>> floatNodes =
                new Dictionary<(ComponentOrGameObject, string), List<AnimatorLayerNodeInfo<float>>>();
            Dictionary<(ComponentOrGameObject target, string prop), List<AnimatorLayerNodeInfo<Object>>> objectNodes =
                new Dictionary<(ComponentOrGameObject, string), List<AnimatorLayerNodeInfo<Object>>>();

            var layerIndex = 0;
            foreach (var (weightState, bendingMode, parsedLayer) in layers)
            {
                if (parsedLayer == null) continue;
                foreach (var (key, value) in parsedLayer.FloatNodes)
                {
                    if (!floatNodes.TryGetValue(key, out var list))
                        floatNodes.Add(key, list = new List<AnimatorLayerNodeInfo<float>>());
                    list.Add(new AnimatorLayerNodeInfo<float>(weightState, bendingMode, value, layerIndex));
                }
                foreach (var (key, value) in parsedLayer.ObjectNodes)
                {
                    if (!objectNodes.TryGetValue(key, out var list))
                        objectNodes.Add(key, list = new List<AnimatorLayerNodeInfo<Object>>());
                    list.Add(new AnimatorLayerNodeInfo<Object>(weightState, bendingMode, value, layerIndex));
                }

                layerIndex++;
            }

            var container = new AnimatorControllerNodeContainer();

            foreach (var ((target, prop), value) in floatNodes)
            {
                var node = AnimatorControllerPropModNode<float>.Create(value);
                if (node != null) container.Add(target, prop, node);
            }
            foreach (var ((target, prop), value) in objectNodes)
            {
                var node = AnimatorControllerPropModNode<Object>.Create(value);
                if (node != null) container.Add(target, prop, node);
            }

            return container;
        }
    }
}
