using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEditor.Animations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.AnimatorParsersV2
{
    class HumanoidAnimatorPropModNode : ComponentPropModNode<FloatValueInfo, Animator>
    {
        public HumanoidAnimatorPropModNode(Animator component) : base(component)
        {
        }

        public override FloatValueInfo Value => FloatValueInfo.Variable;
        public override ApplyState ApplyState => ApplyState.Always;
    }

    internal readonly struct PlayableLayerNodeInfo<TValueInfo> : ILayer<TValueInfo>
        where TValueInfo : struct, IValueInfo<TValueInfo>
    {
        public AnimatorWeightState Weight { get; }
        public AnimatorLayerBlendingMode BlendingMode { get; }
        public int LayerIndex { get; }
        public readonly AnimatorControllerPropModNode<TValueInfo> Node;
        PropModNode<TValueInfo> ILayer<TValueInfo>.Node => Node;
        IPropModNode ILayer.Node => Node;

        public PlayableLayerNodeInfo(AnimatorWeightState weight, AnimatorLayerBlendingMode blendingMode,
            AnimatorControllerPropModNode<TValueInfo> node, int layerIndex)
        {
            Weight = weight;
            BlendingMode = blendingMode;
            LayerIndex = layerIndex;
            Node = node;
        }

        public PlayableLayerNodeInfo(AnimatorControllerPropModNode<TValueInfo> node, int layerIndex)
        {
            Weight = AnimatorWeightState.AlwaysOne;
            BlendingMode = AnimatorLayerBlendingMode.Override;
            Node = node;
            LayerIndex = layerIndex;
        }
    }

    class AnimatorPropModNode<TValueInfo> : ComponentPropModNode<TValueInfo, Animator>
        where TValueInfo : struct, IValueInfo<TValueInfo>
    {
        private readonly IEnumerable<PlayableLayerNodeInfo<TValueInfo>> _layersReversed;

        public AnimatorPropModNode(Animator component,IEnumerable<PlayableLayerNodeInfo<TValueInfo>> layersReversed)
            : base(component)
        {
            _layersReversed = layersReversed;

            _constantInfo = new Lazy<TValueInfo>(
                () => default(TValueInfo).ConstantInfoForOverriding(_layersReversed),
                isThreadSafe: false);

            _applyState = new Lazy<ApplyState>(
                () => NodeImplUtils.ApplyStateForOverriding(_layersReversed),
                isThreadSafe: false);
        }


        private readonly Lazy<TValueInfo> _constantInfo;
        private readonly Lazy<ApplyState> _applyState;

        public IEnumerable<PlayableLayerNodeInfo<TValueInfo>> LayersReversed => _layersReversed;
        public override ApplyState ApplyState => _applyState.Value;
        public override TValueInfo Value => _constantInfo.Value;
        public override IEnumerable<ObjectReference> ContextReferences => base.ContextReferences.Concat(
            _layersReversed.SelectMany(x => x.Node.ContextReferences));
    }

    internal readonly struct AnimatorLayerNodeInfo<TValueInfo> : ILayer<TValueInfo>
        where TValueInfo : struct, IValueInfo<TValueInfo>
    {
        public AnimatorWeightState Weight { get; }
        public AnimatorLayerBlendingMode BlendingMode { get; }
        public int LayerIndex { get; }
        public readonly AnimatorLayerPropModNode<TValueInfo> Node;
        PropModNode<TValueInfo> ILayer<TValueInfo>.Node => Node;
        IPropModNode ILayer.Node => Node;

        public AnimatorLayerNodeInfo(AnimatorWeightState weight, AnimatorLayerBlendingMode blendingMode,
            AnimatorLayerPropModNode<TValueInfo> node, int layerIndex)
        {
            Weight = weight;
            BlendingMode = blendingMode;
            LayerIndex = layerIndex;
            Node = node;
        }
    }

    class AnimatorControllerPropModNode<TValueInfo> : PropModNode<TValueInfo>
        where TValueInfo : struct, IValueInfo<TValueInfo>
    {
        private readonly IEnumerable<AnimatorLayerNodeInfo<TValueInfo>> _layersReversed;

        public static AnimatorControllerPropModNode<TValueInfo> Create(List<AnimatorLayerNodeInfo<TValueInfo>> value)
        {
            value.Reverse();
            return new AnimatorControllerPropModNode<TValueInfo>(value);
        }

        private AnimatorControllerPropModNode(IEnumerable<AnimatorLayerNodeInfo<TValueInfo>> layersReversed) =>
            _layersReversed = layersReversed;

        public IEnumerable<AnimatorLayerNodeInfo<TValueInfo>> LayersReversed => _layersReversed;

        // we may possible to implement complex logic which simulates state machine but not for now.
        public override ApplyState ApplyState => NodeImplUtils.ApplyStateForOverriding(_layersReversed);
        public override TValueInfo Value => default(TValueInfo).ConstantInfoForOverriding(_layersReversed);

        public override IEnumerable<ObjectReference> ContextReferences =>
            _layersReversed.SelectMany(x => x.Node.ContextReferences);
    }

    internal enum AnimatorWeightState
    {
        AlwaysZero,
        AlwaysOne,
        NonZeroOne,
    }

    internal class AnimatorLayerPropModNode<TValueInfo> : ImmutablePropModNode<TValueInfo>
        where TValueInfo : struct, IValueInfo<TValueInfo>
    {
        private readonly IEnumerable<AnimatorStatePropModNode<TValueInfo>> _children;
        internal readonly ApplyState LayerApplyState;

        public AnimatorLayerPropModNode(ICollection<AnimatorStatePropModNode<TValueInfo>> children, ApplyState applyState)
        {
            // expected to pass list or array
            // ReSharper disable once PossibleMultipleEnumeration
            Utils.Assert(children.Count != 0);
            // ReSharper disable once PossibleMultipleEnumeration
            _children = children;
            LayerApplyState = applyState;
        }

        public override ApplyState ApplyState =>
            LayerApplyState.MultiplyApplyState(_children.Select(x => x.ApplyState).MergeSideBySide());
        public override TValueInfo Value => default(TValueInfo).ConstantInfoForSideBySide(_children);
        public override IEnumerable<ObjectReference> ContextReferences => _children.SelectMany(x => x.ContextReferences);
        public IEnumerable<AnimatorStatePropModNode<TValueInfo>> Children => _children;
    }

    internal class AnimatorStatePropModNode<TValueInfo> : ImmutablePropModNode<TValueInfo>
        where TValueInfo : struct, IValueInfo<TValueInfo>
    {
        private readonly ImmutablePropModNode<TValueInfo> _node;
        private readonly AnimatorState _state;

        public AnimatorStatePropModNode(ImmutablePropModNode<TValueInfo> node, AnimatorState state)
        {
            _node = node;
            _state = state;
        }

        public ImmutablePropModNode<TValueInfo> Node => _node;
        public AnimatorState State => _state;
        public override ApplyState ApplyState => _node.ApplyState;
        public override TValueInfo Value => _node.Value;
        public override IEnumerable<ObjectReference> ContextReferences => _node.ContextReferences;
    }
}
