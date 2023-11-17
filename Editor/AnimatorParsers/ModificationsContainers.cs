using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.AnimatorParsers
{
    interface IModificationsContainer
    {
        IReadOnlyDictionary<ComponentOrGameObject, AnimationComponent> ModifiedProperties { get; }
        ModificationsContainer ToMutable();
        ImmutableModificationsContainer ToImmutable();
    }

    readonly struct AnimationComponent
    {
        [CanBeNull] private readonly IReadOnlyDictionary<string, AnimationFloatProperty> _floatProperties;
        [CanBeNull] private readonly IReadOnlyDictionary<string, AnimationObjectProperty> _objectProperties;

        [NotNull]
        public IReadOnlyDictionary<string, AnimationFloatProperty> FloatProperties =>
            _floatProperties ?? Utils.EmptyDictionary<string, AnimationFloatProperty>();

        [NotNull]
        public IReadOnlyDictionary<string, AnimationObjectProperty> ObjectProperties =>
            _objectProperties ?? Utils.EmptyDictionary<string, AnimationObjectProperty>();

        public AnimationComponent(IReadOnlyDictionary<string, AnimationFloatProperty> floatProperties,
            IReadOnlyDictionary<string, AnimationObjectProperty> objectProperties)
        {
            _floatProperties = floatProperties;
            _objectProperties = objectProperties;
        }

        [Obsolete]
        public bool TryGetValue(string name, out AnimationFloatProperty property) => TryGetFloat(name, out property);
        
        public bool TryGetFloat(string name, out AnimationFloatProperty property) =>
            FloatProperties.TryGetValue(name, out property);
        public bool TryGetObject(string name, out AnimationObjectProperty property) =>
            ObjectProperties.TryGetValue(name, out property);

        public bool IsEmpty() => _floatProperties == null || _objectProperties == null;
    }

    static class ModificationsUtils
    {
        public static AnimationComponent GetComponent<T>(this T container, ComponentOrGameObject component)
            where T : IModificationsContainer
            => container.ModifiedProperties.TryGetValue(component, out var value) ? value : default;

        public static bool? GetConstantValue<T>(this T container, ComponentOrGameObject obj, string property, bool currentValue)
            where T : IModificationsContainer
        {
            if (!container.GetComponent(obj).TryGetFloat(property, out var prop))
                return currentValue;

            switch (prop.State)
            {
                case AnimationPropertyState.ConstantAlways:
                    return FloatToBool(prop.ConstValue);
                case AnimationPropertyState.ConstantPartially:
                    var constValue = FloatToBool(prop.ConstValue);
                    if (constValue == currentValue) return currentValue;
                    return null;
                case AnimationPropertyState.Variable:
                    return null;
                case AnimationPropertyState.Invalid:
                default:
                    throw new ArgumentOutOfRangeException();
            }

            bool? FloatToBool(float f)
            {
                switch (f)
                {
                    case 0:
                        return false;
                    case 1:
                        return true;
                    default:
                        return null;
                }
            }
        }

        public static IModificationsContainer MergeContainersSideBySide<T>([ItemNotNull] this IEnumerable<T> enumerable)
            where T : IModificationsContainer
        {
            using (var enumerator = enumerable.GetEnumerator())
            {
                if (!enumerator.MoveNext()) return ImmutableModificationsContainer.Empty;
                var first = enumerator.Current;
                if (!enumerator.MoveNext()) return first;

                // ReSharper disable once PossibleNullReferenceException // miss detections

                // merge all properties
                var merged = first.ToMutable();
                do merged.MergeAsSide(enumerator.Current);
                while (enumerator.MoveNext());

                return merged;
            }
        }
    }

    readonly struct ImmutableModificationsContainer : IModificationsContainer
    {
        private readonly IReadOnlyDictionary<ComponentOrGameObject, AnimationComponent> _components;

        public IReadOnlyDictionary<ComponentOrGameObject, AnimationComponent> ModifiedProperties =>
            _components ?? Utils.EmptyDictionary<ComponentOrGameObject, AnimationComponent>();
        public static ImmutableModificationsContainer Empty => default;

        public ImmutableModificationsContainer(ModificationsContainer from)
        {
            _components =
                from.ModifiedProperties.ToImmutableDictionary(p => p.Key,
                    p => new AnimationComponent(p.Value.FloatProperties.ToImmutableDictionary(),
                        p.Value.ObjectProperties.ToImmutableDictionary()));
        }

        public ModificationsContainer ToMutable() => new ModificationsContainer(this);
        public ImmutableModificationsContainer ToImmutable() => this;
    }

    class ModificationsContainer : IModificationsContainer
    {
        private readonly Dictionary<ComponentOrGameObject, MutableAnimationComponent> _components;
        private readonly Dictionary<ComponentOrGameObject, AnimationComponent> _publicComponents;

        public IReadOnlyDictionary<ComponentOrGameObject, AnimationComponent> ModifiedProperties => _publicComponents;

        public ModificationsContainer()
        {
            _components = new Dictionary<ComponentOrGameObject, MutableAnimationComponent>();
            _publicComponents = new Dictionary<ComponentOrGameObject, AnimationComponent>();
        }

        internal readonly struct MutableAnimationComponent
        {
            public readonly Dictionary<string, AnimationFloatProperty> FloatProperties;
            public readonly Dictionary<string, AnimationObjectProperty> ObjectProperties;

            public MutableAnimationComponent(Dictionary<string, AnimationFloatProperty> floatProperties, Dictionary<string, AnimationObjectProperty> objectProperties)
            {
                FloatProperties = floatProperties;
                ObjectProperties = objectProperties;
            }

            public MutableAnimationComponent(AnimationComponent component)
            {
                FloatProperties = component.FloatProperties.ToDictionary(p => p.Key, p => p.Value);
                ObjectProperties = component.ObjectProperties.ToDictionary(p => p.Key, p => p.Value);
            }

            public AnimationComponent AsPublic() => new AnimationComponent(FloatProperties, ObjectProperties);

            public static MutableAnimationComponent New()
            {
                return new MutableAnimationComponent(new Dictionary<string, AnimationFloatProperty>(),
                    new Dictionary<string, AnimationObjectProperty>());
            }

            public void PartiallyApplied()
            {
                foreach (var property in FloatProperties.Keys.ToArray())
                    FloatProperties[property] = FloatProperties[property].PartiallyApplied();

                foreach (var property in ObjectProperties.Keys.ToArray())
                    ObjectProperties[property] = ObjectProperties[property].PartiallyApplied();
            }
            public void Vairable()
            {
                foreach (var property in FloatProperties.Keys.ToArray())
                    FloatProperties[property] = FloatProperties[property].Variable();

                foreach (var property in ObjectProperties.Keys.ToArray())
                    ObjectProperties[property] = ObjectProperties[property].Variable();
            }

            public bool IsEmpty() => FloatProperties == null && ObjectProperties == null;
        }

        public ModificationsContainer(ImmutableModificationsContainer from)
        {
            _components = from.ModifiedProperties
                .ToDictionary(p => p.Key, p =>
                    new MutableAnimationComponent(p.Value));
            _publicComponents = _components.ToDictionary(p => p.Key, p => p.Value.AsPublic());
        }

        public ModificationsContainer ToMutable() => this;
        public ImmutableModificationsContainer ToImmutable() => new ImmutableModificationsContainer(this);

        #region Adding Modifications

        public ComponentAnimationUpdater ModifyObject(ComponentOrGameObject obj)
        {
            if (!_components.TryGetValue(obj, out var component))
            {
                _components.Add(obj, component = MutableAnimationComponent.New());
                _publicComponents.Add(obj, component.AsPublic());
            }

            return new ComponentAnimationUpdater(component);
        }

        public readonly struct ComponentAnimationUpdater
        {
            private readonly MutableAnimationComponent _component;

            internal ComponentAnimationUpdater(MutableAnimationComponent component)
            {
                _component = component;
            }

            public void AddModificationAsNewLayer(string propertyName, AnimationFloatProperty propertyState)
            {
                if (_component.FloatProperties.TryGetValue(propertyName, out var property))
                    _component.FloatProperties[propertyName] = property.Merge(propertyState, asNewLayer: true);
                else
                    _component.FloatProperties.Add(propertyName, propertyState);
            }
            
            public void AddModificationAsNewAdditiveLayer(string propertyName, AnimationFloatProperty propertyState)
            {
                switch (propertyState.State)
                {
                    case AnimationPropertyState.ConstantAlways:
                    case AnimationPropertyState.ConstantPartially:
                        // const 
                        break;
                    case AnimationPropertyState.Variable:
                        _component.FloatProperties[propertyName] = propertyState.Variable();
                        break;
                    case AnimationPropertyState.Invalid:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            public void AddModificationAsNewLayer(string propertyName, AnimationObjectProperty propertyState)
            {
                if (_component.ObjectProperties.TryGetValue(propertyName, out var property))
                    _component.ObjectProperties[propertyName] = property.Merge(propertyState, asNewLayer: true);
                else
                    _component.ObjectProperties.Add(propertyName, propertyState);
            }
            
            public void AddModificationAsNewAdditiveLayer(string propertyName, AnimationObjectProperty propertyState)
            {
                switch (propertyState.State)
                {
                    case AnimationPropertyState.ConstantAlways:
                    case AnimationPropertyState.ConstantPartially:
                        // const 
                        break;
                    case AnimationPropertyState.Variable:
                        _component.ObjectProperties[propertyName] = propertyState.Variable();
                        break;
                    case AnimationPropertyState.Invalid:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        #endregion

        /// <summary>
        /// Merge the specified Animator as new layer applied after this layer
        /// </summary>
        public void MergeAsNewLayer<T>(T parsed, AnimatorWeightState weightState) where T : IModificationsContainer
        {
            if (weightState == AnimatorWeightState.AlwaysZero) return;
            foreach (var (obj, properties) in parsed.ModifiedProperties)
            {
                var updater = ModifyObject(obj);

                foreach (var (propertyName, propertyState) in properties.FloatProperties)
                    updater.AddModificationAsNewLayer(propertyName, weightState.ApplyToProperty(propertyState));
                foreach (var (propertyName, propertyState) in properties.ObjectProperties)
                    updater.AddModificationAsNewLayer(propertyName, weightState.ApplyToProperty(propertyState));
            }
        }

        /// <summary>
        /// Merge the specified Animator as new layer applied after this layer
        /// </summary>
        public void MergeAsNewAdditiveLayer<T>(T parsed, AnimatorWeightState weightState) where T : IModificationsContainer
        {
            if (weightState == AnimatorWeightState.AlwaysZero) return;
            foreach (var (obj, properties) in parsed.ModifiedProperties)
            {
                var updater = ModifyObject(obj);

                foreach (var (propertyName, propertyState) in properties.FloatProperties)
                    updater.AddModificationAsNewAdditiveLayer(propertyName, weightState.ApplyToProperty(propertyState));
                foreach (var (propertyName, propertyState) in properties.ObjectProperties)
                    updater.AddModificationAsNewAdditiveLayer(propertyName, weightState.ApplyToProperty(propertyState));
            }
        }

        public void MergeAsSide<T>(T other) where T : IModificationsContainer
        {
            foreach (var (obj, (thisComponent, otherComponent)) in _components.ZipByKey(other.ModifiedProperties))
            {
                if (otherComponent.IsEmpty())
                {
                    // the object is modified by current only: everything in thisProperties should be marked partially
                    thisComponent.PartiallyApplied();
                }
                else if (thisComponent.IsEmpty())
                {
                    // the object is modified by other only: copy otherProperties with everything marked partially
                    var newComponent = new MutableAnimationComponent(otherComponent);
                    newComponent.PartiallyApplied();
                    _components.Add(obj, newComponent);
                    _publicComponents.Add(obj, newComponent.AsPublic());
                }
                else
                {
                    // the object id modified by both

                    foreach (var (property, (thisState, otherState)) in thisComponent.FloatProperties.ZipByKey(otherComponent.FloatProperties))
                    {
                        if (otherState.State == AnimationPropertyState.Invalid)
                        {
                            // the property is modified by current only: this modification should be marked partially
                            thisComponent.FloatProperties[property] = thisState.PartiallyApplied();
                        }
                        else if (thisState.State == AnimationPropertyState.Invalid)
                        {
                            // the property is modified by other only: copied with marked partially
                            thisComponent.FloatProperties.Add(property, otherState.PartiallyApplied());
                        }
                        else
                        {
                            // the property is modified by both: merge the property modification
                            thisComponent.FloatProperties[property] = thisState.Merge(otherState, asNewLayer: false);
                        }
                    }
                    
                    foreach (var (property, (thisState, otherState)) in thisComponent.ObjectProperties.ZipByKey(otherComponent.ObjectProperties))
                    {
                        if (otherState.State == AnimationPropertyState.Invalid)
                        {
                            // the property is modified by current only: this modification should be marked partially
                            thisComponent.ObjectProperties[property] = thisState.PartiallyApplied();
                        }
                        else if (thisState.State == AnimationPropertyState.Invalid)
                        {
                            // the property is modified by other only: copied with marked partially
                            thisComponent.ObjectProperties.Add(property, otherState.PartiallyApplied());
                        }
                        else
                        {
                            // the property is modified by both: merge the property modification
                            thisComponent.ObjectProperties[property] = thisState.Merge(otherState, asNewLayer: false);
                        }
                    }
                }
            }
        }

        // make all property modification variable
        public void MakeAllVariable()
        {
            foreach (var properties in _components.Values)
                properties.Vairable();
        }
    }

    interface IAnimationProperty<T>
    {
        T ConstValue { get; }
        AnimationPropertyState State { get; }
        bool IsConst { get; }
        ReadOnlySpan<IModificationSource> Sources { get; }
    }

    readonly struct AnimationProperty<T> : IEquatable<AnimationProperty<T>>, IAnimationProperty<T>
    {
        public AnimationPropertyState State { get; }
        private readonly T _constValue;

        public T ConstValue
        {
            get
            {
                switch (State)
                {
                    case AnimationPropertyState.ConstantAlways:
                    case AnimationPropertyState.ConstantPartially:
                        return _constValue;
                    case AnimationPropertyState.Invalid:
                    case AnimationPropertyState.Variable:
                        throw new InvalidOperationException("Non Const State");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public bool IsConst
        {
            get
            {
                switch (State)
                {
                    case AnimationPropertyState.ConstantAlways:
                    case AnimationPropertyState.ConstantPartially:
                        return true;
                    case AnimationPropertyState.Invalid:
                    case AnimationPropertyState.Variable:
                        return false;
                    default:
                        throw new ArgumentOutOfRangeException();
                } 
            }
        }

        private readonly IModificationSource[] _sources;
        public ReadOnlySpan<IModificationSource> Sources => _sources ?? Array.Empty<IModificationSource>();

        private AnimationProperty(AnimationPropertyState state, T constValue, params IModificationSource[] modifiers) =>
            (State, _constValue, _sources) = (state, constValue, modifiers);

        public static AnimationProperty<T> ConstAlways(T value, IModificationSource modifier) =>
            ConstAlways0(value, new[] { modifier });

        public static AnimationProperty<T> ConstPartially(T value, IModificationSource modifier) =>
            ConstPartially0(value, new[] { modifier });

        public static AnimationProperty<T> Variable(IModificationSource modifier) =>
            Variable0(new[] { modifier });

        private static AnimationProperty<T> ConstAlways0(T value, IModificationSource[] modifiers) =>
            new AnimationProperty<T>(AnimationPropertyState.ConstantAlways, value, modifiers);

        private static AnimationProperty<T> ConstPartially0(T value, IModificationSource[] modifiers) =>
            new AnimationProperty<T>(AnimationPropertyState.ConstantPartially, value, modifiers);

        private static AnimationProperty<T> Variable0(IModificationSource[] modifiers) =>
            new AnimationProperty<T>(AnimationPropertyState.Variable, default, modifiers);

        private IModificationSource[] MergeSource(ReadOnlySpan<IModificationSource> aSource, ReadOnlySpan<IModificationSource> bSource)
        {
            var merged = new IModificationSource[aSource.Length + bSource.Length];
            aSource.CopyTo(merged.AsSpan().Slice(0, aSource.Length));
            bSource.CopyTo(merged.AsSpan().Slice(aSource.Length, bSource.Length));
            return merged;
        }

        public AnimationProperty<T> Merge(AnimationProperty<T> b, bool asNewLayer)
        {
            // if asNewLayer and new layer is constant always, the value is used
            if (asNewLayer && b.State == AnimationPropertyState.ConstantAlways) return b;

            if (State == AnimationPropertyState.Variable) return Variable0(MergeSource(Sources, b.Sources));
            if (b.State == AnimationPropertyState.Variable) return Variable0(MergeSource(Sources, b.Sources));

            // now they are constant.
            if (!ConstValue.Equals(b.ConstValue)) return Variable0(MergeSource(Sources, b.Sources));

            var value = ConstValue;

            if (State == AnimationPropertyState.ConstantPartially) return ConstPartially0(value, MergeSource(Sources, b.Sources));
            if (b.State == AnimationPropertyState.ConstantPartially) return ConstPartially0(value, MergeSource(Sources, b.Sources));

            System.Diagnostics.Debug.Assert(State == AnimationPropertyState.ConstantAlways);
            System.Diagnostics.Debug.Assert(b.State == AnimationPropertyState.ConstantAlways);

            return this;
        }

        public AnimationProperty<T> PartiallyApplied()
        {
            switch (State)
            {
                case AnimationPropertyState.ConstantAlways:
                    return ConstPartially0(ConstValue, _sources);
                case AnimationPropertyState.ConstantPartially:
                case AnimationPropertyState.Variable:
                    return this;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public AnimationProperty<T> Variable() => Variable0(_sources);

        public bool Equals(AnimationProperty<T> other)
        {
            switch (State)
            {
                case AnimationPropertyState.ConstantAlways:
                case AnimationPropertyState.ConstantPartially:
                    return State == other.State && _constValue.Equals(other.ConstValue);
                case AnimationPropertyState.Invalid:
                case AnimationPropertyState.Variable:
                default:
                    return State == other.State;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is AnimationProperty<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            switch (State)
            {
                case AnimationPropertyState.ConstantAlways:
                case AnimationPropertyState.ConstantPartially:
                    return unchecked(((int)State * 397) ^ _constValue.GetHashCode());
                default:
                case AnimationPropertyState.Invalid:
                case AnimationPropertyState.Variable:
                    return unchecked(((int)State * 397));
            }
        }

        public override string ToString()
        {
            switch (State)
            {
                case AnimationPropertyState.Invalid:
                    return "Invalid";
                case AnimationPropertyState.ConstantAlways:
                    return $"ConstantAlways({ConstValue})";
                case AnimationPropertyState.ConstantPartially:
                    return $"ConstantPartially({ConstValue})";
                case AnimationPropertyState.Variable:
                    return "Variable";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    readonly struct AnimationFloatProperty : IEquatable<AnimationFloatProperty>, IAnimationProperty<float>
    {
        private readonly AnimationProperty<float> _impl;
        public AnimationPropertyState State => _impl.State;
        public float ConstValue => _impl.ConstValue;
        public bool IsConst => _impl.IsConst;

        public ReadOnlySpan<IModificationSource> Sources => _impl.Sources;

        private AnimationFloatProperty(AnimationProperty<float> impl) => _impl = impl;

        public static AnimationFloatProperty ConstAlways(float value, IModificationSource modifier) =>
            new AnimationFloatProperty(AnimationProperty<float>.ConstAlways(value, modifier));

        public static AnimationFloatProperty ConstPartially(float value, IModificationSource modifier) =>
            new AnimationFloatProperty(AnimationProperty<float>.ConstPartially(value, modifier));

        public static AnimationFloatProperty Variable(IModificationSource modifier) =>
            new AnimationFloatProperty(AnimationProperty<float>.Variable(modifier));

        public AnimationFloatProperty Merge(AnimationFloatProperty b, bool asNewLayer) =>
            new AnimationFloatProperty(_impl.Merge(b._impl, asNewLayer));
        public AnimationFloatProperty PartiallyApplied() => new AnimationFloatProperty(_impl.PartiallyApplied());
        public AnimationFloatProperty Variable() => new AnimationFloatProperty(_impl.Variable());

        public bool Equals(AnimationFloatProperty other) => _impl.Equals(other._impl);
        public override bool Equals(object obj) => obj is AnimationFloatProperty other && Equals(other);
        public override int GetHashCode() => _impl.GetHashCode();
        public override string ToString() => _impl.ToString();

        public static AnimationFloatProperty? ParseProperty(AnimationCurve curve, IModificationSource source)
        {
            if (curve.keys.Length == 0) return null;
            if (curve.keys.Length == 1)
                return ConstAlways(curve.keys[0].value, source);

            float constValue = 0;
            foreach (var (preKey, postKey) in curve.keys.ZipWithNext())
            {
                var preWeighted = preKey.weightedMode == WeightedMode.Out || preKey.weightedMode == WeightedMode.Both;
                var postWeighted = postKey.weightedMode == WeightedMode.In || postKey.weightedMode == WeightedMode.Both;

                if (preKey.value.CompareTo(postKey.value) != 0) return Variable(source);
                constValue = preKey.value;
                // it's constant
                if (float.IsInfinity(preKey.outWeight) || float.IsInfinity(postKey.inTangent))
                    continue;
                if (preKey.outTangent == 0 && postKey.inTangent == 0)
                    continue;
                if (preWeighted && postWeighted && preKey.outWeight == 0 && postKey.inWeight == 0)
                    continue;
                return Variable(source);
            }

            return ConstAlways(constValue, source);
        }
    }

    readonly struct AnimationObjectProperty : IEquatable<AnimationObjectProperty>, IAnimationProperty<Object>
    {
        private readonly AnimationProperty<Object> _impl;
        public AnimationPropertyState State => _impl.State;

        public Object ConstValue => _impl.ConstValue;
        public bool IsConst => _impl.ConstValue;
        public ReadOnlySpan<IModificationSource> Sources => _impl.Sources;

        public AnimationObjectProperty(AnimationProperty<Object> impl) : this() => _impl = impl;

        public static AnimationObjectProperty ConstAlways(Object value, IModificationSource modifier) =>
            new AnimationObjectProperty(AnimationProperty<Object>.ConstAlways(value, modifier));
        public static AnimationObjectProperty ConstPartially(Object value, IModificationSource modifier) =>
            new AnimationObjectProperty(AnimationProperty<Object>.ConstPartially(value, modifier));
        public static AnimationObjectProperty Variable(IModificationSource modifier) =>
            new AnimationObjectProperty(AnimationProperty<Object>.Variable(modifier));
        public AnimationObjectProperty Merge(AnimationObjectProperty b, bool asNewLayer) =>
            new AnimationObjectProperty(_impl.Merge(b._impl, asNewLayer));

        public static AnimationObjectProperty? ParseProperty(ObjectReferenceKeyframe[] curve, IModificationSource source)
        {
            if (curve.Length == 0) return null;
            var value = curve[0].value;
            for (var i = 1; i < curve.Length; i++)
                if (curve[i].value != value)
                    return Variable(source);
            return ConstAlways(value, source);
        }

        public AnimationObjectProperty PartiallyApplied() => new AnimationObjectProperty(_impl.PartiallyApplied());
        public AnimationObjectProperty Variable() => new AnimationObjectProperty(_impl.Variable());

        public bool Equals(AnimationObjectProperty other) => _impl.Equals(other._impl);
        public override bool Equals(object obj) => obj is AnimationObjectProperty other && Equals(other);
        public override int GetHashCode() => _impl.GetHashCode();
        public override string ToString() => _impl.ToString();
    }

    enum AnimationPropertyState
    {
        Invalid = 0,
        ConstantAlways,
        ConstantPartially,
        Variable,
    }

    enum AnimatorWeightState
    {
        NotChanged,
        AlwaysZero,
        AlwaysOne,
        EitherZeroOrOne,
        Variable
    }

    class AnimatorLayerWeightMap<TKey>
    {
        private Dictionary<TKey, AnimatorWeightState> _backed =
            new Dictionary<TKey, AnimatorWeightState>();

        public AnimatorWeightState this[TKey key]
        {
            get
            {
                _backed.TryGetValue(key, out var state);
                return state;
            }
            set => _backed[key] = value;
        }

        public AnimatorWeightState Get(TKey key) => this[key];
    }

    static class AnimatorLayerWeightStates
    {
        public static AnimatorWeightState WeightStateFor(float duration, float weight) =>
            duration != 0 ? AnimatorWeightState.Variable : WeightStateFor(weight);

        public static AnimatorWeightState WeightStateFor(float weight)
        {
            switch (weight)
            {
                case 0:
                    return AnimatorWeightState.AlwaysZero;
                case 1:
                    return AnimatorWeightState.AlwaysOne;
                default:
                    return AnimatorWeightState.Variable;
            }
        }
        
        public static AnimatorWeightState Merge(this AnimatorWeightState a, AnimatorWeightState b)
        {
            // 25 pattern
            if (a == b) return a;

            if (a == AnimatorWeightState.NotChanged) return b;
            if (b == AnimatorWeightState.NotChanged) return a;

            if (a == AnimatorWeightState.Variable) return AnimatorWeightState.Variable;
            if (b == AnimatorWeightState.Variable) return AnimatorWeightState.Variable;

            if (a == AnimatorWeightState.AlwaysOne && b == AnimatorWeightState.AlwaysZero)
                return AnimatorWeightState.EitherZeroOrOne;
            if (b == AnimatorWeightState.AlwaysOne && a == AnimatorWeightState.AlwaysZero)
                return AnimatorWeightState.EitherZeroOrOne;

            if (a == AnimatorWeightState.EitherZeroOrOne && b == AnimatorWeightState.AlwaysZero)
                return AnimatorWeightState.EitherZeroOrOne;
            if (b == AnimatorWeightState.EitherZeroOrOne && a == AnimatorWeightState.AlwaysZero)
                return AnimatorWeightState.EitherZeroOrOne;

            if (a == AnimatorWeightState.EitherZeroOrOne && b == AnimatorWeightState.AlwaysOne)
                return AnimatorWeightState.EitherZeroOrOne;
            if (b == AnimatorWeightState.EitherZeroOrOne && a == AnimatorWeightState.AlwaysOne)
                return AnimatorWeightState.EitherZeroOrOne;

            throw new ArgumentOutOfRangeException();
        }

        public static AnimationFloatProperty ApplyToProperty(this AnimatorWeightState state, AnimationFloatProperty property)
        {
            switch (state)
            {
                case AnimatorWeightState.AlwaysOne:
                    return property;
                case AnimatorWeightState.EitherZeroOrOne:
                    return property.PartiallyApplied();
                case AnimatorWeightState.Variable:
                    return property.Variable();
                case AnimatorWeightState.NotChanged:
                case AnimatorWeightState.AlwaysZero:
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        public static AnimationObjectProperty ApplyToProperty(this AnimatorWeightState state, AnimationObjectProperty property)
        {
            switch (state)
            {
                case AnimatorWeightState.AlwaysOne:
                    return property;
                case AnimatorWeightState.EitherZeroOrOne:
                    return property.PartiallyApplied();
                case AnimatorWeightState.Variable:
                    return property.Variable();
                case AnimatorWeightState.NotChanged:
                case AnimatorWeightState.AlwaysZero:
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        public static AnimatorWeightState ForAlwaysApplied(bool alwaysApplied) =>
            alwaysApplied ? AnimatorWeightState.AlwaysOne : AnimatorWeightState.EitherZeroOrOne;
    }
}