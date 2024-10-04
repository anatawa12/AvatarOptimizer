using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
            where TSource : notnull
            where TIntermediateFloat : notnull
            where TIntermediateObject : notnull
            where TSourceFloatNode : notnull
            where TSourceObjectNode : notnull

            where TResultContainer : NodeContainerBase<TResultFloatNode, TResultObjectNode>
            where TResultFloatNode : PropModNode<ValueInfo<float>>
            where TResultObjectNode : PropModNode<ValueInfo<Object>>

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
            var sourceCount = 0;

            foreach (var source in sources)
            {
                index++;
                var container = merger.GetContainer(source);
                if (container == null) continue;
                sourceCount++;
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

            var nodes = merger.CreateContainer();

            foreach (var ((target, prop), value) in floats)
                if (merger.MergeNode(value, sourceCount) is TResultFloatNode merged)
                    nodes.Add(target, prop, merged);

            foreach (var ((target, prop), value) in objects)
                if (merger.MergeNode(value, sourceCount) is TResultObjectNode merged)
                    nodes.Add(target, prop, merged);

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
        where TResultContainer : NodeContainerBase<TResultFloatNode, TResultObjectNode>
        where TResultFloatNode : PropModNode<ValueInfo<float>>
        where TResultObjectNode : PropModNode<ValueInfo<Object>>
        where TSourceContainer : INodeContainer<TSourceFloatNode, TSourceObjectNode>

        where TSource : notnull
        where TIntermediateFloat : notnull
        where TIntermediateObject : notnull
        where TSourceFloatNode : notnull
        where TSourceObjectNode : notnull
    {
        TResultContainer CreateContainer();

        TSourceContainer? GetContainer(TSource source);

        TIntermediateFloat GetIntermediate(TSource source, TSourceFloatNode node, int index);
        TIntermediateObject GetIntermediate(TSource source, TSourceObjectNode node, int index);

        TResultFloatNode? MergeNode(List<TIntermediateFloat> nodes, int sourceCount);
        TResultObjectNode? MergeNode(List<TIntermediateObject> nodes, int sourceCount);
    }

    static partial class NodesMerger 
    {
        [return:NotNullIfNotNull("controller")]
        public static ComponentNodeContainer? AnimatorComponentFromController(Animator animator,
            AnimatorControllerNodeContainer? controller)
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

        public static ComponentNodeContainer AnimationComponentFromAnimationClip(Animation animation,
            ImmutableNodeContainer animationClip)
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
            IEnumerable<(AnimatorWeightState, AnimatorLayerBlendingMode, AnimatorControllerNodeContainer?)>
                playableLayers) =>
            Merge<
                ComponentNodeContainer, ComponentPropModNodeBase<ValueInfo<float>>, ComponentPropModNodeBase<ValueInfo<Object>>,
                PlayableLayerNodeInfo<float>, PlayableLayerNodeInfo<Object>,
                (AnimatorWeightState, AnimatorLayerBlendingMode, AnimatorControllerNodeContainer?),
                AnimatorControllerNodeContainer, AnimatorControllerPropModNode<float>,
                AnimatorControllerPropModNode<Object>,
                PlayableLayerMerger
            >(playableLayers, new PlayableLayerMerger(animator));

        readonly struct PlayableLayerMerger : IMergeProperty1<
            ComponentNodeContainer, ComponentPropModNodeBase<ValueInfo<float>>, ComponentPropModNodeBase<ValueInfo<Object>>,
            PlayableLayerNodeInfo<float>, PlayableLayerNodeInfo<Object>,
            (AnimatorWeightState, AnimatorLayerBlendingMode, AnimatorControllerNodeContainer?),
            AnimatorControllerNodeContainer, AnimatorControllerPropModNode<float>, AnimatorControllerPropModNode<Object>
        >
        {
            private readonly Animator _animator;

            public PlayableLayerMerger(Animator animator) => _animator = animator;

            public ComponentNodeContainer CreateContainer() => new ComponentNodeContainer();

            public AnimatorControllerNodeContainer? GetContainer(
                (AnimatorWeightState, AnimatorLayerBlendingMode, AnimatorControllerNodeContainer?) source) =>
                source.Item3;

            public PlayableLayerNodeInfo<float> GetIntermediate(
                (AnimatorWeightState, AnimatorLayerBlendingMode, AnimatorControllerNodeContainer?) source,
                AnimatorControllerPropModNode<float> node, int index) =>
                new PlayableLayerNodeInfo<float>(source.Item1, source.Item2, node, index);

            public PlayableLayerNodeInfo<Object> GetIntermediate(
                (AnimatorWeightState, AnimatorLayerBlendingMode, AnimatorControllerNodeContainer?) source,
                AnimatorControllerPropModNode<Object> node, int index) =>
                new PlayableLayerNodeInfo<Object>(source.Item1, source.Item2, node, index);

            public ComponentPropModNodeBase<ValueInfo<float>> MergeNode(List<PlayableLayerNodeInfo<float>> nodes, int sourceCount) =>
                new AnimatorPropModNode<float>(_animator, nodes);

            public ComponentPropModNodeBase<ValueInfo<Object>> MergeNode(List<PlayableLayerNodeInfo<Object>> nodes, int sourceCount) =>
                new AnimatorPropModNode<Object>(_animator, nodes);
        }

        internal static AnimatorControllerNodeContainer AnimatorControllerFromAnimatorLayers(
            IEnumerable<(AnimatorWeightState, AnimatorLayerBlendingMode, AnimatorLayerNodeContainer?)> layers) =>
            Merge<
                AnimatorControllerNodeContainer, AnimatorControllerPropModNode<float>, AnimatorControllerPropModNode<Object>,
                AnimatorLayerNodeInfo<float>, AnimatorLayerNodeInfo<Object>,
                (AnimatorWeightState, AnimatorLayerBlendingMode, AnimatorLayerNodeContainer?),
                AnimatorLayerNodeContainer, AnimatorLayerPropModNode<float>, AnimatorLayerPropModNode<Object>,
                AnimatorLayerMerger
            >(layers, default);

        private struct AnimatorLayerMerger : IMergeProperty1<
            AnimatorControllerNodeContainer, AnimatorControllerPropModNode<float>, AnimatorControllerPropModNode<Object>
            ,
            AnimatorLayerNodeInfo<float>, AnimatorLayerNodeInfo<Object>,
            (AnimatorWeightState, AnimatorLayerBlendingMode, AnimatorLayerNodeContainer?),
            AnimatorLayerNodeContainer, AnimatorLayerPropModNode<float>, AnimatorLayerPropModNode<Object>
        >
        {
            public AnimatorControllerNodeContainer CreateContainer() => new AnimatorControllerNodeContainer();

            public AnimatorLayerNodeContainer? GetContainer(
                (AnimatorWeightState, AnimatorLayerBlendingMode, AnimatorLayerNodeContainer?) source) =>
                source.Item3;

            public AnimatorLayerNodeInfo<float> GetIntermediate(
                (AnimatorWeightState, AnimatorLayerBlendingMode, AnimatorLayerNodeContainer?) source,
                AnimatorLayerPropModNode<float> node, int index) =>
                new AnimatorLayerNodeInfo<float>(source.Item1, source.Item2, node, index);

            public AnimatorLayerNodeInfo<Object> GetIntermediate(
                (AnimatorWeightState, AnimatorLayerBlendingMode, AnimatorLayerNodeContainer?) source,
                AnimatorLayerPropModNode<Object> node, int index) =>
                new AnimatorLayerNodeInfo<Object>(source.Item1, source.Item2, node, index);

            public AnimatorControllerPropModNode<float>? MergeNode(List<AnimatorLayerNodeInfo<float>> nodes,
                int sourceCount) =>
                AnimatorControllerPropModNode<float>.Create(nodes);

            public AnimatorControllerPropModNode<Object>? MergeNode(List<AnimatorLayerNodeInfo<Object>> nodes,
                int sourceCount) =>
                AnimatorControllerPropModNode<Object>.Create(nodes);
        }
    }
}
