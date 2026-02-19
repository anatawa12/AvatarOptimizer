using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.PrefabSafeUniqueCollection
{
    public abstract partial class BaseEditorUtil<TAdditionValue, TRemoveKey>
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

            public bool Changed => _size != (_prop?.intValue ?? 0);

            public void Updated() => _size = _prop?.intValue ?? 0;
        }

        private abstract class PrefabModificationBase : BaseEditorUtil<TAdditionValue, TRemoveKey>
        {
            private readonly List<ElementImpl> _elements;
            private bool _needsUpstreamUpdate;
            private int _upstreamElementCount;
            protected readonly SerializedProperty RootProperty;
            protected SerializedProperty? CurrentRemoves;
            protected SerializedProperty? CurrentAdditions;
            private int _currentRemovesSize;
            private int _currentAdditionsSize;

            protected readonly int NestCount;
            protected readonly SerializedProperty PrefabLayers;

            // upstream change check
            private ArraySizeCheck _mainSet;
            protected ArraySizeCheck _prefabLayersSize;
            private readonly ArraySizeCheck[] _layerRemoves;
            private readonly ArraySizeCheck[] _layerAdditions;

            public PrefabModificationBase(SerializedProperty property, int nestCount,
                IEditorUtilHelper<TAdditionValue, TRemoveKey> helper) : base(helper)

            {
                _elements = new List<ElementImpl>();
                RootProperty = property ?? throw new ArgumentNullException(nameof(property));
                if (nestCount <= 0) throw new ArgumentException("nestCount is zero or negative", nameof(nestCount));
                NestCount = nestCount;

                var mainSet = property.FindPropertyRelative(Names.MainSet);
                _mainSet = new ArraySizeCheck(mainSet.FindPropertyRelative("Array.size"));
                _layerRemoves = new ArraySizeCheck[nestCount - 1];
                _layerAdditions = new ArraySizeCheck[nestCount - 1];

                // apply modifications until previous one
                PrefabLayers = property.FindPropertyRelative(Names.PrefabLayers);
                _prefabLayersSize = new ArraySizeCheck(PrefabLayers.FindPropertyRelative("Array.size"));

                ClearNonLayerModifications(property, nestCount);
                Normalize();

                InitCurrentLayer();
                DoInitializeUpstream();
                DoInitialize();
            }

            protected abstract void InitCurrentLayer(bool force = false);
            protected abstract void Normalize();

            struct Manipulator : IManipulator<ElementImpl, TRemoveKey>
            {
                public TRemoveKey? GetKey(ElementImpl? value) => value == null ? default : value.RemoveKey;

                public ref TRemoveKey? GetKey(ref ElementImpl? value) => throw new NotImplementedException();
            }

            private void DoInitializeUpstream()
            {
                _elements.Clear();
                var upstreamValues = new ListMap<ElementImpl, TRemoveKey, Manipulator>(Array.Empty<ElementImpl>(), default);

                var mainSet = RootProperty.FindPropertyRelative(Names.MainSet);

                foreach (var valueProp in new ArrayPropertyEnumerable(mainSet))
                {
                    var value = _helper.ReadAdditionValue(valueProp);
                    if (value == null) continue;
                    upstreamValues.Add(ElementImpl.Natural(this, value, 0));
                }

                for (var i = 0; i < NestCount - 1 && i < PrefabLayers.arraySize; i++)
                {
                    var layer = PrefabLayers.GetArrayElementAtIndex(i);
                    var removes = layer.FindPropertyRelative(Names.Removes);
                    var additions = layer.FindPropertyRelative(Names.Additions);

                    foreach (var prop in new ArrayPropertyEnumerable(removes))
                    {
                        var removeKey = _helper.ReadRemoveKey(prop);
                        if (removeKey == null) continue;
                        upstreamValues.Remove(removeKey);
                    }

                    foreach (var prop in new ArrayPropertyEnumerable(additions))
                    {
                        var value = _helper.ReadAdditionValue(prop);
                        if (value == null) continue;
                        var key = _helper.GetRemoveKey(value);
                        if (upstreamValues.ContainsKey(key)) continue;
                        upstreamValues.Add(ElementImpl.Natural(this, value, i + 1));
                    }

                    _layerRemoves[i] = new ArraySizeCheck(removes.FindPropertyRelative("Array.size"));
                    _layerAdditions[i] = new ArraySizeCheck(additions.FindPropertyRelative("Array.size"));
                }

                _elements.AddRange(upstreamValues);

                _upstreamElementCount = _elements.Count;
                _mainSet.Updated();
            }

            protected abstract string[] GetAllowedPropertyPaths(int nestCount);

            private void ClearNonLayerModifications(SerializedProperty property, int nestCount)
            {
                try
                {
                    var thisObjectPropPath = property.propertyPath;
                    var allowedProperties = GetAllowedPropertyPaths(nestCount).Select(x => $"{thisObjectPropPath}.{x}").ToArray();
                    var serialized = property.serializedObject;
                    var obj = serialized.targetObject;

                    foreach (var modification in PrefabUtility.GetPropertyModifications(obj))
                    {
                        // if property is not of the object: do nothing
                        if (!modification.propertyPath.StartsWith(thisObjectPropPath, StringComparison.Ordinal)) continue;
                        if (allowedProperties.Any(x => modification.propertyPath.StartsWith(x, StringComparison.Ordinal))) continue;
                        var prop = serialized.FindProperty(modification.propertyPath);
                        if (prop is null) continue;
                        // allow to make null for ObjectReference to support removing prefab element
                        if (prop.propertyType == SerializedPropertyType.ObjectReference &&
                            prop.objectReferenceValue == null)
                            continue;
                        // that modification is not allowed: revert
                        PrefabUtility.RevertPropertyOverride(prop, InteractionMode.AutomatedAction);
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                    // ignored
                }
            }


            public sealed override IReadOnlyList<IBaseElement<TAdditionValue, TRemoveKey>> Elements => ElementImpls;
            private List<ElementImpl> ElementImpls
            {
                get
                {
                    Initialize();
                    return _elements;
                }
            }

            public sealed override int ElementsCount => _elements.Count;

            /// <summary>
            /// initialize or update cache info if needed
            /// </summary>
            public void Initialize()
            {
                if (_prefabLayersSize.Changed)
                    InitCurrentLayer();

                if (_mainSet.Changed || _layerRemoves.Any(x => x.Changed) || _layerAdditions.Any(x => x.Changed))
                {
                    DoInitializeUpstream();
                    DoInitialize();
                }

                if (_currentRemovesSize != (CurrentRemoves?.arraySize ?? 0) ||
                    _currentAdditionsSize != (CurrentAdditions?.arraySize ?? 0))
                {
                    DoInitialize();
                }
            }

            /// <summary>
            /// initialize or update cache info
            /// </summary>
            public void DoInitialize()
            {
                var removesArray = RemoveKeysToArray(CurrentRemoves);
                var removesSet = new HashSet<TRemoveKey>(removesArray.NonNulls());
                var additionsArray = AdditionsToArray(CurrentAdditions);
                var addsSet = additionsArray.NonNulls().ToDictionary(x => _helper.GetRemoveKey(x));

                // remove elements that are not in upstream
                _elements.RemoveRange(_upstreamElementCount, _elements.Count - _upstreamElementCount);

                // process upstream elements
                for (var i = 0; i < _elements.Count; i++)
                {
                    var element = _elements[i];

                    if (removesSet.Remove(element.RemoveKey))
                    {
                        var index = Array.IndexOf(removesArray, element.RemoveKey);
                        element.MarkRemovedAt(index);
                    }
                    else if (addsSet.Remove(element.RemoveKey, out var value))
                    {
                        var index = Array.IndexOf(additionsArray, value);
                        element.MarkOverridenAt(index, value);
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
                    if (value == null) continue;
                    var key = _helper.GetRemoveKey(value);
                    if (!addsSet.ContainsKey(key)) continue; // it's duplicated addition / override value

                    _elements.Add(ElementImpl.NewElement(this, value, i));
                }

                // fake removed elements
                for (var i = 0; i < removesArray.Length; i++)
                {
                    var value = removesArray[i];
                    if (value == null) continue;
                    if (!removesSet.Contains(value)) continue; // it's removed upper layer

                    _elements.Add(ElementImpl.FakeRemoved(this, value, i));
                }

                _currentRemovesSize = CurrentRemoves?.arraySize ?? 0;
                _currentAdditionsSize = CurrentAdditions?.arraySize ?? 0;
            }

            public sealed override void Clear()
            {
                Initialize();
                for (var i = _elements.Count - 1; i >= 0; i--)
                    _elements[i].EnsureRemoved();
                if (CurrentAdditions != null)
                    _currentAdditionsSize = CurrentAdditions.arraySize = 0;
            }

            public override IBaseElement<TAdditionValue, TRemoveKey> Set(TAdditionValue value) => Set(value, false);
            public override IBaseElement<TAdditionValue, TRemoveKey> Add(TAdditionValue value) => Set(value, true);

            public override IBaseElement<TAdditionValue, TRemoveKey> Remove(TRemoveKey key)
            {
                if (GetElementOf(key) is { } element)
                {
                    element.Remove();
                    return element;
                }
                else
                {
                    var (indexInModifier, _) = AddToRemoves(key);
                    var newElement = ElementImpl.FakeRemoved(this, key, indexInModifier);
                    ElementImpls.Add(newElement);
                    return newElement;
                }
            }

            public IBaseElement<TAdditionValue, TRemoveKey> Set(TAdditionValue value, bool forceAdd)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                var key = _helper.GetRemoveKey(value);
                var element = ElementImpls.FirstOrDefault(x => Equals(x.RemoveKey, key));

                if (element == null)
                {
                    // there is no element with the same key; add new element
                    var (indexInModifier, _) = AddToAdditions(value);
                    var newElement = ElementImpl.NewElement(this, value, indexInModifier);
                    ElementImpls.Add(newElement);
                    return newElement;
                }
                else
                {
                    if (element.Status is ElementStatus.NewElement or ElementStatus.Overriden)
                    {
                        // if there already is an addition, just update it if not force
                        _helper.WriteAdditionValue(element.ModifierProp!, value);
                        element.Value = value;
                    }
                    else
                    {
                        if (element.Status is ElementStatus.Removed or ElementStatus.FakeRemoved)
                        {
                            // if it's removed, remove "remove" entry
                            RemoveRemovesAt(element.IndexInModifier);
                            element.ModifierProp = null;
                        }

                        if (element.Status is ElementStatus.Removed or ElementStatus.Natural)
                        {
                            // if there is entry from upstream, check if it's the same value
                            // if different, add new addition, if same, do nothing
                            var existing = element.Value!;
                            if (!forceAdd && Equals(existing, value))
                            {
                                element.Status = ElementStatus.Natural;
                            }
                            else
                            {
                                (element.IndexInModifier, element.ModifierProp) = AddToAdditions(value);
                                element.Status = ElementStatus.Overriden;
                                element.Value = value;
                            }
                        }
                        else
                        {
                            // there is no upstream entry, add new addition
                            (element.IndexInModifier, element.ModifierProp) = AddToAdditions(value);
                            element.Status = ElementStatus.NewElement;
                            element.Value = value;
                        }
                    }

                    return element;
                }
            }

            public sealed override bool HasPrefabOverride() => _elements.Any(x => x.IsPrefabOverride());

            private class ElementImpl : IBaseElement<TAdditionValue, TRemoveKey>
            {
                public BaseEditorUtil<TAdditionValue, TRemoveKey> Container => _container;
                private readonly PrefabModificationBase _container;
                internal int IndexInModifier;
                internal readonly int SourceNestCount;
                public TAdditionValue? Value { get; internal set; }
                public TRemoveKey RemoveKey { get; }
                public ElementStatus Status { get; internal set; }

                public bool Contains
                {
                    get
                    {
                        switch (Status)
                        {
                            case ElementStatus.Natural:
                            case ElementStatus.NewElement:
                            case ElementStatus.Overriden:
                                return true;
                            case ElementStatus.FakeRemoved:
                            case ElementStatus.Removed:
                            case ElementStatus.Invalid:
                                return false;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }

                public SerializedProperty? ModifierProp { get; internal set; }

                private ElementImpl(PrefabModificationBase container, int indexInModifier, TAdditionValue value, ElementStatus status,
                    int sourceNestCount, SerializedProperty? modifierProp)
                {
                    if (value == null) throw new ArgumentNullException(nameof(value));
                    _container = container;
                    IndexInModifier = indexInModifier;
                    SourceNestCount = sourceNestCount;
                    Value = value;
                    RemoveKey = container._helper.GetRemoveKey(value);
                    Status = status;
                    ModifierProp = modifierProp;
                }

                private ElementImpl(PrefabModificationBase container, int indexInModifier, TRemoveKey removeKey, ElementStatus status,
                    int sourceNestCount, SerializedProperty? modifierProp)
                {
                    if (removeKey == null) throw new ArgumentNullException(nameof(removeKey));
                    _container = container;
                    IndexInModifier = indexInModifier;
                    SourceNestCount = sourceNestCount;
                    Value = default;
                    RemoveKey = removeKey;
                    Status = status;
                    ModifierProp = modifierProp;
                }

                public static ElementImpl Natural(PrefabModificationBase container, TAdditionValue value, int nestCount) =>
                    new ElementImpl(container, -1, value, ElementStatus.Natural, nestCount, null);

                public static ElementImpl NewElement(PrefabModificationBase container, TAdditionValue value, int i)
                {
                    if (container.CurrentAdditions == null) throw new InvalidOperationException("_container._currentAdditions == null");
                    return new ElementImpl(container, i, value, ElementStatus.NewElement, -1,
                        container.CurrentAdditions.GetArrayElementAtIndex(i));
                }

                public static ElementImpl FakeRemoved(PrefabModificationBase container, TRemoveKey value, int i)
                {
                    if (container.CurrentRemoves == null) throw new InvalidOperationException("_container._currentRemoves == null");
                    return new ElementImpl(container, i, value, ElementStatus.FakeRemoved, -1,
                        container.CurrentRemoves.GetArrayElementAtIndex(i));
                }

                public void EnsureRemoved() => DoRemove(false);
                public void Remove() => DoRemove(true);


                private void DoRemove(bool forceRemove)
                {
                    switch (Status) 
                    {
                        case ElementStatus.Natural:
                            (IndexInModifier, ModifierProp) = _container.AddToRemoves(RemoveKey);
                            Status = ElementStatus.Removed;
                            break;
                        case ElementStatus.Removed:
                        case ElementStatus.FakeRemoved:
                            // already removed: nothing to do
                            break;
                        case ElementStatus.NewElement:
                            _container.RemoveAdditionsAt(IndexInModifier);
                            Status = ElementStatus.Invalid;
                            _container._elements.Remove(this);
                            if (forceRemove) goto case ElementStatus.Invalid; // if force remove, do remove again
                            break;
                        case ElementStatus.Overriden:
                            _container.RemoveAdditionsAt(IndexInModifier);
                            (IndexInModifier, ModifierProp) = _container.AddToRemoves(RemoveKey);
                            Status = ElementStatus.Removed;
                            break;
                        case ElementStatus.Invalid:
                            if (forceRemove)
                            {
                                (IndexInModifier, ModifierProp) = _container.AddToRemoves(RemoveKey);
                                Status = ElementStatus.FakeRemoved;
                                _container._elements.Add(this);
                            }
                            break;
                        default:
                            throw new InvalidOperationException($"Invalid element status: {Status}");
                    }
                }

                internal void Revert()
                {
                    switch (Status)
                    {
                        case ElementStatus.Natural:
                            break; // nop
                        case ElementStatus.Removed:
                            _container.RemoveRemovesAt(IndexInModifier);
                            Status = ElementStatus.Natural;
                            ModifierProp = null;
                            break;
                        case ElementStatus.NewElement:
                            _container.RemoveAdditionsAt(IndexInModifier);
                            Status = ElementStatus.Invalid;
                            _container._elements.Remove(this);
                            ModifierProp = null;
                            break;
                        case ElementStatus.Overriden:
                            _container.RemoveAdditionsAt(IndexInModifier);
                            Status = ElementStatus.Natural;
                            ModifierProp = null;
                            break;
                        case ElementStatus.FakeRemoved:
                            _container.RemoveRemovesAt(IndexInModifier);
                            Status = ElementStatus.Invalid;
                            _container._elements.Remove(this);
                            ModifierProp = null;
                            break;
                        case ElementStatus.Invalid:
                            break;
                        default:
                            throw new InvalidOperationException($"Invalid element status: {Status}");
                    }

                    _container.RootProperty.serializedObject.ApplyModifiedProperties();
                }

                public void MarkRemovedAt(int index)
                {
                    if (_container.CurrentRemoves == null) throw new InvalidOperationException("_container._currentRemoves == null");
                    IndexInModifier = index;
                    ModifierProp = _container.CurrentRemoves.GetArrayElementAtIndex(index);
                    Status = ElementStatus.Removed;
                }

                public void MarkOverridenAt(int index, TAdditionValue value)
                {
                    if (_container.CurrentAdditions == null) throw new InvalidOperationException("_container._currentAdditions == null");
                    Value = value;
                    IndexInModifier = index;
                    ModifierProp = _container.CurrentAdditions.GetArrayElementAtIndex(index);
                    Status = ElementStatus.Overriden;
                }

                public void MarkNatural()
                {
                    Status = ElementStatus.Natural;
                    ModifierProp = null;
                }

                public bool IsPrefabOverride()
                {
                    switch (Status)
                    {
                        case ElementStatus.Natural:
                        case ElementStatus.Invalid:
                            return false;
                        case ElementStatus.Removed:
                        case ElementStatus.NewElement:
                        case ElementStatus.Overriden:
                        case ElementStatus.FakeRemoved:
                            return true;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                public override string ToString() => Status switch
                {
                    ElementStatus.Natural or ElementStatus.Removed or ElementStatus.NewElement
                        or ElementStatus.Overriden => $"Element(Prefab, {Value}, {Status})",
                    ElementStatus.FakeRemoved or ElementStatus.Invalid => $"Element(Prefab, {RemoveKey}, {Status})",
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            private (int indexInModifier, SerializedProperty modifierProp) AddToAdditions(TAdditionValue value) => 
                AddToModifications(value, ref _currentAdditionsSize, ref CurrentAdditions, _helper.WriteAdditionValue);
            
            private (int indexInModifier, SerializedProperty modifierProp) AddToRemoves(TRemoveKey value) => 
                AddToModifications(value, ref _currentRemovesSize, ref CurrentRemoves, _helper.WriteRemoveKey);

            private (int indexInModifier, SerializedProperty modifierProp) AddToModifications<T>(T value,
                ref int currentModificationSize,
                ref SerializedProperty? currentModifications, // ref is needed to ensure InitCurrentLayer effect
                Action<SerializedProperty, T> writeValue)
            {
                InitCurrentLayer(true);
                if (currentModifications == null) throw new InvalidOperationException("currentModifications is null (force init failed)");
                var indexInModifier = currentModifications.arraySize;
                var modifierProp = AddArrayElement(currentModifications);
                writeValue(modifierProp, value);
                currentModificationSize += 1;
                return (indexInModifier, modifierProp);
            }

            private void RemoveAdditionsAt(int indexInModifier) =>
                RemoveModificationsAt(indexInModifier, ref _currentAdditionsSize, CurrentAdditions!,
                    ElementStatus.NewElement, ElementStatus.Overriden);

            private void RemoveRemovesAt(int indexInModifier) =>
                RemoveModificationsAt(indexInModifier, ref _currentRemovesSize, CurrentRemoves!,
                    ElementStatus.Removed, ElementStatus.FakeRemoved);

            private void RemoveModificationsAt(int indexInModifier, 
                ref int currentModificationSize,
                SerializedProperty currentModifications,
                ElementStatus userState1, ElementStatus userState2)
            {
                currentModificationSize -= 1;
                RemoveArrayElementAt(currentModifications, indexInModifier);
                foreach (var elementImpl in _elements)
                {
                    if (elementImpl.Status == userState1 || elementImpl.Status == userState2)
                        if (elementImpl.IndexInModifier > indexInModifier)
                        {
                            elementImpl.IndexInModifier--;
                            elementImpl.ModifierProp =
                                currentModifications.GetArrayElementAtIndex(elementImpl.IndexInModifier);
                        }
                }
            }

            public sealed override void HandleApplyRevertMenuItems(IBaseElement<TAdditionValue, TRemoveKey> element, GenericMenu genericMenu)
            {
                var elementImpl = (ElementImpl)element;
                HandleApplyMenuItems(RootProperty.serializedObject.targetObject, elementImpl, genericMenu);
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
                    else if (EditorUtility.IsPersistent(RootProperty.serializedObject.targetObject))
                    {
                        genericMenu.AddDisabledItem(guiContent);
                    }
                    else if (value == null)
                    {
                        genericMenu.AddDisabledItem(guiContent);
                    }
                    else
                    {
                        void RemoveFromAdditionList(SerializedProperty sourceArrayProp)
                        {
                            var serialized = new SerializedObject(componentOrGameObject);
                            var newArray = serialized.FindProperty(sourceArrayProp.propertyPath);
                            var foundIndex = Array.IndexOf(AdditionsToArray(newArray), value);
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
                            _helper.WriteAdditionValue(AddArrayElement(newArray), value);
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
                                            RemoveFromAdditionList(RootProperty.FindPropertyRelative(Names.MainSet));
                                        }
                                        else if (elementImpl.SourceNestCount == nestCount)
                                        {
                                            // apply target is addition
                                            var additionProp = RootProperty.FindPropertyRelative(Names.PrefabLayers +
                                                $".Array.data[{nestCount - 1}]." + Names.Additions);
                                            RemoveFromAdditionList(additionProp);
                                        }
                                        else
                                        {
                                            var property = AddArrayElement(RootProperty.FindPropertyRelative(
                                                Names.PrefabLayers + $".Array.data[{nestCount - 1}]." + Names.Removes));
                                            _helper.WriteAdditionValue(property, elementImpl.Value!);
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
                                        AddToList(RootProperty.FindPropertyRelative(Names.MainSet));
                                    }
                                    else
                                    {
                                        AddToList(RootProperty.FindPropertyRelative(Names.PrefabLayers +
                                            $".Array.data[{nestCount - 1}]." + Names.Additions));
                                    }

                                    elementImpl.Revert();
                                    ForceRebuildInspectors();
                                }, null);
                                break;
                            case ElementStatus.Overriden:
                                genericMenu.AddDisabledItem(guiContent);
                                break;
                            case ElementStatus.FakeRemoved:
                                genericMenu.AddDisabledItem(guiContent);
                                break;
                            case ElementStatus.Invalid:
                                // logic faliure
                                genericMenu.AddDisabledItem(guiContent);
                                break;
                            default:
                                throw new InvalidOperationException($"Invalid element status: {elementImpl.Status}");
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
                if (gameObject != null)
                    return gameObject;
                var component = componentOrGameObject as Component;
                if (component != null)
                    return component.gameObject;
                throw new InvalidOperationException($"componentOrGameObject is not GameObject nor Component ({componentOrGameObject.name})");
            }

            private static GameObject GetRootGameObject(Object componentOrGameObject) =>
                GetGameObject(componentOrGameObject).transform.root.gameObject;

            private static void ForceRebuildInspectors()
            {
                var type = typeof(EditorUtility);
                var method = type.GetMethod("ForceRebuildInspectors",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic,
                    null,
                    Type.EmptyTypes, 
                    null);
                if (method == null)
                {
                    UnityEngine.Debug.LogError("ForceRebuildInspectors not found");
                    return;
                }
                method.Invoke(null, null);
            }

            private static T? ObjectOrCorrespondingObject<T>(T? value)
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
