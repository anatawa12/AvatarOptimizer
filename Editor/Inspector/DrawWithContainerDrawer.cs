using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer;

[CustomPropertyDrawer(typeof(DrawWithContainerAttribute))]
public class DrawWithContainerDrawer : PropertyDrawer
{
    private PropertyDrawer? _upstreamDrawer;
    private bool _initialized;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        InitializeUpstream(property);
        return _upstreamDrawer != null ? _upstreamDrawer.GetPropertyHeight(property, label) :
            Reflections.DefaultPropertyField(new Rect(), property, label) ? 0 :
            GetDefaultPropertyHeight(property, label);
    }


    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        InitializeUpstream(property);
        if (_upstreamDrawer != null)
            _upstreamDrawer.OnGUI(position, property, label);
        else
            OnGUIDefault(position, property, label);
    }

    private void InitializeUpstream(SerializedProperty property)
    {
        if (_initialized) return;

        var containerProperty = property.serializedObject.FindProperty(property.propertyPath[..property.propertyPath.LastIndexOf('.')]);
        if (containerProperty == null)
        {
            _initialized = true;
            return;
        }

        var containerFieldInfo = Reflections.GetFieldInfoAndStaticTypeFromProperty(containerProperty, out _);

        foreach (var propAttr in containerFieldInfo.GetCustomAttributes<PropertyAttribute>()
                     .Reverse())
            HandleDrawnType(property, propAttr.GetType(), propAttr);

        // if we cannot find upstream PropertyDrawer, we find it using 
        if (_upstreamDrawer == null)
        {
            var type = fieldInfo.FieldType;
            // the path ends with array element
            if (Regex.IsMatch(property.propertyPath, "\\.Array\\.data\\[[0-9]+\\]$"))
            {
                if (type.IsArray)
                {
                    type = type.GetElementType() ?? throw new InvalidOperationException();
                }
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    type = type.GetGenericArguments()[0];
                }
            }

            HandleDrawnType(property, type, null);
        }

        _initialized = true;
    }

    private void HandleDrawnType(SerializedProperty property, Type drawnType, PropertyAttribute? attr)
    {
        var forPropertyAndType = Reflections.GetDrawerTypeForPropertyAndType(property, drawnType);
        if (forPropertyAndType == null)
            return;
        if (typeof (PropertyDrawer).IsAssignableFrom(forPropertyAndType))
        {
            _upstreamDrawer = (PropertyDrawer) System.Activator.CreateInstance(forPropertyAndType);
            Reflections.SetFieldAndAttribute(_upstreamDrawer, fieldInfo, attr);
        }
    }

    private float GetDefaultPropertyHeight(SerializedProperty property, GUIContent label)
    {
        property = property.Copy();
        var height = EditorGUI.GetPropertyHeight(property.propertyType, label);
        var enterChildren = property.isExpanded && HasVisibleChildFields(property);
        if (!enterChildren) return height;

        var label1 = new GUIContent(label.text);
        var endProperty = property.GetEndProperty();
        while (property.NextVisible(enterChildren) && !SerializedProperty.EqualContents(property, endProperty))
        {
            height += EditorGUI.GetPropertyHeight(property, label1, true);
            height += EditorGUIUtility.standardVerticalSpacing;
            enterChildren = false;
        }

        return height;
    }

    private void OnGUIDefault(Rect position, SerializedProperty property, GUIContent label)
    {
        // cache value
        var iconSize = EditorGUIUtility.GetIconSize();
        var enabled = GUI.enabled;
        var indentLevel = EditorGUI.indentLevel;

        var indentLevelOffset = indentLevel - property.depth;

        var serializedProperty = property.Copy();
        position.height = EditorGUI.GetPropertyHeight(serializedProperty.propertyType, label);
        EditorGUI.indentLevel = serializedProperty.depth + indentLevelOffset;
        var enterChildren = Reflections.DefaultPropertyField(position, serializedProperty, label) &&
                            HasVisibleChildFields(serializedProperty);
        position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
        if (enterChildren)
        {
            var endProperty = serializedProperty.GetEndProperty();
            while (serializedProperty.NextVisible(enterChildren) &&
                   !SerializedProperty.EqualContents(serializedProperty, endProperty))
            {
                EditorGUI.indentLevel = serializedProperty.depth + indentLevelOffset;
                position.height = EditorGUI.GetPropertyHeight(serializedProperty, null, false);
                EditorGUI.BeginChangeCheck();
                enterChildren = EditorGUI.PropertyField(position, serializedProperty, null, false) &&
                                HasVisibleChildFields(serializedProperty);
                if (EditorGUI.EndChangeCheck())
                    break;
                position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        // restore value
        GUI.enabled = enabled;
        EditorGUIUtility.SetIconSize(iconSize);
        EditorGUI.indentLevel = indentLevel;
    }

    private static bool HasVisibleChildFields(SerializedProperty property)
    {
        switch (property.propertyType)
        {
            case SerializedPropertyType.Vector2:
            case SerializedPropertyType.Vector3:
            case SerializedPropertyType.Rect:
            case SerializedPropertyType.Bounds:
            case SerializedPropertyType.Vector2Int:
            case SerializedPropertyType.Vector3Int:
            case SerializedPropertyType.RectInt:
            case SerializedPropertyType.BoundsInt:
                return false;
            default:
                return property.hasVisibleChildren;
        }
    }

    static class Reflections
    {
        private static readonly MethodInfo GetDrawerTypeForPropertyAndTypeInfo;

        private static readonly GetFieldInfoAndStaticTypeFromPropertyDelegateType
            GetFieldInfoAndStaticTypeFromPropertyDelegate;

        private static readonly FieldInfo FieldInfoInfo;
        private static readonly FieldInfo AttributeInfo;
        private static readonly MethodInfo DefaultPropertyFieldInfo;

        delegate FieldInfo
            GetFieldInfoAndStaticTypeFromPropertyDelegateType(SerializedProperty property, out Type type);

        static Reflections()
        {
            var type = typeof(Editor).Assembly.GetType("UnityEditor.ScriptAttributeUtility");
            var methodInfo = type.GetMethod("GetDrawerTypeForPropertyAndType",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(SerializedProperty), typeof(Type) },
                null);
            GetDrawerTypeForPropertyAndTypeInfo = methodInfo ?? throw new InvalidOperationException();
            var getFieldInfoAndStaticTypeFromPropertyInfo = type.GetMethod("GetFieldInfoAndStaticTypeFromProperty",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(SerializedProperty), typeof(Type).MakeByRefType() },
                null);
            GetFieldInfoAndStaticTypeFromPropertyDelegate =
                (GetFieldInfoAndStaticTypeFromPropertyDelegateType)Delegate.CreateDelegate(
                    typeof(GetFieldInfoAndStaticTypeFromPropertyDelegateType),
                    getFieldInfoAndStaticTypeFromPropertyInfo ?? throw new InvalidOperationException());
            FieldInfoInfo =
                typeof(PropertyDrawer).GetField("m_FieldInfo",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) ??
                throw new InvalidOperationException();
            AttributeInfo =
                typeof(PropertyDrawer).GetField("m_Attribute",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) ??
                throw new InvalidOperationException();
            DefaultPropertyFieldInfo = typeof(EditorGUI)
                .GetMethod("DefaultPropertyField",
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[]
                    {
                        typeof(Rect),
                        typeof(SerializedProperty),
                        typeof(GUIContent),
                    },
                    null) ?? throw new InvalidOperationException();
        }

        public static FieldInfo GetFieldInfoAndStaticTypeFromProperty(SerializedProperty property, out Type type) =>
            GetFieldInfoAndStaticTypeFromPropertyDelegate(property, out type);

        public static Type? GetDrawerTypeForPropertyAndType(SerializedProperty property, Type type) =>
            (Type?)GetDrawerTypeForPropertyAndTypeInfo.Invoke(null, new object[] { property, type });

        public static void SetFieldAndAttribute(PropertyDrawer drawer, FieldInfo fieldInfo, PropertyAttribute attribute)
        {
            FieldInfoInfo.SetValue(drawer, fieldInfo);
            AttributeInfo.SetValue(drawer, attribute);
        }

        internal static bool DefaultPropertyField(Rect position, SerializedProperty property, GUIContent label) =>
            (bool)DefaultPropertyFieldInfo.Invoke(null, new object[] { position, property, label });
    }
}
