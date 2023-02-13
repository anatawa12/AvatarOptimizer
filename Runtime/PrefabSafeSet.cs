using System;
using System.Collections.Generic;
using System.Linq;
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

        private readonly struct ArrayPropertyEnumerable
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

            public struct Enumerator
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

                public bool MoveNext()
                {
                    _index++;
                    return _index < _size;
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

            public static GUIContent RemoveButton = new GUIContent("X")
            {
                tooltip = "Remove Content"
            };

            public static GUIContent ForceAddButton = new GUIContent("+")
            {
                tooltip = "Add this element in current prefab modifications."
            };

            public static GUIContent Restore = new GUIContent("+")
            {
                tooltip = "Restore removed element"
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
                    if (addValue != null) DoAddValue(addValue);
                }
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                if (property.serializedObject.isEditingMultipleObjects || !property.isExpanded)
                    return EditorGUIUtility.singleLineHeight;

                return GetCache(property).GetPropertyHeight();
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
                    GetCache(property).OnGUI(position);
            }
        }

        abstract class PrefabSafeSetPrefabEditorBase<T>
        {
            // common property
            private readonly SerializedProperty _fakeSlot;

            #region prefab overrides

            private readonly List<Element> _elements;
            private readonly int _upstreamElementCount;
            private readonly SerializedProperty _currentRemoves;
            private readonly SerializedProperty _currentAdditions;
            private int _currentRemovesSize;
            private int _currentAdditionsSize;

            #endregion

            #region root object

            private readonly SerializedProperty _mainSet;

            #endregion

            // ReSharper disable StaticMemberInGenericType
            private static readonly float LineHeight =
                EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            private static readonly float ConstantHeightForExpanded =
                EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            // ReSharper restore StaticMemberInGenericType

            private protected abstract T GetValue(SerializedProperty prop);
            private protected abstract void SetValue(SerializedProperty prop, T value);
            private protected abstract float GetAddRegionSize();
            private protected abstract void OnGUIAddRegion(Rect position);

            // ReSharper disable VirtualMemberCallInConstructor
            public PrefabSafeSetPrefabEditorBase(SerializedProperty property, int nestCount)
            {
                _fakeSlot = property.FindPropertyRelative(nameof(Objects<Object, PrefabLayer<Object>>.fakeSlot));

                if (nestCount == 0)
                {
                    _mainSet = property.FindPropertyRelative(nameof(Objects<Object, PrefabLayer<Object>>.mainSet));
                }
                else
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
                    _currentRemoves = currentLayer.FindPropertyRelative(nameof(PrefabLayer<Object>.removes));
                    _currentAdditions = currentLayer.FindPropertyRelative(nameof(PrefabLayer<Object>.additions));
                }

                DoInitialize();
            }
            // ReSharper restore VirtualMemberCallInConstructor

            public void Initialize()
            {
                if (_mainSet != null) return;
                if (_currentRemovesSize != _currentRemoves.arraySize ||
                    _currentAdditionsSize != _currentAdditions.arraySize)
                {
                    DoInitialize();
                }
            }

            public void DoInitialize()
            {
                if (_mainSet != null) return;
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
                        element.Status = Status.Removed;
                        var index = Array.IndexOf(removesArray, value);
                        element.IndexInModifierArray = index;
                        element.ModifierProp = _currentRemoves.GetArrayElementAtIndex(index);
                    }
                    else if (addsSet.Remove(value))
                    {
                        element.Status = Status.AddedTwice;
                        var index = Array.IndexOf(removesArray, value);
                        element.IndexInModifierArray = index;
                        element.ModifierProp = _currentAdditions.GetArrayElementAtIndex(index);
                    }
                    else
                    {
                        element.Status = Status.Natural;
                        element.ModifierProp = null;
                    }

                    _elements[i] = element;
                }

                // newly added elements
                for (var i = 0; i < additionsArray.Length; i++)
                {
                    var value = additionsArray[i];
                    if (!addsSet.Contains(value)) continue; // it's duplicated addition

                    _elements.Add(new Element(value, Status.NewElement,
                        _currentAdditions.GetArrayElementAtIndex(i), i));
                }

                // fake removed elements
                for (var i = 0; i < removesArray.Length; i++)
                {
                    var value = removesArray[i];
                    if (!removesSet.Contains(value)) continue; // it's removed upper layer

                    _elements.Add(new Element(value, Status.FakeRemoved,
                        _currentRemoves.GetArrayElementAtIndex(i), i));
                }

                _currentRemovesSize = _currentRemoves.arraySize;
                _currentAdditionsSize = _currentAdditions.arraySize;
            }

            public float GetPropertyHeight()
            {
                Initialize();
                return (_mainSet?.arraySize ?? _elements.Count) * LineHeight +
                       ConstantHeightForExpanded + GetAddRegionSize();
            }

            // position is 
            public void OnGUI(Rect position)
            {
                Initialize();

                if (_mainSet != null)
                {
                    var newLabel = new GUIContent("");

                    for (var i = 0; i < _mainSet.arraySize; i++)
                    {
                        var prop = _mainSet.GetArrayElementAtIndex(i);
                        newLabel.text = $"Element {i}";

                        position.y += LineHeight;

                        EditorGUI.PropertyField(position, prop, newLabel);
                    }

                    // add element field
                    position.y += LineHeight;
                }
                else
                {
                    var elementI = 0;
                    var newLabel = new GUIContent("");

                    // to avoid changes in for loop
                    var removeIndexInAdditions = -1;

                    foreach (var element in _elements)
                    {
                        position.y += LineHeight;
                        switch (element.Status)
                        {
                            case Status.Natural:
                                newLabel.text = $"Element {elementI++}";

                                switch (OnePrefabElement(position, newLabel, element.Value, false, false))
                                {
                                    case OneElementResult.Nothing:
                                        break;
                                    case OneElementResult.Removed:
                                        SetValue(AddArrayElement(_currentRemoves), element.Value);
                                        break;
                                    case OneElementResult.Added:
                                        SetValue(AddArrayElement(_currentRemoves), element.Value);
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }

                                break;
                            case Status.Removed:
                                newLabel.text = "(Removed)";
                                EditorGUI.BeginProperty(position, newLabel, element.ModifierProp);
                                OnePrefabElement(position, newLabel, element.Value, false, true);
                                EditorGUI.EndProperty();
                                break;
                            case Status.NewElement:
                                newLabel.text = $"Element {elementI++}";
                                EditorGUI.BeginProperty(position, newLabel, element.ModifierProp);
                                switch (OnePrefabElement(position, newLabel, element.Value, true, false))
                                {
                                    case OneElementResult.Nothing:
                                        break;
                                    case OneElementResult.Removed:
                                        removeIndexInAdditions = element.IndexInModifierArray;
                                        break;
                                    case OneElementResult.Added:
                                        // Unreachable
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }

                                EditorGUI.EndProperty();
                                break;
                            case Status.AddedTwice:
                                newLabel.text = $"Element {elementI++} (Added twice)";

                                EditorGUI.BeginProperty(position, newLabel, element.ModifierProp);
                                switch (OnePrefabElement(position, newLabel, element.Value, true, false))
                                {
                                    case OneElementResult.Nothing:
                                        break;
                                    case OneElementResult.Removed:
                                        removeIndexInAdditions = element.IndexInModifierArray;
                                        SetValue(AddArrayElement(_currentRemoves), element.Value);
                                        break;
                                    case OneElementResult.Added:
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }

                                EditorGUI.EndProperty();
                                break;
                            case Status.FakeRemoved:
                                newLabel.text = "(Removed but not found)";
                                EditorGUI.BeginProperty(position, newLabel, element.ModifierProp);
                                OnePrefabElement(position, newLabel, element.Value, false, true);
                                EditorGUI.EndProperty();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    if (removeIndexInAdditions != -1)
                        RemoveArrayElementAt(_currentRemoves, removeIndexInAdditions);

                    position.y += LineHeight;
                }

                OnGUIAddRegion(position);
            }


            private OneElementResult OnePrefabElement(Rect position, GUIContent label, T value, bool added,
                bool removed)
            {
                // layout
                var fieldPosition = position;
                // two buttons
                fieldPosition.width -= EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                var addButtonPosition = new Rect(
                    fieldPosition.xMax + EditorGUIUtility.standardVerticalSpacing, position.y,
                    EditorGUIUtility.singleLineHeight, position.height);

                var result = OneElementResult.Nothing;

                EditorGUI.BeginDisabledGroup(removed);
                // field
                var fieldValue = Field(fieldPosition, label, value);
                if (fieldValue == null)
                    result = OneElementResult.Removed;

                EditorGUI.BeginDisabledGroup(added);
                if (GUI.Button(addButtonPosition, removed ? EditorStatics.Restore : EditorStatics.ForceAddButton))
                    result = OneElementResult.Added;
                EditorGUI.EndDisabledGroup();

                EditorGUI.EndDisabledGroup();

                return result;
            }

            protected void DoAddValue(T addValue)
            {
                if (_mainSet != null)
                {
                    foreach (var prop in new ArrayPropertyEnumerable(_mainSet))
                        if (GetValue(prop).Equals(addValue))
                            return;
                    SetValue(AddArrayElement(_mainSet), addValue);
                }

                else
                {
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
                            case Status.Natural:
                                SetValue(AddArrayElement(_currentAdditions), element.Value);
                                break;
                            case Status.Removed:
                                RemoveArrayElementAt(_currentRemoves, element.IndexInModifierArray);
                                break;
                            case Status.NewElement:
                            case Status.AddedTwice:
                                // already added: nothing to do
                                break;
                            case Status.FakeRemoved:
                                RemoveArrayElementAt(_currentRemoves, element.IndexInModifierArray);
                                SetValue(AddArrayElement(_currentAdditions), element.Value);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }

            protected T Field(Rect position, GUIContent label, T value)
            {
                SetValue(_fakeSlot, value);
                EditorGUI.PropertyField(position, _fakeSlot, label);
                value = GetValue(_fakeSlot);
                return value;
            }

            struct Element
            {
                public readonly T Value;
                public Status Status;
                public SerializedProperty ModifierProp;
                public int IndexInModifierArray;

                public Element(T value)
                {
                    Value = value;
                    Status = Status.Natural;
                    ModifierProp = null;
                    IndexInModifierArray = 0;
                }

                public Element(T value, Status status, SerializedProperty modifierProp, int index)
                {
                    if (value == null) throw new ArgumentNullException(nameof(value));
                    Value = value;
                    Status = status;
                    ModifierProp = modifierProp ?? throw new ArgumentNullException(nameof(modifierProp));
                    IndexInModifierArray = index;
                }
            }

            enum Status
            {
                Natural,
                Removed,
                NewElement,
                AddedTwice,
                FakeRemoved,
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

            enum OneElementResult
            {
                Nothing,
                Removed,
                Added,
            }
        }
    }
}
