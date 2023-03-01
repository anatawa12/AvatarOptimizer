using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.Assertions;

namespace Anatawa12.AvatarOptimizer.PrefabSafeList
{
    public abstract partial class EditorUtil
    {
        private struct ArraySizeCheck
        {
            public int Size { get; private set; }
            private readonly SerializedProperty _prop;

            public ArraySizeCheck(SerializedProperty prop)
            {
                _prop = prop;
                Size = 0;
            }

            public bool Changed => Size != _prop.intValue;

            public void Update() => Size = _prop.intValue;
        }

        private sealed class PrefabModification : EditorUtil
        {
            private readonly List<ElementImpl> _elements;
            private readonly SerializedProperty _rootProperty;

            private readonly SerializedProperty _firstLayerProp;
            private readonly SerializedProperty[] _layerElementsProps;

            // upstream change check
            private ArraySizeCheck _firstLayerCheck;
            private readonly ArraySizeCheck[] _layerChecks;

            public PrefabModification(SerializedProperty property, int nestCount)
            {
                _elements = new List<ElementImpl>();
                _rootProperty = property;

                ClearNonLayerModifications(property, nestCount);

                _firstLayerProp = property.FindPropertyRelative(nameof(Names.firstLayer));
                _firstLayerCheck = new ArraySizeCheck(_firstLayerProp.FindPropertyRelative("Array.size"));
                _layerChecks = new ArraySizeCheck[nestCount];
                _layerElementsProps = new SerializedProperty[nestCount];


                // apply modifications until previous one
                var prefabLayers = property.FindPropertyRelative(nameof(Names.prefabLayers));
                // process current layer
                if (prefabLayers.arraySize < nestCount) prefabLayers.arraySize = nestCount;

                for (var i = 0; i < prefabLayers.arraySize; i++)
                {
                    var elements = prefabLayers.GetArrayElementAtIndex(i)
                        .FindPropertyRelative(nameof(Names.Layer.elements));

                    _layerElementsProps[i] = elements;
                    _layerChecks[i] = new ArraySizeCheck(elements.FindPropertyRelative("Array.size"));
                }

                DoInitialize();
            }

            private void ClearNonLayerModifications(SerializedProperty property, int nestCount)
            {
                // TODO
            }

            public override IEnumerable<IElement> Elements
            {
                get
                {
                    Initialize();
                    return _elements;
                }
            }

            public override int ElementsCount
            {
                get
                {
                    Initialize();
                    return _elements.Count;
                }
            }

            /// <summary>
            /// initialize or update cache info if needed
            /// </summary>
            public void Initialize()
            {
                if (_firstLayerCheck.Changed || _layerChecks.Any(x => x.Changed))
                {
                    DoInitialize();
                }
            }

            /// <summary>
            /// initialize or update cache info
            /// </summary>
            public void DoInitialize()
            {
                var offset = 0;

                DoInitializeLayer(offset, _firstLayerCheck.Size, _firstLayerProp, false);
                offset += _firstLayerProp.arraySize;
                _firstLayerCheck.Update();

                for (var i = 0; i < _layerElementsProps.Length; i++)
                {
                    var layerElementsProp = _layerElementsProps[i];
                    DoInitializeLayer(offset, _layerChecks[i].Size, layerElementsProp,
                        i == _layerElementsProps.Length - 1);
                    offset += layerElementsProp.arraySize;
                    _layerChecks[i].Update();
                }
            }

            void DoInitializeLayer(int offset, int prevSize, SerializedProperty layerElements, bool original)
            {
#if UNITY_ASSERTIONS
                for (int i = 0, j = offset; i < prevSize && i < layerElements.arraySize && j < _elements.Count; i++, j++)
                {
                    Assert.AreEqual(_elements[j].ContainerProp, layerElements.GetArrayElementAtIndex(i));
                }
#endif
                if (prevSize < layerElements.arraySize)
                {
                    // prev size is too small: add more
                    var addingElements = Enumerable.Range(prevSize, layerElements.arraySize - prevSize)
                        .Select(index =>
                            new ElementImpl(this, layerElements.GetArrayElementAtIndex(index), index, original))
                        .ToArray();

                    _elements.InsertRange(offset, addingElements);
                }
                else if (prevSize > layerElements.arraySize)
                {
                    // too big: remove elements
                    _elements.RemoveRange(offset + layerElements.arraySize,
                        layerElements.arraySize - prevSize);
                }
            }

            public override IElement AddElement()
            {
                Initialize();
                var layerIndex = _layerElementsProps.Length - 1;
                var lastElementsProp = _layerElementsProps[layerIndex];
                var addedElement = AddArrayElement(lastElementsProp);
                var element = new ElementImpl(this, addedElement, lastElementsProp.arraySize - 1, true);
                _elements.Add(element);
                _layerChecks[layerIndex].Update();
                return element;
            }

            public override void Clear()
            {
                Initialize();
                for (var i = _elements.Count - 1; i >= 0; i--)
                    _elements[i].Remove();
            }

            private class ElementImpl : IElement
            {
                public EditorUtil Container => _container;
                private readonly PrefabModification _container;
                internal readonly SerializedProperty ContainerProp;

                private PropStatus _status;
                private readonly int _index;

                public ElementStatus Status
                {
                    get
                    {
                        switch (_status)
                        {
                            case PropStatus.Inherited:
                                return RemovedProperty.boolValue ? ElementStatus.RemovedUpper : ElementStatus.AddedUpper;
                            case PropStatus.Original:
                                return ElementStatus.NewElement;
                            case PropStatus.Invalid:
                                return ElementStatus.Invalid;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }

                public SerializedProperty ValueProperty { get; }
                public SerializedProperty RemovedProperty { get; }

                public bool Contains
                {
                    get
                    {
                        switch (Status)
                        {
                            case ElementStatus.AddedUpper:
                            case ElementStatus.NewElement:
                                return true;
                            case ElementStatus.Invalid:
                            case ElementStatus.RemovedUpper:
                                return false;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }

                public ElementImpl(PrefabModification container, SerializedProperty containerProp, int index,
                    bool original)
                {
                    _container = container;
                    ContainerProp = containerProp;
                    _index = index;
                    _status = original ? PropStatus.Original : PropStatus.Inherited;

                    ValueProperty = ContainerProp.FindPropertyRelative(nameof(Names.Container.value));
                    RemovedProperty = ContainerProp.FindPropertyRelative(nameof(Names.Container.removed));
                }

                private enum PropStatus
                {
                    Inherited,
                    Original,
                    Invalid,
                }

                public void Add()
                {
                    switch (_status)
                    {
                        case PropStatus.Inherited:
                            RemovedProperty.boolValue = false;
                            break;
                        case PropStatus.Original:
                            // original means exists.
                            break;
                        case PropStatus.Invalid:
                            throw new InvalidOperationException(ElementErrMsg.AddOnInvalid);
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                public void Remove()
                {
                    switch (_status)
                    {
                        case PropStatus.Inherited:
                            RemovedProperty.boolValue = true;
                            break;
                        case PropStatus.Original:
                            var path = ContainerProp.propertyPath;
                            var arrayProp = ContainerProp.FindPropertyRelative(path.Substring(0,
                                path.LastIndexOf(".Array.data[", StringComparison.Ordinal)));
                            arrayProp.DeleteArrayElementAtIndex(_index);
                            _status = PropStatus.Invalid;
                            break;
                        case PropStatus.Invalid:
                            // invalid means removed
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }
    }
}
