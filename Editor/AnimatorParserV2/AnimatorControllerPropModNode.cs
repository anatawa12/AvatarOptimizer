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

        public override float ConstantValue => throw new Exception("Not Constant");
        public override bool IsConstant => false;
        public override bool AppliedAlways => true;
    }

    internal struct PlayableLayerNodeInfo<T>
    {
        public readonly AnimatorWeightState Weight;
        public readonly AnimatorLayerBlendingMode BlendingMode;
        public readonly AnimatorControllerPropModNode<T> Node;

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

        private (T value, bool isConst) ComputeConstant()
        {
            var variable = (default(T), false);

            T value = default;
            bool initialized = false;

            foreach (var layer in _layersReversed)
            {
                switch (layer.Weight)
                {
                    case AnimatorWeightState.AlwaysOne:
                    case AnimatorWeightState.EitherZeroOrOne:
                        if (!layer.Node.IsConstant) return variable;

                        if (layer.Node.AppliedAlways && layer.Weight == AnimatorWeightState.AlwaysOne &&
                            layer.BlendingMode == AnimatorLayerBlendingMode.Override)
                        {
                            // the layer is always applied at the highest property.
                            return (layer.Node.ConstantValue, true);
                        }

                        // partially applied constants so save that value and continue.
                        if (!initialized)
                        {
                            value = layer.Node.ConstantValue;
                            initialized = true;
                        }
                        else
                        {
                            if (!EqualityComparer<T>.Default.Equals(value, layer.Node.ConstantValue))
                                return variable;
                        }

                        break;
                    case AnimatorWeightState.Variable:
                        return variable;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return (value, true);
        }

        public override bool IsConstant => ComputeConstant().isConst;

        public override T ConstantValue
        {
            get
            {
                var computed = ComputeConstant();
                if (!computed.isConst) throw new InvalidOperationException("Not Constant");
                return computed.value;
            }
        }

        // we may possible to implement complex logic which simulates state machine but not for now.
        public override bool AppliedAlways =>
            _layersReversed.Any(x =>
                x.Weight == AnimatorWeightState.AlwaysOne && x.BlendingMode == AnimatorLayerBlendingMode.Override &&
                x.Node.AppliedAlways);

        public override IEnumerable<ObjectReference> ContextReferences => base.ContextReferences.Concat(
            _layersReversed.SelectMany(x => x.Node.ContextReferences));
    }

    internal struct AnimatorLayerNodeInfo<T>
    {
        public readonly AnimatorWeightState Weight;
        public readonly AnimatorLayerBlendingMode BlendingMode;
        public readonly ImmutablePropModNode<T> Node;

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
            if (value.All(x => x.BlendingMode == AnimatorLayerBlendingMode.Additive && x.Node.IsConstant))
                return null; // unchanged constant

            value.Reverse();
            return new AnimatorControllerPropModNode<T>(value);
        }

        public AnimatorControllerPropModNode(IEnumerable<AnimatorLayerNodeInfo<T>> layersReversed)
        {
            _layersReversed = layersReversed;
        }

        private (T value, bool isConst) ComputeConstant()
        {
            var variable = (default(T), false);

            T value = default;
            bool initialized = false;

            foreach (var layer in _layersReversed)
            {
                switch (layer.Weight)
                {
                    case AnimatorWeightState.AlwaysOne:
                    case AnimatorWeightState.EitherZeroOrOne:
                        if (!layer.Node.IsConstant) return variable;

                        if (layer.Node.AppliedAlways && layer.Weight == AnimatorWeightState.AlwaysOne &&
                            layer.BlendingMode == AnimatorLayerBlendingMode.Override)
                        {
                            // the layer is always applied at the highest property.
                            return (layer.Node.ConstantValue, true);
                        }

                        // partially applied constants so save that value and continue.
                        if (!initialized)
                        {
                            value = layer.Node.ConstantValue;
                            initialized = true;
                        }
                        else
                        {
                            if (!EqualityComparer<T>.Default.Equals(value, layer.Node.ConstantValue))
                                return variable;
                        }

                        break;
                    case AnimatorWeightState.Variable:
                        return variable;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return (value, true);
        }

        public override bool IsConstant => ComputeConstant().isConst;

        public override T ConstantValue
        {
            get
            {
                var computed = ComputeConstant();
                if (!computed.isConst) throw new InvalidOperationException("Not Constant");
                return computed.value;
            }
        }

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

            _appliedAlways = new Lazy<bool>(() =>
            {
                if (partial) return false;
                return _children.All(x => x.AppliedAlways);
            }, isThreadSafe: false);
            

            _constantInfo = new Lazy<(bool, T)>(() =>
            {
                using (var enumerator = _children.GetEnumerator())
                {
                    Debug.Assert(enumerator.MoveNext());

                    if (!enumerator.Current.IsConstant) return (false, default);

                    var value = enumerator.Current.ConstantValue;

                    while (enumerator.MoveNext())
                    {
                        if (!enumerator.Current.IsConstant) return (false, default);

                        if (!EqualityComparer<T>.Default.Equals(value, enumerator.Current.ConstantValue))
                            return (false, default);
                    }

                    return (true, value);
                }
            }, isThreadSafe: false);
        }


        private readonly Lazy<bool> _appliedAlways;
        public override bool AppliedAlways => _appliedAlways.Value;
        public override IEnumerable<ObjectReference> ContextReferences => _children.SelectMany(x => x.ContextReferences);

        private readonly Lazy<(bool, T)> _constantInfo;
        public override bool IsConstant => _constantInfo.Value.Item1;

        public override T ConstantValue
        {
            get
            {
                if (!_constantInfo.Value.Item1) throw new InvalidOperationException("Not Constant");
                return _constantInfo.Value.Item2;
            }
        }
    }
}