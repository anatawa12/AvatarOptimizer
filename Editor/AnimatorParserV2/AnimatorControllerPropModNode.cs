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

        public override ConstantInfo<float> Constant => ConstantInfo<float>.Variable;
        public override bool AppliedAlways => true;
    }

    internal readonly struct PlayableLayerNodeInfo<T> : ILayer<T>
    {
        public AnimatorWeightState Weight { get; }
        public AnimatorLayerBlendingMode BlendingMode { get; }
        public readonly AnimatorControllerPropModNode<T> Node;
        PropModNode<T> ILayer<T>.Node => Node;

        public PlayableLayerNodeInfo(AnimatorWeightState weight, AnimatorLayerBlendingMode blendingMode,
            AnimatorControllerPropModNode<T> node)
        {
            Weight = weight;
            BlendingMode = blendingMode;
            Node = node;
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
        }

        public override ConstantInfo<T> Constant =>
            NodeImplUtils.ConstantInfoForOverriding<T, PlayableLayerNodeInfo<T>>(_layersReversed);

        // we may possible to implement complex logic which simulates state machine but not for now.
        public override bool AppliedAlways =>
            _layersReversed.Any(x =>
                x.Weight == AnimatorWeightState.AlwaysOne && x.BlendingMode == AnimatorLayerBlendingMode.Override &&
                x.Node.AppliedAlways);

        public override IEnumerable<ObjectReference> ContextReferences => base.ContextReferences.Concat(
            _layersReversed.SelectMany(x => x.Node.ContextReferences));
    }

    internal readonly struct AnimatorLayerNodeInfo<T> : ILayer<T>
    {
        public AnimatorWeightState Weight { get; }
        public AnimatorLayerBlendingMode BlendingMode { get; }
        public readonly ImmutablePropModNode<T> Node;
        PropModNode<T> ILayer<T>.Node => Node;

        public AnimatorLayerNodeInfo(AnimatorWeightState weight, AnimatorLayerBlendingMode blendingMode, ImmutablePropModNode<T> node)
        {
            Weight = weight;
            BlendingMode = blendingMode;
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
            if (value.All(x => x.BlendingMode == AnimatorLayerBlendingMode.Additive && x.Node.Constant.IsConstant))
                return null; // unchanged constant

            value.Reverse();
            return new AnimatorControllerPropModNode<T>(value);
        }

        private AnimatorControllerPropModNode(IEnumerable<AnimatorLayerNodeInfo<T>> layersReversed) =>
            _layersReversed = layersReversed;

        public override ConstantInfo<T> Constant =>
            NodeImplUtils.ConstantInfoForOverriding<T, AnimatorLayerNodeInfo<T>>(_layersReversed);

        // we may possible to implement complex logic which simulates state machine but not for now.
        public override bool AppliedAlways =>
            _layersReversed.Any(x =>
                x.Weight == AnimatorWeightState.AlwaysOne && x.BlendingMode == AnimatorLayerBlendingMode.Override &&
                x.Node.AppliedAlways);

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
        private readonly IEnumerable<ImmutablePropModNode<T>> _children;

        public AnimatorLayerPropModNode(IEnumerable<ImmutablePropModNode<T>> children, bool partial)
        {
            // expected to pass list or array
            // ReSharper disable once PossibleMultipleEnumeration
            Debug.Assert(children.Any());
            // ReSharper disable once PossibleMultipleEnumeration
            _children = children;

            _appliedAlways = new Lazy<bool>(() => !partial && _children.All(x => x.AppliedAlways), isThreadSafe: false);
            _constantInfo = new Lazy<ConstantInfo<T>>(() => NodeImplUtils.ConstantInfoForSideBySide(_children),
                isThreadSafe: false);
        }


        private readonly Lazy<bool> _appliedAlways;
        private readonly Lazy<ConstantInfo<T>> _constantInfo;
        public override bool AppliedAlways => _appliedAlways.Value;
        public override ConstantInfo<T> Constant => _constantInfo.Value;
        public override IEnumerable<ObjectReference> ContextReferences => _children.SelectMany(x => x.ContextReferences);
    }
}