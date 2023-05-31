using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;

namespace Anatawa12.AvatarOptimizer.PrefabSafeSet
{
    public abstract partial class EditorUtil<T>
    {
        private sealed class Root : EditorUtil<T>
        {
            private List<ElementImpl> _list;
            [NotNull] private readonly SerializedProperty _mainSet;
            public override int Count => _mainSet.arraySize;

            public Root(SerializedProperty property, Func<SerializedProperty, T> getValue,
                Action<SerializedProperty, T> setValue) : base(getValue, setValue)
            {
                _mainSet = property.FindPropertyRelative(Names.MainSet)
                           ?? throw new ArgumentException("mainSet not found", nameof(property));
            }

            public override IReadOnlyList<IElement<T>> Elements
            {
                get
                {
                    if (_list?.Count != _mainSet.arraySize)
                        _list = new ArrayPropertyEnumerable(_mainSet)
                            .Select((x, i) => new ElementImpl(this, x, i))
                            .ToList();
                    return _list;
                }
            }

            private void ReIndexAll()
            {
                for (var i = 0; i < _list.Count; i++)
                    _list[i].UpdateIndex(i);
            }

            public override int ElementsCount => _mainSet.arraySize;

            public override void Clear() => _mainSet.arraySize = 0;

            protected override IElement<T> NewSlotElement(T value) => new ElementImpl(this, value);

            public override bool HasPrefabOverride() => false;

            public override void HandleApplyRevertMenuItems(IElement<T> element, GenericMenu genericMenu)
            {
                // logic failure
            }

            private class ElementImpl : IElement<T>
            {
                public EditorUtil<T> Container => _container;
                public T Value { get; }
                public ElementStatus Status => Contains ? ElementStatus.Natural : ElementStatus.NewSlot;
                public bool Contains => _index >= 0;
                public SerializedProperty ModifierProp { get; private set; }

                private readonly Root _container;
                private int _index;

                public ElementImpl(Root container, SerializedProperty prop, int index)
                {
                    Value = container._getValue(prop);
                    _container = container;
                    _index = index;
                    ModifierProp = prop;
                }

                public ElementImpl(Root container, T value)
                {
                    Value = value;
                    _container = container;
                    _index = -1;
                    ModifierProp = null;
                }

                public void EnsureAdded() => Add();

                public void Add()
                {
                    if (Contains) return;
                    _index = _container._mainSet.arraySize;
                    _container._setValue(ModifierProp = AddArrayElement(_container._mainSet), Value);
                    _container._list.Add(this);
                    //_container.ReIndexAll(); // appending does not change index so no reindex is required
                }

                public void EnsureRemoved() => Remove();

                public void Remove()
                {
                    if (!Contains) return;
                    _container.RemoveArrayElementAt(_container._mainSet, _index);
                    _index = -1;
                    ModifierProp = null;
                    _container._list.Remove(this);
                    _container.ReIndexAll();
                }

                public void SetExistence(bool existence)
                {
                    if (existence) Add();
                    else Remove();
                }

                public void UpdateIndex(int index)
                {
                    _index = index;
                    ModifierProp = _container._mainSet.GetArrayElementAtIndex(index);
                }
            }
        }
    }
}
