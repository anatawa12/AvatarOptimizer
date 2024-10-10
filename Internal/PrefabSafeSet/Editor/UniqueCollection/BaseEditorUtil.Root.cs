using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Anatawa12.AvatarOptimizer.PrefabSafeUniqueCollection
{
    public abstract partial class BaseEditorUtil<TAdditionValue, TRemoveKey>
    {
        private sealed class Root : BaseEditorUtil<TAdditionValue, TRemoveKey>
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

            public override IReadOnlyList<IBaseElement<TAdditionValue, TRemoveKey>> Elements => List;
            private List<ElementImpl> List
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

            public override bool HasPrefabOverride() => false;

            public override IBaseElement<TAdditionValue, TRemoveKey> Add(TAdditionValue value) => Set(value);

            public override IBaseElement<TAdditionValue, TRemoveKey> Set(TAdditionValue value)
            {
                var key = _helper.GetRemoveKey(value);

                var elementIndex = List.FindIndex(e => Equals(e.RemoveKey, key));

                if (elementIndex == -1)
                {
                    // new element
                    var index = _mainSet.arraySize;
                    var newElementProperty = AddArrayElement(_mainSet);
                    _helper.WriteAdditionValue(newElementProperty, value);
                    var element = new ElementImpl(this, newElementProperty, index);
                    List.Add(element);

                    return element;
                }
                else
                {
                    var element = List[elementIndex];
                    element.Value = value;
                    _helper.WriteAdditionValue(element.ModifierProp!, value);

                    return element;
                }
            }

            public override IBaseElement<TAdditionValue, TRemoveKey>? Remove(TRemoveKey key)
            {
                var element = GetElementOf(key);
                element?.EnsureRemoved();
                return element;
            }

            public override void HandleApplyRevertMenuItems(IBaseElement<TAdditionValue, TRemoveKey> element, GenericMenu genericMenu)
            {
                // logic failure
            }

            private class ElementImpl : IBaseElement<TAdditionValue, TRemoveKey>
            {
                public BaseEditorUtil<TAdditionValue, TRemoveKey> Container => _container;
                public TAdditionValue Value { get; internal set; }
                public TRemoveKey RemoveKey { get; }
                public ElementStatus Status => Contains ? ElementStatus.Natural : ElementStatus.Invalid;
                public bool Contains => ModifierProp != null;
                public SerializedProperty? ModifierProp { get; private set; }

                private readonly Root _container;
                private int _index;

                public ElementImpl(Root container, SerializedProperty prop, int index)
                {
                    Value = container._helper.ReadAdditionValue(prop)!; // TODO: null check
                    RemoveKey = container._helper.GetRemoveKey(Value);
                    _container = container;
                    _index = index;
                    ModifierProp = prop;
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

                public void UpdateIndex(int index)
                {
                    _index = index;
                    ModifierProp = _container._mainSet.GetArrayElementAtIndex(index);
                }

                public override string ToString() => $"Element(Root, {Value}, {Contains})";
            }
        }
    }
}
