using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    interface IModificationsContainer
    {
        IReadOnlyDictionary<Object, IReadOnlyDictionary<string, AnimationProperty>> ModifiedProperties { get; }
        ModificationsContainer ToMutable();
        ImmutableModificationsContainer ToImmutable();
    }

    static class ModificationsContainerExtensions
    {
        public static IReadOnlyDictionary<string, AnimationProperty> GetModifiedProperties<T>(this T container,
            ComponentOrGameObject component)
            where T : IModificationsContainer
            => container.ModifiedProperties.TryGetValue(component, out var value)
                ? value
                : Utils.EmptyDictionary<string, AnimationProperty>();

        public static bool IsAlwaysTrue<T>(this T container, ComponentOrGameObject obj, string property, bool currentValue)
            where T : IModificationsContainer
        {
            if (!container.GetModifiedProperties(obj).TryGetValue(property, out var prop))
                return currentValue;

            switch (prop.State)
            {
                case AnimationProperty.PropertyState.ConstantAlways:
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    return prop.ConstValue == 1;
                case AnimationProperty.PropertyState.ConstantPartially:
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    return prop.ConstValue == 1 && currentValue;
                case AnimationProperty.PropertyState.Variable:
                    return false;
                case AnimationProperty.PropertyState.Invalid:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    readonly struct ImmutableModificationsContainer : IModificationsContainer
    {
        private readonly IReadOnlyDictionary<Object, IReadOnlyDictionary<string, AnimationProperty>> _modifiedProperties;

        public IReadOnlyDictionary<Object, IReadOnlyDictionary<string, AnimationProperty>> ModifiedProperties =>
            _modifiedProperties ?? Utils.EmptyDictionary<Object, IReadOnlyDictionary<string, AnimationProperty>>();
        public static ImmutableModificationsContainer Empty => default;

        public ImmutableModificationsContainer(ModificationsContainer from)
        {
            IReadOnlyDictionary<string, AnimationProperty> MapDictionary(IReadOnlyDictionary<string, AnimationProperty> dict) =>
                new ReadOnlyDictionary<string, AnimationProperty>(dict.ToDictionary(p1 => p1.Key, p1 => p1.Value));

            _modifiedProperties = new ReadOnlyDictionary<Object, IReadOnlyDictionary<string, AnimationProperty>>(from.ModifiedProperties
                .ToDictionary(p => p.Key, p => MapDictionary(p.Value)));
        }

        public ModificationsContainer ToMutable() => new ModificationsContainer(this);
        public ImmutableModificationsContainer ToImmutable() => this;
    }

    class ModificationsContainer : IModificationsContainer
    {
        private readonly Dictionary<Object, Dictionary<string, AnimationProperty>> _modifiedProperties;
        
        private static readonly IReadOnlyDictionary<string, AnimationProperty> EmptyProperties =
            new ReadOnlyDictionary<string, AnimationProperty>(new Dictionary<string, AnimationProperty>());

        public IReadOnlyDictionary<Object, IReadOnlyDictionary<string, AnimationProperty>> ModifiedProperties { get; }

        public ModificationsContainer()
        {
            _modifiedProperties = new Dictionary<Object, Dictionary<string, AnimationProperty>>();
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

        public ComponentAnimationUpdater ModifyComponent(ComponentOrGameObject component) =>
            ModifyObjectUnsafe(component);

        public ComponentAnimationUpdater ModifyObjectUnsafe(Object obj)
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
        }

        #endregion

        /// <summary>
        /// Merge the specified Animator as new layer applied after this layer
        /// </summary>
        public void MergeAsNewLayer<T>(T parsed, bool alwaysAppliedLayer) where T : IModificationsContainer
        {
            foreach (var (obj, properties) in parsed.ModifiedProperties)
            {
                var updater = ModifyObjectUnsafe(obj);

                foreach (var (propertyName, propertyState) in properties)
                    updater.AddModificationAsNewLayer(propertyName,
                        alwaysAppliedLayer ? propertyState : propertyState.PartiallyApplied());
            }
        }

        public void MergeAsSide<T>(T other) where T : IModificationsContainer
        {
            foreach (var (obj, (thisProperties, otherProperties)) in _modifiedProperties.ZipByKey(other.ModifiedProperties))
            {
                if (otherProperties == null)
                {
                    // the object is modified by current only: everything in thisProperties should be marked partially
                    foreach (var (property, state) in thisProperties)
                        thisProperties[property] = state.PartiallyApplied();
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
    }
    
    public readonly struct ComponentOrGameObject
    {
        private readonly Object _object;

        public ComponentOrGameObject(Object o) => _object = o;

        public static implicit operator ComponentOrGameObject(GameObject gameObject) =>
            new ComponentOrGameObject(gameObject);
        public static implicit operator ComponentOrGameObject(Component component) =>
            new ComponentOrGameObject(component);
        public static implicit operator Object(ComponentOrGameObject componentOrGameObject) =>
            componentOrGameObject._object;
    }

    readonly struct AnimationProperty
    {
        public readonly PropertyState State;
        public readonly float ConstValue;

        private AnimationProperty(PropertyState state, float constValue) =>
            (State, ConstValue) = (state, constValue);

        public static AnimationProperty ConstAlways(float value) =>
            new AnimationProperty(PropertyState.ConstantAlways, value);

        public static AnimationProperty ConstPartially(float value) =>
            new AnimationProperty(PropertyState.ConstantAlways, value);

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
    }
}