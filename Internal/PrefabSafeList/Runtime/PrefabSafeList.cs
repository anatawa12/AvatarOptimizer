using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.PrefabSafeList
{
    internal static class PrefabSafeListRuntimeUtil
    {
        public static T[] ResizeArray<T>(T[] source, int size) where T: new()
        {        
            var result = new T[size];
            Array.Copy(source, result, Math.Min(size, source.Length));
            for (var i = source.Length; i < result.Length; i++)
                result[i] = new T();
            return result;
        }

#if UNITY_EDITOR
        private static readonly Type OnBeforeSerializeImplType;

        static PrefabSafeListRuntimeUtil()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                OnBeforeSerializeImplType =
                    assembly.GetType("Anatawa12.AvatarOptimizer.PrefabSafeList.OnBeforeSerializeImpl`3");
                if (OnBeforeSerializeImplType != null) return;
            }
        }

        public static MethodInfo GetOnBeforeSerializeCallbackMethod(
            Type tType, Type tLayerType, Type tContainerType,
            Type setType)
        {
            var implType = OnBeforeSerializeImplType.MakeGenericType(tType, tLayerType, tContainerType);
            return implType.GetMethod("Impl", BindingFlags.Public | BindingFlags.Static, null, new[] { setType }, null);
        }
#endif
    }


    /// <summary>
    /// The serializable class to express list.
    /// using array will make prefab modifications too big so I made this class
    /// </summary>
    /// <typeparam name="T">Element Type</typeparam>
    /// <typeparam name="TLayer">Layer Type</typeparam>
    /// <typeparam name="TContainer">Container Type</typeparam>
    [Serializable]
    public class PrefabSafeList<T, TLayer, TContainer> : ISerializationCallbackReceiver
        where TLayer : PrefabLayer<T, TContainer>, new()
        where TContainer : ValueContainer<T>, new()
    {
        [SerializeField] internal TContainer[] firstLayer = Array.Empty<TContainer>();
        [SerializeField] internal TLayer[] prefabLayers = Array.Empty<TLayer>();

#if UNITY_EDITOR
        internal readonly Object OuterObject;
        private static MethodInfo _onBeforeSerializeCallback = PrefabSafeListRuntimeUtil
            .GetOnBeforeSerializeCallbackMethod(typeof(T), typeof(TLayer), typeof(TContainer), 
                typeof(PrefabSafeList<T, TLayer, TContainer>));
#endif

        protected PrefabSafeList(Object outerObject)
        {
#if UNITY_EDITOR
            if (!outerObject) throw new ArgumentNullException(nameof(outerObject));
            OuterObject = outerObject;
#endif
        }

        public List<T> GetAsList()
        {
            var result = new List<T>();
            foreach (var element in firstLayer)
                element.ApplyTo(result);
            foreach (var layer in prefabLayers)
                layer.ApplyTo(result);
            return result;
        }

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            _onBeforeSerializeCallback.Invoke(null, new object[] {this});
#endif
        }

        public void OnAfterDeserialize()
        {
            // there's nothing to do after deserialization.
        }
    }

    [Serializable]
    public class PrefabLayer<T, TContainer> where TContainer : ValueContainer<T>
    {
        // if some value is in both removes and additions, the values should be added
        [SerializeField] internal TContainer[] elements = Array.Empty<TContainer>();

        public void ApplyTo([NotNull] List<T> list)
        {
            foreach (var element in elements)
                element.ApplyTo(list);
        }
    }

    [Serializable]
    public class ValueContainer<T>
    {
        // if some value is in both removes and additions, the values should be added
        [SerializeField] internal T value;
        [SerializeField] internal bool removed;

        public void ApplyTo([NotNull] List<T> list)
        {
            if (!removed) list.Add(value);
        }
    }

    internal readonly struct ListSet<T>
    {
        [NotNull] private readonly List<T> _list;
        [NotNull] private readonly HashSet<T> _set;
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
