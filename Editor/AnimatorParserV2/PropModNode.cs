using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.AnimatorParsersV2
{
    interface IPropModNode
    {
        ApplyState ApplyState { get; }
        bool IsConstantValue { get; }
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
    }

    /// <summary>
    /// The apply state of PropModNode.
    /// </summary>
    enum ApplyState
    {
        Always,
        Partially,
        Never,
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
        public abstract ApplyState ApplyState { get; }
        public bool IsConstantValue => Value.IsConstant;

        public abstract TValueInfo Value { get; }
        public abstract IEnumerable<ObjectReference> ContextReferences { get; }
    }

    internal readonly struct FloatValueInfo : IValueInfo<FloatValueInfo>, IEquatable<FloatValueInfo>
    {
        /// <summary>
        /// The application state of this value. Does not matter with variable value.
        ///
        /// In this context Partial Application means the value can be applied with weight in between 0 and 1.
        /// </summary>
        public bool PartialApplication { get; }
        public bool IsConstant => _possibleValues is { Length: 1 };
        private readonly float[]? _possibleValues;

        public FloatValueInfo(float values, bool partialApplication = false) : this(new[] { values }, partialApplication) { }
        public FloatValueInfo(float[] values, bool partialApplication = false)
        {
            _possibleValues = values ?? throw new ArgumentNullException(nameof(values));
            PartialApplication = partialApplication;
            if (_possibleValues.Length == 0) PartialApplication = true;
        }

        public float ConstantValue
        {
            get
            {
                if (!IsConstant) throw new InvalidOperationException("Not Constant");
                return _possibleValues![0]; // non constant => there is value
            }
        }

        /// <summary>
        /// The possible animated values. If null, any value is possible (variable).
        /// If the array has one value, it will be animated to the value (or just not animated)
        /// If the array has more than one value, it will be animated to any of the values, or in between.
        /// </summary>
        public float[]? PossibleValues => _possibleValues;
        /// <summary>
        /// The ValueInfo express any value.
        /// </summary>
        public static FloatValueInfo Variable => default;

        public bool TryGetConstantValue(float currentValue, out float o)
        {
            if (IsConstant && (!PartialApplication || currentValue.Equals(ConstantValue)))
            {
                o = ConstantValue;
                return true;
            }

            o = default;
            return false;
        }

        public FloatValueInfo ConstantInfoForSideBySide(IEnumerable<PropModNode<FloatValueInfo>> nodes)
        {
            var allPossibleValues = new HashSet<float>();
            foreach (var propModNode in nodes)
            {
                if (propModNode.Value.PossibleValues is not { } values) return Variable;
                allPossibleValues.UnionWith(values);
            }
            return new FloatValueInfo(allPossibleValues.ToArray());
        }

        public FloatValueInfo ConstantInfoForBlendTree(IEnumerable<PropModNode<FloatValueInfo>> nodes,
            BlendTreeType blendTreeType) =>
            blendTreeType == BlendTreeType.Direct ? Variable : ConstantInfoForSideBySide(nodes);

        public FloatValueInfo ConstantInfoForOverriding<TLayer>(IEnumerable<TLayer> layersReversed)
            where TLayer : ILayer<FloatValueInfo>
        {
            var allPossibleValues = new HashSet<float>();

            foreach (var layer in layersReversed)
            {
                switch (layer.Weight)
                {
                    case AnimatorWeightState.AlwaysZero:
                        continue; // Might have effect with write defaults true?
                    case AnimatorWeightState.AlwaysOne:
                    case AnimatorWeightState.NonZeroOne:
                    {
                        if (layer.Node.Value.PossibleValues is not { } otherValues) return Variable;

                        switch (layer.BlendingMode)
                        {
                            case AnimatorLayerBlendingMode.Additive:
                                // having multiple possible value means animated, and this means variable.
                                // if only one value is exists with additive layer, noting is added so skip this layer.
                                // for additive reference pose, length of otherValues will be two or more with 
                                // reference post value.
                                // see implementation of FloatAnimationCurveNode.ParseProperty
                                if (otherValues.Length != 1) return Variable;
                                break;
                            case AnimatorLayerBlendingMode.Override:
                                allPossibleValues.UnionWith(otherValues);

                                if (layer.IsAlwaysOverride())
                                {
                                    // the layer is always applied at the highest property.
                                    return new FloatValueInfo(allPossibleValues.ToArray());
                                }

                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return new FloatValueInfo(allPossibleValues.ToArray(), partialApplication: true);
        }

        public bool Equals(FloatValueInfo other) => NodeImplUtils.SetEquals(_possibleValues, other._possibleValues) && PartialApplication == other.PartialApplication;
        public override bool Equals(object? obj) => obj is FloatValueInfo other && Equals(other);
        public override int GetHashCode() => _possibleValues != null ? _possibleValues.GetSetHashCode() : 0;
        public static bool operator ==(FloatValueInfo left, FloatValueInfo right) => left.Equals(right);
        public static bool operator !=(FloatValueInfo left, FloatValueInfo right) => !left.Equals(right);

        public override string ToString()
        {
            if (PossibleValues is not float[] values) return "Variable";
            if (values.Length == 0) return "Never";
            var isPartial = PartialApplication ? "Partial" : "Always";
            return $"{isPartial}:Const:{string.Join(",", values)}";
        }
    }

    // note: no default is allowed
    internal readonly struct ObjectValueInfo : IValueInfo<ObjectValueInfo>, IEquatable<ObjectValueInfo>
    {
        private readonly Object?[] _possibleValues;

        public ObjectValueInfo(Object? value) => _possibleValues = new[] { value };
        public ObjectValueInfo(Object?[] values) => _possibleValues = values;

        public bool IsConstant => _possibleValues is { Length: 1 };

        public Object?[] PossibleValues => _possibleValues ?? Array.Empty<Object?>();

        public ObjectValueInfo ConstantInfoForSideBySide(IEnumerable<PropModNode<ObjectValueInfo>> nodes) =>
            new(nodes.SelectMany(node => node.Value.PossibleValues).Distinct().ToArray());

        public ObjectValueInfo ConstantInfoForBlendTree(IEnumerable<PropModNode<ObjectValueInfo>> nodes,
            BlendTreeType blendTreeType) => ConstantInfoForSideBySide(nodes);

        public ObjectValueInfo ConstantInfoForOverriding<TLayer>(IEnumerable<TLayer> layersReversed)
            where TLayer : ILayer<ObjectValueInfo>
        {
            return new ObjectValueInfo(layersReversed.WhileApplied().SelectMany(layer => layer.Node.Value.PossibleValues).Distinct().ToArray());
        }

        public bool Equals(ObjectValueInfo other) => NodeImplUtils.SetEquals(PossibleValues, PossibleValues);
        public override bool Equals(object? obj) => obj is ObjectValueInfo other && Equals(other);
        public override int GetHashCode() => _possibleValues.GetHashCode();
        public static bool operator ==(ObjectValueInfo left, ObjectValueInfo right) => left.Equals(right);
        public static bool operator !=(ObjectValueInfo left, ObjectValueInfo right) => !left.Equals(right);
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

        public static ApplyState ApplyStateForOverriding<TLayer>(IEnumerable<TLayer> layersReversed)
            where TLayer : ILayer
        {
            var current = ApplyState.Never;
            foreach (var layer in layersReversed)
            {
                switch (layer.BlendingMode)
                {
                    case AnimatorLayerBlendingMode.Override:
                    {
                        var layerState = ApplyStateForWeightState(layer.Weight)
                            .MultiplyApplyState(layer.Node.ApplyState);
                        switch (layerState)
                        {
                            case ApplyState.Always:
                                return ApplyState.Always;
                            case ApplyState.Partially:
                                current = ApplyState.Partially;
                                break;
                            case ApplyState.Never:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        break;
                    }
                    case AnimatorLayerBlendingMode.Additive:
                    {
                        var layerState = ApplyStateForWeightState(layer.Weight)
                            .MultiplyApplyState(layer.Node.ApplyState);
                        switch (layerState)
                        {
                            case ApplyState.Always:
                            case ApplyState.Partially:
                                // constant node does not have effect with additive layer
                                if (layer.Node.IsConstantValue) continue;
                                current = ApplyState.Partially;
                                break;
                            case ApplyState.Never:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return current;
        }

        private static ApplyState ApplyStateForWeightState(AnimatorWeightState weight) => weight switch
        {
            AnimatorWeightState.AlwaysZero => ApplyState.Never, // Might have effect with write defaults true?
            AnimatorWeightState.AlwaysOne => ApplyState.Always,
            AnimatorWeightState.NonZeroOne => ApplyState.Partially,
            _ => throw new ArgumentOutOfRangeException()
        };

        public static bool IsAlwaysOverride<TLayer>(this TLayer layer)
            where TLayer : ILayer
        {
            return layer.Node.ApplyState == ApplyState.Always &&
                   layer.Weight == AnimatorWeightState.AlwaysOne &&
                   layer.BlendingMode == AnimatorLayerBlendingMode.Override;
        }

        public static IEnumerable<TLayer> WhileApplied<TLayer>(this IEnumerable<TLayer> layer)
            where TLayer : ILayer
        {
            foreach (var layerInfo in layer)
            {
                if (layerInfo.Weight == AnimatorWeightState.AlwaysZero) continue; // might have effect with write defaults true?
                yield return layerInfo;
                if (layerInfo.IsAlwaysOverride()) yield break;
            }
        }

        public static ApplyState MergeSideBySide(this IEnumerable<ApplyState> states)
        {
            if (states == null) throw new ArgumentNullException(nameof(states));

            using IEnumerator<ApplyState> enumerator = states.GetEnumerator();
            if (!enumerator.MoveNext()) return ApplyState.Never;
            var result = enumerator.Current;
            if (result == ApplyState.Partially) return ApplyState.Partially;
            while (enumerator.MoveNext())
                if (result != enumerator.Current) return ApplyState.Partially;
            return result;
        }

        public static ApplyState MergeSideBySide(this ApplyState a, ApplyState b) => (a, b) switch
        {
            (ApplyState.Always, ApplyState.Always) => ApplyState.Always,
            (ApplyState.Never, ApplyState.Never) => ApplyState.Never,
            _ => ApplyState.Partially
        };
        
        // multiply: Apply either inside the other
        public static ApplyState MultiplyApplyState(this ApplyState a, ApplyState b) => (a, b) switch
        {
            (ApplyState.Always, ApplyState.Always) => ApplyState.Always,
            (ApplyState.Never, _) => ApplyState.Never,
            (_, ApplyState.Never) => ApplyState.Never,
            _ => ApplyState.Partially
        };
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

    internal sealed class RootPropModNode<TValueInfo> : PropModNode<TValueInfo>, IErrorContext
        where TValueInfo : struct, IValueInfo<TValueInfo>
    {
        internal readonly struct ComponentInfo
        {
            public readonly ComponentPropModNodeBase<TValueInfo> Node;
            public readonly ApplyState ComponentApplyState;

            public ApplyState ApplyState => Node.ApplyState.MultiplyApplyState(ComponentApplyState);
            public IEnumerable<ObjectReference> ContextReferences => Node.ContextReferences;
            public Component Component => Node.Component;

            public ComponentInfo(ComponentPropModNodeBase<TValueInfo> node, ApplyState applyState)
            {
                Node = node;
                ComponentApplyState = applyState;
            }

        }

        private readonly List<ComponentInfo> _children = new List<ComponentInfo>();

        public IEnumerable<ComponentInfo> Children => _children;

        public override ApplyState ApplyState => _children.Select(x => x.ApplyState).MergeSideBySide();

        public override IEnumerable<ObjectReference> ContextReferences =>
            _children.SelectMany(x => x.ContextReferences);

        public override TValueInfo Value => default(TValueInfo).ConstantInfoForSideBySide(_children.Select(x => x.Node));

        public bool RequestPreserve => _children.Any(x => x.Node.RequestPreserve);

        public bool IsEmpty => _children.Count == 0 || ApplyState == ApplyState.Never;

        public IEnumerable<Component> SourceComponents => _children.Select(x => x.Component);
        public IEnumerable<ComponentPropModNodeBase<TValueInfo>> ComponentNodes => _children.Select(x => x.Node);

        public void Add(ComponentPropModNodeBase<TValueInfo> node, ApplyState applyState)
        {
            _children.Add(new ComponentInfo(node, applyState));
            DestroyTracker.Track(node.Component, OnDestroy);
        }

        public void Add(RootPropModNode<TValueInfo> toAdd)
        {
            if (toAdd == null) throw new ArgumentNullException(nameof(toAdd));
            foreach (var child in toAdd._children)
                Add(child.Node, child.ApplyState);
        }

        private void OnDestroy(int objectId)
        {
            _children.RemoveAll(x => x.Component.GetInstanceID() == objectId);
        }

        public void Remove(Component sourceComponent)
        {
            var removed = _children.RemoveAll(x => x.Component == sourceComponent);
            if (removed > 0)
                DestroyTracker.Untrack(sourceComponent, OnDestroy);
        }

        public void Invalidate()
        {
            foreach (var componentInfo in _children)
                DestroyTracker.Untrack(componentInfo.Component, OnDestroy);
            _children.Clear();
        }
    }

    internal abstract class ImmutablePropModNode<TValueInfo> : PropModNode<TValueInfo>
        where TValueInfo: struct, IValueInfo<TValueInfo>
    {
    }

    internal class FloatAnimationCurveNode : ImmutablePropModNode<FloatValueInfo>
    {
        public AnimationCurve Curve { get; }
        public AnimationClip Clip { get; }

        public static FloatAnimationCurveNode Create(AnimationClip clip, EditorCurveBinding binding,
            AnimationClip? additiveReferenceClip, float additiveReferenceFrame)
        {
            return new FloatAnimationCurveNode(clip, binding, additiveReferenceClip, additiveReferenceFrame);
        }

        private FloatAnimationCurveNode(AnimationClip clip, EditorCurveBinding binding,
            AnimationClip? additiveReferenceClip, float additiveReferenceFrame)
        {
            if (!clip) throw new ArgumentNullException(nameof(clip));
            Clip = clip;
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            Curve = curve ?? throw new ArgumentNullException(nameof(curve));
            _constantInfo = new Lazy<FloatValueInfo>(() => ParseProperty(curve, additiveReferenceClip, binding, additiveReferenceFrame), isThreadSafe: false);
        }

        private readonly Lazy<FloatValueInfo> _constantInfo;

        public override ApplyState ApplyState => ApplyState.Always;
        public override FloatValueInfo Value => _constantInfo.Value;
        public override IEnumerable<ObjectReference> ContextReferences => new[] { ObjectRegistry.GetReference(Clip) };

        private static FloatValueInfo ParseProperty(AnimationCurve curve,
            AnimationClip? additiveReferenceClip, EditorCurveBinding binding, float additiveReferenceFrame)
        {
            var curveValue = ParseCurve(curve);
            if (curveValue.PossibleValues == null) return FloatValueInfo.Variable;

            float referenceValue = 0;
            if (additiveReferenceClip != null 
                && AnimationUtility.GetEditorCurve(additiveReferenceClip, binding) is { } referenceCurve)
                referenceValue = referenceCurve.Evaluate(additiveReferenceFrame);
            else
                referenceValue = curve.Evaluate(0);

            return new FloatValueInfo(curveValue.PossibleValues.Concat(new[] { referenceValue }).Distinct().ToArray());
        }

        private static FloatValueInfo ParseCurve(AnimationCurve curve)
        {
            // TODO: we should check actual behavior with no keyframes
            if (curve.keys.Length == 0) return FloatValueInfo.Variable; 
            if (curve.keys.Length == 1) return new FloatValueInfo(curve.keys[0].value);

            float constValue = 0;
            foreach (var (preKey, postKey) in curve.keys.ZipWithNext())
            {
                var preWeighted = preKey.weightedMode == WeightedMode.Out || preKey.weightedMode == WeightedMode.Both;
                var postWeighted = postKey.weightedMode == WeightedMode.In || postKey.weightedMode == WeightedMode.Both;

                if (preKey.value.CompareTo(postKey.value) != 0) return FloatValueInfo.Variable;
                constValue = preKey.value;
                // it's constant
                if (float.IsInfinity(preKey.outWeight) || float.IsInfinity(postKey.inTangent)) continue;
                if (preKey.outTangent == 0 && postKey.inTangent == 0) continue;
                if (preWeighted && postWeighted && preKey.outWeight == 0 && postKey.inWeight == 0) continue;
                return FloatValueInfo.Variable;
            }

            return new FloatValueInfo(constValue);
        }
    }

    internal class ObjectAnimationCurveNode : ImmutablePropModNode<ObjectValueInfo>
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
            Utils.Assert(frames.Length > 0);
            Clip = clip;
            Frames = frames;
            _constantInfo = new Lazy<ObjectValueInfo>(() => ParseProperty(frames), isThreadSafe: false);
        }


        private readonly Lazy<ObjectValueInfo> _constantInfo;

        public override ApplyState ApplyState => ApplyState.Always;
        public override ObjectValueInfo Value => _constantInfo.Value;
        public override IEnumerable<ObjectReference> ContextReferences => new[] { ObjectRegistry.GetReference(Clip) };

        private static ObjectValueInfo ParseProperty(ObjectReferenceKeyframe[] frames) =>
            new(frames.Select(x => x.value).Distinct().ToArray());
    }

    internal struct BlendTreeElement<TValueInfo>
        where TValueInfo : struct, IValueInfo<TValueInfo>
    {
        public int Index;
        public ImmutablePropModNode<TValueInfo> Node;

        public BlendTreeElement(int index, ImmutablePropModNode<TValueInfo> node)
        {
            Index = index;
            Node = node ?? throw new ArgumentNullException(nameof(node));
        }
    }

    internal class BlendTreeNode<TValueInfo> : ImmutablePropModNode<TValueInfo>
        where TValueInfo : struct, IValueInfo<TValueInfo>
    {
        private readonly List<BlendTreeElement<TValueInfo>> _children;
        private readonly BlendTreeType _blendTreeType;
        private readonly bool _partial;

        public BlendTreeNode(List<BlendTreeElement<TValueInfo>> children,
            BlendTreeType blendTreeType, bool partial)
        {
            // expected to pass list or array
            // ReSharper disable once PossibleMultipleEnumeration
            Utils.Assert(children.Any());
            // ReSharper disable once PossibleMultipleEnumeration
            _children = children;
            _blendTreeType = blendTreeType;
            _partial = partial;
        }


        private bool WeightSumIsOne => _blendTreeType != BlendTreeType.Direct;
        public IReadOnlyList<BlendTreeElement<TValueInfo>> Children => _children;
        public bool IsPartialApplication => _partial || !WeightSumIsOne;

        public override ApplyState ApplyState =>
            (WeightSumIsOne && !_partial ? ApplyState.Always : ApplyState.Partially)
            .MultiplyApplyState(_children.Select(x => x.Node.ApplyState).MergeSideBySide());

        public override TValueInfo Value
        {
            get => default(TValueInfo).ConstantInfoForBlendTree(_children.Select(x => x.Node), _blendTreeType);
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

        public virtual bool RequestPreserve => false;
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

    class VariableComponentPropModNode : ComponentPropModNode<FloatValueInfo, Component>
    {
        public VariableComponentPropModNode(Component component) : base(component)
        {
        }
        public VariableComponentPropModNode(Component component, bool preserve) : base(component)
        {
            RequestPreserve = preserve;
        }

        public override ApplyState ApplyState => ApplyState.Partially;
        public override FloatValueInfo Value => FloatValueInfo.Variable;
        public override bool RequestPreserve { get; }
    }

    class AnimationComponentPropModNode<TValueInfo> : ComponentPropModNode<TValueInfo, Animation>
        where TValueInfo : struct, IValueInfo<TValueInfo>
    {
        public ImmutablePropModNode<TValueInfo> Animation { get; }

        public AnimationComponentPropModNode(Animation component, ImmutablePropModNode<TValueInfo> animation) : base(component)
        {
            Animation = animation;
            _constantInfo = new Lazy<TValueInfo>(() => animation.Value, isThreadSafe: false);
        }

        private readonly Lazy<TValueInfo> _constantInfo;

        public override ApplyState ApplyState => Animation.ApplyState;
        public override TValueInfo Value => _constantInfo.Value;

        public override IEnumerable<ObjectReference> ContextReferences =>
            base.ContextReferences.Concat(Animation.ContextReferences);
    }
}
