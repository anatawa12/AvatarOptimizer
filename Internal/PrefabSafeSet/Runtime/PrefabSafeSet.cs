using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Animations;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.PrefabSafeSet
{
    internal static class PrefabSafeSetRuntimeUtil
    {
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

        internal static bool IsNotNull<T>(this T arg) => !arg.IsNull();

#if UNITY_EDITOR
        private static readonly Type OnBeforeSerializeImplType;

        static PrefabSafeSetRuntimeUtil()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                OnBeforeSerializeImplType =
                    assembly.GetType("Anatawa12.AvatarOptimizer.PrefabSafeSet.OnBeforeSerializeImpl`2");
                if (OnBeforeSerializeImplType != null) return;
            }
        }

        public static MethodInfo GetOnBeforeSerializeCallbackMethod(Type tType, Type tLayerType, Type setType)
        {
            var implType = OnBeforeSerializeImplType.MakeGenericType(tType, tLayerType);
            return implType.GetMethod("Impl", BindingFlags.Public | BindingFlags.Static, null, new[] { setType }, null);
        }
#endif
    }

    public abstract class PrefabSafeSetApi<T>
    {
        public abstract void SetValueNonPrefab(IEnumerable<T> values);
        public abstract HashSet<T> GetAsSet();
        public abstract List<T> GetAsList();
        public abstract bool AddRange(IEnumerable<T> values);
        public abstract bool RemoveRange(IEnumerable<T> values);
        public abstract void RemoveIf(Func<T, bool> predicate);
        public abstract void Clear();
    }

    /// <summary>
    /// The serializable class to express hashset.
    /// using array will make prefab modifications too big so I made this class
    /// </summary>
    /// <typeparam name="T">Element Type</typeparam>
    /// <typeparam name="TLayer">Layer Type</typeparam>
    [NotKeyable, Serializable]
    public class PrefabSafeSet<T, TLayer> : PrefabSafeSetApi<T>, ISerializationCallbackReceiver where TLayer : PrefabLayer<T>, new()
    {
        [SerializeField] internal T[] mainSet = Array.Empty<T>();
        [SerializeField] internal TLayer[] prefabLayers = Array.Empty<TLayer>();

#if UNITY_EDITOR
        [SerializeField, HideInInspector] internal T fakeSlot;
        internal readonly Object OuterObject;
        internal T[] CheckedCurrentLayerRemoves;
        internal T[] CheckedCurrentLayerAdditions;
        private static MethodInfo _onBeforeSerializeCallback = PrefabSafeSetRuntimeUtil
            .GetOnBeforeSerializeCallbackMethod(typeof(T), typeof(TLayer), typeof(PrefabSafeSet<T, TLayer>));
#endif

        protected PrefabSafeSet(Object outerObject)
        {
#if UNITY_EDITOR
            // I don't know why but Unity 2022 reports `this == null` in constructor of MonoBehaviour may be false
            // so use actual null check instead of destroy check
            // ReSharper disable once Unity.NoNullCoalescing
            OuterObject = outerObject ?? throw new ArgumentNullException(nameof(outerObject));
#endif
        }

        public override void SetValueNonPrefab(IEnumerable<T> values)
        {
#if UNITY_EDITOR
            if (OuterObject && UnityEditor.PrefabUtility.IsPartOfPrefabInstance(OuterObject)
                            && UnityEditor.PrefabUtility.IsPartOfAnyPrefab(OuterObject))
                throw new InvalidOperationException("You cannot set value to Prefab Instance or Prefab");
#endif
            // in some (rare) cases, unpacked prefab may have prefabLayers so we need to clear it. 
            prefabLayers = Array.Empty<TLayer>();
            mainSet = values.ToArray();
        }

        public override HashSet<T> GetAsSet()
        {
            var result = new HashSet<T>(mainSet.Where(x => x.IsNotNull()));
            foreach (var layer in prefabLayers)
                layer.ApplyTo(result);
            return result;
        }

        public override List<T> GetAsList()
        {
            var result = new List<T>(mainSet.Where(x => x.IsNotNull()));
            var set = new HashSet<T>(result);
            foreach (var layer in prefabLayers)
                layer.ApplyTo(set, result);
            return result;
        }

#if UNITY_EDITOR
        private (HashSet<T>, TLayer) GetBaseSetAndLayer(int nestCount)
        {
            if (prefabLayers.Length < nestCount)
                PrefabSafeSetRuntimeUtil.ResizeArray(ref prefabLayers, nestCount);
            var baseSet = new HashSet<T>(mainSet.Where(x => x.IsNotNull()));
            for (var i = 0; i < nestCount - 1; i++) prefabLayers[i].ApplyTo(baseSet);
            var layer = prefabLayers[nestCount - 1];

            return (baseSet, layer);
        }

        private static int PrefabNestCount(Object instance)
        {
            var nestCount = 0;
            while ((bool)(instance = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(instance)))
                nestCount++;

            return nestCount;
        }

        public override bool AddRange(IEnumerable<T> values)
        {
            var valueEnumerable = values.Where(x => x.IsNotNull());
            var nestCount = PrefabNestCount(OuterObject);

            if (nestCount == 0)
            {
                var originalSize = mainSet.Length;
                mainSet = mainSet.Concat(valueEnumerable.Except(mainSet)).ToArray();
                return originalSize != mainSet.Length;
            }
            else
            {
                var (baseSet, layer) = GetBaseSetAndLayer(nestCount);
                var valuesList = new List<T>(valueEnumerable);

                var originalRemoves = layer.removes.Length;
                var originalAdditions = layer.additions.Length;

                layer.removes = layer.removes.Except(valuesList).ToArray();
                layer.additions = layer.additions.Union(valuesList.Except(baseSet)).ToArray();

                return originalRemoves != layer.removes.Length || originalAdditions != layer.additions.Length;
            }
        }

        public override bool RemoveRange(IEnumerable<T> values)
        {
            var valueEnumerable = values.Where(x => x.IsNotNull());
            var nestCount = PrefabNestCount(OuterObject);

            if (nestCount == 0)
            {
                var originalSize = mainSet.Length;
                mainSet = mainSet.Except(valueEnumerable).ToArray();
                return originalSize != mainSet.Length;
            }
            else
            {
                var (baseSet, layer) = GetBaseSetAndLayer(nestCount);
                var valuesList = new List<T>(valueEnumerable);

                var originalRemoves = layer.removes.Length;
                var originalAdditions = layer.additions.Length;

                layer.removes = layer.removes.Union(valuesList.Intersect(baseSet)).ToArray();
                layer.additions = layer.additions.Except(valuesList).ToArray();
                
                return originalRemoves != layer.removes.Length || originalAdditions != layer.additions.Length;
            }
        }

        public override void RemoveIf(Func<T, bool> predicate)
        {
            var nestCount = PrefabNestCount(OuterObject);

            if (nestCount == 0)
            {
                mainSet = mainSet.Where(x => x.IsNull() || !predicate(x)).ToArray();
            }
            else
            {
                var (baseSet, layer) = GetBaseSetAndLayer(nestCount);

                layer.removes = layer.removes.Concat(baseSet.Where(predicate)).ToArray();
                layer.additions = layer.additions.Where(x => !predicate(x)).ToArray();
            }
        }

        public override void Clear()
        {
            var nestCount = PrefabNestCount(OuterObject);

            if (nestCount == 0)
            {
                mainSet = Array.Empty<T>();
            }
            else
            {
                var (baseSet, layer) = GetBaseSetAndLayer(nestCount);

                layer.removes = layer.removes.Concat(baseSet).ToArray();
                layer.additions = Array.Empty<T>();
            }
        }
#else
        public override void AddRange(IEnumerable<T> values) => throw new Exception("Not supported in Player build");
        public override void RemoveRange(IEnumerable<T> values) => throw new Exception("Not supported in Player build");
        public override void RemoveIf(Predicate<T> predicate) => throw new Exception("Not supported in Player build");
        public override void Clear() => throw new Exception("Not supported in Player build");
#endif

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
#if UNITY_EDITOR
            _onBeforeSerializeCallback.Invoke(null, new object[] {this});
#endif
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // there's nothing to do after deserialization.
        }
    }

    [Serializable]
    public class PrefabLayer<T>
    {
        // if some value is in both removes and additions, the values should be added
        [SerializeField] internal T[] removes = Array.Empty<T>();
        [SerializeField] internal T[] additions = Array.Empty<T>();

        public void ApplyTo(HashSet<T> result, [CanBeNull] List<T> list = null)
        {
            foreach (var remove in removes)
                if (remove.IsNotNull() && result.Remove(remove))
                    list?.Remove(remove);
            foreach (var addition in additions)
                if (addition.IsNotNull() && result.Add(addition))
                    list?.Add(addition);
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
