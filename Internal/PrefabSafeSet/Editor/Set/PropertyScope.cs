using System;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.PrefabSafeSet
{
    public readonly struct PropertyScope<T> : IDisposable where T : notnull
    {
        private readonly SerializedProperty? _property;
        private readonly Rect _totalPosition;
        public readonly IElement<T> Element;
        public readonly GUIContent Label;

        public PropertyScope(IElement<T> element, Rect totalPosition, GUIContent label)
        {
            _property = element.ModifierProp;
            Element = element;
            _totalPosition = totalPosition;
            Label = label;
            if (_property != null)
                Label = EditorGUI.BeginProperty(totalPosition, Label, _property);
        }

        public void Dispose()
        {
            if (_property != null)
            {
                if (Event.current.type == EventType.ContextClick &&
                    _totalPosition.Contains(Event.current.mousePosition))
                {
                    var genericMenu = new GenericMenu();
                    if (!_property.serializedObject.isEditingMultipleObjects
                        && _property.isInstantiatedPrefab
                        && _property.prefabOverride)
                    {
                        Event.current.Use();
                        Element.Container.HandleApplyRevertMenuItems(Element, genericMenu);
                        genericMenu.ShowAsContext();
                    }
                }

                EditorGUI.EndProperty();
            }
        }
    }
}
