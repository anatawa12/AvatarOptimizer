using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
namespace Anatawa12.AvatarOptimizer.PrefabSafeList
{
    /// <summary>
    ///  because there are many generic arguments, nameof is too long. so I use this class 
    /// </summary>
    internal class Names : PrefabSafeList<object, Names.Layer, Names.Container>
    {
        internal class Container : ValueContainer<object>
        {
        }

        internal class Layer : PrefabLayer<object, Container>
        {
        }

        protected Names(Object outerObject) : base(outerObject)
        {
        }
    }

#if true
    internal static class EditorStatics
    {
        public static readonly GUIContent MultiEditingNotSupported = new GUIContent("Multi editing not supported");
        public static readonly GUIContent UnsupportedType = new GUIContent("Element type is not supported");
        public static readonly GUIContent AddNotSupported = new GUIContent("Add Not Supported");

        public static readonly GUIContent ToAdd = new GUIContent("Element to add")
        {
            tooltip = "Drag & Drop value to here to add element to this set."
        };

        public static readonly GUIContent RemoveButton = new GUIContent("x")
        {
            tooltip = "Remove Element from the list."
        };

        public static readonly GUIContent Removed = new GUIContent("(Removed)");
        public static readonly GUIContent AddElement = new GUIContent("AddElement");
    }

    [CustomPropertyDrawer(typeof(PrefabSafeList<,,>), true)]
    internal class ObjectsEditor : PropertyDrawer
    {
        private int _nestCountCache = -1;

        private int GetNestCount(Object obj) =>
            _nestCountCache != -1 ? _nestCountCache : _nestCountCache = PrefabSafeListUtil.PrefabNestCount(obj);

        private readonly Dictionary<string, Editor> _caches =
            new Dictionary<string, Editor>();

        [CanBeNull]
        private Editor GetCache(SerializedProperty property)
        {
            if (!_caches.TryGetValue(property.propertyPath, out var cached))
            {
                _caches[property.propertyPath] = cached =
                    new Editor(property, GetNestCount(property.serializedObject.targetObject));
            }

            return cached;
        }

        private class Editor : EditorBase
        {
            public Editor(SerializedProperty property, int nestCount) : base(property, nestCount)
            {
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

    internal abstract class EditorBase
    {
        protected readonly EditorUtil EditorUtil;

        public EditorBase(SerializedProperty property, int nestCount)
        {
            if (property.serializedObject.isEditingMultipleObjects)
                throw new ArgumentException("multi editing not supported", nameof(property));
            EditorUtil = EditorUtil.Create(property, nestCount);
        }

        public float GetPropertyHeight() =>
            EditorUtil.Elements.Sum(x => FieldHeightOf(x) + EditorGUIUtility.standardVerticalSpacing) 
            + GetAddRegionSize(); // add region

        private float FieldHeightOf(IElement element) =>
            !element.Contains ? EditorGUIUtility.singleLineHeight : FieldHeight(element.ValueProperty);

        // position is 
        public void OnGUI(Rect position)
        {
            var elementI = 0;
            var newLabel = new GUIContent("");

            // to avoid changes in for loop
            Action action = null;

            foreach (var element in EditorUtil.Elements)
            {
                newLabel.text = $"Element {elementI++}";
                if (!element.Contains)
                {
                    position.height = EditorGUIUtility.singleLineHeight;
                    EditorGUI.BeginProperty(position, newLabel, element.RemovedProperty);
                    EditorGUI.LabelField(position, newLabel, EditorStatics.Removed);
                    EditorGUI.EndProperty();
                    // TODO: restore in context menu
                }
                else
                {
                    position.height = FieldHeight(element.ValueProperty);
                    EditorGUI.BeginProperty(position, newLabel, element.ValueProperty);
                    if (Field(position, newLabel, element.ValueProperty))
                        action = element.Remove;
                    EditorGUI.EndProperty();
                }
                position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
            }
            action?.Invoke();

            position.height = EditorGUIUtility.singleLineHeight;

            OnGUIAddRegion(position);
        }

        private protected virtual float GetAddRegionSize() => EditorGUIUtility.singleLineHeight;

        private protected virtual void OnGUIAddRegion(Rect position)
        {
            if (GUI.Button(position, EditorStatics.AddElement))
            {
                EditorUtil.AddElement();
            }
        }

        protected virtual float FieldHeight(SerializedProperty serializedProperty)
        {
            if (HasVisibleChildFields(serializedProperty))
                return EditorGUI.GetPropertyHeight(serializedProperty) 
                       + EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight; // remove button
            else
                return EditorGUI.GetPropertyHeight(serializedProperty);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="label"></param>
        /// <param name="serializedProperty"></param>
        /// <returns>True if removing the value is requested</returns>
        protected virtual bool Field(Rect position, GUIContent label, SerializedProperty serializedProperty)
        {
            if (HasVisibleChildFields(serializedProperty))
            {
                var prevHeight = position.height;
                position.height = EditorGUIUtility.singleLineHeight;
                
                if (GUI.Button(position, EditorStatics.RemoveButton))
                    return true;

                position.y += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
                position.height = prevHeight - EditorGUIUtility.standardVerticalSpacing +
                                  EditorGUIUtility.singleLineHeight;

                EditorGUI.PropertyField(position, serializedProperty, label);
                return false;
            }
            else
            {
                position.width -= EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(position, serializedProperty, label);
                position.x += position.width + EditorGUIUtility.standardVerticalSpacing;
                position.width = EditorGUIUtility.singleLineHeight;

                if (GUI.Button(position, EditorStatics.RemoveButton))
                    return true;

                return false;
            }
        }
        
        internal static bool HasVisibleChildFields(SerializedProperty property)
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
    }
#endif
}
