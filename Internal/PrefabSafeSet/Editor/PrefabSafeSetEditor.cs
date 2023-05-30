using System;
using System.Collections.Generic;
using System.Reflection;
using CustomLocalization4EditorExtension;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.PrefabSafeSet
{
    internal static class EditorStatics
    {
        private static GUIContent WithLocalization(GUIContent content, string key, string tooltip = null)
        {
            content.text = CL4EE.Tr(key);
            if (tooltip != null)
                content.tooltip = CL4EE.Tr(tooltip);
            return content;
        }

        private static readonly GUIContent MultiEditingNotSupportedBacked = new GUIContent();
        public static GUIContent MultiEditingNotSupported =>
            WithLocalization(MultiEditingNotSupportedBacked, "PrefabSafeSet:label:Multi editing not supported");

        private static readonly GUIContent UnsupportedTypeBacked = new GUIContent();
        public static GUIContent UnsupportedType =>
            WithLocalization(UnsupportedTypeBacked, "PrefabSafeSet:label:Element type is not supported");

        private static readonly GUIContent AddNotSupportedBacked = new GUIContent();
        public static GUIContent AddNotSupported =>
            WithLocalization(AddNotSupportedBacked, "PrefabSafeSet:label:Add Not Supported");

        private static readonly GUIContent ToAddBacked = new GUIContent();
        public static GUIContent ToAdd => WithLocalization(ToAddBacked, 
            "PrefabSafeSet:label:Element to add",
            "PrefabSafeSet:tooltip:Element to add");

        private static readonly GUIContent ForceAddButtonBacked = new GUIContent();
        public static GUIContent ForceAddButton => WithLocalization(ForceAddButtonBacked, 
            "+", "PrefabSafeSet:tooltip:Force Add Button");
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

            public EditorUtil<object> GetEditorUtil() => EditorUtil;
            public SerializedProperty GetFakeSlot() => FakeSlot;

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
                    var current = Event.current;
                    var eventType = current.type;
                    switch (eventType)
                    {
                        case EventType.DragUpdated:
                        case EventType.DragPerform:
                        case EventType.DragExited:
                        {
                            var controlId = GUIUtility.GetControlID("s_PPtrHash".GetHashCode(), FocusType.Keyboard, position);
                            position = EditorGUI.PrefixLabel(position, controlId, EditorStatics.ToAdd);
                            HandleDragEvent(position, controlId, current, this);
                            break;
                        }
                        default:
                        {
                            SetValue(FakeSlot, default);
                            EditorGUI.PropertyField(position, FakeSlot, EditorStatics.ToAdd);
                            var addValue = FakeSlot.objectReferenceValue;
                            if (addValue != null) EditorUtil.GetElementOf(addValue).Add();
                            break;
                        }
                    }
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

            property.isExpanded = PropertyGUI(position, property, label, cache);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                cache.OnGUI(position);
                EditorGUI.indentLevel--;
            }
        }

        private bool PropertyGUI(Rect position, SerializedProperty property, GUIContent label, Editor editor)
        {
            var hasOverride = editor.GetEditorUtil().HasPrefabOverride();
            if (hasOverride)
                EditorGUI.BeginProperty(position, label, property);

            var @event = new Event(Event.current);
            var isExpanded = property.isExpanded;
            using (new EditorGUI.DisabledScope(!property.editable))
            {
                var style = DragAndDrop.activeControlID == -10 ? EditorStyles.foldoutPreDrop : EditorStyles.foldout;
                isExpanded = EditorGUI.Foldout(position, isExpanded, label, true, style);
            }

            property.isExpanded = isExpanded;

            int lastControlId = GetLastControlId();

            HandleDragEvent(position, lastControlId, @event, editor);

            if (hasOverride)
                EditorGUI.EndProperty();
            return isExpanded;
        }

        private static void HandleDragEvent(Rect position, int lastControlId, Event @event, Editor editor)
        {
            switch (@event.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (position.Contains(@event.mousePosition) && GUI.enabled)
                    {
                        var objectReferences = DragAndDrop.objectReferences;
                        var referencesCache = new Object[1];
                        var flag3 = false;
                        foreach (var object1 in objectReferences)
                        {
                            referencesCache[0] = object1;
                            var object2 = ValidateObjectFieldAssignment(referencesCache, editor.GetFakeSlot());
                            if (object2 == null) continue;
                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                            if (@event.type == EventType.DragPerform)
                            {
                                editor.GetEditorUtil().GetElementOf(object2).EnsureAdded();
                                flag3 = true;
                                DragAndDrop.activeControlID = 0;
                            }
                            else
                                DragAndDrop.activeControlID = lastControlId;
                        }

                        if (flag3)
                        {
                            GUI.changed = true;
                            DragAndDrop.AcceptDrag();
                        }
                    }

                    break;
                case EventType.DragExited:
                    if (GUI.enabled)
                        HandleUtility.Repaint();
                    break;
            }
        }

        private static readonly FieldInfo LastControlIdField =
            typeof(EditorGUIUtility).GetField("s_LastControlID", BindingFlags.Static | BindingFlags.NonPublic);

        private static int GetLastControlId()
        {
            if (LastControlIdField == null)
            {
                Debug.LogError("Compatibility with Unity broke: can't find s_LastControlID field in EditorGUIUtility");
                return 0;
            }

            return (int)LastControlIdField.GetValue(null);
        }

        private static readonly MethodInfo ValidateObjectFieldAssignmentMethod =
            typeof(EditorGUI).GetMethod("ValidateObjectFieldAssignment", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly Type ObjectFieldValidatorOptionsType =
            typeof(EditorGUI).Assembly.GetType("UnityEditor.EditorGUI+ObjectFieldValidatorOptions");

        private static Object ValidateObjectFieldAssignment(Object[] references,
            SerializedProperty property)
        {
            if (ValidateObjectFieldAssignmentMethod == null)
            {
                Debug.LogError(
                    "Compatibility with Unity broke: can't find ValidateObjectFieldAssignment method in EditorGUI");
                return null;
            }

            if (ObjectFieldValidatorOptionsType == null)
            {
                Debug.LogError(
                    "Compatibility with Unity broke: can't find ObjectFieldValidatorOptions type in EditorGUI");
                return null;
            }

            return ValidateObjectFieldAssignmentMethod.Invoke(null,
                new[] { references, null, property, Enum.ToObject(ObjectFieldValidatorOptionsType, 0) }) as Object;
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
        [NotNull] protected readonly SerializedProperty FakeSlot;
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
                        newLabel.text = string.Format(CL4EE.Tr("PrefabSafeSet:label:Element {0}"), elementI++);
                        fieldModKind = ModificationKind.Natural;
                        break;
                    case ElementStatus.Removed:
                        newLabel.text = CL4EE.Tr("PrefabSafeSet:label:(Removed)");
                        fieldModKind = ModificationKind.Remove;
                        break;
                    case ElementStatus.NewElement:
                        newLabel.text = string.Format(CL4EE.Tr("PrefabSafeSet:label:Element {0}"), elementI++);
                        fieldModKind = ModificationKind.Add;
                        break;
                    case ElementStatus.AddedTwice:
                        newLabel.text =
                            string.Format(CL4EE.Tr("PrefabSafeSet:label:Element {0} (Added twice)"), elementI++);
                        fieldModKind = ModificationKind.Add;
                        break;
                    case ElementStatus.FakeRemoved:
                        newLabel.text =
                            CL4EE.Tr("PrefabSafeSet:label:(Removed but not found)");
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
