using System;
using UnityEditor;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(MakeChildren))]
    [CanEditMultipleObjects]
    internal class MakeChildrenEditor : Editor
    {
        private SerializedProperty _children;

        private void OnEnable()
        {
            _children = serializedObject.FindProperty(nameof(MakeChildren.children));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("This component will make children at build time", MessageType.Info);
            EditorGUILayout.PropertyField(_children);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
