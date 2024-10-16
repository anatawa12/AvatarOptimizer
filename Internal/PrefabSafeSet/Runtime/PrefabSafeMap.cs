using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Anatawa12.AvatarOptimizer.PrefabSafeUniqueCollection;
using UnityEngine;
using UnityEngine.Animations;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.PrefabSafeMap
{
    public struct PrefabSafeMapManipulator<TKey, TValue> : IManipulator<MapEntry<TKey, TValue>, TKey>
    {
        public ref TKey? GetKey(ref MapEntry<TKey, TValue> value) => ref value.key;
        public TKey? GetKey(MapEntry<TKey, TValue> value) => value.key;
    }

    /// <summary>
    /// The serializable class to express dictionary.
    /// using array will make prefab modifications too big so I made this class
    /// </summary>
    /// <typeparam name="TKey">Key Type</typeparam>
    /// <typeparam name="TValue">Value Type</typeparam>
    [NotKeyable, Serializable]
    public class PrefabSafeMap<TKey, TValue> : PrefabSafeUniqueCollection<MapEntry<TKey, TValue>, TKey, PrefabSafeMapManipulator<TKey, TValue>>
        where TKey : notnull
    {
        public PrefabSafeMap(Object outerObject) : base(outerObject)
        {
        }

        public Dictionary<TKey, TValue?> GetAsMap() =>
            GetCollection().ToDictionary(entry => entry.key!, entry => entry.value);
    }

    [Serializable]
    public struct MapEntry<TKey, TValue>
    {
        [SerializeField] internal TKey? key;
        [SerializeField] internal TValue? value;
    }

    public static class PrefabSafeMap {
        public static void OnValidate<TKey, TValue, TComponent>(TComponent component, Func<TComponent, PrefabSafeMap<TKey, TValue>> getPrefabSafeMap) where TComponent : Component
            where TKey : notnull
        {
            PrefabSafeUniqueCollection.PrefabSafeUniqueCollection.OnValidate(component, getPrefabSafeMap);
        }
    }
}
