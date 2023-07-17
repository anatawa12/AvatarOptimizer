using UnityEditor;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(AutomaticConfiguration))]
    internal class AutomaticConfigurationEditor : AvatarGlobalComponentEditorBase
    {
        private SerializedProperty _freezeBlendShape;
        private SerializedProperty _dontFreezeMmdShapes;

        private void OnEnable()
        {
            _freezeBlendShape = serializedObject.FindProperty(nameof(AutomaticConfiguration.freezeBlendShape));
            _dontFreezeMmdShapes = serializedObject.FindProperty(nameof(AutomaticConfiguration.dontFreezeMmdShapes));
        }

        protected override void OnInspectorGUIInner()
        {
            base.OnInspectorGUIInner();
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.PropertyField(_freezeBlendShape);
            if (_freezeBlendShape.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_dontFreezeMmdShapes);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}