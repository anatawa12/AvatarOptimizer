using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Animations;
using Object = UnityEngine.Object;

// We allow Collection even if it's not IEnumerable
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix

namespace Anatawa12.AvatarOptimizer.PrefabSafeUniqueCollection
{
    // PSUC: PrefabSafeUniqueCollection
    internal static class PSUCRuntimeUtil
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
                // isAsset |= currentPrefabStage.IsPartOfPrefabContents(instanceGameObject);
                // but ^^ will cause InvalidOperationException.
                // This is because `OnValidate` invocation is from `PrefabStageUtility:LoadPrefabIntoPreviewScene`
                // invocation in PrefabStage.LoadStage method, which assigns m_PrefabContentsRoot.
                // scene property is already available so we use it instead for detecting GameObjects in the PrefabStage. 
                isAsset |= instanceGameObject?.scene == currentPrefabStage.scene;
            }

            return isInstance && !isAsset;
        }
#endif

        public static IEnumerable<T> NonNulls<T>(this IEnumerable<T?> enumerable)
            where T : notnull => enumerable.Where(item => item.IsNotNull())!;

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

#if UNITY_EDITOR
        private static readonly Type OnBeforeSerializeImplType;

        static PSUCRuntimeUtil()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                OnBeforeSerializeImplType =
                    assembly.GetType(
                        "Anatawa12.AvatarOptimizer.PrefabSafeUniqueCollection.PrefabSafeUniqueCollectionRuntimeEditorImpl`3");
                if (OnBeforeSerializeImplType != null) return;
            }

            if (OnBeforeSerializeImplType == null)
                throw new InvalidOperationException("PrefabSafeUniqueCollectionRuntimeEditorImpl not found");
        }

        public static MethodInfo GetOnValidateCallbackMethod(Type tAdditionValueType, Type tRemoveKeyType,
            Type tManipulatorType, Type tComponentType)
        {
            var implType =
                OnBeforeSerializeImplType.MakeGenericType(tAdditionValueType, tRemoveKeyType, tManipulatorType);
            return implType.GetMethod("OnValidate", BindingFlags.Public | BindingFlags.Static)!
                .MakeGenericMethod(tComponentType);
        }

        // Avoid buffer overflow on exit
        public static void SetIsNewFalse<TAdditionValue, TRemoveKey, TManipulator>(
            this PrefabSafeUniqueCollection<TAdditionValue, TRemoveKey, TManipulator> collection)
            where TAdditionValue : notnull
            where TRemoveKey : notnull
            where TManipulator : struct, IManipulator<TAdditionValue, TRemoveKey>
        {
            collection.IsNew = false;
        }
#endif
    }

    public interface IManipulator<TAdditionValue, TRemoveKey>
    {
        TRemoveKey? GetKey(TAdditionValue? value);
        ref TRemoveKey? GetKey(ref TAdditionValue? value);
    }
    
    public struct IdentityManipulator<T> : IManipulator<T, T>
    {
        public ref T? GetKey(ref T? value) => ref value;
        public T? GetKey(T? value) => value;
    }

    /// <summary>
    /// The serializable class to express hashset.
    /// using array will make prefab modifications too big so I made this class
    /// </summary>
    /// <typeparam name="TAdditionValue">The value used to represent addition. this value must also hod remove key internally</typeparam>
    /// <typeparam name="TRemoveKey">The value used to represent removal.</typeparam>
    /// <typeparam name="TManipulator">The manipulator to get key from value</typeparam>
    [NotKeyable, Serializable]
    public abstract class PrefabSafeUniqueCollection<
        TAdditionValue,
        TRemoveKey,
        TManipulator
    >
        where TAdditionValue : notnull
        where TRemoveKey : notnull
        where TManipulator : struct, IManipulator<TAdditionValue, TRemoveKey>
    {
        [SerializeField] internal TAdditionValue?[] mainSet = Array.Empty<TAdditionValue?>();

        [SerializeField] internal PrefabLayer<TAdditionValue, TRemoveKey>[] prefabLayers =
            Array.Empty<PrefabLayer<TAdditionValue, TRemoveKey>>();

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
            // This would cause buffer overflow on [Performance] logging on exiting Unity.
            // This would make unity crash on exit so I replaced with SetIsNewFalse extension method on PSUCRuntimeUtil
            // This buffer overflow is caused by the very long type name as a result of the generic type name expansion.
            // The log does not include the generic parameters of the function, so we can shorten by moving 
            // the generic parameters from type generic to function generic.
            // In this case, I created a extension method to set IsNew to false and used it as a delayCall callback.
            //
            // Please refer Performance::Tracker::Report (crash location) in Unity source code / assembly.
            // You would see sprintf (not snprintf) is used to format the log message with fixed buffer size.
            // TODO: report to Unity
            //  UnityEditor.EditorApplication.delayCall += () => IsNew = false;
            UnityEditor.EditorApplication.delayCall += this.SetIsNewFalse;
#endif
        }

        private protected ListMap<TAdditionValue, TRemoveKey, TManipulator> GetCollection()
        {
            var baseMap = new ListMap<TAdditionValue, TRemoveKey, TManipulator>(mainSet, default);
            foreach (var layer in prefabLayers)
                layer.ApplyTo(baseMap);
            if (usingOnSceneLayer) onSceneLayer.ApplyTo(baseMap);
            return baseMap;
        }

#if UNITY_EDITOR
        private (ListMap<TAdditionValue, TRemoveKey, TManipulator>, PrefabLayer<TAdditionValue, TRemoveKey>)
            GetBaseAndLayer(int nestCount, bool useOnSceneLayer)
        {
            if (useOnSceneLayer)
            {
                if (!usingOnSceneLayer)
                    usingOnSceneLayer = true;
                var baseMap = new ListMap<TAdditionValue, TRemoveKey, TManipulator>(mainSet, default);
                return (baseMap, onSceneLayer);
            }
            else
            {
                if (prefabLayers.Length < nestCount)
                    PSUCRuntimeUtil.ResizeArray(ref prefabLayers, nestCount);

                var baseMap = new ListMap<TAdditionValue, TRemoveKey, TManipulator>(mainSet, default);

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

        private protected bool AddRange(IEnumerable<TAdditionValue> values)
        {
            var valueEnumerable = values.Where(x => x.IsNotNull());
            var nestCount = PrefabNestCount(OuterObject);
            var useOnSceneLayer = PSUCRuntimeUtil.ShouldUsePrefabOnSceneLayer(OuterObject);

            if (nestCount == 0)
            {
                var originalSize = mainSet.Length;
                var list = new ListMap<TAdditionValue, TRemoveKey, TManipulator>(mainSet, default);
                list.AddRange(valueEnumerable);
                mainSet = list.ToArray();
                return originalSize != mainSet.Length;
            }
            else
            {
                var (baseSet, layer) = GetBaseAndLayer(nestCount, useOnSceneLayer);
                var valuesList = new List<TAdditionValue>(valueEnumerable);
                var addingKeys = valuesList.Select(x => default(TManipulator).GetKey(x))
                    .NonNulls()
                    .ToHashSet();

                var originalRemoves = layer.removes.Length;
                var originalAdditions = layer.additions.Length;

                layer.removes = layer.removes.Where(x => x.IsNotNull() && !addingKeys.Contains(x)).ToArray();

                var additions = new ListMap<TAdditionValue, TRemoveKey, TManipulator>(layer.additions, default);
                additions.AddRange(valuesList.Where(x => !baseSet.ContainsKey(x)));
                layer.additions = additions.ToArray();

                return originalRemoves != layer.removes.Length || originalAdditions != layer.additions.Length;
            }
        }

        private protected bool RemoveRange(IEnumerable<TRemoveKey> values)
        {
            var valueEnumerable = values.Where(x => x.IsNotNull());
            var nestCount = PrefabNestCount(OuterObject);
            var useOnSceneLayer = PSUCRuntimeUtil.ShouldUsePrefabOnSceneLayer(OuterObject);

            if (nestCount == 0)
            {
                var originalSize = mainSet.Length;
                var removeSet = new HashSet<TRemoveKey?>(valueEnumerable);
                mainSet = mainSet.Where(v => removeSet.Contains(default(TManipulator).GetKey(v))).ToArray();
                return originalSize != mainSet.Length;
            }
            else
            {
                var (baseSet, layer) = GetBaseAndLayer(nestCount, useOnSceneLayer);
                var valuesList = new List<TRemoveKey>(valueEnumerable);

                var originalRemoves = layer.removes.Length;
                var originalAdditions = layer.additions.Length;

                layer.removes = layer.removes.Union<TRemoveKey?>(valuesList.Where(x => baseSet.ContainsKey(x))).ToArray();
                layer.additions = layer.additions.Where(x =>
                {
                    var key = default(TManipulator).GetKey(x);
                    return key.IsNotNull() && !valuesList.Contains(key);
                }).ToArray();

                return originalRemoves != layer.removes.Length || originalAdditions != layer.additions.Length;
            }
        }

        private protected void RemoveIf(Func<TAdditionValue, bool> predicate)
        {
            var nestCount = PrefabNestCount(OuterObject);
            var useOnSceneLayer = PSUCRuntimeUtil.ShouldUsePrefabOnSceneLayer(OuterObject);

            if (nestCount == 0)
            {
                mainSet = mainSet.Where(x => x.IsNotNull() && !predicate(x)).ToArray();
            }
            else
            {
                var (baseSet, layer) = GetBaseAndLayer(nestCount, useOnSceneLayer);

                layer.removes = layer.removes.Concat(baseSet.Where(predicate).Select(x => default(TManipulator).GetKey(x))).NonNulls().ToArray();
                layer.additions = layer.additions.NonNulls().Where(x => !predicate(x)).ToArray();
            }
        }

        private protected void Clear()
        {
            var nestCount = PrefabNestCount(OuterObject);
            var useSceneLayer = PSUCRuntimeUtil.ShouldUsePrefabOnSceneLayer(OuterObject);

            if (nestCount == 0)
            {
                mainSet = Array.Empty<TAdditionValue>();
            }
            else
            {
                var (baseSet, layer) = GetBaseAndLayer(nestCount, useSceneLayer);

                layer.removes = layer.removes.Concat<TRemoveKey?>(baseSet.Select(k => default(TManipulator).GetKey(k))).NonNulls().ToArray();
                layer.additions = Array.Empty<TAdditionValue>();
            }
        }
#else
        private protected bool AddRange(IEnumerable<TAdditionValue> values) => throw new NotSupportedException();
        private protected bool RemoveRange(IEnumerable<TRemoveKey> values) => throw new NotSupportedException();
        private protected void RemoveIf(Func<TAdditionValue, bool> predicate) => throw new NotSupportedException();
        private protected void Clear() => throw new NotSupportedException();
#endif
    }

    public static class PrefabSafeUniqueCollection
    {
        public static void OnValidate<TAdditionValue, TRemoveKey, TManipulator, TComponent>(TComponent component,
            Func<TComponent, PrefabSafeUniqueCollection<TAdditionValue, TRemoveKey, TManipulator>>
                getPrefabSafeUniqueCollection) where TComponent : Component
            where TAdditionValue : notnull
            where TRemoveKey : notnull
            where TManipulator : struct, IManipulator<TAdditionValue, TRemoveKey>
        {
#if UNITY_EDITOR
            ValidateMethodHolder<TAdditionValue, TRemoveKey, TManipulator, TComponent>.OnValidateCallbackMethodGeneric
                .Invoke(null, new object[] { component, getPrefabSafeUniqueCollection });
#endif
        }
#if UNITY_EDITOR
        private static class ValidateMethodHolder<TAdditionValue, TRemoveKey, TManipulator, TComponent>
        {
            public static MethodInfo OnValidateCallbackMethodGeneric =
                PSUCRuntimeUtil.GetOnValidateCallbackMethod(typeof(TAdditionValue),
                    typeof(TRemoveKey), typeof(TManipulator), typeof(TComponent));
        }
#endif
    }

    [Serializable]
    public class PrefabLayer<TAdditionValue, TRemoveKey>
    {
        // if some value is in both removes and additions, the values should be added
        [SerializeField] internal TRemoveKey?[] removes = Array.Empty<TRemoveKey?>();
        [SerializeField] internal TAdditionValue?[] additions = Array.Empty<TAdditionValue?>();

        internal void ApplyTo<TManipulator>(ListMap<TAdditionValue, TRemoveKey, TManipulator> baseMap)
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
        private readonly TManipulator _manipulator;
        private readonly LinkedList<TAdditionValue> _list;
        private readonly Dictionary<TRemoveKey, LinkedListNode<TAdditionValue>> _index;

        public ListMap(TAdditionValue?[] initialize, TManipulator manipulator)
        {
            _list = new LinkedList<TAdditionValue>();
            _index = new Dictionary<TRemoveKey, LinkedListNode<TAdditionValue>>();
            _manipulator = manipulator;

            foreach (var value in initialize)
            {
                if (value.IsNull()) continue;
                var key = manipulator.GetKey(value);
                if (key.IsNull()) continue;
                _index.Add(key, _list.AddLast(value));
            }
        }

        public void AddRange(IEnumerable<TAdditionValue?> values)
        {
            foreach (var value in values) Add(value);
        }

        public void Add(TAdditionValue? value)
        {
            if (value.IsNull()) return;
            var key = _manipulator.GetKey(value);
            if (key.IsNull()) return;
            if (_index.TryGetValue(key, out var node))
                node.Value = value;
            else
                _index.Add(key, _list.AddLast(value));
        }

        public void RemoveRange(IEnumerable<TRemoveKey?> values)
        {
            foreach (var value in values) Remove(value);
        }

        public void Remove(TRemoveKey? remove)
        {
            if (remove.IsNotNull() && _index.Remove(remove, out var node))
                _list.Remove(node);
        }

        public bool Remove(TRemoveKey remove, [NotNullWhen(true)] out TAdditionValue? o)
        {
            if (remove.IsNotNull() && _index.Remove(remove, out var node))
            {
                o = node.Value!;
                _list.Remove(node);
                return true;
            }
            else
            {
                o = default;
                return false;
            }
        }

        public TAdditionValue[] ToArray() => _list.ToArray();

        public bool ContainsKey(TAdditionValue additionValue) =>
            ContainsKey(_manipulator.GetKey(additionValue));

        public bool ContainsKey(TRemoveKey? removeKey) => removeKey.IsNotNull() && _index.ContainsKey(removeKey);

        public LinkedList<TAdditionValue>.Enumerator GetEnumerator() => _list.GetEnumerator();
        IEnumerator<TAdditionValue> IEnumerable<TAdditionValue>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
