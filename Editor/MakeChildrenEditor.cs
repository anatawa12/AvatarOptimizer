using System;
using CustomLocalization4EditorExtension;
using UnityEditor;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(MakeChildren))]
    [CanEditMultipleObjects]
    internal class MakeChildrenEditor : AvatarTagComponentEditorBase
    {
        private SerializedProperty _executeEarly;
        private SerializedProperty _children;

        private void OnEnable()
        {
            _executeEarly = serializedObject.FindProperty(nameof(MakeChildren.executeEarly));
            _children = serializedObject.FindProperty(nameof(MakeChildren.children));
        }

        protected override void OnInspectorGUIInner()
        {
            EditorGUILayout.PropertyField(_executeEarly);
            EditorGUILayout.PropertyField(_children);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
