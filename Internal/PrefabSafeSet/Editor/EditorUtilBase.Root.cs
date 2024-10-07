using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Anatawa12.AvatarOptimizer.PrefabSafeUniqueCollection
{
    public abstract partial class EditorUtil<TAdditionValue, TRemoveKey>
    {
        private sealed class Root : EditorUtil<TAdditionValue, TRemoveKey>
        {
            private List<ElementImpl>? _list;
            private readonly SerializedProperty _mainSet;
            public override int Count => _mainSet.arraySize;

            public Root(SerializedProperty property, IEditorUtilHelper<TAdditionValue, TRemoveKey> helper) :
                base(helper)
            {
                _mainSet = property.FindPropertyRelative(Names.MainSet)
                           ?? throw new ArgumentException("mainSet not found", nameof(property));
            }

            public override IReadOnlyList<IElement<TAdditionValue, TRemoveKey>> Elements
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

            // Caller's responsibility to ensure _list is not null
            private void ReIndexAll()
            {
                for (var i = 0; i < _list!.Count; i++)
                    _list[i].UpdateIndex(i);
            }

            public override int ElementsCount => _mainSet.arraySize;

            public override void Clear() => _mainSet.arraySize = 0;

            protected override IElement<TAdditionValue, TRemoveKey> NewSlotElement(TAdditionValue value) => new ElementImpl(this, value);

            public override bool HasPrefabOverride() => false;

            public override void HandleApplyRevertMenuItems(IElement<TAdditionValue, TRemoveKey> element, GenericMenu genericMenu)
            {
                // logic failure
            }

            private class ElementImpl : IElement<TAdditionValue, TRemoveKey>
            {
                public EditorUtil<TAdditionValue, TRemoveKey> Container => _container;
                public TAdditionValue Value { get; }
                public TRemoveKey RemoveKey => _container._helper.GetRemoveKey(Value);
                public ElementStatus Status => Contains ? ElementStatus.Natural : ElementStatus.NewSlot;
                public bool Contains => _index >= 0;
                public SerializedProperty? ModifierProp { get; private set; }

                private readonly Root _container;
                private int _index;

                public ElementImpl(Root container, SerializedProperty prop, int index)
                {
                    Value = container._helper.ReadAdditionValue(prop)!; // TODO: null check
                    _container = container;
                    _index = index;
                    ModifierProp = prop;
                }

                public ElementImpl(Root container, TAdditionValue value)
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
                    ModifierProp = AddArrayElement(_container._mainSet);
                    _container._helper.WriteAdditionValue(ModifierProp, Value);
                    // ElementImpl instance will not exist unless _list is not null
                    _container._list!.Add(this);
                    //_container.ReIndexAll(); // appending does not change index so no reindex is required
                }

                public void EnsureRemoved() => Remove();

                public void Remove()
                {
                    if (!Contains) return;
                    _container.RemoveArrayElementAt(_container._mainSet, _index);
                    _index = -1;
                    ModifierProp = null;
                    // ElementImpl instance will not exist unless _list is not null
                    _container._list!.Remove(this);
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
