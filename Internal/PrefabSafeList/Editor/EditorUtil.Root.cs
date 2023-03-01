using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;

namespace Anatawa12.AvatarOptimizer.PrefabSafeList
{
    public abstract partial class EditorUtil
    {
        private sealed class Root : EditorUtil
        {
            private List<ElementImpl> _elements;
            [NotNull] private readonly SerializedProperty _firstLayer;
            public override int Count => _firstLayer.arraySize;

            public Root(SerializedProperty property)
            {
                _firstLayer = property.FindPropertyRelative(nameof(Names.firstLayer))
                           ?? throw new ArgumentException(nameof(Names.firstLayer) + "not found", nameof(property));
            }

            public override IEnumerable<IElement> Elements
            {
                get
                {
                    Initialize();
                    return _elements;
                }
            }

            private void Initialize()
            {
                if (_elements?.Count != _firstLayer.arraySize)
                    _elements = new ArrayPropertyEnumerable(_firstLayer)
                        .Select((x, i) => new ElementImpl(this, x, i))
                        .ToList();
            }

            public override int ElementsCount => _firstLayer.arraySize;

            public override void Clear() => _firstLayer.arraySize = 0;

            public override IElement AddElement()
            {
                Initialize();
                var addedElement = AddArrayElement(_firstLayer);
                var element = new ElementImpl(this, addedElement, _firstLayer.arraySize - 1);
                _elements.Add(element);
                return element;
            }

            private class ElementImpl : IElement
            {
                public EditorUtil Container => _container;
                public ElementStatus Status => Contains ? ElementStatus.NewElement : ElementStatus.Invalid;
                public SerializedProperty RemovedProperty => null;
                public bool Contains => _index >= 0;

                public SerializedProperty ValueProperty =>
                    _containerProp?.FindPropertyRelative(nameof(Names.Container.value));

                private SerializedProperty _containerProp;
                private readonly Root _container;
                private int _index;

                public ElementImpl(Root container, SerializedProperty prop, int index)
                {
                    _container = container;
                    _index = index;
                    _containerProp = prop;
                }

                public void Add()
                {
                    if (Contains) return;
                    throw new InvalidOperationException(ElementErrMsg.AddOnInvalid);
                }

                public void Remove()
                {
                    if (!Contains) return;
                    _container._firstLayer.DeleteArrayElementAtIndex(_index);
                    _index = -1;
                    _containerProp = null;
                    _container._elements.Remove(this);
                }
            }
        }
    }
}
