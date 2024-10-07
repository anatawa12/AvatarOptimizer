using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Animations;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.PrefabSafeUniqueCollection
{
    internal static class PrefabSafeUniqueCollectionRuntimeUtil
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

        static PrefabSafeUniqueCollectionRuntimeUtil()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                OnBeforeSerializeImplType =
                    assembly.GetType("Anatawa12.AvatarOptimizer.PrefabSafeUniqueCollection.PrefabSafeUniqueCollectionRuntimeEditorImpl`3");
                if (OnBeforeSerializeImplType != null) return;
            }
            if (OnBeforeSerializeImplType == null)
                throw new InvalidOperationException("PrefabSafeUniqueCollectionRuntimeEditorImpl not found");
        }

        public static MethodInfo GetOnValidateCallbackMethod(Type tAdditionValueType, Type tRemoveKeyType, Type tManipulatorType, Type tComponentType)
        {
            var implType = OnBeforeSerializeImplType.MakeGenericType(tAdditionValueType, tRemoveKeyType, tManipulatorType);
            return implType.GetMethod("OnValidate", BindingFlags.Public | BindingFlags.Static)!
                .MakeGenericMethod(tComponentType);
        }
#endif
    }

    public interface IManipulator<TAdditionValue, TRemoveKey>
    {
        TRemoveKey? GetKey(TAdditionValue value);
        ref TRemoveKey? GetKey(ref TAdditionValue value);
    }

    /// <summary>
    /// The serializable class to express hashset.
    /// using array will make prefab modifications too big so I made this class
    /// </summary>
    /// <typeparam name="T">Element Type</typeparam>
    [NotKeyable, Serializable]
    public abstract class PrefabSafeUniqueCollection<
        TAdditionValue,
        TRemoveKey,
        TManipulator
    >
    where TManipulator : struct, IManipulator<TAdditionValue, TRemoveKey>
    {
        [SerializeField] internal TAdditionValue[] mainSet = Array.Empty<TAdditionValue>();
        [SerializeField] internal PrefabLayer<TAdditionValue, TRemoveKey>[] prefabLayers = Array.Empty<PrefabLayer<TAdditionValue, TRemoveKey>>();
        // If the PrefabSafeUniqueCollection is on scene and prefab instance, this will be used
        // This is added AAO 1.8.0 to support replacing base prefab on the scene, since Unity 2022
        [SerializeField] internal bool usingOnSceneLayer;
        [SerializeField] internal PrefabLayer<TAdditionValue, TRemoveKey> onSceneLayer = new();

#if UNITY_EDITOR
        internal Object OuterObject;
        internal Object? CorrespondingObject;
        internal int? NestCount;
        internal bool IsNew;
#endif

        protected PrefabSafeUniqueCollection(Object outerObject)
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

        private protected ListMap<TAdditionValue, TRemoveKey, TManipulator> GetCollection()
        {
            var baseMap = new ListMap<TAdditionValue, TRemoveKey, TManipulator>(mainSet);
            foreach (var layer in prefabLayers)
                layer.ApplyTo(baseMap);
            if (usingOnSceneLayer) onSceneLayer.ApplyTo(baseMap);
            return baseMap;
        }

#if UNITY_EDITOR
        private protected (ListMap<TAdditionValue, TRemoveKey, TManipulator>, PrefabLayer<TAdditionValue, TRemoveKey>) GetBaseAndLayer(int nestCount, bool useOnSceneLayer)
        {
            if (useOnSceneLayer)
            {
                if (!usingOnSceneLayer)
                    usingOnSceneLayer = true;
                var baseMap = new ListMap<TAdditionValue, TRemoveKey, TManipulator>(mainSet);
                return (baseMap, onSceneLayer);
            }
            else
            {
                if (prefabLayers.Length < nestCount)
                    PrefabSafeUniqueCollectionRuntimeUtil.ResizeArray(ref prefabLayers, nestCount);

                var baseMap = new ListMap<TAdditionValue, TRemoveKey, TManipulator>(mainSet);

                for (var i = 0; i < nestCount - 1; i++) prefabLayers[i].ApplyTo(baseMap);
                var layer = prefabLayers[nestCount - 1];

                return (baseMap, layer);
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

    public static class PrefabSafeUniqueCollection {
        public static void OnValidate<TAdditionValue, TRemoveKey, TManipulator, TComponent>(TComponent component, Func<TComponent, PrefabSafeUniqueCollection<TAdditionValue, TRemoveKey, TManipulator>> getPrefabSafeUniqueCollection) where TComponent : Component
        where TManipulator : struct, IManipulator<TAdditionValue, TRemoveKey>
        {
#if UNITY_EDITOR
            ValidateMethodHolder<TAdditionValue, TRemoveKey, TManipulator, TComponent>.OnValidateCallbackMethodGeneric.Invoke(null,
                new object[] { component, getPrefabSafeUniqueCollection });
#endif
        }
#if UNITY_EDITOR
        private static class ValidateMethodHolder<TAdditionValue, TRemoveKey, TManipulator, TComponent>
        {
            public static MethodInfo OnValidateCallbackMethodGeneric =
                PrefabSafeUniqueCollectionRuntimeUtil.GetOnValidateCallbackMethod(typeof(TAdditionValue), typeof(TRemoveKey), typeof(TManipulator), typeof(TComponent));
        }
#endif
    }

    [Serializable]
    public class PrefabLayer<TAdditionValue, TRemoveKey>
    {
        // if some value is in both removes and additions, the values should be added
        [SerializeField] internal TRemoveKey[] removes = Array.Empty<TRemoveKey>();
        [SerializeField] internal TAdditionValue[] additions = Array.Empty<TAdditionValue>();

        internal void ApplyTo<TManipulator>(ListMap<TAdditionValue,TRemoveKey,TManipulator> baseMap)
            where TManipulator : struct, IManipulator<TAdditionValue, TRemoveKey>
        {
            foreach (var remove in removes)
                baseMap.Remove(remove);
            foreach (var addition in additions)
                baseMap.Add(addition);
        }
    }

    internal readonly struct ListMap<TAdditionValue, TRemoveKey, TManipulator> : IEnumerable<TAdditionValue>
        where TManipulator : struct, IManipulator<TAdditionValue, TRemoveKey>
    {
        private readonly LinkedList<TAdditionValue> _list;
        private readonly Dictionary<TRemoveKey, LinkedListNode<TAdditionValue>> _index;
        public ListMap(TAdditionValue[] initialize)
        {
            _list = new LinkedList<TAdditionValue>();
            _index = new Dictionary<TRemoveKey, LinkedListNode<TAdditionValue>>();

            var manipulator = default(TManipulator);
            foreach (var value in initialize)
            {
                var key = manipulator.GetKey(value);
                if (key == null) continue;
                _index.Add(key, _list.AddLast(value));
            }
        }

        public void AddRange(IEnumerable<TAdditionValue> values)
        {
            foreach (var value in values) Add(value);
        }

        public void Add(TAdditionValue value)
        {
            var key = default(TManipulator).GetKey(value);
            if (key == null) return;
            if (_index.TryGetValue(key, out var node))
                node.Value = value;
            else
                _index.Add(key, _list.AddLast(value));
        }

        public void RemoveRange(IEnumerable<TRemoveKey> values)
        {
            foreach (var value in values) Remove(value);
        }
        
        public void Remove(TRemoveKey remove)
        {
            if (remove != null && _index.Remove(remove, out var node))
                _list.Remove(node);
        }

        public TAdditionValue[] ToArray() => _list.ToArray();

        public LinkedList<TAdditionValue>.Enumerator GetEnumerator() => _list.GetEnumerator();
        IEnumerator<TAdditionValue> IEnumerable<TAdditionValue>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
