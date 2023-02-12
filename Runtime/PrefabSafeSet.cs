using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
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
        public class Objects<T, TLayer> : ISerializationCallbackReceiver where T : Object where TLayer : PrefabLayer<T>
        {
            private readonly Object _outerObject;
            [SerializeField] internal T[] mainSet = Array.Empty<T>();
            [SerializeField] internal TLayer[] prefabLayers = Array.Empty<TLayer>();

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
                if (!_outerObject) return;

                // match prefabLayers count.
                var nestCount = PrefabNestCount(_outerObject);

                if (prefabLayers.Length == nestCount) return; // nothing to do

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
        public abstract class PrefabLayer<T> where T : Object
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
            public static GUIContent ToAdd = new GUIContent("Element to add");
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
            private static readonly float LineHeight =
                EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // heading line and fold 
            private static readonly float ConstantHeightForExpanded =
                EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;

            private void CollectPrevLayerValues(ListSet<Object> resultSet, SerializedProperty property, int nestCount)
            {
                var mainSet = property.FindPropertyRelative(nameof(Objects<Object, PrefabLayer<Object>>.mainSet));
                for (var i = 0; i < mainSet.arraySize; i++)
                    resultSet.Add(mainSet.GetArrayElementAtIndex(i).objectReferenceValue);

                // apply modifications until previous one
                var prefabLayers = property.FindPropertyRelative(nameof(Objects<Object, PrefabLayer<Object>>.prefabLayers));
                foreach (var layer in new ArrayPropertyEnumerable(prefabLayers).Take(nestCount - 1))
                {
                    var removes = layer.FindPropertyRelative(nameof(PrefabLayer<Object>.removes));
                    var additions = layer.FindPropertyRelative(nameof(PrefabLayer<Object>.additions));

                    foreach (var prop in new ArrayPropertyEnumerable(removes))
                        resultSet.Remove(prop.objectReferenceValue);
                    
                    foreach (var prop in new ArrayPropertyEnumerable(additions))
                        resultSet.Add(prop.objectReferenceValue);
                }
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                if (property.serializedObject.isEditingMultipleObjects)
                    return EditorGUIUtility.singleLineHeight;
                
                if (!property.isExpanded) return EditorGUIUtility.singleLineHeight;

                var nestCount = PrefabNestCount(property.serializedObject.targetObject);

                if (nestCount == 0)
                {
                    return property.FindPropertyRelative(nameof(Objects<Object, PrefabLayer<Object>>.mainSet)).arraySize * LineHeight 
                           + ConstantHeightForExpanded;
                }

                var resultSet = new ListSet<Object>(true);
                CollectPrevLayerValues(resultSet, property, nestCount); 

                // apply modifications until previous one
                var prefabLayers = property.FindPropertyRelative(nameof(Objects<Object, PrefabLayer<Object>>.prefabLayers));

                var layer = prefabLayers.GetArrayElementAtIndex(nestCount - 1);
                if (layer != null) {
                    var removes = layer.FindPropertyRelative(nameof(PrefabLayer<Object>.removes));
                    var additions = layer.FindPropertyRelative(nameof(PrefabLayer<Object>.additions));

                    foreach (var prop in new ArrayPropertyEnumerable(removes))
                        resultSet.Add(prop.objectReferenceValue);

                    foreach (var prop in new ArrayPropertyEnumerable(additions))
                        resultSet.Add(prop.objectReferenceValue);
                }

                return resultSet.Count * LineHeight + ConstantHeightForExpanded;
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
                    var nestCount = PrefabNestCount(property.serializedObject.targetObject);

                    if (nestCount == 0)
                    {
                        // simple set
                        var mainSet = property.FindPropertyRelative(nameof(Objects<Object, PrefabLayer<Object>>.mainSet));

                        var newLabel = new GUIContent("");
                        var toRemoveIndex = -1;

                        for (var i = 0; i < mainSet.arraySize; i++)
                        {
                            var prop = mainSet.GetArrayElementAtIndex(i);
                            newLabel.text = $"Element {i}";
                            
                            position.y += LineHeight;

                            switch (OneElement(position, newLabel, prop))
                            {
                                case OneElementResult.Nothing:
                                    break;
                                case OneElementResult.Removed:
                                    toRemoveIndex = i;
                                    break;
                                case OneElementResult.Added:
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }

                        if (toRemoveIndex != -1)
                            RemoveArrayElementAt(mainSet, toRemoveIndex);

                        // add element field
                        position.y += LineHeight;
                        var value = Field(position, EditorStatics.ToAdd, null);
                        if (value != null)
                        {
                            var found = false;
                            foreach (var prop in new ArrayPropertyEnumerable(mainSet))
                                // ReSharper disable once AssignmentInConditionalExpression
                                if (found |= prop.objectReferenceValue.Equals(value))
                                    break;
                            if (!found)
                            {
                                mainSet.arraySize += 1;
                                mainSet.GetArrayElementAtIndex(mainSet.arraySize - 1).objectReferenceValue = value;
                            }
                        }
                    }
                    else
                    {
                        var resultSet = new ListSet<Object>(false);
                        CollectPrevLayerValues(resultSet, property, nestCount);
                        
                        var prefabLayers = property.FindPropertyRelative(nameof(Objects<Object, PrefabLayer<Object>>.prefabLayers));
                        if (prefabLayers.arraySize < nestCount) prefabLayers.arraySize = nestCount;

                        var layer = prefabLayers.GetArrayElementAtIndex(nestCount - 1);
                        var removes = layer.FindPropertyRelative(nameof(PrefabLayer<Object>.removes));
                        var additions = layer.FindPropertyRelative(nameof(PrefabLayer<Object>.additions));

                        // remove nulls which will be generated by reverting
                        for (var i = 0; i < removes.arraySize; i++)
                            if (!removes.GetArrayElementAtIndex(i).objectReferenceValue)
                                RemoveArrayElementAt(removes, i--);
                        for (var i = 0; i < additions.arraySize; i++)
                            if (!additions.GetArrayElementAtIndex(i).objectReferenceValue)
                                RemoveArrayElementAt(additions, i--);

                        var removesArray = ToArray(removes);
                        var additionsArray = ToArray(additions);

                        var removesSet = new HashSet<Object>(removesArray);
                        var addsSet = new HashSet<Object>(additionsArray);

                        var elementI = 0;
                        var newLabel = new GUIContent("");

                        foreach (var value in resultSet)
                        {
                            position.y += LineHeight;
                            if (removesSet.Remove(value))
                            {
                                newLabel.text = "(Removed)";
                                EditorGUI.BeginProperty(position, label,
                                    removes.GetArrayElementAtIndex(Array.IndexOf(removesArray, value)));
                                OnePrefabElement(position, newLabel, value, false, true);
                                EditorGUI.EndProperty();
                            }
                            else if (addsSet.Remove(value))
                            {
                                newLabel.text = $"Element {elementI++} (Added twice)";

                                EditorGUI.BeginProperty(position, label,
                                    additions.GetArrayElementAtIndex(Array.IndexOf(additionsArray, value)));
                                switch (OnePrefabElement(position, newLabel, value, true, false))
                                {
                                    case OneElementResult.Nothing:
                                        break;
                                    case OneElementResult.Removed:
                                        removes.arraySize += 1;
                                        removes.GetArrayElementAtIndex(removes.arraySize - 1).objectReferenceValue = value;
                                        break;
                                    case OneElementResult.Added:
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }
                                EditorGUI.EndProperty();
                            }
                            else
                            {
                                newLabel.text = $"Element {elementI++}";

                                switch (OnePrefabElement(position, newLabel, value, false, false))
                                {
                                    case OneElementResult.Nothing:
                                        break;
                                    case OneElementResult.Removed:
                                        removes.arraySize += 1;
                                        removes.GetArrayElementAtIndex(removes.arraySize - 1).objectReferenceValue = value;
                                        break;
                                    case OneElementResult.Added:
                                        additions.arraySize += 1;
                                        additions.GetArrayElementAtIndex(additions.arraySize - 1).objectReferenceValue = value;
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }   
                            }
                        }

                        // show added elements
                        for (var i = 0; i < additionsArray.Length; i++)
                        {
                            var value = additionsArray[i];
                            if (!addsSet.Contains(value)) continue; // it's duplicated addition

                            position.y += LineHeight;
                            newLabel.text = $"Element {elementI++}";
                            resultSet.Add(value);

                            EditorGUI.BeginProperty(position, label, additions.GetArrayElementAtIndex(i));
                            switch (OnePrefabElement(position, newLabel, value, true, false))
                            {
                                case OneElementResult.Nothing:
                                    break;
                                case OneElementResult.Removed:
                                    RemoveArrayElementAt(additions, i);
                                    break;
                                case OneElementResult.Added:
                                    // Unreachable
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                            EditorGUI.EndProperty();
                        }

                        // show removed elements
                        for (var i = 0; i < removesArray.Length; i++)
                        {
                            var value = removesArray[i];
                            if (!removesSet.Contains(value)) continue; // it's removed upper layer
                            position.y += LineHeight;
                            newLabel.text = "(Removed)";
                            EditorGUI.BeginProperty(position, label, removes.GetArrayElementAtIndex(i));
                            OnePrefabElement(position, newLabel, value, false, true);
                            EditorGUI.EndProperty();
                        }

                        position.y += LineHeight;

                        var addValue = Field(position, EditorStatics.ToAdd, null);
                        if (addValue != null)
                        {
                            if (!resultSet.Contains(addValue))
                            {
                                additions.arraySize += 1;
                                additions.GetArrayElementAtIndex(additions.arraySize - 1).objectReferenceValue = addValue;                                
                            }
                        }
                    }
                }
            }

            private void RemoveArrayElementAt(SerializedProperty array, int index)
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

            private Object[] ToArray(SerializedProperty array)
            {
                var result = new Object[array.arraySize];
                for (var i = 0; i < result.Length; i++)
                    result[i] = array.GetArrayElementAtIndex(i).objectReferenceValue;
                return result;
            }

            private OneElementResult OneElement(Rect position, GUIContent label, SerializedProperty property)
            {
                // layout
                var fieldPosition = position;
                fieldPosition.width -= EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                var removeButtonPosition = new Rect(
                    fieldPosition.xMax + EditorGUIUtility.standardVerticalSpacing, position.y,
                    EditorGUIUtility.singleLineHeight, position.height);

                // field
                EditorGUI.PropertyField(fieldPosition, property, label);

                // button to remove
                if (GUI.Button(removeButtonPosition, EditorStatics.RemoveButton))
                {
                    return OneElementResult.Removed;
                }

                return OneElementResult.Nothing;
            }

            private OneElementResult OnePrefabElement(Rect position, GUIContent label, Object value, bool added, bool removed)
            {
                // layout
                var fieldPosition = position;
                // two buttons
                fieldPosition.width -= EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                fieldPosition.width -= EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                var addButtonPosition = new Rect(
                    fieldPosition.xMax + EditorGUIUtility.standardVerticalSpacing, position.y,
                    EditorGUIUtility.singleLineHeight, position.height);
                var removeButtonPosition = new Rect(
                    addButtonPosition.xMax + EditorGUIUtility.standardVerticalSpacing, position.y,
                    EditorGUIUtility.singleLineHeight, position.height);

                var result = OneElementResult.Nothing;

                EditorGUI.BeginDisabledGroup(removed);
                // field
                Field(fieldPosition, label, value);

                EditorGUI.BeginDisabledGroup(added);
                if (GUI.Button(addButtonPosition, removed ? EditorStatics.Restore : EditorStatics.ForceAddButton))
                    result = OneElementResult.Added;
                EditorGUI.EndDisabledGroup();

                    // button to remove
                if (GUI.Button(removeButtonPosition, EditorStatics.RemoveButton))
                    result = OneElementResult.Removed;

                EditorGUI.EndDisabledGroup();

                return result;
            }

            private protected Object Field(Rect position, GUIContent label, Object value)
            {
                var type = fieldInfo.FieldType;
                while (!(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Objects<,>)))
                    type = type.BaseType;
                return EditorGUI.ObjectField(position, label, value, type.GetGenericArguments()[0], true);
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
