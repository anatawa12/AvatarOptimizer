using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    interface IModificationsContainer
    {
        IReadOnlyDictionary<ComponentOrGameObject, IReadOnlyDictionary<string, AnimationProperty>> ModifiedProperties { get; }
        ModificationsContainer ToMutable();
        ImmutableModificationsContainer ToImmutable();
    }

    static class ModificationsUtils
    {
        public static IReadOnlyDictionary<string, AnimationProperty> GetModifiedProperties<T>(this T container,
            ComponentOrGameObject component)
            where T : IModificationsContainer
            => container.ModifiedProperties.TryGetValue(component, out var value)
                ? value
                : Utils.EmptyDictionary<string, AnimationProperty>();

        public static bool? GetConstantValue<T>(this T container, ComponentOrGameObject obj, string property, bool currentValue)
            where T : IModificationsContainer
        {
            if (!container.GetModifiedProperties(obj).TryGetValue(property, out var prop))
                return currentValue;

            switch (prop.State)
            {
                case AnimationProperty.PropertyState.ConstantAlways:
                    return FloatToBool(prop.ConstValue);
                case AnimationProperty.PropertyState.ConstantPartially:
                    var constValue = FloatToBool(prop.ConstValue);
                    if (constValue == currentValue) return currentValue;
                    return null;
                case AnimationProperty.PropertyState.Variable:
                    return false;
                case AnimationProperty.PropertyState.Invalid:
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
        private readonly IReadOnlyDictionary<ComponentOrGameObject, IReadOnlyDictionary<string, AnimationProperty>> _modifiedProperties;

        public IReadOnlyDictionary<ComponentOrGameObject, IReadOnlyDictionary<string, AnimationProperty>> ModifiedProperties =>
            _modifiedProperties ?? Utils.EmptyDictionary<ComponentOrGameObject, IReadOnlyDictionary<string, AnimationProperty>>();
        public static ImmutableModificationsContainer Empty => default;

        public ImmutableModificationsContainer(ModificationsContainer from)
        {
            IReadOnlyDictionary<string, AnimationProperty> MapDictionary(IReadOnlyDictionary<string, AnimationProperty> dict) =>
                new ReadOnlyDictionary<string, AnimationProperty>(dict.ToDictionary(p1 => p1.Key, p1 => p1.Value));

            _modifiedProperties =
                new ReadOnlyDictionary<ComponentOrGameObject, IReadOnlyDictionary<string, AnimationProperty>>(from
                    .ModifiedProperties.ToDictionary(p => p.Key, p => MapDictionary(p.Value)));
        }

        public ModificationsContainer ToMutable() => new ModificationsContainer(this);
        public ImmutableModificationsContainer ToImmutable() => this;
    }

    class ModificationsContainer : IModificationsContainer
    {
        private readonly Dictionary<ComponentOrGameObject, Dictionary<string, AnimationProperty>> _modifiedProperties;
        
        private static readonly IReadOnlyDictionary<string, AnimationProperty> EmptyProperties =
            new ReadOnlyDictionary<string, AnimationProperty>(new Dictionary<string, AnimationProperty>());

        public IReadOnlyDictionary<ComponentOrGameObject, IReadOnlyDictionary<string, AnimationProperty>> ModifiedProperties { get; }

        public ModificationsContainer()
        {
            _modifiedProperties = new Dictionary<ComponentOrGameObject, Dictionary<string, AnimationProperty>>();
            ModifiedProperties = Utils.CastDic<IReadOnlyDictionary<string, AnimationProperty>>()
                .CastedDic(_modifiedProperties);
        }

        public ModificationsContainer(ImmutableModificationsContainer from)
        {
            Dictionary<string, AnimationProperty> MapDictionary(IReadOnlyDictionary<string, AnimationProperty> dict) =>
                dict.ToDictionary(p1 => p1.Key, p1 => p1.Value);

            _modifiedProperties = from.ModifiedProperties
                .ToDictionary(p => p.Key, p => MapDictionary(p.Value));

            ModifiedProperties = Utils.CastDic<IReadOnlyDictionary<string, AnimationProperty>>()
                .CastedDic(_modifiedProperties);
        }

        public ModificationsContainer ToMutable() => this;
        public ImmutableModificationsContainer ToImmutable() => new ImmutableModificationsContainer(this);

        #region Adding Modifications

        public ComponentAnimationUpdater ModifyObject(ComponentOrGameObject obj)
        {
            if (!_modifiedProperties.TryGetValue(obj, out var properties))
                _modifiedProperties.Add(obj, properties = new Dictionary<string, AnimationProperty>());
            return new ComponentAnimationUpdater(properties);
        }

        public readonly struct ComponentAnimationUpdater
        {
            private readonly Dictionary<string, AnimationProperty> _properties;

            public ComponentAnimationUpdater(Dictionary<string, AnimationProperty> properties) => _properties = properties;

            public void AddModificationAsNewLayer(string propertyName, AnimationProperty propertyState)
            {
                if (_properties.TryGetValue(propertyName, out var property))
                    _properties[propertyName] = property.Merge(propertyState, asNewLayer: true);
                else
                    _properties.Add(propertyName, propertyState);
            }
            
            public void AddModificationAsNewAdditiveLayer(string propertyName, AnimationProperty propertyState)
            {
                switch (propertyState.State)
                {
                    case AnimationProperty.PropertyState.ConstantAlways:
                    case AnimationProperty.PropertyState.ConstantPartially:
                        // const 
                        break;
                    case AnimationProperty.PropertyState.Variable:
                        _properties[propertyName] = AnimationProperty.Variable();
                        break;
                    case AnimationProperty.PropertyState.Invalid:
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

                foreach (var (propertyName, propertyState) in properties)
                {
                    updater.AddModificationAsNewLayer(propertyName, weightState.ApplyToProperty(propertyState));
                }
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

                foreach (var (propertyName, propertyState) in properties)
                    updater.AddModificationAsNewAdditiveLayer(propertyName, weightState.ApplyToProperty(propertyState));
            }
        }

        public void MergeAsSide<T>(T other) where T : IModificationsContainer
        {
            foreach (var (obj, (thisProperties, otherProperties)) in _modifiedProperties.ZipByKey(other.ModifiedProperties))
            {
                if (otherProperties == null)
                {
                    // the object is modified by current only: everything in thisProperties should be marked partially
                    foreach (var property in thisProperties.Keys.ToArray())
                        thisProperties[property] = thisProperties[property].PartiallyApplied();
                }
                else if (thisProperties == null)
                {
                    // the object is modified by other only: copy otherProperties with everything marked partially
                    _modifiedProperties.Add(obj, EverythingPartially(otherProperties));
                }
                else
                {
                    // the object id modified by both

                    foreach (var (property, (thisState, otherState)) in thisProperties.ZipByKey(otherProperties))
                    {
                        if (otherState.State == AnimationProperty.PropertyState.Invalid)
                        {
                            // the property is modified by current only: this modification should be marked partially
                            thisProperties[property] = thisState.PartiallyApplied();
                        }
                        else if (thisState.State == AnimationProperty.PropertyState.Invalid)
                        {
                            // the property is modified by other only: copied with marked partially
                            thisProperties.Add(property, otherState.PartiallyApplied());
                        }
                        else
                        {
                            // the property is modified by both: merge the property modification
                            thisProperties[property] = thisState.Merge(otherState, asNewLayer: false);
                        }
                    }
                }
            }

            Dictionary<string, AnimationProperty>
                EverythingPartially(IReadOnlyDictionary<string, AnimationProperty> dictionary) =>
                dictionary.ToDictionary(k => k.Key, v => v.Value.PartiallyApplied());
        }

        // make all property modification variable
        public void MakeAllVariable()
        {
            foreach (var properties in _modifiedProperties.Values)
                foreach (var name in properties.Keys.ToArray())
                    properties[name] = AnimationProperty.Variable();
        }
    }

    public readonly struct ComponentOrGameObject : IEquatable<ComponentOrGameObject>
    {
        private readonly Object _object;

        public ComponentOrGameObject(Object o) => _object = o;

        public static implicit operator ComponentOrGameObject(GameObject gameObject) =>
            new ComponentOrGameObject(gameObject);

        public static implicit operator ComponentOrGameObject(Component component) =>
            new ComponentOrGameObject(component);

        public static implicit operator Object(ComponentOrGameObject componentOrGameObject) =>
            componentOrGameObject._object;

        public GameObject SelfOrAttachedGameObject => _object as GameObject ?? ((Component)_object).gameObject;
        public Object Value => _object;

        public bool AsGameObject(out GameObject gameObject)
        {
            gameObject = _object as GameObject;
            return gameObject;
        }

        public bool AsComponent<T>(out T gameObject) where T : Component
        {
            gameObject = _object as T;
            return gameObject;
        }

        public bool Equals(ComponentOrGameObject other) => Equals(_object, other._object);
        public override bool Equals(object obj) => obj is ComponentOrGameObject other && Equals(other);
        public override int GetHashCode() => _object != null ? _object.GetHashCode() : 0;
    }

    readonly struct AnimationProperty : IEquatable<AnimationProperty>
    {
        public readonly PropertyState State;
        private readonly float _constValue;

        public float ConstValue
        {
            get
            {
                switch (State)
                {
                    case PropertyState.ConstantAlways:
                    case PropertyState.ConstantPartially:
                        return _constValue;
                    case PropertyState.Invalid:
                    case PropertyState.Variable:
                        throw new InvalidOperationException("Non Const State");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private AnimationProperty(PropertyState state, float constValue) =>
            (State, _constValue) = (state, constValue);

        public static AnimationProperty ConstAlways(float value) =>
            new AnimationProperty(PropertyState.ConstantAlways, value);

        public static AnimationProperty ConstPartially(float value) =>
            new AnimationProperty(PropertyState.ConstantPartially, value);

        public static AnimationProperty Variable() =>
            new AnimationProperty(PropertyState.Variable, float.NaN);

        public AnimationProperty Merge(AnimationProperty b, bool asNewLayer)
        {
            // if asNewLayer and new layer is constant always, the value is used
            if (asNewLayer && b.State == PropertyState.ConstantAlways) return b;

            if (State == PropertyState.Variable) return Variable();
            if (b.State == PropertyState.Variable) return Variable();

            // now they are constant.
            if (ConstValue.CompareTo(b.ConstValue) != 0) return Variable();

            var value = ConstValue;

            if (State == PropertyState.ConstantPartially) return ConstPartially(value);
            if (b.State == PropertyState.ConstantPartially) return ConstPartially(value);

            System.Diagnostics.Debug.Assert(State == PropertyState.ConstantAlways);
            System.Diagnostics.Debug.Assert(b.State == PropertyState.ConstantAlways);

            return this;
        }

        public static AnimationProperty? ParseProperty(AnimationCurve curve)
        {
            if (curve.keys.Length == 0) return null;
            if (curve.keys.Length == 1)
                return ConstAlways(curve.keys[0].value);

            float constValue = 0;
            foreach (var (preKey, postKey) in curve.keys.ZipWithNext())
            {
                var preWeighted = preKey.weightedMode == WeightedMode.Out || preKey.weightedMode == WeightedMode.Both;
                var postWeighted = postKey.weightedMode == WeightedMode.In || postKey.weightedMode == WeightedMode.Both;

                if (preKey.value.CompareTo(postKey.value) != 0) return Variable();
                constValue = preKey.value;
                // it's constant
                if (float.IsInfinity(preKey.outWeight) || float.IsInfinity(postKey.inTangent))
                    continue;
                if (preKey.outTangent == 0 && postKey.inTangent == 0)
                    continue;
                if (preWeighted && postWeighted && preKey.outWeight == 0 && postKey.inWeight == 0)
                    continue;
                return Variable();
            }

            return ConstAlways(constValue);
        }

        public AnimationProperty PartiallyApplied()
        {
            switch (State)
            {
                case PropertyState.ConstantAlways:
                    return ConstPartially(ConstValue);
                case PropertyState.ConstantPartially:
                case PropertyState.Variable:
                    return this;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public enum PropertyState
        {
            Invalid = 0,
            ConstantAlways,
            ConstantPartially,
            Variable,
        }

        public bool Equals(AnimationProperty other)
        {
            switch (State)
            {
                case PropertyState.ConstantAlways:
                case PropertyState.ConstantPartially:
                    return State == other.State && _constValue.Equals(other.ConstValue);
                case PropertyState.Invalid:
                case PropertyState.Variable:
                default:
                    return State == other.State;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is AnimationProperty other && Equals(other);
        }

        public override int GetHashCode()
        {
            switch (State)
            {
                case PropertyState.ConstantAlways:
                case PropertyState.ConstantPartially:
                    return unchecked(((int)State * 397) ^ _constValue.GetHashCode());
                default:
                case PropertyState.Invalid:
                case PropertyState.Variable:
                    return unchecked(((int)State * 397));
            }
        }

        public override string ToString()
        {
            switch (State)
            {
                case PropertyState.Invalid:
                    return "Invalid";
                case PropertyState.ConstantAlways:
                    return $"ConstantAlways({ConstValue})";
                case PropertyState.ConstantPartially:
                    return $"ConstantPartially({ConstValue})";
                case PropertyState.Variable:
                    return "Variable";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
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

        public static AnimationProperty ApplyToProperty(this AnimatorWeightState state, AnimationProperty property)
        {
            switch (state)
            {
                case AnimatorWeightState.AlwaysOne:
                    return property;
                case AnimatorWeightState.EitherZeroOrOne:
                    return property.PartiallyApplied();
                case AnimatorWeightState.Variable:
                    return AnimationProperty.Variable();
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