using System;
using System.Collections;
using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.PrefabSafeUniqueCollection;
using UnityEditor;

namespace Anatawa12.AvatarOptimizer.PrefabSafeSet
{
    /// <summary>
    /// Utility to edit PrefabSafeSet in CustomEditor with SerializedProperty
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class PSSEditorUtil<T> where T : notnull
    {
        BaseEditorUtil<T, T> _upstream;

        class EditorUtilHelper : IEditorUtilHelper<T, T>
        {
            private readonly Func<SerializedProperty, T> _getValue;
            private readonly Action<SerializedProperty, T> _setValue;

            public EditorUtilHelper(Func<SerializedProperty, T> getValue, Action<SerializedProperty, T> setValue)
            {
                _getValue = getValue;
                _setValue = setValue;
            }

            public T? ReadAdditionValue(SerializedProperty property) => _getValue(property);
            public void WriteAdditionValue(SerializedProperty property, T value) => _setValue(property, value);
            public T? ReadRemoveKey(SerializedProperty property) => _getValue(property);
            public void WriteRemoveKey(SerializedProperty property, T value) => _setValue(property, value);
            public T GetRemoveKey(T value) => value;
        }

#pragma warning disable CA1000 // Do not declare static members on generic types
        public static PSSEditorUtil<T> Create(SerializedProperty property,
            Func<SerializedProperty, T> getValue, Action<SerializedProperty, T> setValue) =>
            new(BaseEditorUtil<T, T>.Create(property, new EditorUtilHelper(getValue, setValue)));
#pragma warning restore CA1000

        private PSSEditorUtil(BaseEditorUtil<T, T> upstream)
        {
            _upstream = upstream;
        }

        public int ElementsCount => _upstream.ElementsCount;
        public int Count => _upstream.Count;
        public IEnumerable<T> Values => _upstream.Values;
        public void Clear() => _upstream.Clear();
        public bool HasPrefabOverride() => _upstream.HasPrefabOverride();

        public IReadOnlyList<IElement<T>> Elements => new ElementsWrapper(_upstream.Elements, this);

        public IElement<T> GetElementOf(T value)
        {
            var element = _upstream.GetElementOf(value);
            return element == null ? new ElementWrapper(value, this) : new ElementWrapper(element, this);
        }

        public void HandleApplyRevertMenuItems(IElement<T> element, GenericMenu genericMenu)
        {
            var upstreamEleement = ((ElementWrapper)element).Upstream;
            if (upstreamEleement == null) return;
            _upstream.HandleApplyRevertMenuItems(upstreamEleement, genericMenu);
        }

        private static ElementStatus MapStatus(PrefabSafeUniqueCollection.ElementStatus status) =>
            status switch
            {
                PrefabSafeUniqueCollection.ElementStatus.Natural => ElementStatus.Natural,
                PrefabSafeUniqueCollection.ElementStatus.Removed => ElementStatus.Removed,
                PrefabSafeUniqueCollection.ElementStatus.NewElement => ElementStatus.NewElement,
                PrefabSafeUniqueCollection.ElementStatus.Overriden => ElementStatus.AddedTwice,
                PrefabSafeUniqueCollection.ElementStatus.FakeRemoved => ElementStatus.FakeRemoved,
                PrefabSafeUniqueCollection.ElementStatus.Invalid => ElementStatus.NewSlot,
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
            };

        private class ElementsWrapper : IReadOnlyList<IElement<T>>
        {
            private readonly PSSEditorUtil<T> _container;
            private IReadOnlyList<IBaseElement<T, T>> _upstream;

            public ElementsWrapper(IReadOnlyList<IBaseElement<T, T>> upstream, PSSEditorUtil<T> container)
            {
                _upstream = upstream;
                _container = container;
            }

            public int Count => _upstream.Count;

            public IElement<T> this[int index] => new ElementWrapper(_upstream[index], _container);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public IEnumerator<IElement<T>> GetEnumerator() => new Enumerator(_upstream.GetEnumerator(), _container);

            private class Enumerator : IEnumerator<IElement<T>>
            {
                private IEnumerator<IBaseElement<T, T>> _upstream;
                private readonly PSSEditorUtil<T> _container;

                public Enumerator(IEnumerator<IBaseElement<T, T>> upstream, PSSEditorUtil<T> container)
                {
                    _upstream = upstream;
                    _container = container;
                }

                public IElement<T> Current => new ElementWrapper(_upstream.Current, _container);
                object IEnumerator.Current => Current;

                public void Dispose() => _upstream.Dispose();
                public bool MoveNext() => _upstream.MoveNext();
                public void Reset() => _upstream.Reset();
            }
        }

        private class ElementWrapper : IElement<T>
        {
            internal IBaseElement<T, T>? Upstream;
            private readonly PSSEditorUtil<T> _container;

            public ElementWrapper(IBaseElement<T, T> upstream, PSSEditorUtil<T> container)
            {
                _container = container;
                Upstream = upstream;
                Value = upstream.RemoveKey;
            }

            public ElementWrapper(T value, PSSEditorUtil<T> container)
            {
                Value = value;
                _container = container;
            }

            public T Value { get; }
            public PSSEditorUtil<T> Container => _container;
            public ElementStatus Status => Upstream == null ? ElementStatus.NewSlot : MapStatus(Upstream.Status);
            public bool Contains => Upstream?.Contains ?? false;
            public SerializedProperty? ModifierProp => Upstream?.ModifierProp;

            public void EnsureAdded() => Upstream = _container._upstream.Set(Value);
            public void Add() => Upstream = _container._upstream.Add(Value);
            public void EnsureRemoved() => Upstream?.EnsureRemoved();

            public void Remove()
            {
                if (Upstream == null) Upstream = _container._upstream.Remove(Value);
                else Upstream.Remove();
            }

            public void SetExistence(bool existence)
            {
                if (existence)
                    Add();
                else
                    Remove();
            }

            public override string ToString() => $"Wrapper({Upstream?.ToString() ?? "<none>"})";
        }
    }

    public interface IElement<T> where T : notnull
    {
        PSSEditorUtil<T> Container { get; }
        T Value { get; }
        ElementStatus Status { get; }
        bool Contains { get; }
        SerializedProperty? ModifierProp { get; }
        void EnsureAdded();
        void Add();
        void EnsureRemoved();
        void Remove();
        void SetExistence(bool existence);
    }
    
    public enum ElementStatus
    {
        Natural,
        Removed,
        NewElement,
        AddedTwice,
        FakeRemoved,
        NewSlot,
    }
}
