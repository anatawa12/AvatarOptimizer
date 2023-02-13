using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer
{
    public class PrefabSafeSet
    {
        #region utilities

        private static int PrefabNestCount(Object instance)
        {
            var nestCount = 0;
            while ((bool)(instance = PrefabUtility.GetCorrespondingObjectFromSource(instance)))
                nestCount++;

            return nestCount;
        }

        private readonly struct ListSet<T>
        {
            private readonly List<T> _list;
            private readonly HashSet<T> _set;

            public ListSet(bool setOnly)
            {
                _list = setOnly ? null : new List<T>();
                _set = new HashSet<T>();
            }

            public ListSet(T[] initialize, bool setOnly = false)
            {
                _list = setOnly ? null : new List<T>(initialize);
                _set = new HashSet<T>(initialize);
            }

            public int Count => _set.Count;

            public bool Add(T value)
            {
                if (_set.Add(value))
                {
                    _list?.Add(value);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public bool Remove(T value)
            {
                if (_set.Remove(value))
                {
                    _list?.Remove(value);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public void AddRange(IEnumerable<T> values)
            {
                foreach (var value in values) Add(value);
            }

            public void RemoveRange(IEnumerable<T> values)
            {
                foreach (var value in values)
                    Remove(value);
            }

            public List<T>.Enumerator GetEnumerator() => _list.GetEnumerator();

            public bool Contains(T value) => _set.Contains(value);

            public T[] ToArray() => _list?.ToArray() ?? throw new InvalidOperationException("set only");
        }

        private readonly struct ArrayPropertyEnumerable : IEnumerable<SerializedProperty>
        {
            private readonly SerializedProperty _property;
            private readonly int _begin;
            private readonly int _end;

            public ArrayPropertyEnumerable(SerializedProperty property)
            {
                _property = property;
                _begin = 0;
                _end = property.arraySize;
            }

            private ArrayPropertyEnumerable(SerializedProperty property, int begin, int end)
            {
                _property = property;
                _begin = begin;
                _end = end;
            }

            public ArrayPropertyEnumerable SkipLast(int n) => new ArrayPropertyEnumerable(_property, _begin, _end - n);

            public ArrayPropertyEnumerable Take(int count) =>
                new ArrayPropertyEnumerable(_property, _begin, Math.Min(_end, _begin + count));

            public Enumerator GetEnumerator() => new Enumerator(this);

            IEnumerator<SerializedProperty> IEnumerable<SerializedProperty>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public struct Enumerator : IEnumerator<SerializedProperty>
            {
                private readonly SerializedProperty _property;
                private int _index;
                private int _size;

                public Enumerator(ArrayPropertyEnumerable enumerable)
                {
                    _property = enumerable._property;
                    _index = enumerable._begin - 1;
                    _size = enumerable._end;
                }

                public SerializedProperty Current => _property.GetArrayElementAtIndex(_index);
                SerializedProperty IEnumerator<SerializedProperty>.Current => Current;
                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _index++;
                    return _index < _size;
                }

                public void Reset() => throw new NotSupportedException();

                public void Dispose()
                {
                }
            }
        }

        #endregion

        /// <summary>
        /// The serializable class to express hashset.
        /// using array will make prefab modifications too big so I made this class
        /// </summary>
        /// <typeparam name="T"></typeparam>
        [Serializable]
        public class Objects<T, TLayer> : ISerializationCallbackReceiver where TLayer : PrefabLayer<T>
        {
            private readonly Object _outerObject;
            [SerializeField, HideInInspector] internal T fakeSlot;
            [SerializeField] internal T[] mainSet = Array.Empty<T>();
            [SerializeField] internal TLayer[] prefabLayers = Array.Empty<TLayer>();

            // for nestCount == 0, _checkedCurrentLayerAdditions is used
            private T[] _checkedCurrentLayerRemoves;
            private T[] _checkedCurrentLayerAdditions;

            protected Objects(Object outerObject)
            {
                if (!outerObject) throw new ArgumentNullException(nameof(outerObject));
                _outerObject = outerObject;
            }

            public HashSet<T> GetAsSet()
            {
                var result = new HashSet<T>(mainSet);
                foreach (var layer in prefabLayers)
                    layer.ApplyTo(result);
                return result;
            }

            public void OnBeforeSerialize()
            {
                fakeSlot = default;
                if (!_outerObject) return;

                // match prefabLayers count.
                var nestCount = PrefabNestCount(_outerObject);

                if (prefabLayers.Length == nestCount)
                {
                    // check static contracts
                    void DistinctCheckArray(ref T[] source, ref T[] checkedArray, Func<T, bool> filter)
                    {
                        if (checkedArray == source && source.All(filter)) return;
                        var array = source.Distinct().Where(filter).ToArray();
                        if (array.Length != source.Length)
                            source = array;
                        checkedArray = source;
                    }

                    if (nestCount == 0)
                    {
                        DistinctCheckArray(ref mainSet, ref _checkedCurrentLayerAdditions, x => x != null);
                    }
                    else
                    {
                        var currentLayer = prefabLayers[nestCount - 1];
                        DistinctCheckArray(ref currentLayer.additions, ref _checkedCurrentLayerAdditions,
                            x => x != null);
                        DistinctCheckArray(ref currentLayer.removes, ref _checkedCurrentLayerRemoves,
                            x => x != null && !currentLayer.additions.Contains(x));
                    }

                    return;
                }

                if (prefabLayers.Length > nestCount)
                {
                    // after apply modifications?: apply to latest layer
                    if (nestCount == 0)
                    {
                        // nestCount is 0: apply everything to mainSet
                        var result = new ListSet<T>(mainSet);
                        foreach (var layer in prefabLayers)
                        {
                            result.RemoveRange(layer.removes);
                            result.AddRange(layer.additions);
                        }

                        mainSet = result.ToArray();
                        prefabLayers = Array.Empty<TLayer>();
                    }
                    else
                    {
                        // nestCount is not zero: apply to latest layer
                        var targetLayer = prefabLayers[nestCount - 1];
                        var additions = new ListSet<T>(targetLayer.additions);
                        var removes = new ListSet<T>(targetLayer.removes);

                        foreach (var layer in prefabLayers.Skip(nestCount))
                        {
                            additions.RemoveRange(layer.removes);
                            removes.AddRange(layer.removes);

                            removes.RemoveRange(layer.additions);
                            additions.AddRange(layer.additions);
                        }

                        targetLayer.additions = additions.ToArray();
                        targetLayer.removes = removes.ToArray();

                        // resize array.                        
                        var src = prefabLayers;
                        prefabLayers = new TLayer[nestCount];
                        for (var i = 0; i < nestCount; i++)
                            prefabLayers[i] = src[i];
                    }

                    return;
                }

                if (prefabLayers.Length < nestCount)
                {
                    // resize array
                    // resize array.                        
                    var src = prefabLayers;
                    prefabLayers = new TLayer[nestCount];
                    for (var i = 0; i < src.Length; i++)
                        prefabLayers[i] = src[i];

                    return;
                }
            }

            public void OnAfterDeserialize()
            {
                // there's nothing to do after deserialization.
            }
        }

        [Serializable]
        public abstract class PrefabLayer<T>
        {
            // if some value is in both removes and additions, the values should be added
            [SerializeField] internal T[] removes = Array.Empty<T>();
            [SerializeField] internal T[] additions = Array.Empty<T>();

            public void ApplyTo(HashSet<T> result)
            {
                foreach (var remove in removes)
                    result.Remove(remove);
                foreach (var addition in additions)
                    result.Add(addition);
            }
        }

        private static class EditorStatics
        {
            public static GUIContent MultiEditingNotSupported = new GUIContent("Multi editing not supported");

            public static GUIContent ToAdd = new GUIContent("Element to add")
            {
                tooltip = "Drag & Drop value to here to add element to this set."
            };

            public static GUIContent ForceAddButton = new GUIContent("+")
            {
                tooltip = "Add this element in current prefab modifications."
            };
        }

        [CustomPropertyDrawer(typeof(Objects<,>), true)]
        private class ObjectsEditor : PropertyDrawer
        {
            public ObjectsEditor()
            {
                Debug.Log("ObjectsEditor Constructor");
            }

            private int _nestCountCache = -1;

            private int GetNestCount(Object obj) =>
                _nestCountCache != -1 ? _nestCountCache : _nestCountCache = PrefabNestCount(obj);

            private Type _typeCache;

            private readonly Dictionary<string, PrefabSafeSetPrefabEditor> _caches =
                new Dictionary<string, PrefabSafeSetPrefabEditor>();

            private PrefabSafeSetPrefabEditor GetCache(SerializedProperty property)
            {
                if (!_caches.TryGetValue(property.propertyPath, out var cached))
                {
                    _caches[property.propertyPath] = cached =
                        new PrefabSafeSetPrefabEditor(property, GetNestCount(property.serializedObject.targetObject));
                }

                return cached;
            }

            class PrefabSafeSetPrefabEditor : PrefabSafeSetPrefabEditorBase<Object>
            {
                public PrefabSafeSetPrefabEditor(SerializedProperty property, int nestCount) : base(property, nestCount)
                {
                }

                private protected override Object GetValue(SerializedProperty prop) => prop.objectReferenceValue;

                private protected override void SetValue(SerializedProperty prop, Object value) =>
                    prop.objectReferenceValue = value;

                private protected override float GetAddRegionSize() => EditorGUIUtility.singleLineHeight;

                private protected override void OnGUIAddRegion(Rect position)
                {
                    var addValue = Field(position, EditorStatics.ToAdd, default);
                    if (addValue != null) EditorUtil.AddValue(addValue);
                }
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                if (property.serializedObject.isEditingMultipleObjects || !property.isExpanded)
                    return EditorGUIUtility.singleLineHeight;

                return GetCache(property).GetPropertyHeight() + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                if (property.serializedObject.isEditingMultipleObjects)
                {
                    EditorGUI.LabelField(position, label, EditorStatics.MultiEditingNotSupported);
                    return;
                }

                position.height = EditorGUIUtility.singleLineHeight;

                property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label);

                if (property.isExpanded)
                {
                    position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    GetCache(property).OnGUI(position);
                }
            }
        }

        abstract class PrefabSafeSetPrefabEditorBase<T>
        {
            private readonly SerializedProperty _fakeSlot;
            protected readonly EditorUtil<T> EditorUtil;
            // ReSharper disable VirtualMemberCallInConstructor
            public PrefabSafeSetPrefabEditorBase(SerializedProperty property, int nestCount)
            {
                _fakeSlot = property.FindPropertyRelative(nameof(Objects<Object, PrefabLayer<Object>>.fakeSlot));
                EditorUtil = EditorUtil<T>.Create(property, nestCount, GetValue, SetValue);
            }

            private protected abstract T GetValue(SerializedProperty prop);
            private protected abstract void SetValue(SerializedProperty prop, T value);
            private protected abstract float GetAddRegionSize();
            private protected abstract void OnGUIAddRegion(Rect position);
            
            // ReSharper disable StaticMemberInGenericType
            private static readonly float LineHeight =
                EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            // ReSharper restore StaticMemberInGenericType

            public float GetPropertyHeight() =>
                EditorUtil.ElementsCount * LineHeight + GetAddRegionSize();

            // position is 
            public void OnGUI(Rect position)
            {
                var elementI = 0;
                var newLabel = new GUIContent("");

                // to avoid changes in for loop
                var modKind = ModificationKind.Natural;
                T modValue = default;

                foreach (var element in EditorUtil.Elements)
                {
                    position.y += LineHeight;
                    ModificationKind fieldModKind;

                    switch (element.Status)
                    {
                        case ElementStatus.Natural:
                            newLabel.text = $"Element {elementI++}";
                            fieldModKind = ModificationKind.Natural;
                            break;
                        case ElementStatus.Removed:
                            newLabel.text = "(Removed)";
                            fieldModKind = ModificationKind.Remove;
                            break;
                        case ElementStatus.NewElement:
                            newLabel.text = $"Element {elementI++}";
                            fieldModKind = ModificationKind.Add;
                            break;
                        case ElementStatus.AddedTwice:
                            newLabel.text = $"Element {elementI++} (Added twice)";
                            fieldModKind = ModificationKind.Add;
                            break;
                        case ElementStatus.FakeRemoved:
                            newLabel.text = "(Removed but not found)";
                            fieldModKind = ModificationKind.Remove;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    var currentModKind = OnePrefabElement(position, newLabel, element.Value, fieldModKind, element.ModifierProp);
                    if (currentModKind != ModificationKind.Natural)
                        (modKind, modValue) = (currentModKind, element.Value);
                }

                switch (modKind)
                {
                    case ModificationKind.Natural:
                        break;
                    case ModificationKind.Remove:
                        EditorUtil.AddValue(modValue);
                        break;
                    case ModificationKind.Add:
                        EditorUtil.RemoveValue(modValue);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                position.y += LineHeight;

                OnGUIAddRegion(position);
            }

            private enum ModificationKind
            {
                Natural,
                Add,
                Remove
            }

            private ModificationKind OnePrefabElement(Rect position, GUIContent label, T value, ModificationKind kind,
                SerializedProperty modifierProp)
            {
                // layout
                var fieldPosition = position;
                // two buttons
                fieldPosition.width -= EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                var addButtonPosition = new Rect(
                    fieldPosition.xMax + EditorGUIUtility.standardVerticalSpacing, position.y,
                    EditorGUIUtility.singleLineHeight, position.height);

                var result = ModificationKind.Natural;

                EditorGUI.BeginDisabledGroup(kind == ModificationKind.Remove);
                if (modifierProp != null) EditorGUI.BeginProperty(fieldPosition, label, modifierProp);
                // field
                var fieldValue = Field(fieldPosition, label, value);
                if (modifierProp != null) EditorGUI.EndProperty();
                if (fieldValue == null)
                    result = ModificationKind.Remove;

                EditorGUI.BeginDisabledGroup(kind == ModificationKind.Add);
                if (GUI.Button(addButtonPosition, EditorStatics.ForceAddButton))
                    result = ModificationKind.Add;
                EditorGUI.EndDisabledGroup();

                EditorGUI.EndDisabledGroup();

                return result;
            }

            protected T Field(Rect position, GUIContent label, T value)
            {
                SetValue(_fakeSlot, value);
                EditorGUI.PropertyField(position, _fakeSlot, label);
                value = GetValue(_fakeSlot);
                return value;
            }
        }

        public abstract class EditorUtil<T>
        {
            // common property;
            [NotNull] private readonly Func<SerializedProperty, T> GetValue;
            [NotNull] private readonly Action<SerializedProperty, T> SetValue;
            public abstract IEnumerable<Element> Elements { get; }
            public abstract int ElementsCount { get; }

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
                GetValue = getValue ?? throw new ArgumentNullException(nameof(getValue));
                SetValue = setValue ?? throw new ArgumentNullException(nameof(setValue));
            }

            public abstract void AddValue(T addValue);

            public abstract void RemoveValue(T value);

            private sealed class Root : EditorUtil<T>
            {
                [NotNull] private readonly SerializedProperty _mainSet;

                public Root(SerializedProperty property, Func<SerializedProperty, T> getValue,
                    Action<SerializedProperty, T> setValue) : base(getValue, setValue)
                {
                    _mainSet = property.FindPropertyRelative(nameof(Objects<Object, PrefabLayer<Object>>.mainSet))
                               ?? throw new ArgumentException("mainSet not found", nameof(property));
                }

                public override IEnumerable<Element> Elements => new ArrayPropertyEnumerable(_mainSet)
                    .Select(x => new Element(GetValue(x)));

                public override int ElementsCount => _mainSet.arraySize;

                public override void AddValue(T addValue)
                {
                    foreach (var prop in new ArrayPropertyEnumerable(_mainSet))
                        if (GetValue(prop).Equals(addValue))
                            return;
                    SetValue(AddArrayElement(_mainSet), addValue);
                }

                public override void RemoveValue(T value)
                {
                    for (var i = 0; i < _mainSet.arraySize; i++)
                    {
                        if (GetValue(_mainSet.GetArrayElementAtIndex(i)).Equals(value))
                        {
                            RemoveArrayElementAt(_mainSet, i);
                            return;
                        }
                    }
                }
            }

            private sealed class PrefabModification : EditorUtil<T>
            {
                private readonly List<Element> _elements;
                private readonly int _upstreamElementCount;
                private readonly SerializedProperty _currentRemoves;
                private readonly SerializedProperty _currentAdditions;
                private int _currentRemovesSize;
                private int _currentAdditionsSize;

                public PrefabModification(SerializedProperty property, int nestCount,
                    Func<SerializedProperty, T> getValue, Action<SerializedProperty, T> setValue) : base(getValue,
                    setValue)
                {
                    _elements = new List<Element>();
                    var upstreamValues = new HashSet<T>();
                    var mainSet = property.FindPropertyRelative(nameof(Objects<Object, PrefabLayer<Object>>.mainSet));
                    foreach (var valueProp in new ArrayPropertyEnumerable(mainSet))
                    {
                        var value = GetValue(valueProp);
                        if (value == null) continue;
                        if (upstreamValues.Contains(value)) continue;
                        upstreamValues.Add(value);
                        _elements.Add(new Element(value));
                    }

                    // apply modifications until previous one
                    var prefabLayers =
                        property.FindPropertyRelative(nameof(Objects<Object, PrefabLayer<Object>>.prefabLayers));
                    foreach (var layer in new ArrayPropertyEnumerable(prefabLayers).Take(nestCount - 1))
                    {
                        var removes = layer.FindPropertyRelative(nameof(PrefabLayer<Object>.removes));
                        var additions = layer.FindPropertyRelative(nameof(PrefabLayer<Object>.additions));

                        foreach (var prop in new ArrayPropertyEnumerable(removes))
                        {
                            var value = GetValue(prop);
                            if (upstreamValues.Remove(value))
                                _elements.RemoveAll(x => x.Value.Equals(value));
                        }

                        foreach (var prop in new ArrayPropertyEnumerable(additions))
                        {
                            var value = GetValue(prop);
                            if (upstreamValues.Add(value))
                                _elements.Add(new Element(value));
                        }
                    }

                    _upstreamElementCount = _elements.Count;

                    // process current layer
                    if (prefabLayers.arraySize < nestCount) prefabLayers.arraySize = nestCount;

                    var currentLayer = prefabLayers.GetArrayElementAtIndex(nestCount - 1);
                    _currentRemoves = currentLayer.FindPropertyRelative(nameof(PrefabLayer<Object>.removes))
                                      ?? throw new ArgumentException("prefabLayers.removes not found",
                                          nameof(property));
                    _currentAdditions = currentLayer.FindPropertyRelative(nameof(PrefabLayer<Object>.additions))
                                        ?? throw new ArgumentException("prefabLayers.additions not found",
                                            nameof(property));

                    DoInitialize();
                }

                public override IEnumerable<Element> Elements => _elements;
                public override int ElementsCount => _elements.Count;

                /// <summary>
                /// initialize or update cache info if needed
                /// </summary>
                public void Initialize()
                {
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
                            element.ChangeState(ElementStatus.Removed, _currentRemoves, index);
                        }
                        else if (addsSet.Remove(value))
                        {
                            var index = Array.IndexOf(removesArray, value);
                            element.ChangeState(ElementStatus.AddedTwice, _currentAdditions, index);
                        }
                        else
                        {
                            element.ChangeStateToNatural();
                        }

                        _elements[i] = element;
                    }

                    // newly added elements
                    for (var i = 0; i < additionsArray.Length; i++)
                    {
                        var value = additionsArray[i];
                        if (!addsSet.Contains(value)) continue; // it's duplicated addition

                        _elements.Add(new Element(value, ElementStatus.NewElement, _currentAdditions, i));
                    }

                    // fake removed elements
                    for (var i = 0; i < removesArray.Length; i++)
                    {
                        var value = removesArray[i];
                        if (!removesSet.Contains(value)) continue; // it's removed upper layer

                        _elements.Add(new Element(value, ElementStatus.FakeRemoved, _currentRemoves, i));
                    }

                    _currentRemovesSize = _currentRemoves.arraySize;
                    _currentAdditionsSize = _currentAdditions.arraySize;
                }

                public override void AddValue(T addValue)
                {
                    Initialize();
                    var index = _elements.FindIndex(x => x.Value.Equals(addValue));
                    if (index == -1)
                    {
                        // not on list: just add
                        SetValue(AddArrayElement(_currentAdditions), addValue);
                    }
                    else
                    {
                        var element = _elements[index];
                        switch (element.Status)
                        {
                            case ElementStatus.Natural:
                                SetValue(AddArrayElement(_currentAdditions), element.Value);
                                break;
                            case ElementStatus.Removed:
                                RemoveArrayElementAt(_currentRemoves, element.IndexInModifierArray);
                                break;
                            case ElementStatus.NewElement:
                            case ElementStatus.AddedTwice:
                                // already added: nothing to do
                                break;
                            case ElementStatus.FakeRemoved:
                                RemoveArrayElementAt(_currentRemoves, element.IndexInModifierArray);
                                SetValue(AddArrayElement(_currentAdditions), element.Value);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }

                public override void RemoveValue(T value)
                {
                    Initialize();
                    var index = _elements.FindIndex(x => x.Value.Equals(value));
                    if (index == -1)
                    {
                        // not found in the set: add fake removes
                        SetValue(AddArrayElement(_currentRemoves), value);
                    }
                    else
                    {
                        var element = _elements[index];
                        switch (element.Status)
                        {
                            case ElementStatus.Natural:
                                SetValue(AddArrayElement(_currentRemoves), element.Value);
                                break;
                            case ElementStatus.Removed:
                            case ElementStatus.FakeRemoved:
                                // already removed: nothing to do
                                break;
                            case ElementStatus.NewElement:
                                RemoveArrayElementAt(_currentAdditions, element.IndexInModifierArray);
                                break;
                            case ElementStatus.AddedTwice:
                                RemoveArrayElementAt(_currentAdditions, element.IndexInModifierArray);
                                SetValue(AddArrayElement(_currentRemoves), element.Value);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }

            public struct Element
            {
                public T Value { get; }
                public ElementStatus Status { get; private set; }
                internal SerializedProperty ModifierProp;
                internal int IndexInModifierArray;

                internal Element(T value)
                {
                    if (value == null) throw new ArgumentNullException(nameof(value));
                    Value = value;
                    Status = ElementStatus.Natural;
                    ModifierProp = null;
                    IndexInModifierArray = 0;
                }

                internal Element(T value, ElementStatus status, SerializedProperty arrayProp, int index) : this(value)
                {
                    if (status == ElementStatus.Natural) throw new ArgumentException("Use value only ctor", nameof(status));
                    ChangeState(status, arrayProp, index);
                }

                internal void ChangeState(ElementStatus status, SerializedProperty arrayProp, int index)
                {
                    if (status == ElementStatus.Natural)
                        throw new ArgumentException("Use ChangeStateToNatural");
                    if (arrayProp == null) throw new ArgumentNullException(nameof(arrayProp));
                    Status = status;
                    ModifierProp = arrayProp.GetArrayElementAtIndex(index) ?? 
                                   throw new ArgumentException("element not found", nameof(arrayProp));
                    IndexInModifierArray = index;
                }

                internal void ChangeStateToNatural()
                {
                    Status = ElementStatus.Natural;
                    ModifierProp = null;
                    IndexInModifierArray = 0;
                }
            }

            private static SerializedProperty AddArrayElement(SerializedProperty array)
            {
                array.arraySize += 1;
                return array.GetArrayElementAtIndex(array.arraySize - 1);
            }

            private static void RemoveArrayElementAt(SerializedProperty array, int index)
            {
                var prevProp = array.GetArrayElementAtIndex(index);
                for (var i = index + 1; i < array.arraySize; i++)
                {
                    var curProp = array.GetArrayElementAtIndex(i);
                    prevProp.objectReferenceValue = curProp.objectReferenceValue;
                    prevProp = curProp;
                }

                array.arraySize -= 1;
            }

            private T[] ToArray(SerializedProperty array)
            {
                var result = new T[array.arraySize];
                for (var i = 0; i < result.Length; i++)
                    result[i] = GetValue(array.GetArrayElementAtIndex(i));
                return result;
            }
        }

        public enum ElementStatus
        {
            Natural,
            Removed,
            NewElement,
            AddedTwice,
            FakeRemoved,
        }
    }
}
