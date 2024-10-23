using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Profiling;
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
            where TResultFloatNode : PropModNode<FloatValueInfo>
            where TResultObjectNode : PropModNode<ObjectValueInfo>

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

            Profiler.BeginSample("NodesMerger.Merge (main)");

            var nodes = merger.CreateContainer();

            foreach (var ((target, prop), value) in floats)
                nodes.Add(target, prop, merger.MergeNode(value, sourceCount));

            foreach (var ((target, prop), value) in objects)
                nodes.Add(target, prop, merger.MergeNode(value, sourceCount));

            Profiler.EndSample();
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
        where TResultFloatNode : PropModNode<FloatValueInfo>
        where TResultObjectNode : PropModNode<ObjectValueInfo>
        where TSourceContainer : INodeContainer<TSourceFloatNode, TSourceObjectNode>

        where TSource : notnull
        where TIntermediateFloat : notnull
        where TIntermediateObject : notnull
        where TSourceFloatNode : notnull
        where TSourceObjectNode : notnull
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
        [return:NotNullIfNotNull("controller")]
        public static ComponentNodeContainer? AnimatorComponentFromController(Animator animator,
            AnimatorControllerNodeContainer? controller)
        {
            if (controller == null) return null;

            var animatorNodeContainer = new ComponentNodeContainer();

            foreach (var ((target, prop), value) in controller.FloatNodes)
            {
                animatorNodeContainer.Add(target, prop,
                    new AnimatorPropModNode<FloatValueInfo>(animator, new[] { new PlayableLayerNodeInfo<FloatValueInfo>(value, 0) }));
            }

            foreach (var ((target, prop), value) in controller.ObjectNodes)
            {
                animatorNodeContainer.Add(target, prop,
                    new AnimatorPropModNode<ObjectValueInfo>(animator, new[] { new PlayableLayerNodeInfo<ObjectValueInfo>(value, 0) }));
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
                animatorNodeContainer.Add(target, prop, new AnimationComponentPropModNode<FloatValueInfo>(animation, node));
            }

            foreach (var ((target, prop), value) in animationClip.ObjectNodes)
            {
                animatorNodeContainer.Add(target, prop, new AnimationComponentPropModNode<ObjectValueInfo>(animation, value));
            }

            return animatorNodeContainer;
        }

        public static ComponentNodeContainer ComponentFromPlayableLayers(Animator animator,
            IEnumerable<(AnimatorWeightState, AnimatorLayerBlendingMode, AnimatorControllerNodeContainer)>
                playableLayers) =>
            Merge<
                ComponentNodeContainer, ComponentPropModNodeBase<FloatValueInfo>, ComponentPropModNodeBase<ObjectValueInfo>,
                PlayableLayerNodeInfo<FloatValueInfo>, PlayableLayerNodeInfo<ObjectValueInfo>,
                (AnimatorWeightState, AnimatorLayerBlendingMode, AnimatorControllerNodeContainer),
                AnimatorControllerNodeContainer, AnimatorControllerPropModNode<FloatValueInfo>,
                AnimatorControllerPropModNode<ObjectValueInfo>,
                PlayableLayerMerger
            >(playableLayers, new PlayableLayerMerger(animator));

        readonly struct PlayableLayerMerger : IMergeProperty1<
            ComponentNodeContainer, ComponentPropModNodeBase<FloatValueInfo>, ComponentPropModNodeBase<ObjectValueInfo>,
            PlayableLayerNodeInfo<FloatValueInfo>, PlayableLayerNodeInfo<ObjectValueInfo>,
            (AnimatorWeightState, AnimatorLayerBlendingMode, AnimatorControllerNodeContainer),
            AnimatorControllerNodeContainer, AnimatorControllerPropModNode<FloatValueInfo>, AnimatorControllerPropModNode<ObjectValueInfo>
        >
        {
            private readonly Animator _animator;

            public PlayableLayerMerger(Animator animator) => _animator = animator;

            public ComponentNodeContainer CreateContainer() => new ComponentNodeContainer();

            public AnimatorControllerNodeContainer GetContainer(
                (AnimatorWeightState, AnimatorLayerBlendingMode, AnimatorControllerNodeContainer) source) =>
                source.Item3;

            public PlayableLayerNodeInfo<FloatValueInfo> GetIntermediate(
                (AnimatorWeightState, AnimatorLayerBlendingMode, AnimatorControllerNodeContainer?) source,
                AnimatorControllerPropModNode<FloatValueInfo> node, int index) =>
                new(source.Item1, source.Item2, node, index);

            public PlayableLayerNodeInfo<ObjectValueInfo> GetIntermediate(
                (AnimatorWeightState, AnimatorLayerBlendingMode, AnimatorControllerNodeContainer?) source,
                AnimatorControllerPropModNode<ObjectValueInfo> node, int index) =>
                new(source.Item1, source.Item2, node, index);

            public ComponentPropModNodeBase<FloatValueInfo> MergeNode(List<PlayableLayerNodeInfo<FloatValueInfo>> nodes, int sourceCount) =>
                new AnimatorPropModNode<FloatValueInfo>(_animator, nodes);

            public ComponentPropModNodeBase<ObjectValueInfo> MergeNode(List<PlayableLayerNodeInfo<ObjectValueInfo>> nodes, int sourceCount) =>
                new AnimatorPropModNode<ObjectValueInfo>(_animator, nodes);
        }

        internal static AnimatorControllerNodeContainer AnimatorControllerFromAnimatorLayers(
            IEnumerable<(AnimatorWeightState, AnimatorLayerBlendingMode, AnimatorLayerNodeContainer)> layers) =>
            Merge<
                AnimatorControllerNodeContainer, AnimatorControllerPropModNode<FloatValueInfo>, AnimatorControllerPropModNode<ObjectValueInfo>,
                AnimatorLayerNodeInfo<FloatValueInfo>, AnimatorLayerNodeInfo<ObjectValueInfo>,
                (AnimatorWeightState, AnimatorLayerBlendingMode, AnimatorLayerNodeContainer),
                AnimatorLayerNodeContainer, AnimatorLayerPropModNode<FloatValueInfo>, AnimatorLayerPropModNode<ObjectValueInfo>,
                AnimatorLayerMerger
            >(layers, default);

        private struct AnimatorLayerMerger : IMergeProperty1<
            AnimatorControllerNodeContainer, AnimatorControllerPropModNode<FloatValueInfo>, AnimatorControllerPropModNode<ObjectValueInfo>
            ,
            AnimatorLayerNodeInfo<FloatValueInfo>, AnimatorLayerNodeInfo<ObjectValueInfo>,
            (AnimatorWeightState, AnimatorLayerBlendingMode, AnimatorLayerNodeContainer),
            AnimatorLayerNodeContainer, AnimatorLayerPropModNode<FloatValueInfo>, AnimatorLayerPropModNode<ObjectValueInfo>
        >
        {
            public AnimatorControllerNodeContainer CreateContainer() => new AnimatorControllerNodeContainer();

            public AnimatorLayerNodeContainer GetContainer(
                (AnimatorWeightState, AnimatorLayerBlendingMode, AnimatorLayerNodeContainer) source) =>
                source.Item3;

            public AnimatorLayerNodeInfo<FloatValueInfo> GetIntermediate(
                (AnimatorWeightState, AnimatorLayerBlendingMode, AnimatorLayerNodeContainer) source,
                AnimatorLayerPropModNode<FloatValueInfo> node, int index) =>
                new AnimatorLayerNodeInfo<FloatValueInfo>(source.Item1, source.Item2, node, index);

            public AnimatorLayerNodeInfo<ObjectValueInfo> GetIntermediate(
                (AnimatorWeightState, AnimatorLayerBlendingMode, AnimatorLayerNodeContainer) source,
                AnimatorLayerPropModNode<ObjectValueInfo> node, int index) =>
                new AnimatorLayerNodeInfo<ObjectValueInfo>(source.Item1, source.Item2, node, index);

            public AnimatorControllerPropModNode<FloatValueInfo> MergeNode(List<AnimatorLayerNodeInfo<FloatValueInfo>> nodes,
                int sourceCount) =>
                AnimatorControllerPropModNode<FloatValueInfo>.Create(nodes);

            public AnimatorControllerPropModNode<ObjectValueInfo> MergeNode(List<AnimatorLayerNodeInfo<ObjectValueInfo>> nodes,
                int sourceCount) =>
                AnimatorControllerPropModNode<ObjectValueInfo>.Create(nodes);
        }
    }
}
