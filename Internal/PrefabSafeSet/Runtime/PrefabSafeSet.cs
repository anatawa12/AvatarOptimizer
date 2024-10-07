using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.PrefabSafeUniqueCollection;
using UnityEngine;
using UnityEngine.Animations;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.PrefabSafeSet
{
    /// <summary>
    /// The serializable class to express hashset.
    /// using array will make prefab modifications too big so I made this class
    /// </summary>
    /// <typeparam name="T">Element Type</typeparam>
    [NotKeyable, Serializable]
    public class PrefabSafeSet<T> : PrefabSafeUniqueCollection<T, T, IdentityManipulator<T>> 
        where T : notnull
    {
#if UNITY_EDITOR
        [SerializeField, HideInInspector] internal T? fakeSlot;
#endif

        public PrefabSafeSet(Object outerObject) : base(outerObject)
        {
#if UNITY_EDITOR
#endif
        }

        public void SetValueNonPrefab(IEnumerable<T> values)
        {
#if UNITY_EDITOR
            if (OuterObject && UnityEditor.PrefabUtility.IsPartOfPrefabInstance(OuterObject)
                            && UnityEditor.PrefabUtility.IsPartOfAnyPrefab(OuterObject))
                throw new InvalidOperationException("You cannot set value to Prefab Instance or Prefab");
#endif
            // in some (rare) cases, unpacked prefab may have prefabLayers so we need to clear it. 
            prefabLayers = Array.Empty<PrefabLayer<T, T>>();
            mainSet = values.ToArray();
        }

        public HashSet<T> GetAsSet() => new(GetCollection());

        public List<T> GetAsList() => new(GetCollection());

        public new bool AddRange(IEnumerable<T> values) => base.AddRange(values);
        public new bool RemoveRange(IEnumerable<T> values) => base.RemoveRange(values);
        public new void RemoveIf(Func<T, bool> predicate) => base.RemoveIf(v => v.IsNotNull() && predicate(v));
        public new void Clear() => base.Clear();
    }

    public static class PrefabSafeSet {
        public static void OnValidate<T, TComponent>(TComponent component, Func<TComponent, PrefabSafeSet<T>> getPrefabSafeSet)
            where T : notnull
            where TComponent : Component
        {
            PrefabSafeUniqueCollection.PrefabSafeUniqueCollection.OnValidate(component, getPrefabSafeSet);
        }
    }
}
