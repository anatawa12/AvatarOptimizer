using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Anatawa12.AvatarOptimizer.PrefabSafeUniqueCollection;
using UnityEngine;
using UnityEngine.Animations;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.PrefabSafeSet
{
    internal static class PrefabSafeSetRuntimeUtil
    {
#if UNITY_EDITOR
        public static bool ShouldUsePrefabOnSceneLayer(Object instance)
        {
            var isInstance = UnityEditor.PrefabUtility.IsPartOfPrefabInstance(instance);
            var isAsset = UnityEditor.PrefabUtility.IsPartOfPrefabAsset(instance);

            var currentPrefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (currentPrefabStage != null)
            {
                var instanceGameObject = instance as GameObject ?? (instance as Component)?.gameObject;
                isAsset |= currentPrefabStage.IsPartOfPrefabContents(instanceGameObject);
            }

            return isInstance && !isAsset;
        }
#endif

        public static void ResizeArray<T>(ref T[] array, int size) where T : new()
        {
            var source = array;
            var result = new T[size];
            Array.Copy(source, result, Math.Min(size, source.Length));
            for (var i = source.Length; i < result.Length; i++)
                result[i] = new T();
            array = result;
        }

        internal static bool IsNull<T>([NotNullWhen(false)] this T arg)
        {
            if (arg == null) return true;
            if (typeof(Object).IsAssignableFrom(typeof(T)))
                return (Object)(object)arg == null;
            return false;
        }

        internal static bool IsNotNull<T>([NotNullWhen(true)] this T arg) => !arg.IsNull();
    }

    public interface IPrefabSafeSetApi<T>
    {
        public void SetValueNonPrefab(IEnumerable<T> values);
        public HashSet<T> GetAsSet();
        public List<T> GetAsList();
        public bool AddRange(IEnumerable<T> values);
        public bool RemoveRange(IEnumerable<T> values);
        public void RemoveIf(Func<T, bool> predicate);
        public void Clear();
    }

    public struct PrefabSafeSetManipulator<T> : IManipulator<T?, T>
    {
        public ref T? GetKey(ref T? value) => ref value;
        public T? GetKey(T? value) => value;
    }

    /// <summary>
    /// The serializable class to express hashset.
    /// using array will make prefab modifications too big so I made this class
    /// </summary>
    /// <typeparam name="T">Element Type</typeparam>
    [NotKeyable, Serializable]
    public class PrefabSafeSet<T> : PrefabSafeUniqueCollection<T?, T, PrefabSafeSetManipulator<T>>, IPrefabSafeSetApi<T>
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
            prefabLayers = Array.Empty<PrefabLayer<T?, T>>();
            mainSet = values.ToArray();
        }

        public HashSet<T> GetAsSet() => new(GetCollection());

        public List<T> GetAsList() => new(GetCollection());

        public new bool AddRange(IEnumerable<T> values) => base.AddRange(values);
        public new bool RemoveRange(IEnumerable<T> values) => base.RemoveRange(values);
        public new void RemoveIf(Func<T, bool> predicate) => base.RemoveIf(v => v != null && predicate(v));
        public new void Clear() => base.Clear();
    }

    public static class PrefabSafeSet {
        public static void OnValidate<T, TComponent>(TComponent component, Func<TComponent, PrefabSafeSet<T>> getPrefabSafeSet) where TComponent : Component
        {
            PrefabSafeUniqueCollection.PrefabSafeUniqueCollection.OnValidate(component, getPrefabSafeSet);
        }
    }

    internal readonly struct ListSet<T>
    {
        private readonly List<T> _list;
        private readonly HashSet<T> _set;
        public ListSet(T[] initialize)
        {
            _list = new List<T>(initialize);
            _set = new HashSet<T>(initialize);
        }

        public void AddRange(IEnumerable<T> values)
        {
            foreach (var value in values)
                if (value != null && _set.Add(value))
                    _list?.Add(value);
        }

        public void RemoveRange(IEnumerable<T> values)
        {
            foreach (var value in values)
                if (value != null && _set.Remove(value))
                    _list?.Remove(value);
        }

        public T[] ToArray() => _list.ToArray();
    }
}
