using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Animations;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.PrefabSafeMap
{
    internal static class PrefabSafeMapRuntimeUtil
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

        internal static bool IsNull<T>(this T arg)
        {
            if (arg == null) return true;
            if (typeof(Object).IsAssignableFrom(typeof(T)))
                return (Object)(object)arg == null;
            return false;
        }

        internal static bool IsNotNull<T>([NotNullWhen(true)] this T arg) => !arg.IsNull();

#if UNITY_EDITOR
        private static readonly Type OnBeforeSerializeImplType;

        static PrefabSafeMapRuntimeUtil()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                OnBeforeSerializeImplType =
                    assembly.GetType("Anatawa12.AvatarOptimizer.PrefabSafeMap.PrefabSafeMapRuntimeEditorImpl`2");
                if (OnBeforeSerializeImplType != null) return;
            }
            if (OnBeforeSerializeImplType == null)
                throw new InvalidOperationException("OnBeforeSerializeImpl`2 not found");
        }

        public static MethodInfo GetOnValidateCallbackMethod(Type tKeyType, Type tValueType, Type tComponentType)
        {
            var implType = OnBeforeSerializeImplType.MakeGenericType(tKeyType, tValueType);
            return implType.GetMethod("OnValidate", BindingFlags.Public | BindingFlags.Static)!
                .MakeGenericMethod(tComponentType);
        }
#endif
    }

    /// <summary>
    /// The serializable class to express dictionary.
    /// using array will make prefab modifications too big so I made this class
    /// </summary>
    /// <typeparam name="TKey">Key Type</typeparam>
    /// <typeparam name="TValue">Value Type</typeparam>
    [NotKeyable, Serializable]
    public class PrefabSafeMap<TKey, TValue>
    {
        [SerializeField] internal MapEntry<TKey, TValue>[] mainSet = Array.Empty<MapEntry<TKey, TValue>>();
        [SerializeField] internal PrefabLayer<TKey, TValue>[] prefabLayers = Array.Empty<PrefabLayer<TKey, TValue>>();
        // If the PrefabSafeMap is on scene and prefab instance, this will be used
        // This is added AAO 1.8.0 to support replacing base prefab on the scene, since Unity 2022
        [SerializeField] internal bool usingOnSceneLayer;
        [SerializeField] internal PrefabLayer<TKey, TValue> onSceneLayer = new();

#if UNITY_EDITOR
        [SerializeField, HideInInspector] internal TKey? fakeSlot;
        internal Object OuterObject;
        internal Object? CorrespondingObject;
        internal int? NestCount;
        internal bool IsNew;
#endif

        public PrefabSafeMap(Object outerObject)
        {
#if UNITY_EDITOR
            // I don't know why but Unity 2022 reports `this == null` in constructor of MonoBehaviour may be false
            // so use actual null check instead of destroy check
            // ReSharper disable once Unity.NoNullCoalescing
            OuterObject = outerObject ?? throw new ArgumentNullException(nameof(outerObject));
            IsNew = true;
            UnityEditor.EditorApplication.delayCall += () => IsNew = false;
#endif
        }

        public Dictionary<TKey, TValue?> GetAsMap()
        {
            var result = new Dictionary<TKey, TValue?>();
            foreach (var layer in prefabLayers)
                layer.ApplyTo(result);
            onSceneLayer.ApplyTo(result);
            return result;
        }

#if UNITY_EDITOR
        private (Dictionary<TKey, TValue?>, PrefabLayer<TKey, TValue>) GetBaseMapAndLayer(int nestCount, bool useOnSceneLayer)
        {
            if (useOnSceneLayer)
            {
                if (!usingOnSceneLayer)
                    usingOnSceneLayer = true;
                var baseDict = new Dictionary<TKey, TValue?>();
                foreach (var entry in mainSet)
                    if (entry.key.IsNotNull())
                        baseDict[entry.key] = entry.value;

                return (baseDict, onSceneLayer);
            }
            else
            {
                if (prefabLayers.Length < nestCount)
                    PrefabSafeMapRuntimeUtil.ResizeArray(ref prefabLayers, nestCount);

                var baseDict = new Dictionary<TKey, TValue?>();
                foreach (var entry in mainSet)
                    if (entry.key.IsNotNull())
                        baseDict[entry.key] = entry.value;

                for (var i = 0; i < nestCount - 1; i++) prefabLayers[i].ApplyTo(baseDict);
                var layer = prefabLayers[nestCount - 1];

                return (baseDict, layer);
            }
        }

        private static int PrefabNestCount(Object instance)
        {
            var nestCount = 0;
            while ((bool)(instance = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(instance)))
                nestCount++;

            return nestCount;
        }
#else
#endif
    }

    [Serializable]
    internal struct MapEntry<TKey, TValue>
    {
        [SerializeField] internal TKey? key;
        [SerializeField] internal TValue? value;
    }

    public static class PrefabSafeMap {
        public static void OnValidate<TKey, TValue, TComponent>(TComponent component, Func<TComponent, PrefabSafeMap<TKey, TValue>> getPrefabSafeMap) where TComponent : Component
        {
#if UNITY_EDITOR
            ValidateMethodHolder<TKey, TValue, TComponent>.OnValidateCallbackMethodGeneric.Invoke(null,
                new object[] { component, getPrefabSafeMap });
#endif
        }
#if UNITY_EDITOR
        private static class ValidateMethodHolder<TKey, TValue, TComponent>
        {
            public static MethodInfo OnValidateCallbackMethodGeneric =
                PrefabSafeMapRuntimeUtil.GetOnValidateCallbackMethod(typeof(TKey), typeof(TValue), typeof(TComponent));
        }
#endif
    }

    [Serializable]
    public class PrefabLayer<TKey, TValue>
    {
        // if some value is in both removes and additions, the values should be added
        [SerializeField] internal TKey[] removes = Array.Empty<TKey>();
        [SerializeField] internal MapEntry<TKey, TValue>[] additions = Array.Empty<MapEntry<TKey, TValue>>();

        public void ApplyTo(Dictionary<TKey, TValue?> result)
        {
            foreach (var remove in removes)
                if (remove.IsNotNull())
                    result.Remove(remove);
            foreach (var addition in additions)
                if (addition.key.IsNotNull())
                    result[addition.key] = addition.value;
        }
    }
    
    internal readonly struct ListMap<TKey, TValue>
    {
        private readonly List<MapEntry<TKey, TValue>> _list;
        private readonly Dictionary<TKey, int> _index;
        public ListMap(MapEntry<TKey, TValue>[] initialize)
        {
            _list = new List<MapEntry<TKey, TValue>>(initialize);
            _index = initialize.Select((entry, index) => (entry.key, index))
                .Where(entry => entry.key != null)
                .ToDictionary(entry => entry.key!, entry => entry.index);
        }

        public void AddRange(IEnumerable<MapEntry<TKey, TValue>> values)
        {
            foreach (var value in values)
            {
                if (value.key == null) continue;
                if (_index.TryGetValue(value.key, out var index))
                    _list[index] = value;
                else
                {
                    _index.Add(value.key, _list.Count);
                    _list.Add(value);
                }
            }
        }

        public void RemoveRange(IEnumerable<TKey> values)
        {
            foreach (var value in values)
                if (value != null && _index.Remove(value, out var index))
                    _list?.RemoveAt(index);
        }

        public MapEntry<TKey, TValue>[] ToArray() => _list.ToArray();
    }
}
