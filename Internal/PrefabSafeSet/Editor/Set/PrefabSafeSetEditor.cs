using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Anatawa12.AvatarOptimizer.PrefabSafeUniqueCollection;

namespace Anatawa12.AvatarOptimizer.PrefabSafeSet
{
    internal static class EditorStatics
    {
        private static GUIContent WithLocalization(GUIContent content, string key, string? tooltip = null)
        {
            content.text = key.StartsWith('\0') ? key.Substring(1) : AAOL10N.Tr(key);
            if (tooltip != null)
                content.tooltip = AAOL10N.Tr(tooltip);
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
            "\0+", "PrefabSafeSet:tooltip:Force Add Button");

        private static readonly GUIContent RemoveButtonBacked = new GUIContent();
        public static GUIContent RemoveButton => WithLocalization(RemoveButtonBacked, 
            "\0-", "PrefabSafeSet:tooltip:Remove Button");
    }

    [CustomPropertyDrawer(typeof(PrefabSafeSet<>), true)]
    internal class ObjectsEditor : PropertyDrawer
    {
        private int _nestCountCache = -1;

        private int GetNestCount(Object obj) =>
            _nestCountCache != -1 ? _nestCountCache : _nestCountCache = PSUCUtil.PrefabNestCount(obj);

        private readonly Dictionary<string, EditorBase?> _caches = new ();

        private EditorBase? GetCache(SerializedProperty property)
        {
            if (!_caches.TryGetValue(property.propertyPath, out var cached))
            {
                var prop = property.FindPropertyRelative(Names.FakeSlot);
                _caches[property.propertyPath] =
                    cached = GetEditorImpl(prop.propertyType, property, fieldInfo.FieldType, 
                        GetNestCount(property.serializedObject.targetObject));
            }

            return cached;
        }

        private static EditorBase? GetEditorImpl(SerializedPropertyType type, SerializedProperty property,
            Type fieldType, int nestCount)
        {
            switch (type)
            {
                case SerializedPropertyType.Integer:
                    return new IntegerEditorImpl(property, nestCount);
                case SerializedPropertyType.String:
                    return new StringEditorImpl(property, nestCount);
                case SerializedPropertyType.ObjectReference:
                    return new ObjectEditorImpl(property, fieldType, nestCount);
                case SerializedPropertyType.Boolean:
                case SerializedPropertyType.Float:
                case SerializedPropertyType.Color:
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
                default:
                    return null;
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

        private bool PropertyGUI(Rect position, SerializedProperty property, GUIContent label, EditorBase editor)
        {
            var hasOverride = editor.HasPrefabOverride();
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

            (editor as ObjectEditorImpl)?.HandleDragEvent(position, lastControlId, @event);

            if (hasOverride)
                EditorGUI.EndProperty();
            return isExpanded;
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
    }

    /// <summary>
    ///  because there are many generic arguments, nameof is too long. so I use this class 
    /// </summary>
    internal static class Names
    {
        public const string FakeSlot = nameof(PrefabSafeSet<object>.fakeSlot);
    }

    internal abstract class EditorBase
    {
        public abstract float GetPropertyHeight();
        public abstract void OnGUI(Rect position);
        public abstract bool HasPrefabOverride();
    }

    internal abstract class EditorBase<T> : EditorBase where T : notnull
    {
        protected readonly SerializedProperty FakeSlot;
        internal readonly PSSEditorUtil<T> EditorUtil;

        public EditorBase(SerializedProperty property, int nestCount)
        {
            if (property.serializedObject.isEditingMultipleObjects)
                throw new ArgumentException("multi editing not supported", nameof(property));
            FakeSlot = property.FindPropertyRelative(Names.FakeSlot)
                        ?? throw new ArgumentException("fakeSlot not found");
            EditorUtil = PSSEditorUtil<T>.Create(property, GetValue, SetValue);
        }

        private protected abstract T GetValue(SerializedProperty prop);
        private protected abstract void SetValue(SerializedProperty prop, T value);
        private protected abstract float GetAddRegionSize();
        private protected abstract void OnGUIAddRegion(Rect position);

        public override bool HasPrefabOverride() => EditorUtil.HasPrefabOverride();

        private static readonly GUIContent content = new GUIContent("Element 0");

        public override float GetPropertyHeight() =>
            EditorUtil.ElementsCount * (FieldHeight(content) + EditorGUIUtility.standardVerticalSpacing) 
            + GetAddRegionSize();

        // position is 
        public override void OnGUI(Rect position)
        {
            var elementI = 0;
            var newLabel = new GUIContent("");

            // to avoid changes in for loop
            Action? action = null;

            foreach (var element in 
                     EditorUtil.Elements)
            {
                ModificationKind fieldModKind;

                switch (element.Status)
                {
                    case ElementStatus.Natural:
                        newLabel.text = string.Format(AAOL10N.Tr("PrefabSafeSet:label:Element {0}"), elementI++);
                        fieldModKind = ModificationKind.Natural;
                        break;
                    case ElementStatus.Removed:
                        newLabel.text = AAOL10N.Tr("PrefabSafeSet:label:(Removed)");
                        fieldModKind = ModificationKind.Remove;
                        break;
                    case ElementStatus.NewElement:
                        newLabel.text = string.Format(AAOL10N.Tr("PrefabSafeSet:label:Element {0}"), elementI++);
                        fieldModKind = ModificationKind.Add;
                        break;
                    case ElementStatus.AddedTwice:
                        newLabel.text =
                            string.Format(AAOL10N.Tr("PrefabSafeSet:label:Element {0} (Added twice)"), elementI++);
                        fieldModKind = ModificationKind.Add;
                        break;
                    case ElementStatus.FakeRemoved:
                        newLabel.text =
                            AAOL10N.Tr("PrefabSafeSet:label:(Removed but not found)");
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

                position.y += FieldHeight(newLabel) + EditorGUIUtility.standardVerticalSpacing;
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
            var buttonWidth = EditorGUIUtility.singleLineHeight;
            var spacing = EditorGUIUtility.standardVerticalSpacing;

            var fieldPosition = position with
            {
                width = position.width - (buttonWidth + spacing) * 2,
            };
            // two buttons
            var addButtonPosition = position with
            {
                x = fieldPosition.xMax + spacing,
                width = buttonWidth,
            };
            var removeButtonPosition = position with
            {
                x = addButtonPosition.xMax + spacing,
                width = buttonWidth,
            };

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
                
                EditorGUI.BeginDisabledGroup(kind == ModificationKind.Remove);
                if (GUI.Button(removeButtonPosition, EditorStatics.RemoveButton))
                    result = ModificationKind.Remove;
                EditorGUI.EndDisabledGroup();
                EditorGUI.EndDisabledGroup();
            }

            return result;
        }

        protected abstract float FieldHeight(GUIContent label);
        protected abstract T Field(Rect position, GUIContent label, T value);
    }
}
