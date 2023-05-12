using System;
using CustomLocalization4EditorExtension;
using UnityEditor;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(MakeChildren))]
    [CanEditMultipleObjects]
    internal class MakeChildrenEditor : AvatarTagComponentEditorBase
    {
        private SerializedProperty _children;

        private void OnEnable()
        {
            _children = serializedObject.FindProperty(nameof(MakeChildren.children));
        }

        protected override void OnInspectorGUIInner()
        {
            EditorGUILayout.PropertyField(_children);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
