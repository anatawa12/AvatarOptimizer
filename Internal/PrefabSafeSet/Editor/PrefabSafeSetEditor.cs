using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.PrefabSafeSet
{
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
                    if (addValue != null) EditorUtil.GetElementOf(addValue).Add();
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
            Action action = null;

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

                var currentModKind = OnePrefabElement(position, newLabel, element, fieldModKind);

                switch (currentModKind)
                {
                    case ModificationKind.Natural:
                        break;
                    case ModificationKind.Remove:
                        action = element.Remove;
                        break;
                    case ModificationKind.Add:
                        action = element.Add;
                        break;
                }

                position.y += FieldHeight() + EditorGUIUtility.standardVerticalSpacing;
            }

            action?.Invoke();

            OnGUIAddRegion(position);
        }

        private enum ModificationKind
        {
            Natural,
            Add,
            Remove
        }

        private ModificationKind OnePrefabElement(Rect position, GUIContent label, IElement<T> element, 
            ModificationKind kind)
        {
            // layout
            var fieldPosition = position;
            // two buttons
            fieldPosition.width -= EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var addButtonPosition = new Rect(
                fieldPosition.xMax + EditorGUIUtility.standardVerticalSpacing, position.y,
                EditorGUIUtility.singleLineHeight, position.height);

            var result = ModificationKind.Natural;

            using (new PropertyScope<T>(element, fieldPosition, label))
            {
                EditorGUI.BeginDisabledGroup(kind == ModificationKind.Remove);
                // field
                var fieldValue = Field(fieldPosition, label, element.Value);
                if (fieldValue.IsNull())
                    result = ModificationKind.Remove;

                EditorGUI.BeginDisabledGroup(kind == ModificationKind.Add);
                if (GUI.Button(addButtonPosition, EditorStatics.ForceAddButton))
                    result = ModificationKind.Add;
                EditorGUI.EndDisabledGroup();
                EditorGUI.EndDisabledGroup();
            }

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
}
