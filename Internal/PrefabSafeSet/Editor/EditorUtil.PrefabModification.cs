using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.PrefabSafeSet
{
    public abstract partial class EditorUtil<T>
    {
        private struct ArraySizeCheck
        {
            private int _size;
            private readonly SerializedProperty _prop;

            public ArraySizeCheck(SerializedProperty prop)
            {
                _prop = prop;
                _size = _prop.intValue;
            }

            public bool Changed => _size != _prop.intValue;

            public void Updated() => _size = _prop.intValue;
        }

        private sealed class PrefabModification : EditorUtil<T>
        {
            private readonly List<ElementImpl> _elements;
            private bool _needsUpstreamUpdate;
            private int _upstreamElementCount;
            private readonly SerializedProperty _rootProperty;
            private SerializedProperty _currentRemoves;
            private SerializedProperty _currentAdditions;
            private int _currentRemovesSize;
            private int _currentAdditionsSize;

            private int _nestCount;
            private SerializedProperty _prefabLayers;

            // upstream change check
            private ArraySizeCheck _mainSet;
            private readonly ArraySizeCheck[] _layerRemoves;
            private readonly ArraySizeCheck[] _layerAdditions;

            public PrefabModification(SerializedProperty property, int nestCount,
                Func<SerializedProperty, T> getValue, Action<SerializedProperty, T> setValue) : base(getValue,
                setValue)
            {
                _elements = new List<ElementImpl>();
                _rootProperty = property;
                _nestCount = nestCount;

                ClearNonLayerModifications(property, nestCount);

                var mainSet = property.FindPropertyRelative(Names.MainSet);
                _mainSet = new ArraySizeCheck(mainSet.FindPropertyRelative("Array.size"));
                _layerRemoves = new ArraySizeCheck[nestCount - 1];
                _layerAdditions = new ArraySizeCheck[nestCount - 1];

                // apply modifications until previous one
                _prefabLayers = property.FindPropertyRelative(Names.PrefabLayers);
                ResetLayers();
                DoInitializeUpstream();
                DoInitialize();
            }

            private void ResetLayers()
            {
                // process current layer
                if (_prefabLayers.arraySize < _nestCount) _prefabLayers.arraySize = _nestCount;

                var currentLayer = _prefabLayers.GetArrayElementAtIndex(_nestCount - 1);
                _currentRemoves = currentLayer.FindPropertyRelative(Names.Removes)
                                  ?? throw new InvalidOperationException("prefabLayers.removes not found");
                _currentAdditions = currentLayer.FindPropertyRelative(Names.Additions)
                                    ?? throw new InvalidOperationException("prefabLayers.additions not found");
            }

            private void DoInitializeUpstream()
            {
                _elements.Clear();
                var upstreamValues = new HashSet<T>();

                var mainSet = _rootProperty.FindPropertyRelative(Names.MainSet);

                foreach (var valueProp in new ArrayPropertyEnumerable(mainSet))
                {
                    var value = _getValue(valueProp);
                    if (value == null) continue;
                    if (upstreamValues.Contains(value)) continue;
                    upstreamValues.Add(value);
                    _elements.Add(ElementImpl.Natural(this, value, 0));
                }

                _prefabLayers = _rootProperty.FindPropertyRelative(Names.PrefabLayers);

                for (var i = 0; i < _prefabLayers.arraySize - 1; i++)
                {
                    var layer = _prefabLayers.GetArrayElementAtIndex(i);
                    var removes = layer.FindPropertyRelative(Names.Removes);
                    var additions = layer.FindPropertyRelative(Names.Additions);

                    foreach (var prop in new ArrayPropertyEnumerable(removes))
                    {
                        var value = _getValue(prop);
                        if (upstreamValues.Remove(value))
                            _elements.RemoveAll(x => x.Value.Equals(value));
                    }

                    foreach (var prop in new ArrayPropertyEnumerable(additions))
                    {
                        var value = _getValue(prop);
                        if (upstreamValues.Add(value))
                            _elements.Add(ElementImpl.Natural(this, value, i + 1));
                    }

                    _layerRemoves[i] = new ArraySizeCheck(removes.FindPropertyRelative("Array.size"));
                    _layerAdditions[i] = new ArraySizeCheck(additions.FindPropertyRelative("Array.size"));
                }

                _upstreamElementCount = _elements.Count;
                _mainSet.Updated();
            }

            private void ClearNonLayerModifications(SerializedProperty property, int nestCount)
            {
                try
                {
                    var thisObjectPropPath = property.propertyPath;
                    var arraySizeProp = property.FindPropertyRelative(Names.PrefabLayers)
                        .FindPropertyRelative("Array.size").propertyPath;
                    var arrayValueProp = property.FindPropertyRelative(Names.PrefabLayers)
                        .GetArrayElementAtIndex(nestCount - 1).propertyPath;
                    var serialized = property.serializedObject;
                    var obj = serialized.targetObject;

                    foreach (var modification in PrefabUtility.GetPropertyModifications(obj))
                    {
                        // if property is not of the object: do nothing
                        if (!modification.propertyPath.StartsWith(thisObjectPropPath)) continue;
                        // if property is Array.size or current layer of nest: allow modification
                        if (modification.propertyPath.StartsWith(arraySizeProp)) continue;
                        if (modification.propertyPath.StartsWith(arrayValueProp)) continue;
                        // that modification is not allowed: revert
                        PrefabUtility.RevertPropertyOverride(serialized.FindProperty(modification.propertyPath),
                            InteractionMode.AutomatedAction);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    // ignored
                }
            }

            public override IReadOnlyList<IElement<T>> Elements
            {
                get
                {
                    Initialize();
                    return _elements;
                }
            }

            public override int ElementsCount => _elements.Count;

            /// <summary>
            /// initialize or update cache info if needed
            /// </summary>
            public void Initialize()
            {
                if (_nestCount != _prefabLayers.arraySize)
                {
                    ResetLayers();
                    DoInitializeUpstream();
                    DoInitialize();
                }

                if (_mainSet.Changed || _layerRemoves.Any(x => x.Changed) || _layerAdditions.Any(x => x.Changed))
                {
                    DoInitializeUpstream();
                    DoInitialize();
                }

                if (_currentRemovesSize != _currentRemoves.arraySize ||
                    _currentAdditionsSize != _currentAdditions.arraySize)
                {
                    DoInitialize();
                }
            }

            /// <summary>
            /// initialize or update cache info
            /// </summary>
            public void DoInitialize()
            {
                var removesArray = ToArray(_currentRemoves);
                var removesSet = new HashSet<T>(removesArray);
                var additionsArray = ToArray(_currentAdditions);
                var addsSet = new HashSet<T>(additionsArray);

                _elements.RemoveRange(_upstreamElementCount, _elements.Count - _upstreamElementCount);

                for (var i = 0; i < _elements.Count; i++)
                {
                    var element = _elements[i];

                    var value = element.Value;
                    if (removesSet.Remove(value))
                    {
                        var index = Array.IndexOf(removesArray, value);
                        element.MarkRemovedAt(index);
                    }
                    else if (addsSet.Remove(value))
                    {
                        var index = Array.IndexOf(additionsArray, value);
                        element.MarkAddedTwiceAt(index);
                    }
                    else
                    {
                        element.MarkNatural();
                    }

                    _elements[i] = element;
                }

                // newly added elements
                for (var i = 0; i < additionsArray.Length; i++)
                {
                    var value = additionsArray[i];
                    if (!addsSet.Contains(value)) continue; // it's duplicated addition

                    _elements.Add(ElementImpl.NewElement(this, value, i));
                }

                // fake removed elements
                for (var i = 0; i < removesArray.Length; i++)
                {
                    var value = removesArray[i];
                    if (!removesSet.Contains(value)) continue; // it's removed upper layer

                    _elements.Add(ElementImpl.FakeRemoved(this, value, i));
                }

                _currentRemovesSize = _currentRemoves.arraySize;
                _currentAdditionsSize = _currentAdditions.arraySize;
            }

            public override void Clear()
            {
                Initialize();
                for (var i = _elements.Count - 1; i >= _upstreamElementCount; i--)
                    _elements[i].EnsureRemoved();
                _currentAdditionsSize = _currentAdditions.arraySize = 0;
            }

            protected override IElement<T> NewSlotElement(T value) => ElementImpl.NewSlot(this, value);

            private class ElementImpl : IElement<T>
            {
                public EditorUtil<T> Container => _container;
                private readonly PrefabModification _container;
                private int _indexInModifier;
                internal readonly int SourceNestCount;
                public T Value { get; }
                public ElementStatus Status { get; private set; }

                public bool Contains
                {
                    get
                    {
                        switch (Status)
                        {
                            case ElementStatus.Natural:
                            case ElementStatus.NewElement:
                            case ElementStatus.AddedTwice:
                                return true;
                            case ElementStatus.FakeRemoved:
                            case ElementStatus.Removed:
                            case ElementStatus.NewSlot:
                                return false;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }

                public SerializedProperty ModifierProp { get; private set; }

                private ElementImpl(PrefabModification container, int indexInModifier, T value, ElementStatus status,
                    int sourceNestCount, SerializedProperty modifierProp)
                {
                    _container = container;
                    _indexInModifier = indexInModifier;
                    SourceNestCount = sourceNestCount;
                    Value = value;
                    Status = status;
                    ModifierProp = modifierProp;
                }

                public static ElementImpl Natural(PrefabModification container, T value, int nestCount) =>
                    new ElementImpl(container, -1, value, ElementStatus.Natural, nestCount, null);

                public static ElementImpl NewElement(PrefabModification container, T value, int i) =>
                    new ElementImpl(container, i, value, ElementStatus.NewElement, -1,
                        container._currentAdditions.GetArrayElementAtIndex(i));

                public static ElementImpl FakeRemoved(PrefabModification container, T value, int i) =>
                    new ElementImpl(container, i, value, ElementStatus.FakeRemoved, -1,
                        container._currentRemoves.GetArrayElementAtIndex(i));

                public static ElementImpl NewSlot(PrefabModification container, T value) =>
                    new ElementImpl(container, -1, value, ElementStatus.NewSlot, -1, null);

                public void EnsureAdded() => DoAdd(false);
                public void Add() => DoAdd(true);
                public void EnsureRemoved() => DoRemove(false);
                public void Remove() => DoRemove(true);

                private void DoAdd(bool forceAdd)
                {
                    void AddToAdditions(ElementStatus status)
                    {
                        _indexInModifier = _container._currentAdditions.arraySize;
                        _container._setValue(ModifierProp = AddArrayElement(_container._currentAdditions), Value);
                        _container._currentAdditionsSize += 1;
                        Status = status;
                    }

                    switch (Status)
                    {
                        case ElementStatus.Natural:
                            if (forceAdd)
                                AddToAdditions(ElementStatus.AddedTwice);
                            break;
                        case ElementStatus.Removed:
                            _container._currentRemovesSize -= 1;
                            _container.RemoveArrayElementAt(_container._currentRemoves, _indexInModifier);
                            Status = ElementStatus.Natural;
                            ModifierProp = null;
                            break;
                        case ElementStatus.NewElement:
                        case ElementStatus.AddedTwice:
                            // already added
                            break;
                        case ElementStatus.FakeRemoved:
                            _container._currentRemovesSize -= 1;
                            _container.RemoveArrayElementAt(_container._currentRemoves, _indexInModifier);
                            AddToAdditions(ElementStatus.NewElement);
                            break;
                        case ElementStatus.NewSlot:
                            AddToAdditions(ElementStatus.NewElement);
                            _container._elements.Add(this);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                private void DoRemove(bool forceRemove)
                {
                    void AddToRemoves(ElementStatus status)
                    {
                        _indexInModifier = _container._currentRemoves.arraySize;
                        _container._setValue(ModifierProp = AddArrayElement(_container._currentRemoves), Value);
                        _container._currentRemovesSize += 1;
                        Status = status;
                    }

                    switch (Status) 
                    {
                        case ElementStatus.Natural:
                            AddToRemoves(ElementStatus.Removed);
                            break;
                        case ElementStatus.Removed:
                        case ElementStatus.FakeRemoved:
                            // already removed: nothing to do
                            break;
                        case ElementStatus.NewElement:
                            _container._currentAdditionsSize -= 1;
                            _container.RemoveArrayElementAt(_container._currentAdditions, _indexInModifier);
                            Status = ElementStatus.NewSlot;
                            _container._elements.Remove(this);
                            ModifierProp = null;
                            break;
                        case ElementStatus.AddedTwice:
                            _container._currentAdditionsSize -= 1;
                            _container.RemoveArrayElementAt(_container._currentAdditions, _indexInModifier);
                            AddToRemoves(ElementStatus.Removed);
                            break;
                        case ElementStatus.NewSlot:
                            if (forceRemove)
                            {
                                AddToRemoves(ElementStatus.FakeRemoved);
                                _container._elements.Add(this);
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                internal void Revert()
                {
                    switch (Status)
                    {
                        case ElementStatus.Natural:
                            break; // nop
                        case ElementStatus.Removed:
                            _container._currentRemovesSize -= 1;
                            _container.RemoveArrayElementAt(_container._currentRemoves, _indexInModifier);
                            Status = ElementStatus.Natural;
                            ModifierProp = null;
                            break;
                        case ElementStatus.NewElement:
                            _container._currentAdditionsSize -= 1;
                            _container.RemoveArrayElementAt(_container._currentAdditions, _indexInModifier);
                            Status = ElementStatus.NewSlot;
                            _container._elements.Remove(this);
                            ModifierProp = null;
                            break;
                        case ElementStatus.AddedTwice:
                            _container._currentAdditionsSize -= 1;
                            _container.RemoveArrayElementAt(_container._currentAdditions, _indexInModifier);
                            Status = ElementStatus.Natural;
                            ModifierProp = null;
                            break;
                        case ElementStatus.FakeRemoved:
                            _container._currentRemovesSize -= 1;
                            _container.RemoveArrayElementAt(_container._currentRemoves, _indexInModifier);
                            Status = ElementStatus.NewSlot;
                            _container._elements.Remove(this);
                            ModifierProp = null;
                            break;
                        case ElementStatus.NewSlot:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    _container._rootProperty.serializedObject.ApplyModifiedProperties();
                }

                public void MarkRemovedAt(int index)
                {
                    _indexInModifier = index;
                    ModifierProp = _container._currentRemoves.GetArrayElementAtIndex(index);
                    Status = ElementStatus.Removed;
                }

                public void MarkAddedTwiceAt(int index)
                {
                    _indexInModifier = index;
                    ModifierProp = _container._currentAdditions.GetArrayElementAtIndex(index);
                    Status = ElementStatus.AddedTwice;
                }

                public void MarkNatural()
                {
                    Status = ElementStatus.Natural;
                }

                public void SetExistence(bool existence)
                {
                    if (existence != Contains)
                    {
                        switch (Status)
                        {
                            case ElementStatus.NewElement:
                                Remove();
                                Remove();
                                break;
                            case ElementStatus.Natural:
                            case ElementStatus.AddedTwice:
                                Remove();
                                break;

                            case ElementStatus.Removed:
                                Add();
                                Add();
                                break;
                            case ElementStatus.FakeRemoved:
                            case ElementStatus.NewSlot:
                                Add();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }

            public override void HandleApplyRevertMenuItems(IElement<T> element, GenericMenu genericMenu)
            {
                var elementImpl = (ElementImpl)element;
                HandleApplyMenuItems(_currentAdditions.serializedObject.targetObject, elementImpl, genericMenu);
                HandleRevertMenuItem(genericMenu, elementImpl);
            }

            private void HandleApplyMenuItems(Object instanceOrAssetObject,
                ElementImpl elementImpl,
                GenericMenu genericMenu,
                bool defaultOverrideComparedToSomeSources = false)
            {
                var applyTargets = GetApplyTargets(instanceOrAssetObject,
                    defaultOverrideComparedToSomeSources);
                if (applyTargets == null || applyTargets.Count == 0)
                    return;

                var valueIter = elementImpl.Value;
                for (var index = 0; index < applyTargets.Count; ++index)
                {
                    var componentOrGameObject = applyTargets[index];

                    var rootGameObject = GetRootGameObject(componentOrGameObject);
                    var format = L10n.Tr(index == applyTargets.Count - 1
                        ? "Apply to Prefab '{0}'"
                        : "Apply as Override in Prefab '{0}'");
                    var guiContent = new GUIContent(string.Format(format, rootGameObject.name));

                    valueIter = ObjectOrCorrespondingObject(valueIter);
                    var value = valueIter;

                    var nestCount = applyTargets.Count - index - 1;

                    if (!PrefabUtility.IsPartOfPrefabThatCanBeAppliedTo(GetRootGameObject(componentOrGameObject)))
                    {
                        genericMenu.AddDisabledItem(guiContent);
                    }
                    else if (EditorUtility.IsPersistent(_rootProperty.serializedObject.targetObject))
                    {
                        genericMenu.AddDisabledItem(guiContent);
                    }
                    else if (value == null)
                    {
                        genericMenu.AddDisabledItem(guiContent);
                    }
                    else
                    {
                        void RemoveFromList(SerializedProperty sourceArrayProp)
                        {
                            var serialized = new SerializedObject(componentOrGameObject);
                            var newArray = serialized.FindProperty(sourceArrayProp.propertyPath);
                            var foundIndex = Array.IndexOf(ToArray(newArray), value);
                            if (foundIndex == -1) return;
                            RemoveArrayElementAt(newArray, foundIndex);
                            serialized.ApplyModifiedProperties();
                            PrefabUtility.SavePrefabAsset(rootGameObject);
                            sourceArrayProp.serializedObject.Update();
                        }

                        void AddToList(SerializedProperty sourceArrayProp)
                        {
                            var serialized = new SerializedObject(componentOrGameObject);
                            var newArray = serialized.FindProperty(sourceArrayProp.propertyPath);
                            _setValue(AddArrayElement(newArray), value);
                            serialized.ApplyModifiedProperties();
                            PrefabUtility.SavePrefabAsset(rootGameObject);
                            sourceArrayProp.serializedObject.Update();
                        }

                        switch (elementImpl.Status)
                        {
                            case ElementStatus.Natural:
                                // logic failure: Natural means nothing to override
                                genericMenu.AddDisabledItem(guiContent);
                                break;
                            case ElementStatus.Removed:
                                if (elementImpl.SourceNestCount <= nestCount)
                                {
                                    // the value is already added in the Property
                                    genericMenu.AddItem(guiContent, false, _ =>
                                    {
                                        if (nestCount == 0)
                                        {
                                            // apply target is base object: nestCount
                                            RemoveFromList(_rootProperty.FindPropertyRelative(Names.MainSet));
                                        }
                                        else if (elementImpl.SourceNestCount == nestCount)
                                        {
                                            // apply target is addition
                                            var additionProp = _rootProperty.FindPropertyRelative(Names.PrefabLayers +
                                                $".Array.data[{nestCount - 1}]." + Names.Additions);
                                            RemoveFromList(additionProp);
                                        }
                                        else
                                        {
                                            _setValue(
                                                AddArrayElement(_rootProperty.FindPropertyRelative(Names.PrefabLayers +
                                                    $".Array.data[{nestCount - 1}]." + Names.Removes)), elementImpl.Value);
                                        }

                                        elementImpl.Revert();
                                        ForceRebuildInspectors();
                                    }, null);
                                }
                                else
                                {
                                    genericMenu.AddDisabledItem(guiContent);
                                }
                                break;
                            case ElementStatus.NewElement:
                                // if possible, check for deleting this element in parent prefabs
                                genericMenu.AddItem(guiContent, false, (_) =>
                                {
                                    if (nestCount == 0)
                                    {
                                        AddToList(_rootProperty.FindPropertyRelative(Names.MainSet));
                                    }
                                    else
                                    {
                                        AddToList(_rootProperty.FindPropertyRelative(Names.PrefabLayers +
                                            $".Array.data[{nestCount - 1}]." + Names.Additions));
                                    }

                                    elementImpl.Revert();
                                    ForceRebuildInspectors();
                                }, null);
                                break;
                            case ElementStatus.AddedTwice:
                                genericMenu.AddDisabledItem(guiContent);
                                break;
                            case ElementStatus.FakeRemoved:
                                genericMenu.AddDisabledItem(guiContent);
                                break;
                            case ElementStatus.NewSlot:
                                // logic faliure
                                genericMenu.AddDisabledItem(guiContent);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }

            private void HandleRevertMenuItem(GenericMenu genericMenu, ElementImpl element)
            {
                var guiContent = new GUIContent(L10n.Tr("Revert"));
                genericMenu.AddItem(guiContent, false, _ => element.Revert(), null);
            }

            private static List<Object> GetApplyTargets(
                Object instanceOrAssetObject,
                bool defaultOverrideComparedToSomeSources)
            {
                var applyTargets = new List<Object>();
                // verify the value is GameObject or Component
                if (!(instanceOrAssetObject is GameObject)) _ = (Component)instanceOrAssetObject;
                var obj = PrefabUtility.GetCorrespondingObjectFromSource(instanceOrAssetObject);
                if (obj == null)
                    return applyTargets;
                for (; obj != null; obj = PrefabUtility.GetCorrespondingObjectFromSource(obj))
                {
                    if (defaultOverrideComparedToSomeSources)
                    {
                        var gameObject2 = GetGameObject(obj);
                        if (gameObject2.transform.root == gameObject2.transform)
                            break;
                    }

                    applyTargets.Add(obj);
                }

                return applyTargets;
            }

            private static GameObject GetGameObject(Object componentOrGameObject)
            {
                var gameObject = componentOrGameObject as GameObject;
                if (gameObject)
                    return gameObject;
                var component = componentOrGameObject as Component;
                return component ? component.gameObject : null;
            }

            private static GameObject GetRootGameObject(Object componentOrGameObject)
            {
                var gameObject = GetGameObject(componentOrGameObject);
                return gameObject == null ? null : gameObject.transform.root.gameObject;
            }

            private static void ForceRebuildInspectors()
            {
                var type = typeof(EditorUtility);
                var method = type.GetMethod("ForceRebuildInspectors",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic,
                    null,
                    Type.EmptyTypes, 
                    null);
                System.Diagnostics.Debug.Assert(method != null, nameof(method) + " != null");
                method.Invoke(null, null);
            }

            private static T ObjectOrCorrespondingObject(T value)
            {
                if (!(value is Object obj)) return value;
                if (EditorUtility.IsPersistent(obj)) return value;
                var corresponding = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                if (corresponding is T t) return t;
                return default;
            }
        }
    }
}
