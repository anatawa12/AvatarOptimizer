using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;

namespace Anatawa12.AvatarOptimizer.PrefabSafeSet
{
    /// <summary>
    /// Utility to edit PrefabSafeSet in CustomEditor with SerializedProperty
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract partial class EditorUtil<T>
    {
        // common property;
        [NotNull] private readonly Func<SerializedProperty, T> _getValue;
        [NotNull] private readonly Action<SerializedProperty, T> _setValue;

        public abstract IReadOnlyList<IElement<T>> Elements { get; }
        public abstract int ElementsCount { get; }
        public virtual int Count => Elements.Count(x => x.Contains);
        public virtual IEnumerable<T> Values => Elements.Where(x => x.Contains).Select(x => x.Value);

        public static EditorUtil<T> Create(SerializedProperty property, int nestCount,
            Func<SerializedProperty, T> getValue,
            Action<SerializedProperty, T> setValue)
        {
            if (nestCount == 0)
                return new Root(property, getValue, setValue);
            return new PrefabModification(property, nestCount, getValue, setValue);
        }

        private EditorUtil(Func<SerializedProperty, T> getValue, Action<SerializedProperty, T> setValue)
        {
            _getValue = getValue ?? throw new ArgumentNullException(nameof(getValue));
            _setValue = setValue ?? throw new ArgumentNullException(nameof(setValue));
        }

        public abstract void Clear();

        protected abstract IElement<T> NewSlotElement(T value);

        public IElement<T> GetElementOf(T value) =>
            Elements.FirstOrDefault(x => x.Value.Equals(value)) ?? NewSlotElement(value);

        public abstract void HandleApplyRevertMenuItems(IElement<T> element, GenericMenu genericMenu);

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
                _setValue(prevProp, _getValue(curProp));;
                prevProp = curProp;
            }

            array.arraySize -= 1;
        }

        private T[] ToArray(SerializedProperty array)
        {
            var result = new T[array.arraySize];
            for (var i = 0; i < result.Length; i++)
                result[i] = _getValue(array.GetArrayElementAtIndex(i));
            return result;
        }
    }

    public interface IElement<T>
    {
        EditorUtil<T> Container { get; }
        T Value { get; }
        ElementStatus Status { get; }
        bool Contains { get; }
        SerializedProperty ModifierProp { get; }
        void EnsureAdded();
        void Add();
        void EnsureRemoved();
        void Remove();
        void SetExistence(bool existence);
    }
}
