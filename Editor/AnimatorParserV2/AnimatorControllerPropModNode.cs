using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using nadena.dev.ndmf;
using UnityEditor.Animations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.AnimatorParsersV2
{
    class HumanoidAnimatorPropModNode : ComponentPropModNode<float>
    {
        public HumanoidAnimatorPropModNode([NotNull] Animator component) : base(component)
        {
        }

        public override ValueInfo<float> Value => ValueInfo<float>.Variable;
        public override bool AppliedAlways => true;
    }

    internal readonly struct PlayableLayerNodeInfo<T> : ILayer<T>
    {
        public AnimatorWeightState Weight { get; }
        public AnimatorLayerBlendingMode BlendingMode { get; }
        public int LayerIndex { get; }
        public readonly AnimatorControllerPropModNode<T> Node;
        PropModNode<T> ILayer<T>.Node => Node;
        IPropModNode ILayer.Node => Node;

        public PlayableLayerNodeInfo(AnimatorWeightState weight, AnimatorLayerBlendingMode blendingMode,
            AnimatorControllerPropModNode<T> node, int layerIndex)
        {
            Weight = weight;
            BlendingMode = blendingMode;
            LayerIndex = layerIndex;
            Node = node;
        }
        
        public PlayableLayerNodeInfo(AnimatorControllerPropModNode<T> node, int layerIndex)
        {
            Weight = AnimatorWeightState.AlwaysOne;
            BlendingMode = AnimatorLayerBlendingMode.Override;
            Node = node;
            LayerIndex = layerIndex;
        }
    }

    class AnimatorPropModNode<T> : ComponentPropModNode<T>
    {
        private readonly IEnumerable<PlayableLayerNodeInfo<T>> _layersReversed;

        public AnimatorPropModNode(
            [NotNull] Animator component,
            IEnumerable<PlayableLayerNodeInfo<T>> layersReversed
        ) : base(component)
        {
            _layersReversed = layersReversed;

            _appliedAlways = new Lazy<bool>(
                () => NodeImplUtils.AlwaysAppliedForOverriding<T, PlayableLayerNodeInfo<T>>(_layersReversed),
                isThreadSafe: false);

            _constantInfo = new Lazy<ValueInfo<T>>(
                () => NodeImplUtils.ConstantInfoForOverriding<T, PlayableLayerNodeInfo<T>>(_layersReversed),
                isThreadSafe: false);
        }


        private readonly Lazy<bool> _appliedAlways;
        private readonly Lazy<ValueInfo<T>> _constantInfo;

        public IEnumerable<PlayableLayerNodeInfo<T>> LayersReversed => _layersReversed;
        public override bool AppliedAlways => _appliedAlways.Value;
        public override ValueInfo<T> Value => _constantInfo.Value;
        public override IEnumerable<ObjectReference> ContextReferences => base.ContextReferences.Concat(
            _layersReversed.SelectMany(x => x.Node.ContextReferences));
    }

    internal readonly struct AnimatorLayerNodeInfo<T> : ILayer<T>
    {
        public AnimatorWeightState Weight { get; }
        public AnimatorLayerBlendingMode BlendingMode { get; }
        public int LayerIndex { get; }
        public readonly AnimatorLayerPropModNode<T> Node;
        PropModNode<T> ILayer<T>.Node => Node;
        IPropModNode ILayer.Node => Node;

        public AnimatorLayerNodeInfo(AnimatorWeightState weight, AnimatorLayerBlendingMode blendingMode,
            AnimatorLayerPropModNode<T> node, int layerIndex)
        {
            Weight = weight;
            BlendingMode = blendingMode;
            LayerIndex = layerIndex;
            Node = node;
        }
    }

    class AnimatorControllerPropModNode<T> : PropModNode<T>
    {
        private readonly IEnumerable<AnimatorLayerNodeInfo<T>> _layersReversed;

        [CanBeNull]
        public static AnimatorControllerPropModNode<T> Create(List<AnimatorLayerNodeInfo<T>> value)
        {
            if (value.Count == 0) return null;
            if (value.All(x => x.BlendingMode == AnimatorLayerBlendingMode.Additive && x.Node.Value.IsConstant))
                return null; // unchanged constant

            value.Reverse();
            return new AnimatorControllerPropModNode<T>(value);
        }

        private AnimatorControllerPropModNode(IEnumerable<AnimatorLayerNodeInfo<T>> layersReversed) =>
            _layersReversed = layersReversed;

        public IEnumerable<AnimatorLayerNodeInfo<T>> LayersReversed => _layersReversed;

        public override ValueInfo<T> Value =>
            NodeImplUtils.ConstantInfoForOverriding<T, AnimatorLayerNodeInfo<T>>(_layersReversed);

        // we may possible to implement complex logic which simulates state machine but not for now.
        public override bool AppliedAlways =>
            NodeImplUtils.AlwaysAppliedForOverriding<T, AnimatorLayerNodeInfo<T>>(_layersReversed);

        public override IEnumerable<ObjectReference> ContextReferences =>
            _layersReversed.SelectMany(x => x.Node.ContextReferences);
    }

    public enum AnimatorWeightState
    {
        AlwaysOne,
        EitherZeroOrOne,
        Variable
    }

    internal class AnimatorLayerPropModNode<T> : ImmutablePropModNode<T>
    {
        private readonly IEnumerable<AnimatorStatePropModNode<T>> _children;
        private readonly bool _partial;

        public AnimatorLayerPropModNode(IEnumerable<AnimatorStatePropModNode<T>> children, bool partial)
        {
            // expected to pass list or array
            // ReSharper disable once PossibleMultipleEnumeration
            Debug.Assert(children.Any());
            // ReSharper disable once PossibleMultipleEnumeration
            _children = children;
            _partial = partial;
        }

        public override bool AppliedAlways => !_partial && _children.All(x => x.AppliedAlways);
        public override ValueInfo<T> Value => NodeImplUtils.ConstantInfoForSideBySide(_children);
        public override IEnumerable<ObjectReference> ContextReferences => _children.SelectMany(x => x.ContextReferences);
        public IEnumerable<AnimatorStatePropModNode<T>> Children => _children;
    }

    internal class AnimatorStatePropModNode<T> : ImmutablePropModNode<T>
    {
        private readonly ImmutablePropModNode<T> _node;
        private readonly AnimatorState _state;

        public AnimatorStatePropModNode(ImmutablePropModNode<T> node, AnimatorState state)
        {
            _node = node;
            _state = state;
        }

        public ImmutablePropModNode<T> Node => _node;
        public AnimatorState State => _state;
        public override bool AppliedAlways => _node.AppliedAlways;
        public override ValueInfo<T> Value => _node.Value;
        public override IEnumerable<ObjectReference> ContextReferences => _node.ContextReferences;
    }
}