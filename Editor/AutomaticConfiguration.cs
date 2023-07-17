using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(AutomaticConfiguration))]
    internal class AutomaticConfigurationEditor : AvatarGlobalComponentEditorBase
    {
        private SerializedProperty _freezeBlendShape;
        private SerializedProperty _mmdWorldCompatibility;

        private void OnEnable()
        {
            _freezeBlendShape = serializedObject.FindProperty(nameof(AutomaticConfiguration.freezeBlendShape));
            _mmdWorldCompatibility = serializedObject.FindProperty(nameof(AutomaticConfiguration.mmdWorldCompatibility));
        }

        protected override void OnInspectorGUIInner()
        {
            base.OnInspectorGUIInner();
            serializedObject.UpdateIfRequiredOrScript();

            GUILayout.Label("General Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_mmdWorldCompatibility);
            GUILayout.Label("Features", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_freezeBlendShape);

            serializedObject.ApplyModifiedProperties();
        }
    }
}