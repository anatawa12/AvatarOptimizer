using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [CustomPropertyDrawer(typeof(ToggleLeftAttribute))]
    internal class ToggleLeftDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();
            var flag = EditorGUI.ToggleLeft(position, label, property.boolValue);
            if (EditorGUI.EndChangeCheck())
                property.boolValue = flag;
            EditorGUI.EndProperty();
        }
    }
}
