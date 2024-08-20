using UnityEditor;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(MakeChildren))]
    [CanEditMultipleObjects]
    internal class MakeChildrenEditor : AvatarTagComponentEditorBase
    {
        private SerializedProperty _executeEarly = null!; // initialized in OnEnable
        private SerializedProperty _children = null!; // initialized in OnEnable

        private void OnEnable()
        {
            _executeEarly = serializedObject.FindProperty(nameof(MakeChildren.executeEarly));
            _children = serializedObject.FindProperty(nameof(MakeChildren.children));
        }

        protected override void OnInspectorGUIInner()
        {
            EditorGUILayout.PropertyField(_executeEarly);
            if (_executeEarly.boolValue)
            {
                EditorGUILayout.HelpBox(AAOL10N.Tr("MakeChildren:executeEarly does not support animation"), MessageType.Warning);
            }
            EditorGUILayout.PropertyField(_children);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
