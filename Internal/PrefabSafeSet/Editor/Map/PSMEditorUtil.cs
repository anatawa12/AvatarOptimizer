using System;
using System.Collections;
using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.PrefabSafeUniqueCollection;
using UnityEditor;

namespace Anatawa12.AvatarOptimizer.PrefabSafeMap
{
    internal static class Names
    {
        public const string Key = nameof(MapEntry<object, object>.key);
        public const string Value = nameof(MapEntry<object, object>.value);
    }

    /// <summary>
    /// Utility to edit PrefabSafeMap in CustomEditor with SerializedProperty
    /// </summary>
    public sealed class PSMEditorUtil<TKey, TValue>
        where TKey : notnull
        where TValue : notnull
    {
        readonly BaseEditorUtil<KeyValuePair<TKey, TValue>, TKey> _upstream;

        class EditorUtilHelper : IEditorUtilHelper<KeyValuePair<TKey, TValue>, TKey>
        {
            private readonly Func<SerializedProperty, TKey> _getKey;
            private readonly Action<SerializedProperty, TKey> _setKey;
            private readonly Func<SerializedProperty, TValue> _getValue;
            private readonly Action<SerializedProperty, TValue> _setValue;

            public EditorUtilHelper(Func<SerializedProperty, TKey> getKey, Action<SerializedProperty, TKey> setKey,
                Func<SerializedProperty, TValue> getValue, Action<SerializedProperty, TValue> setValue)
            {
                _getKey = getKey;
                _setKey = setKey;
                _getValue = getValue;
                _setValue = setValue;
            }

            public KeyValuePair<TKey, TValue> ReadAdditionValue(SerializedProperty property) => new(
                _getKey(property.FindPropertyRelative(Names.Key)),
                _getValue(property.FindPropertyRelative(Names.Value)));

            public void WriteAdditionValue(SerializedProperty property, KeyValuePair<TKey, TValue> value)
            {
                _setKey(property.FindPropertyRelative(Names.Key), value.Key);
                _setValue(property.FindPropertyRelative(Names.Value), value.Value);
            }

            public TKey? ReadRemoveKey(SerializedProperty property) => _getKey(property);

            public void WriteRemoveKey(SerializedProperty property, TKey value) => _setKey(property, value);

            public TKey GetRemoveKey(KeyValuePair<TKey, TValue> value) => value.Key;
        }

#pragma warning disable CA1000 // Do not declare static members on generic types
        public static PSMEditorUtil<TKey, TValue> Create(SerializedProperty property,
            Func<SerializedProperty, TKey> getKey, Action<SerializedProperty, TKey> setKey,
            Func<SerializedProperty, TValue> getValue, Action<SerializedProperty, TValue> setValue) => new(
            BaseEditorUtil<KeyValuePair<TKey, TValue>, TKey>.Create(property,
                new EditorUtilHelper(getKey, setKey, getValue, setValue)));
#pragma warning restore CA1000

        private PSMEditorUtil(BaseEditorUtil<KeyValuePair<TKey, TValue>, TKey> upstream) => _upstream = upstream;

        public int ElementsCount => _upstream.ElementsCount;
        public int Count => _upstream.Count;
        public IEnumerable<KeyValuePair<TKey, TValue>> Entries => _upstream.Values;
        public void Clear() => _upstream.Clear();
        public bool HasPrefabOverride() => _upstream.HasPrefabOverride();
        public IReadOnlyList<IElement<TKey, TValue>> Elements => new ElementsWrapper(_upstream.Elements, this);

        public  IElement<TKey, TValue> Add(TKey key, TValue value) =>
            new ElementWrapper(_upstream.Add(new KeyValuePair<TKey, TValue>(key, value)), this);

        public IElement<TKey, TValue>? GetElementOf(TKey key) =>
            _upstream.GetElementOf(key) is { } element ? new ElementWrapper(element, this) : null;

        public void HandleApplyRevertMenuItems(IElement<TKey, TValue> element, GenericMenu genericMenu)
        {
            _upstream.HandleApplyRevertMenuItems(((ElementWrapper)element).Upstream, genericMenu);
        }

        private static ElementStatus MapStatus(PrefabSafeUniqueCollection.ElementStatus status) =>
            status switch
            {
                PrefabSafeUniqueCollection.ElementStatus.Natural => ElementStatus.Natural,
                PrefabSafeUniqueCollection.ElementStatus.Removed => ElementStatus.Removed,
                PrefabSafeUniqueCollection.ElementStatus.NewElement => ElementStatus.NewElement,
                PrefabSafeUniqueCollection.ElementStatus.Overriden => ElementStatus.Overriden,
                PrefabSafeUniqueCollection.ElementStatus.FakeRemoved => ElementStatus.FakeRemoved,
                PrefabSafeUniqueCollection.ElementStatus.Invalid => ElementStatus.Invalid,
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
            };

        private class ElementsWrapper : IReadOnlyList<IElement<TKey, TValue>>
        {
            private readonly PSMEditorUtil<TKey, TValue> _container;
            private IReadOnlyList<IBaseElement<KeyValuePair<TKey, TValue>, TKey>> _upstream;

            public ElementsWrapper(IReadOnlyList<IBaseElement<KeyValuePair<TKey, TValue>, TKey>> upstream,
                PSMEditorUtil<TKey, TValue> container)
            {
                _upstream = upstream;
                _container = container;
            }

            public int Count => _upstream.Count;

            public IElement<TKey, TValue> this[int index] => new ElementWrapper(_upstream[index], _container);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public IEnumerator<IElement<TKey, TValue>> GetEnumerator() =>
                new Enumerator(_upstream.GetEnumerator(), _container);

            private class Enumerator : IEnumerator<IElement<TKey, TValue>>
            {
                private IEnumerator<IBaseElement<KeyValuePair<TKey, TValue>, TKey>> _upstream;
                private readonly PSMEditorUtil<TKey, TValue> _container;

                public Enumerator(IEnumerator<IBaseElement<KeyValuePair<TKey, TValue>, TKey>> upstream,
                    PSMEditorUtil<TKey, TValue> container)
                {
                    _upstream = upstream;
                    _container = container;
                }

                public IElement<TKey, TValue> Current => new ElementWrapper(_upstream.Current, _container);
                object IEnumerator.Current => Current;

                public void Dispose() => _upstream.Dispose();
                public bool MoveNext() => _upstream.MoveNext();
                public void Reset() => _upstream.Reset();
            }
        }

        private class ElementWrapper : IElement<TKey, TValue>
        {
            internal IBaseElement<KeyValuePair<TKey, TValue>, TKey> Upstream;
            private readonly PSMEditorUtil<TKey, TValue> _container;

            public ElementWrapper(IBaseElement<KeyValuePair<TKey, TValue>, TKey> upstream,
                PSMEditorUtil<TKey, TValue> container)
            {
                _container = container;
                Upstream = upstream;
            }

            public TKey Key => Upstream.RemoveKey;
            public TValue? Value => Upstream.Value.Value;

            public PSMEditorUtil<TKey, TValue> Container => _container;
            public ElementStatus Status => MapStatus(Upstream.Status);
            public bool Contains => Upstream.Contains;
            public SerializedProperty? ModifierProp => Upstream.ModifierProp;

            public void Set(TValue value) =>
                Upstream = _container._upstream.Set(new KeyValuePair<TKey, TValue>(Key, value));

            public void Add(TValue value) =>
                Upstream = _container._upstream.Add(new KeyValuePair<TKey, TValue>(Key, value));

            public void EnsureRemoved() => Upstream.EnsureRemoved();
            public void Remove() => Upstream.Remove();
            public override string ToString() => $"Wrapper({Upstream})";
        }
    }

    public interface IElement<TKey, TValue>
        where TKey : notnull
        where TValue : notnull
    {
        PSMEditorUtil<TKey, TValue> Container { get; }
        TKey Key { get; }
        TValue? Value { get; }
        ElementStatus Status { get; }
        bool Contains { get; }
        SerializedProperty? ModifierProp { get; }
        void Set(TValue value);
        void Add(TValue value);
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
