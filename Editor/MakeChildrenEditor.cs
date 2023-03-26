using System;
using CustomLocalization4EditorExtension;
using UnityEditor;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(MakeChildren))]
    [CanEditMultipleObjects]
    internal class MakeChildrenEditor : Editor
    {
        private readonly SaveVersionDrawer _saveVersion = new SaveVersionDrawer();
        private SerializedProperty _children;

        private void OnEnable()
        {
            _children = serializedObject.FindProperty(nameof(MakeChildren.children));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(CL4EE.Tr("MakeChildren:description"), MessageType.None);
            _saveVersion.Draw(serializedObject);
            EditorGUILayout.PropertyField(_children);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
