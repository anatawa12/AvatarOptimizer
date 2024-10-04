using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Debug = System.Diagnostics.Debug;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.AnimatorParsersV2
{
    interface IPropModNode
    {
        bool AppliedAlways { get; }
    }

    interface IValueInfo<TValueInfo>
        where TValueInfo : struct, IValueInfo<TValueInfo>
    {
        public bool IsConstant { get; }

        // following functions are intended to be called on default(TValueInfo) and "this" will not be affected
        // Those functions should be static abstract but Unity doesn't support static abstract functions.

        TValueInfo ConstantInfoForSideBySide(IEnumerable<PropModNode<TValueInfo>> nodes);
        TValueInfo ConstantInfoForBlendTree(IEnumerable<PropModNode<TValueInfo>> nodes, BlendTreeType blendTreeType);

        TValueInfo ConstantInfoForOverriding<TLayer>(IEnumerable<TLayer> layersReversed)
            where TLayer : ILayer<TValueInfo>;

        bool AlwaysAppliedForOverriding<TLayer>(IEnumerable<TLayer> layersReversed)
            where TLayer : ILayer<TValueInfo>;
    }
    
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
    internal abstract class PropModNode<TValueInfo> : IErrorContext, IPropModNode
        where TValueInfo: struct, IValueInfo<TValueInfo>
    {
        /// <summary>
        /// Returns true if this node is always applied. For inactive nodes, this returns false.
        /// </summary>
        public abstract bool AppliedAlways { get; }

        public abstract TValueInfo Value { get; }
        public abstract IEnumerable<ObjectReference> ContextReferences { get; }
    }

    /// <summary>
    /// The abstract information about actual value of PropModNode.
    /// </summary>
    // by design, this struct doesn't handle blending between two states.
    internal readonly struct ValueInfo<T> : IValueInfo<ValueInfo<T>>
        where T : notnull
    {
        public bool IsConstant => _possibleValues != null && _possibleValues.Length == 1;
        private readonly T[]? _possibleValues;

        public T ConstantValue
        {
            get
            {
                if (!IsConstant) throw new InvalidOperationException("Not Constant");
                return _possibleValues![0]; // non constant => there is value
            }
        }

        public T[]? PossibleValues => _possibleValues;

        public static ValueInfo<T> Variable
        {
            get
            {
                if (default(T) == null) throw new InvalidOperationException("Variable type is not allowed with Object");
                return default;
            }
        }

        public ValueInfo(T value) => _possibleValues = new[] { value };

        public ValueInfo(T[] possibleValues)
        {
            if (possibleValues == null) throw new ArgumentNullException(nameof(possibleValues));
            if (possibleValues.Length == 0)
                throw new ArgumentException("Value cannot be an empty array.", nameof(possibleValues));
            if (possibleValues.Distinct().Count() != possibleValues.Length)
                throw new ArgumentException("Value cannot contain duplicate values.", nameof(possibleValues));
            _possibleValues = possibleValues;
        }

        public bool TryGetConstantValue([NotNullWhen(true)] out T? o)
        {
            if (IsConstant)
            {
                o = ConstantValue;
                return true;
            }
            else
            {
                o = default;
                return false;
            }
        }

        public bool Equals(ValueInfo<T> other)
        {
            return NodeImplUtils.SetEquals(_possibleValues, other._possibleValues);
        }

        public ValueInfo<T> ConstantInfoForSideBySide(IEnumerable<PropModNode<ValueInfo<T>>> nodes) =>
            NodeImplUtils.ConstantInfoForSideBySide(nodes);

        public ValueInfo<T> ConstantInfoForBlendTree(IEnumerable<PropModNode<ValueInfo<T>>> nodes, BlendTreeType blendTreeType)
        {
            if (default(T) == null) return ConstantInfoForSideBySide(nodes);
            return blendTreeType == BlendTreeType.Direct ? Variable : ConstantInfoForSideBySide(nodes);
        }

        public ValueInfo<T> ConstantInfoForOverriding<TLayer>(IEnumerable<TLayer> layersReversed)
            where TLayer : ILayer<ValueInfo<T>> => NodeImplUtils.ConstantInfoForOverriding<T, TLayer>(layersReversed);

        public bool AlwaysAppliedForOverriding<TLayer>(IEnumerable<TLayer> layersReversed)
            where TLayer : ILayer<ValueInfo<T>> => NodeImplUtils.AlwaysAppliedForOverriding<T, TLayer>(layersReversed);

        public override bool Equals(object obj) => obj is ValueInfo<T> other && Equals(other);

        public override int GetHashCode() => _possibleValues == null ? 0 : _possibleValues.GetSetHashCode();

        public override string ToString() =>
            _possibleValues == null
                ? $"ValueInfo<{typeof(T).Name}>{{Variable}}"
                : $"ValueInfo<{typeof(T).Name}>{{PossibleValues={string.Join(",", _possibleValues)}}}";
    }

    internal static class NodeImplUtils
    {
        public static bool SetEquals<T>(T[]? a, T[]? b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            return new HashSet<T>(a).SetEquals(b);
        }

        public static ValueInfo<T> ConstantInfoForSideBySide<T>(IEnumerable<PropModNode<ValueInfo<T>>> nodes) where T : notnull
        {
            using (var enumerator = nodes.GetEnumerator())
            {
                Debug.Assert(enumerator.MoveNext());

                if (!(enumerator.Current.Value.PossibleValues is T[] possibleValues))
                    return ValueInfo<T>.Variable;

                var allPossibleValues = new HashSet<T>(possibleValues);

                while (enumerator.MoveNext())
                {
                    if (!(enumerator.Current.Value.PossibleValues is T[] otherValues))
                        return ValueInfo<T>.Variable;

                    allPossibleValues.UnionWith(otherValues);
                }

                return new ValueInfo<T>(allPossibleValues.ToArray());
            }
        }

        public static bool AlwaysAppliedForOverriding<T, TLayer>(IEnumerable<TLayer> layersReversed)
            where T : notnull
            where TLayer : ILayer<ValueInfo<T>>
        {
            return layersReversed.Any(x =>
                x.Weight == AnimatorWeightState.AlwaysOne && x.BlendingMode == AnimatorLayerBlendingMode.Override &&
                x.Node.AppliedAlways);
        }

        public static ValueInfo<T> ConstantInfoForOverriding<T, TLayer>(IEnumerable<TLayer> layersReversed)
            where T : notnull
            where TLayer : ILayer<ValueInfo<T>>
        {
            var canAdditive = default(T) != null;
            var allPossibleValues = new HashSet<T>();

            foreach (var layer in layersReversed)
            {
                switch (layer.Weight)
                {
                    case AnimatorWeightState.AlwaysOne:
                    case AnimatorWeightState.EitherZeroOrOne:
                    {
                        if (!(layer.Node.Value.PossibleValues is T[] otherValues)) return ValueInfo<T>.Variable;

                        switch (layer.BlendingMode)
                        {
                            case AnimatorLayerBlendingMode.Additive:
                                // ObjectReference will work as override even with additive mode.
                                if (!canAdditive) goto case AnimatorLayerBlendingMode.Override;

                                // additive with changing value: value cannot be determined 
                                if (otherValues.Length != 1) return ValueInfo<T>.Variable;
                                break;
                            case AnimatorLayerBlendingMode.Override:
                                allPossibleValues.UnionWith(otherValues);

                                if (layer.IsAlwaysOverride())
                                {
                                    // the layer is always applied at the highest property.
                                    return new ValueInfo<T>(allPossibleValues.ToArray());
                                }

                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                        break;
                    case AnimatorWeightState.Variable:
                    {
                        if (default(T) != null) return ValueInfo<T>.Variable; // float: variable
                        if (!(layer.Node.Value.PossibleValues is T[] otherValues))
                            throw new InvalidOperationException();
                        allPossibleValues.UnionWith(otherValues);
                    }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return new ValueInfo<T>(allPossibleValues.ToArray());
        }

        public static bool IsAlwaysOverride<TLayer>(this TLayer layer)
            where TLayer : ILayer
        {
            return layer.Node.AppliedAlways && layer.Weight == AnimatorWeightState.AlwaysOne &&
                   layer.BlendingMode == AnimatorLayerBlendingMode.Override;
        }

        public static IEnumerable<TLayer> WhileApplied<TLayer>(this IEnumerable<TLayer> layer)
            where TLayer : ILayer
        {
            foreach (var layerInfo in layer)
            {
                yield return layerInfo;
                if (layerInfo.IsAlwaysOverride()) yield break;
            }
        }
    }

    interface ILayer
    {
        AnimatorWeightState Weight { get; }
        AnimatorLayerBlendingMode BlendingMode { get; }
        IPropModNode Node { get; }
    }

    internal interface ILayer<TValueInfo> : ILayer
        where TValueInfo : struct, IValueInfo<TValueInfo>
    {
        new AnimatorWeightState Weight { get; }
        new AnimatorLayerBlendingMode BlendingMode { get; }
        new PropModNode<TValueInfo> Node { get; }
    }

    internal sealed class RootPropModNode<T> : PropModNode<ValueInfo<T>>, IErrorContext
        where T : notnull
    {
        internal readonly struct ComponentInfo
        {
            public readonly ComponentPropModNodeBase<ValueInfo<T>> Node;
            public readonly bool AlwaysApplied;

            public bool AppliedAlways => AlwaysApplied && Node.AppliedAlways;
            public IEnumerable<ObjectReference> ContextReferences => Node.ContextReferences;
            public Component Component => Node.Component;

            public ComponentInfo(ComponentPropModNodeBase<ValueInfo<T>> node, bool alwaysApplied)
            {
                Node = node;
                AlwaysApplied = alwaysApplied;
            }
        }

        private readonly List<ComponentInfo> _children = new List<ComponentInfo>();

        public IEnumerable<ComponentInfo> Children => _children;

        public override bool AppliedAlways => _children.All(x => x.AppliedAlways);

        public override IEnumerable<ObjectReference> ContextReferences =>
            _children.SelectMany(x => x.ContextReferences);

        public override ValueInfo<T> Value => default(ValueInfo<T>).ConstantInfoForSideBySide(_children.Select(x => x.Node));

        public bool IsEmpty => _children.Count == 0;

        public IEnumerable<Component> SourceComponents => _children.Select(x => x.Component);
        public IEnumerable<ComponentPropModNodeBase<ValueInfo<T>>> ComponentNodes => _children.Select(x => x.Node);

        public void Add(ComponentPropModNodeBase<ValueInfo<T>> node, bool alwaysApplied)
        {
            _children.Add(new ComponentInfo(node, alwaysApplied));
            DestroyTracker.Track(node.Component, OnDestroy);
        }

        public void Add(RootPropModNode<T> toAdd)
        {
            if (toAdd == null) throw new ArgumentNullException(nameof(toAdd));
            foreach (var child in toAdd._children)
                Add(child.Node, child.AppliedAlways);
        }

        private void OnDestroy(int objectId)
        {
            _children.RemoveAll(x => x.Component.GetInstanceID() == objectId);
        }

        public void Invalidate()
        {
            foreach (var componentInfo in _children)
                DestroyTracker.Untrack(componentInfo.Component, OnDestroy);
            _children.Clear();
        }

        public RootPropModNode<T>? Normalize() => IsEmpty ? null : this;
    }

    internal abstract class ImmutablePropModNode<TValueInfo> : PropModNode<TValueInfo>
        where TValueInfo: struct, IValueInfo<TValueInfo>
    {
    }

    internal class FloatAnimationCurveNode : ImmutablePropModNode<ValueInfo<float>>
    {
        public AnimationCurve Curve { get; }
        public AnimationClip Clip { get; }

        public static FloatAnimationCurveNode? Create(AnimationClip clip, EditorCurveBinding binding,
            AnimationClip? additiveReferenceClip, float additiveReferenceFrame)
        {
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve == null) return null;
            if (curve.keys.Length == 0) return null;
            
            float referenceValue = 0;
            if (additiveReferenceClip != null 
                && AnimationUtility.GetEditorCurve(additiveReferenceClip, binding) is { } referenceCurve)
                referenceValue = referenceCurve.Evaluate(additiveReferenceFrame);
            else
                referenceValue = curve.Evaluate(0);

            return new FloatAnimationCurveNode(clip, curve, referenceValue);
        }

        private FloatAnimationCurveNode(AnimationClip clip, AnimationCurve curve, float referenceValue)
        {
            if (!clip) throw new ArgumentNullException(nameof(clip));
            if (curve == null) throw new ArgumentNullException(nameof(curve));
            Debug.Assert(curve.keys.Length > 0);
            Clip = clip;
            Curve = curve;
            _constantInfo = new Lazy<ValueInfo<float>>(() => ParseProperty(curve, referenceValue), isThreadSafe: false);
        }

        private readonly Lazy<ValueInfo<float>> _constantInfo;

        public override bool AppliedAlways => true;
        public override ValueInfo<float> Value => _constantInfo.Value;
        public override IEnumerable<ObjectReference> ContextReferences => new[] { ObjectRegistry.GetReference(Clip) };

        private static ValueInfo<float> ParseProperty(AnimationCurve curve, float referenceValue)
        {
            var curveValue = ParseCurve(curve);
            if (curveValue.PossibleValues == null) return ValueInfo<float>.Variable;
            return new ValueInfo<float>(curveValue.PossibleValues.Concat(new[] { referenceValue }).Distinct()
                .ToArray());
        }

        private static ValueInfo<float> ParseCurve(AnimationCurve curve)
        {
            if (curve.keys.Length == 1) return new ValueInfo<float>(curve.keys[0].value);

            float constValue = 0;
            foreach (var (preKey, postKey) in curve.keys.ZipWithNext())
            {
                var preWeighted = preKey.weightedMode == WeightedMode.Out || preKey.weightedMode == WeightedMode.Both;
                var postWeighted = postKey.weightedMode == WeightedMode.In || postKey.weightedMode == WeightedMode.Both;

                if (preKey.value.CompareTo(postKey.value) != 0) return ValueInfo<float>.Variable;
                constValue = preKey.value;
                // it's constant
                if (float.IsInfinity(preKey.outWeight) || float.IsInfinity(postKey.inTangent)) continue;
                if (preKey.outTangent == 0 && postKey.inTangent == 0) continue;
                if (preWeighted && postWeighted && preKey.outWeight == 0 && postKey.inWeight == 0) continue;
                return ValueInfo<float>.Variable;
            }

            return new ValueInfo<float>(constValue);
        }
    }

    internal class ObjectAnimationCurveNode : ImmutablePropModNode<ValueInfo<Object>>
    {
        public ObjectReferenceKeyframe[] Frames { get; set; }
        public AnimationClip Clip { get; }

        public static ObjectAnimationCurveNode? Create(AnimationClip clip, EditorCurveBinding binding)
        {
            var curve = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            if (curve == null) return null;
            if (curve.Length == 0) return null;
            return new ObjectAnimationCurveNode(clip, curve);
        }

        private ObjectAnimationCurveNode(AnimationClip clip, ObjectReferenceKeyframe[] frames)
        {
            Debug.Assert(frames.Length > 0);
            Clip = clip;
            Frames = frames;
            _constantInfo = new Lazy<ValueInfo<Object>>(() => ParseProperty(frames), isThreadSafe: false);
        }


        private readonly Lazy<ValueInfo<Object>> _constantInfo;

        public override bool AppliedAlways => true;
        public override ValueInfo<Object> Value => _constantInfo.Value;
        public override IEnumerable<ObjectReference> ContextReferences => new[] { ObjectRegistry.GetReference(Clip) };

        private static ValueInfo<Object> ParseProperty(ObjectReferenceKeyframe[] frames) =>
            new ValueInfo<Object>(frames.Select(x => x.value).Distinct().ToArray());
    }

    internal struct BlendTreeElement<T>
        where T : notnull
    {
        public int Index;
        public ImmutablePropModNode<ValueInfo<T>> Node;

        public BlendTreeElement(int index, ImmutablePropModNode<ValueInfo<T>> node)
        {
            Index = index;
            Node = node ?? throw new ArgumentNullException(nameof(node));
        }
    }

    internal class BlendTreeNode<T> : ImmutablePropModNode<ValueInfo<T>>
        where T : notnull
    {
        private readonly List<BlendTreeElement<T>> _children;
        private readonly BlendTreeType _blendTreeType;
        private readonly bool _partial;

        public BlendTreeNode(List<BlendTreeElement<T>> children,
            BlendTreeType blendTreeType, bool partial)
        {
            // expected to pass list or array
            // ReSharper disable once PossibleMultipleEnumeration
            Debug.Assert(children.Any());
            // ReSharper disable once PossibleMultipleEnumeration
            _children = children;
            _blendTreeType = blendTreeType;
            _partial = partial;
        }


        private bool WeightSumIsOne => _blendTreeType != BlendTreeType.Direct;
        public IReadOnlyList<BlendTreeElement<T>> Children => _children;
        public override bool AppliedAlways => WeightSumIsOne && !_partial && _children.All(x => x.Node.AppliedAlways);

        public override ValueInfo<T> Value
        {
            get
            {
                if (default(T) == null)
                    return default(ValueInfo<T>).ConstantInfoForBlendTree(_children.Select(x => x.Node), _blendTreeType);
                return !WeightSumIsOne
                    ? ValueInfo<T>.Variable
                    : default(ValueInfo<T>).ConstantInfoForSideBySide(_children.Select(x => x.Node));
            }
        }

        public override IEnumerable<ObjectReference> ContextReferences =>
            _children.SelectMany(x => x.Node.ContextReferences);
    }

    abstract class ComponentPropModNodeBase<TValueInfo> : PropModNode<TValueInfo>
        where TValueInfo : struct, IValueInfo<TValueInfo>
    {
        protected ComponentPropModNodeBase(Component component)
        {
            if (!component) throw new ArgumentNullException(nameof(component));
            Component = component;
        }

        public Component Component { get; }

        public override IEnumerable<ObjectReference> ContextReferences =>
            new[] { ObjectRegistry.GetReference(Component) };
    }

    abstract class ComponentPropModNode<TValueInfo, TComponent> : ComponentPropModNodeBase<TValueInfo>
        where TValueInfo : struct, IValueInfo<TValueInfo>
        where TComponent : Component
    {
        protected ComponentPropModNode(TComponent component) : base(component)
        {
            if (!component) throw new ArgumentNullException(nameof(component));
            Component = component;
        }

        public new TComponent Component { get; }

        public override IEnumerable<ObjectReference> ContextReferences =>
            new[] { ObjectRegistry.GetReference(Component) };
    }

    class VariableComponentPropModNode : ComponentPropModNode<ValueInfo<float>, Component>
    {
        public VariableComponentPropModNode(Component component) : base(component)
        {
        }

        public override bool AppliedAlways => false;
        public override ValueInfo<float> Value => ValueInfo<float>.Variable;
    }

    class AnimationComponentPropModNode<T> : ComponentPropModNode<ValueInfo<T>, Animation>
        where T : notnull
    {
        public ImmutablePropModNode<ValueInfo<T>> Animation { get; }

        public AnimationComponentPropModNode(Animation component, ImmutablePropModNode<ValueInfo<T>> animation) : base(component)
        {
            Animation = animation;
            _constantInfo = new Lazy<ValueInfo<T>>(() => animation.Value, isThreadSafe: false);
        }

        private readonly Lazy<ValueInfo<T>> _constantInfo;

        public override bool AppliedAlways => true;
        public override ValueInfo<T> Value => _constantInfo.Value;

        public override IEnumerable<ObjectReference> ContextReferences =>
            base.ContextReferences.Concat(Animation.ContextReferences);
    }
}
