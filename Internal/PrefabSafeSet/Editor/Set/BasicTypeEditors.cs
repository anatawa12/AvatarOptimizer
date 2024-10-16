using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.PrefabSafeSet
{
    abstract class BasicEditorBase<T> : EditorBase<T> where T : notnull
    {
        private protected override float GetAddRegionSize() => EditorGUIUtility.singleLineHeight;

        private protected override void OnGUIAddRegion(Rect position) =>
            EditorGUI.LabelField(position, EditorStatics.ToAdd, EditorStatics.AddNotSupported);

        protected BasicEditorBase(SerializedProperty property, int nestCount) : base(property, nestCount)
        {
        }
    }

    class IntegerEditorImpl : BasicEditorBase<long>
    {
        public IntegerEditorImpl(SerializedProperty property, int nestCount) : base(property, nestCount)
        {
        }

        private protected override long GetValue(SerializedProperty prop) => prop.longValue;
        private protected override void SetValue(SerializedProperty prop, long value) => prop.longValue = value;

        protected override float FieldHeight(GUIContent label) =>
            EditorGUI.GetPropertyHeight(SerializedPropertyType.Integer, label);

        protected override long Field(Rect position, GUIContent label, long value) =>
            EditorGUI.LongField(position, label, value);
    }

    class StringEditorImpl : BasicEditorBase<string>
    {
        public StringEditorImpl(SerializedProperty property, int nestCount) : base(property, nestCount)
        {
        }

        private protected override string GetValue(SerializedProperty prop) => prop.stringValue;
        private protected override void SetValue(SerializedProperty prop, string value) => prop.stringValue = value;

        protected override float FieldHeight(GUIContent label) =>
            EditorGUI.GetPropertyHeight(SerializedPropertyType.String, label);

        protected override string Field(Rect position, GUIContent label, string value) =>
            EditorGUI.TextField(position, label, value);
    }

    class ObjectEditorImpl : EditorBase<Object>
    {
        private readonly Type? _elementType;

        public ObjectEditorImpl(SerializedProperty property, Type fieldType, int nestCount) : base(property, nestCount)
        {
            while (fieldType != null)
            {
                if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(PrefabSafeSet<>))
                    break;
                fieldType = fieldType.BaseType;
            }
            if (fieldType == null)
                _elementType = null;
            else
                _elementType = fieldType.GenericTypeArguments[0];
        }

        private protected override Object GetValue(SerializedProperty prop) => prop.objectReferenceValue;
        private protected override void SetValue(SerializedProperty prop, Object value) =>
            prop.objectReferenceValue = value;

        private protected override float GetAddRegionSize() => EditorGUIUtility.singleLineHeight;

        private protected override void OnGUIAddRegion(Rect position)
        {
            var current = Event.current;
            var eventType = current.type;
            switch (eventType)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                case EventType.DragExited:
                {
                    var controlId =
                        GUIUtility.GetControlID("s_PPtrHash".GetHashCode(), FocusType.Keyboard, position);
                    position = EditorGUI.PrefixLabel(position, controlId, EditorStatics.ToAdd);
                    HandleDragEvent(position, controlId, current);
                    break;
                }
                default:
                {
                    var addValue = Field(position, EditorStatics.ToAdd, null);
                    if (addValue != null) EditorUtil.GetElementOf(addValue).Add();
                    break;
                }
            }
        }

        protected override float FieldHeight(GUIContent label) =>
            EditorGUI.GetPropertyHeight(SerializedPropertyType.ObjectReference, label);

        protected override Object Field(Rect position, GUIContent label, Object? value)
        {
            bool allowSceneObjects = false;
            var targetObject = FakeSlot.serializedObject.targetObject;
            if (targetObject != null && !EditorUtility.IsPersistent(targetObject))
                allowSceneObjects = true;
            return EditorGUI.ObjectField(position, label, value, _elementType, allowSceneObjects);
        }

        public void HandleDragEvent(Rect position, int lastControlId, Event @event)
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
                            var object2 = ValidateObjectFieldAssignment(referencesCache, _elementType, FakeSlot);
                            if (object2 == null) continue;
                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                            if (@event.type == EventType.DragPerform)
                            {
                                if (object1 is GameObject gameObject && object2 is Component)
                                {
                                    // if object is dropped from hierarchy view, add all components of the object
                                    foreach (var component in gameObject.GetComponents(_elementType))
                                        if (component != null)
                                            EditorUtil.GetElementOf(component).Add();
                                }
                                else
                                {
                                    EditorUtil.GetElementOf(object2).Add();
                                }
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
        
        private static readonly MethodInfo? ValidateObjectFieldAssignmentMethod =
            typeof(EditorGUI).GetMethod("ValidateObjectFieldAssignment", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly Type? ObjectFieldValidatorOptionsType =
            typeof(EditorGUI).Assembly.GetType("UnityEditor.EditorGUI+ObjectFieldValidatorOptions");

        private static Object? ValidateObjectFieldAssignment(Object[] references,
            Type? elementType,
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
                new[] { references, elementType, property, Enum.ToObject(ObjectFieldValidatorOptionsType, 0) }) as Object;
        }
    }
}
