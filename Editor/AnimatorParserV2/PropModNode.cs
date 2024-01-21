using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using JetBrains.Annotations;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace Anatawa12.AvatarOptimizer.AnimatorParsersV2
{
    /// <summary>
    /// This class represents a node in the property modification tree.
    ///
    /// In AnimatorParser V2, Modifications of each property are represented as a tree to make it possible to
    /// remove modifications of a property.
    ///
    /// This class is the abstract class for the nodes.
    ///
    /// Most nodes are immutable but some nodes are mutable.
    /// </summary>
    internal abstract class PropModNode<T> : IErrorContext
    {
        /// <summary>
        /// Returns the constant value of this node. If not constant, throws an exception.
        /// </summary>
        /// <throws cref="InvalidConstraintException">If not constant, or not active</throws>
        public abstract T ConstantValue { get; }

        /// <summary>
        /// Returns true if this node is constant. For inactive nodes, this returns false.
        /// </summary>
        public abstract bool IsConstant { get; }

        /// <summary>
        /// Returns true if this node is always applied. For inactive nodes, this returns false.
        /// </summary>
        public abstract bool AppliedAlways { get; }
        public abstract IEnumerable<ObjectReference> ContextReferences { get; }
    }

    internal readonly struct ConstInfo<T>
    {
        public bool IsConst { get; }
        private readonly T _value;

        public T Value
        {
            get
            {
                if (!IsConst) throw new InvalidOperationException("Not Constant");
                return _value;
            }
        }

        public static ConstInfo<T> Variable => default;

        public ConstInfo(T value)
        {
            _value = value;
            IsConst = true;
        }
    }

    internal static class NodeImplUtils
    {
        public static ConstInfo<T> ConstantInfoForSideBySide<T>(IEnumerable<PropModNode<T>> nodes)
        {
            using (var enumerator = nodes.GetEnumerator())
            {
                Debug.Assert(enumerator.MoveNext());

                if (!enumerator.Current.IsConstant) return ConstInfo<T>.Variable;

                var value = enumerator.Current.ConstantValue;

                while (enumerator.MoveNext())
                {
                    if (!enumerator.Current.IsConstant) return ConstInfo<T>.Variable;

                    if (!EqualityComparer<T>.Default.Equals(value, enumerator.Current.ConstantValue))
                        return ConstInfo<T>.Variable;
                }

                return new ConstInfo<T>(value);
            }
        }

        public static ConstInfo<T> ConstantInfoForOverriding<T, TLayer>(IEnumerable<TLayer> layersReversed)
            where TLayer : ILayer<T>
        {
            T value = default;
            bool initialized = false;

            foreach (var layer in layersReversed)
            {
                switch (layer.Weight)
                {
                    case AnimatorWeightState.AlwaysOne:
                    case AnimatorWeightState.EitherZeroOrOne:
                        if (!layer.Node.IsConstant) return ConstInfo<T>.Variable;

                        if (layer.Node.AppliedAlways && layer.Weight == AnimatorWeightState.AlwaysOne &&
                            layer.BlendingMode == AnimatorLayerBlendingMode.Override)
                        {
                            // the layer is always applied at the highest property.
                            return new ConstInfo<T>(layer.Node.ConstantValue);
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
                                return ConstInfo<T>.Variable;
                        }

                        break;
                    case AnimatorWeightState.Variable:
                        return ConstInfo<T>.Variable;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return new ConstInfo<T>(value);
        }
    }

    internal interface ILayer<T>
    {
        AnimatorWeightState Weight { get; }
        AnimatorLayerBlendingMode BlendingMode { get; }
        PropModNode<T> Node { get; }
    }

    internal sealed class RootPropModNode<T> : PropModNode<T>, IErrorContext
    {
        private readonly List<ComponentPropModNode<T>> _children = new List<ComponentPropModNode<T>>();

        public RootPropModNode(params RootPropModNode<T>[] props)
        {
            foreach (var prop in props)
            foreach (var child in prop._children)
                Add(child);
        }

        (bool, T) ComputeConstantInfo()
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
        }

        public override bool AppliedAlways => _children.All(x => x.AppliedAlways);
        public override IEnumerable<ObjectReference> ContextReferences => _children.SelectMany(x => x.ContextReferences);
        public override bool IsConstant => ComputeConstantInfo().Item1;

        public override T ConstantValue
        {
            get
            {
                var info = ComputeConstantInfo();
                if (!info.Item1) throw new InvalidOperationException("Not Constant");
                return info.Item2;
            }
        }

        public IEnumerable<Component> SourceComponents => _children.Select(x => x.Component);

        public void Add(ComponentPropModNode<T> value)
        {
            _children.Add(value);
        }
    }

    internal abstract class ImmutablePropModNode<T> : PropModNode<T>
    {
    }

    internal class FloatAnimationCurveNode : ImmutablePropModNode<float>
    {
        public AnimationCurve Curve { get; }
        public AnimationClip Clip { get; }

        [CanBeNull]
        public static FloatAnimationCurveNode Create([NotNull] AnimationClip clip, EditorCurveBinding binding)
        {
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve == null) return null;
            if (curve.keys.Length == 0) return null;
            return new FloatAnimationCurveNode(clip, curve);
        }

        private FloatAnimationCurveNode(AnimationClip clip, AnimationCurve curve)
        {
            System.Diagnostics.Debug.Assert(curve.keys.Length > 0);
            Clip = clip;
            Curve = curve;
        }

        private object _parsedProperty;
        private object Parsed
        {
            get
            {
                if (_parsedProperty == null) return _parsedProperty = ParseProperty(Curve);
                return _parsedProperty;
            }
        }

        private static readonly object Variable = new object();

        public override bool IsConstant => Parsed is float;
        public override bool AppliedAlways => true;
        public override float ConstantValue => Parsed as float? ?? throw new InvalidOperationException("Non Constant");
        public override IEnumerable<ObjectReference> ContextReferences => new []{ ObjectRegistry.GetReference(Clip) };

        private static object ParseProperty(AnimationCurve curve)
        {
            if (curve.keys.Length == 1) return curve.keys[0].value;

            float constValue = 0;
            foreach (var (preKey, postKey) in curve.keys.ZipWithNext())
            {
                var preWeighted = preKey.weightedMode == WeightedMode.Out || preKey.weightedMode == WeightedMode.Both;
                var postWeighted = postKey.weightedMode == WeightedMode.In || postKey.weightedMode == WeightedMode.Both;

                if (preKey.value.CompareTo(postKey.value) != 0) return Variable;
                constValue = preKey.value;
                // it's constant
                if (float.IsInfinity(preKey.outWeight) || float.IsInfinity(postKey.inTangent)) continue;
                if (preKey.outTangent == 0 && postKey.inTangent == 0) continue;
                if (preWeighted && postWeighted && preKey.outWeight == 0 && postKey.inWeight == 0) continue;
                return Variable;
            }

            return constValue;
        }
    }

    internal class BlendTreeNode<T> : ImmutablePropModNode<T>
    {
        private readonly IEnumerable<ImmutablePropModNode<T>> _children;
        private readonly BlendTreeType _blendTreeType;

        public BlendTreeNode(IEnumerable<ImmutablePropModNode<T>> children, BlendTreeType blendTreeType, bool partial)
        {
            // expected to pass list or array
            // ReSharper disable once PossibleMultipleEnumeration
            Debug.Assert(children.Any());
            // ReSharper disable once PossibleMultipleEnumeration
            _children = children;
            _blendTreeType = blendTreeType;

            _appliedAlways = new Lazy<bool>(() =>
            {
                if (!WeightSumIsOne) return false;
                return !partial && _children.All(x => x.AppliedAlways);
            }, isThreadSafe: false);

            _constantInfo = new Lazy<ConstInfo<T>>(() =>
            {
                if (!WeightSumIsOne) return ConstInfo<T>.Variable;
                return NodeImplUtils.ConstantInfoForSideBySide(_children);
            }, isThreadSafe: false);
        }


        private bool WeightSumIsOne => _blendTreeType != BlendTreeType.Direct;

        private readonly Lazy<bool> _appliedAlways;
        private readonly Lazy<ConstInfo<T>> _constantInfo;

        public override bool AppliedAlways => _appliedAlways.Value;
        public override IEnumerable<ObjectReference> ContextReferences => _children.SelectMany(x => x.ContextReferences);

        public override bool IsConstant => _constantInfo.Value.IsConst;
        public override T ConstantValue => _constantInfo.Value.Value;
    }

    abstract class ComponentPropModNode<T> : PropModNode<T>
    {
        protected ComponentPropModNode([NotNull] Component component)
        {
            if (!component) throw new ArgumentNullException(nameof(component));
            Component = component;
        }

        public Component Component { get; }

        public override IEnumerable<ObjectReference> ContextReferences => new [] { ObjectRegistry.GetReference(Component) };
    }

    class VariableComponentPropModNode<T> : ComponentPropModNode<T>
    {
        public VariableComponentPropModNode([NotNull] Component component) : base(component)
        {
        }

        public override T ConstantValue => throw new InvalidOperationException("Not Constant");
        public override bool IsConstant => false;
        public override bool AppliedAlways => false;
    }

    class AnimationComponentPropModNode<T> : ComponentPropModNode<T>
    {
        private readonly ImmutablePropModNode<T> _animation;

        public AnimationComponentPropModNode([NotNull] Component component, ImmutablePropModNode<T> animation) : base(component)
        {
            _animation = animation;
        }

        public override T ConstantValue => _animation.ConstantValue;
        public override bool IsConstant => _animation.IsConstant;
        public override bool AppliedAlways => false;
    }
}
