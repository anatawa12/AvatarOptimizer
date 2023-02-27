using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.PrefabSafeSet
{
    internal static class PrefabSafeSetUtil
    {
        public static int PrefabNestCount(Object instance)
        {
            var nestCount = 0;
            while ((bool)(instance = PrefabUtility.GetCorrespondingObjectFromSource(instance)))
                nestCount++;

            return nestCount;
        }

        public static bool IsNotNull<T>(T arg)
        {
            if (arg == null) return false;
            if (typeof(Object).IsAssignableFrom(typeof(T)))
                return (Object)(object)arg;
            return true;
        }
    }

    [UsedImplicitly] // used by reflection
    internal static class OnBeforeSerializeImpl<T, TLayer> where TLayer : PrefabLayer<T>
    {
        [UsedImplicitly] // used by reflection
        public static void Impl(PrefabSafeSet<T, TLayer> self)
        {
            // fakeSlot must not be modified,
            self.fakeSlot = default;
            if (!self.OuterObject) return;

            // match prefabLayers count.
            var nestCount = PrefabSafeSetUtil.PrefabNestCount(self.OuterObject);

            if (self.prefabLayers.Length == nestCount)
                DistinctCheck(self, nestCount);
            else if (self.prefabLayers.Length < nestCount)
                self.prefabLayers = PrefabSafeSetRuntimeUtil.ResizeArray(self.prefabLayers, nestCount);
            else if (self.prefabLayers.Length > nestCount)
                ApplyModificationsToLatestLayer(self, nestCount);
        }

        private static void DistinctCheck(PrefabSafeSet<T, TLayer> self, int nestCount)
        {
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
                DistinctCheckArray(ref self.mainSet, ref self.CheckedCurrentLayerAdditions, 
                    PrefabSafeSetUtil.IsNotNull);
            }
            else
            {
                var currentLayer = self.prefabLayers[nestCount - 1];
                DistinctCheckArray(ref currentLayer.additions, ref self.CheckedCurrentLayerAdditions,
                    PrefabSafeSetUtil.IsNotNull);
                DistinctCheckArray(ref currentLayer.removes, ref self.CheckedCurrentLayerRemoves,
                    x => PrefabSafeSetUtil.IsNotNull(x) && !currentLayer.additions.Contains(x));
            }
        }

        private static void ApplyModificationsToLatestLayer(PrefabSafeSet<T,TLayer> self, int nestCount)
        {
            // after apply modifications?: apply to latest layer
            if (nestCount == 0)
            {
                // nestCount is 0: apply everything to mainSet
                var result = new ListSet<T>(self.mainSet);
                foreach (var layer in self.prefabLayers)
                {
                    result.RemoveRange(layer.removes);
                    result.AddRange(layer.additions);
                }

                self.mainSet = result.ToArray();
                self.prefabLayers = Array.Empty<TLayer>();
            }
            else
            {
                // nestCount is not zero: apply to current layer
                var targetLayer = self.prefabLayers[nestCount - 1];
                var additions = new ListSet<T>(targetLayer.additions);
                var removes = new ListSet<T>(targetLayer.removes);

                foreach (var layer in self.prefabLayers.Skip(nestCount))
                {
                    additions.RemoveRange(layer.removes);
                    removes.AddRange(layer.removes);

                    additions.AddRange(layer.additions);
                    removes.RemoveRange(layer.additions);
                }

                targetLayer.additions = additions.ToArray();
                targetLayer.removes = removes.ToArray();

                // resize array.               
                self.prefabLayers = PrefabSafeSetRuntimeUtil.ResizeArray(self.prefabLayers, nestCount);
            }
        }
    }

    internal static class EditorStatics
    {
        public static readonly GUIContent MultiEditingNotSupported = new GUIContent("Multi editing not supported");
        public static readonly GUIContent UnsupportedType = new GUIContent("Element type is not supported");
        public static readonly GUIContent AddNotSupported = new GUIContent("Add Not Supported");

        public static readonly GUIContent ToAdd = new GUIContent("Element to add")
        {
            tooltip = "Drag & Drop value to here to add element to this set."
        };

        public static readonly GUIContent ForceAddButton = new GUIContent("+")
        {
            tooltip = "Add this element in current prefab modifications."
        };
    }

    [CustomPropertyDrawer(typeof(PrefabSafeSet<,>), true)]
    internal class ObjectsEditor : PropertyDrawer
    {
        public ObjectsEditor()
        {
            Debug.Log("ObjectsEditor Constructor");
        }

        private int _nestCountCache = -1;

        private int GetNestCount(Object obj) =>
            _nestCountCache != -1 ? _nestCountCache : _nestCountCache = PrefabSafeSetUtil.PrefabNestCount(obj);

        private readonly Dictionary<string, Editor> _caches =
            new Dictionary<string, Editor>();

        [CanBeNull]
        private Editor GetCache(SerializedProperty property)
        {
            if (!_caches.TryGetValue(property.propertyPath, out var cached))
            {
                var prop = property.FindPropertyRelative(Names.FakeSlot);
                if (IsSupportedPropType(prop.propertyType))
                {
                    _caches[property.propertyPath] = cached =
                        new Editor(property, GetNestCount(property.serializedObject.targetObject));
                }
                else
                {
                    _caches[property.propertyPath] = cached = null;
                }
            }

            return cached;
        }

        private static bool IsSupportedPropType(SerializedPropertyType type)
        {
            switch (type)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Boolean:
                case SerializedPropertyType.Float:
                case SerializedPropertyType.String:
                case SerializedPropertyType.Color:
                case SerializedPropertyType.ObjectReference:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.Enum:
                case SerializedPropertyType.Vector2:
                case SerializedPropertyType.Vector3:
                case SerializedPropertyType.Vector4:
                case SerializedPropertyType.Rect:
                case SerializedPropertyType.ArraySize:
                case SerializedPropertyType.Character:
                case SerializedPropertyType.AnimationCurve:
                case SerializedPropertyType.Bounds:
                case SerializedPropertyType.ExposedReference:
                case SerializedPropertyType.Vector2Int:
                case SerializedPropertyType.Vector3Int:
                case SerializedPropertyType.RectInt:
                case SerializedPropertyType.BoundsInt:
                    return true;
                default:
                    return false;
            }
        }

        private class Editor : EditorBase<object>
        {
            public Editor(SerializedProperty property, int nestCount) : base(property, nestCount)
            {
            }

            private protected override object GetValue(SerializedProperty prop)
            {
                switch (prop.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        return prop.intValue;
                    case SerializedPropertyType.Boolean:
                        return prop.boolValue;
                    case SerializedPropertyType.Float:
                        return prop.floatValue;
                    case SerializedPropertyType.String:
                        return prop.stringValue;
                    case SerializedPropertyType.Color:
                        return prop.colorValue;
                    case SerializedPropertyType.ObjectReference:
                        return prop.objectReferenceValue;
                    case SerializedPropertyType.LayerMask:
                        return (LayerMask) prop.intValue;
                    case SerializedPropertyType.Enum:
                        return prop.enumValueIndex;
                    case SerializedPropertyType.Vector2:
                        return prop.vector2Value;
                    case SerializedPropertyType.Vector3:
                        return prop.vector3Value;
                    case SerializedPropertyType.Vector4:
                        return prop.vector4Value;
                    case SerializedPropertyType.Rect:
                        return prop.rectValue;
                    case SerializedPropertyType.ArraySize:
                        return prop.intValue;
                    case SerializedPropertyType.Character:
                        return (char) prop.intValue;
                    case SerializedPropertyType.AnimationCurve:
                        return prop.animationCurveValue;
                    case SerializedPropertyType.Bounds:
                        return prop.boundsValue;
                    case SerializedPropertyType.ExposedReference:
                        return prop.exposedReferenceValue;
                    case SerializedPropertyType.Vector2Int:
                        return prop.vector2IntValue;
                    case SerializedPropertyType.Vector3Int:
                        return prop.vector3IntValue;
                    case SerializedPropertyType.RectInt:
                        return prop.rectIntValue;
                    case SerializedPropertyType.BoundsInt:
                        return prop.boundsIntValue;
                    default:
                        throw new InvalidOperationException();
                }
            }

            private protected override void SetValue(SerializedProperty prop, object value) 
            {
                switch (prop.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        prop.intValue = (int)value;
                        break;
                    case SerializedPropertyType.Boolean:
                        prop.boolValue = (bool)value;
                        break;
                    case SerializedPropertyType.Float:
                        prop.floatValue = (float)value;
                        break;
                    case SerializedPropertyType.String:
                        prop.stringValue = (string)value;
                        break;
                    case SerializedPropertyType.Color:
                        prop.colorValue = (Color)value;
                        break;
                    case SerializedPropertyType.ObjectReference:
                        prop.objectReferenceValue = (Object)value;
                        break;
                    case SerializedPropertyType.LayerMask:
                        prop.intValue = (LayerMask)value;
                        break;
                    case SerializedPropertyType.Enum:
                        prop.enumValueIndex = (int)value;
                        break;
                    case SerializedPropertyType.Vector2:
                        prop.vector2Value = (Vector2)value;
                        break;
                    case SerializedPropertyType.Vector3:
                        prop.vector3Value = (Vector3)value;
                        break;
                    case SerializedPropertyType.Vector4:
                        prop.vector4Value = (Vector4)value;
                        break;
                    case SerializedPropertyType.Rect:
                        prop.rectValue = (Rect)value;
                        break;
                    case SerializedPropertyType.ArraySize:
                        prop.intValue = (int)value;
                        break;
                    case SerializedPropertyType.Character:
                        prop.intValue = (char)value;
                        break;
                    case SerializedPropertyType.AnimationCurve:
                        prop.animationCurveValue = (AnimationCurve)value;
                        break;
                    case SerializedPropertyType.Bounds:
                        prop.boundsValue = (Bounds)value;
                        break;
                    case SerializedPropertyType.ExposedReference:
                        prop.exposedReferenceValue = (Object)value;
                        break;
                    case SerializedPropertyType.Vector2Int:
                        prop.vector2IntValue = (Vector2Int)value;
                        break;
                    case SerializedPropertyType.Vector3Int:
                        prop.vector3IntValue = (Vector3Int)value;
                        break;
                    case SerializedPropertyType.RectInt:
                        prop.rectIntValue = (RectInt)value;
                        break;
                    case SerializedPropertyType.BoundsInt:
                        prop.boundsIntValue = (BoundsInt)value;
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }

            private protected override float GetAddRegionSize() => EditorGUIUtility.singleLineHeight;

            private protected override void OnGUIAddRegion(Rect position)
            {
                if (FakeSlot.propertyType == SerializedPropertyType.ObjectReference)
                {
                    var addValue = Field(position, EditorStatics.ToAdd, default);
                    if (addValue != null) EditorUtil.AddValue(addValue);
                }
                else
                {
                    EditorGUI.LabelField(position, EditorStatics.ToAdd, EditorStatics.AddNotSupported);
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.serializedObject.isEditingMultipleObjects || !property.isExpanded)
                return EditorGUIUtility.singleLineHeight;
            var cache = GetCache(property);
            if (cache == null)
                return EditorGUIUtility.singleLineHeight;

            return cache.GetPropertyHeight() + EditorGUIUtility.singleLineHeight +
                   EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.serializedObject.isEditingMultipleObjects)
            {
                EditorGUI.LabelField(position, label, EditorStatics.MultiEditingNotSupported);
                return;
            }
            var cache = GetCache(property);
            if (cache == null)
            {
                EditorGUI.LabelField(position, label, EditorStatics.UnsupportedType);
                return;
            }

            position.height = EditorGUIUtility.singleLineHeight;

            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label);

            if (property.isExpanded)
            {
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                cache.OnGUI(position);
            }
        }
    }

    /// <summary>
    ///  because there are many generic arguments, nameof is too long. so I use this class 
    /// </summary>
    internal static class Names
    {
        public const string FakeSlot = nameof(PrefabSafeSet<object, PrefabLayer<object>>.fakeSlot);
        public const string MainSet = nameof(PrefabSafeSet<object, PrefabLayer<object>>.mainSet);
        public const string PrefabLayers = nameof(PrefabSafeSet<object, PrefabLayer<object>>.prefabLayers);
        public const string Additions = nameof(PrefabLayer<object>.additions);
        public const string Removes = nameof(PrefabLayer<object>.removes);
    }

    internal abstract class EditorBase<T>
    {
        protected readonly SerializedProperty FakeSlot;
        protected readonly EditorUtil<T> EditorUtil;

        public EditorBase(SerializedProperty property, int nestCount)
        {
            if (property.serializedObject.isEditingMultipleObjects)
                throw new ArgumentException("multi editing not supported", nameof(property));
            FakeSlot = property.FindPropertyRelative(Names.FakeSlot)
                        ?? throw new ArgumentException("fakeSlot not found");
            EditorUtil = EditorUtil<T>.Create(property, nestCount, GetValue, SetValue);
        }

        private protected abstract T GetValue(SerializedProperty prop);
        private protected abstract void SetValue(SerializedProperty prop, T value);
        private protected abstract float GetAddRegionSize();
        private protected abstract void OnGUIAddRegion(Rect position);

        public float GetPropertyHeight() =>
            EditorUtil.ElementsCount * (FieldHeight() + EditorGUIUtility.standardVerticalSpacing) 
            + GetAddRegionSize();

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

                var currentModKind =
                    OnePrefabElement(position, newLabel, element.Value, fieldModKind, element.ModifierProp);
                if (currentModKind != ModificationKind.Natural)
                    (modKind, modValue) = (currentModKind, element.Value);
                position.y += FieldHeight() + EditorGUIUtility.standardVerticalSpacing;
            }

            switch (modKind)
            {
                case ModificationKind.Natural:
                    break;
                case ModificationKind.Remove:
                    EditorUtil.RemoveValue(modValue);
                    break;
                case ModificationKind.Add:
                    EditorUtil.AddValue(modValue);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

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

            if (modifierProp != null) EditorGUI.BeginProperty(fieldPosition, label, modifierProp);
            EditorGUI.BeginDisabledGroup(kind == ModificationKind.Remove);
            // field
            var fieldValue = Field(fieldPosition, label, value);
            if (fieldValue == null)
                result = ModificationKind.Remove;

            EditorGUI.BeginDisabledGroup(kind == ModificationKind.Add);
            if (GUI.Button(addButtonPosition, EditorStatics.ForceAddButton))
                result = ModificationKind.Add;
            EditorGUI.EndDisabledGroup();
            EditorGUI.EndDisabledGroup();
            if (modifierProp != null) EditorGUI.EndProperty();

            return result;
        }

        protected virtual float FieldHeight() => EditorGUI.GetPropertyHeight(FakeSlot);

        protected virtual T Field(Rect position, GUIContent label, T value)
        {
            SetValue(FakeSlot, value);
            EditorGUI.PropertyField(position, FakeSlot, label);
            value = GetValue(FakeSlot);
            return value;
        }
    }

    /// <summary>
    /// Utility to edit PrefabSafeSet in CustomEditor with SerializedProperty
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal abstract class EditorUtil<T>
    {
        // common property;
        [NotNull] private readonly Func<SerializedProperty, T> _getValue;
        [NotNull] private readonly Action<SerializedProperty, T> _setValue;

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
            _getValue = getValue ?? throw new ArgumentNullException(nameof(getValue));
            _setValue = setValue ?? throw new ArgumentNullException(nameof(setValue));
        }

        public void AddValue(T addValue) => DoAdd(addValue, true);

        /// <summary>
        /// add value if not added.
        /// </summary>
        /// <param name="addValue"></param>
        public void EnsureAdded(T addValue) => DoAdd(addValue, false);

        protected abstract void DoAdd(T addValue, bool forceAdd);

        public void RemoveValue(T value) => DoRemove(value, true);

        /// <summary>
        /// remove value if exists.
        /// </summary>
        /// <param name="value"></param>
        public void EnsureRemoved(T value) => DoRemove(value, false);
        
        protected abstract void DoRemove(T value, bool forceRemove);

        public virtual bool Contains(T value) => Elements.Any(x => x.LiveValue && x.Value.Equals(value));

        public abstract void Clear();

        private sealed class Root : EditorUtil<T>
        {
            [NotNull] private readonly SerializedProperty _mainSet;

            public Root(SerializedProperty property, Func<SerializedProperty, T> getValue,
                Action<SerializedProperty, T> setValue) : base(getValue, setValue)
            {
                _mainSet = property.FindPropertyRelative(Names.MainSet)
                           ?? throw new ArgumentException("mainSet not found", nameof(property));
            }

            public override IEnumerable<Element> Elements => new ArrayPropertyEnumerable(_mainSet)
                .Select(x => new Element(_getValue(x)));

            public override int ElementsCount => _mainSet.arraySize;

            protected override void DoAdd(T addValue, bool forceAdd)
            {
                foreach (var prop in new ArrayPropertyEnumerable(_mainSet))
                    if (_getValue(prop).Equals(addValue))
                        return;
                _setValue(AddArrayElement(_mainSet), addValue);
            }

            protected override void DoRemove(T value, bool forceRemove)
            {
                for (var i = 0; i < _mainSet.arraySize; i++)
                {
                    if (_getValue(_mainSet.GetArrayElementAtIndex(i)).Equals(value))
                    {
                        RemoveArrayElementAt(_mainSet, i);
                        return;
                    }
                }
            }

            public override void Clear() => _mainSet.arraySize = 0;
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

                ClearNonLayerModifications(property, nestCount);

                var mainSet = property.FindPropertyRelative(Names.MainSet);
                foreach (var valueProp in new ArrayPropertyEnumerable(mainSet))
                {
                    var value = _getValue(valueProp);
                    if (value == null) continue;
                    if (upstreamValues.Contains(value)) continue;
                    upstreamValues.Add(value);
                    _elements.Add(new Element(value));
                }

                // apply modifications until previous one
                var prefabLayers = property.FindPropertyRelative(Names.PrefabLayers);
                foreach (var layer in new ArrayPropertyEnumerable(prefabLayers).Take(nestCount - 1))
                {
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
                            _elements.Add(new Element(value));
                    }
                }

                _upstreamElementCount = _elements.Count;

                // process current layer
                if (prefabLayers.arraySize < nestCount) prefabLayers.arraySize = nestCount;

                var currentLayer = prefabLayers.GetArrayElementAtIndex(nestCount - 1);
                _currentRemoves = currentLayer.FindPropertyRelative(Names.Removes)
                                  ?? throw new ArgumentException("prefabLayers.removes not found",
                                      nameof(property));
                _currentAdditions = currentLayer.FindPropertyRelative(Names.Additions)
                                    ?? throw new ArgumentException("prefabLayers.additions not found",
                                        nameof(property));

                DoInitialize();
            }

            private void ClearNonLayerModifications(SerializedProperty property, int nestCount)
            {
                try
                {
                    var thisObjectPropPath = property.propertyPath;
                    var arraySizeProp = property.FindPropertyRelative(Names.PrefabLayers).FindPropertyRelative("Array.size").propertyPath;
                    var arrayValueProp = property.FindPropertyRelative(Names.PrefabLayers).GetArrayElementAtIndex(nestCount - 1).propertyPath;
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

            public override IEnumerable<Element> Elements
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
                        var index = Array.IndexOf(additionsArray, value);
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

            protected override void DoAdd(T addValue, bool forceAdd)
            {
                Initialize();
                var index = _elements.FindIndex(x => x.Value.Equals(addValue));
                if (index == -1)
                {
                    // not on list: just add
                    _setValue(AddArrayElement(_currentAdditions), addValue);
                }
                else
                {
                    var element = _elements[index];
                    switch (element.Status)
                    {
                        case ElementStatus.Natural:
                            if (forceAdd)
                                _setValue(AddArrayElement(_currentAdditions), element.Value);
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
                            _setValue(AddArrayElement(_currentAdditions), element.Value);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            protected override void DoRemove(T value, bool forceRemove)
            {
                Initialize();
                var index = _elements.FindIndex(x => x.Value.Equals(value));
                if (index == -1)
                {
                    // not found in the set: add fake removes
                    if (forceRemove)
                        _setValue(AddArrayElement(_currentRemoves), value);
                }
                else
                {
                    var element = _elements[index];
                    switch (element.Status)
                    {
                        case ElementStatus.Natural:
                            _setValue(AddArrayElement(_currentRemoves), element.Value);
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
                            _setValue(AddArrayElement(_currentRemoves), element.Value);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            public override void Clear()
            {
                Initialize();
                foreach (var element in _elements)
                {
                    switch (element.Status)
                    {
                        case ElementStatus.Natural:
                            _setValue(AddArrayElement(_currentRemoves), element.Value);
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
                            _setValue(AddArrayElement(_currentRemoves), element.Value);
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

            public bool LiveValue
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
                            return false;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

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
                result[i] = _getValue(array.GetArrayElementAtIndex(i));
            return result;
        }
    }

    internal enum ElementStatus
    {
        Natural,
        Removed,
        NewElement,
        AddedTwice,
        FakeRemoved,
    }

    internal readonly struct ArrayPropertyEnumerable : IEnumerable<SerializedProperty>
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
}
