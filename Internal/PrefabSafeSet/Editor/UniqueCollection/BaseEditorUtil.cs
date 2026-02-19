using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Anatawa12.AvatarOptimizer.PrefabSafeUniqueCollection
{
    /// <summary>
    ///  because there are many generic arguments, nameof is too long. so I use this class 
    /// </summary>
    internal static class Names
    {
        public const string MainSet = nameof(PrefabSafeUniqueCollection<object, object, IdentityManipulator<object>>.mainSet);
        public const string PrefabLayers = nameof(PrefabSafeUniqueCollection<object, object, IdentityManipulator<object>>.prefabLayers);
        public const string UsingOnSceneLayer = nameof(PrefabSafeUniqueCollection<object, object, IdentityManipulator<object>>.usingOnSceneLayer);
        public const string OnSceneLayer = nameof(PrefabSafeUniqueCollection<object, object, IdentityManipulator<object>>.onSceneLayer);
        public const string Additions = nameof(PrefabLayer<object, object>.additions);
        public const string Removes = nameof(PrefabLayer<object, object>.removes);
    }

    public interface IEditorUtilHelper<TAdditionValue, TRemoveKey>
        where TAdditionValue : notnull
        where TRemoveKey : notnull
    {
        TAdditionValue? ReadAdditionValue(SerializedProperty property);
        void WriteAdditionValue(SerializedProperty property, TAdditionValue value);

        TRemoveKey? ReadRemoveKey(SerializedProperty property);
        void WriteRemoveKey(SerializedProperty property, TRemoveKey value);
        TRemoveKey GetRemoveKey(TAdditionValue value);
    }

    /// <summary>
    /// Utility to edit PrefabSafeUniqueCollection in CustomEditor with SerializedProperty
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract partial class BaseEditorUtil<TAdditionValue, TRemoveKey>
        where TAdditionValue : notnull
        where TRemoveKey : notnull
    {
        private readonly IEditorUtilHelper<TAdditionValue, TRemoveKey> _helper;

        public abstract IReadOnlyList<IBaseElement<TAdditionValue, TRemoveKey>> Elements { get; }
        public abstract int ElementsCount { get; }
        public virtual int Count => Elements.Count(x => x.Contains);
        public virtual IEnumerable<TAdditionValue> Values => Elements.Where(x => x.Contains).Select(x => x.Value!);

#pragma warning disable CA1000
        public static BaseEditorUtil<TAdditionValue, TRemoveKey> Create(SerializedProperty property, IEditorUtilHelper<TAdditionValue, TRemoveKey> helper) 
            => new Wrapper(property, helper);
#pragma warning restore CA1000

        static BaseEditorUtil<TAdditionValue, TRemoveKey> CreateImpl(SerializedProperty property, int nestCount,
            IEditorUtilHelper<TAdditionValue, TRemoveKey> helper)
        {
            if (nestCount == 0)
                return new Root(property, helper);
            var useOnSceneLayer = PSUCUtil.ShouldUsePrefabOnSceneLayer(property.serializedObject.targetObject);
            if (useOnSceneLayer)
                return new PrefabModificationOnScene(property, nestCount, helper);
            return new PrefabModificationOnAsset(property, nestCount, helper);
        }

        private BaseEditorUtil(IEditorUtilHelper<TAdditionValue, TRemoveKey> helper)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
        }

        public abstract void Clear();

        public abstract bool HasPrefabOverride();

        public IBaseElement<TAdditionValue, TRemoveKey>? GetElementOf(TRemoveKey key) =>
            Elements.FirstOrDefault(x => x.RemoveKey.Equals(key));

        // do not create overrides if the value is already in the set / map
        public abstract IBaseElement<TAdditionValue, TRemoveKey> Set(TAdditionValue value);
        // tries to add modification to the element.
        public abstract IBaseElement<TAdditionValue, TRemoveKey> Add(TAdditionValue value);
        public abstract IBaseElement<TAdditionValue, TRemoveKey>? Remove(TRemoveKey key);

        public abstract void HandleApplyRevertMenuItems(IBaseElement<TAdditionValue, TRemoveKey> element, GenericMenu genericMenu);

        private static SerializedProperty AddArrayElement(SerializedProperty array)
        {
            array.arraySize += 1;
            return array.GetArrayElementAtIndex(array.arraySize - 1);
        }

        private void RemoveArrayElementAt(SerializedProperty array, int index)
        {
            var prevProp = array.GetArrayElementAtIndex(index);
            for (var i = index + 1; i < array.arraySize; i++)
            {
                var curProp = array.GetArrayElementAtIndex(i);
                prevProp.CopyDataFrom(curProp); // TODO? performance improvement
                prevProp = curProp;
            }

            array.arraySize -= 1;
        }

        private T[] ToArray<T>(SerializedProperty? array, Func<SerializedProperty, T> readValue)
        {
            if (array == null) return Array.Empty<T>();
            var result = new T[array.arraySize];
            for (var i = 0; i < result.Length; i++)
                result[i] = readValue(array.GetArrayElementAtIndex(i));
            return result;
        }

        private TAdditionValue?[] AdditionsToArray(SerializedProperty? array) => ToArray(array, _helper.ReadAdditionValue);
        private TRemoveKey?[] RemoveKeysToArray(SerializedProperty? array) => ToArray(array, _helper.ReadRemoveKey);

        private void SetArray<T>(SerializedProperty array, T[] values, Action<SerializedProperty, T> setValue)
        {
            array.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                setValue(array.GetArrayElementAtIndex(i), values[i]);
        }
    }

    public interface IBaseElement<TAdditionValue, TRemoveKey>
        where TAdditionValue : notnull
        where TRemoveKey : notnull
    {
        BaseEditorUtil<TAdditionValue, TRemoveKey> Container { get; }

        TAdditionValue? Value { get; } // can be null if fake removed
        TRemoveKey RemoveKey { get; }

        ElementStatus Status { get; }
        bool Contains { get; }
        SerializedProperty? ModifierProp { get; }
        void EnsureRemoved();
        void Remove();
    }
    
    public enum ElementStatus
    {
        Natural,
        Removed,
        NewElement,
        Overriden,
        FakeRemoved,
        Invalid,
    }
}
