using CustomLocalization4EditorExtension;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(TraceAndOptimize))]
    internal class TraceAndOptimizeEditor : AvatarGlobalComponentEditorBase
    {
        private SerializedProperty _freezeBlendShape;
        private SerializedProperty _removeUnusedObjects;
        private SerializedProperty _mmdWorldCompatibility;
        private SerializedProperty _advancedAnimatorParser;
        private SerializedProperty _advancedSettings;
        private SerializedProperty _exclusions;
        private GUIContent _advancedSettingsLabel = new GUIContent();

        private void OnEnable()
        {
            _freezeBlendShape = serializedObject.FindProperty(nameof(TraceAndOptimize.freezeBlendShape));
            _removeUnusedObjects = serializedObject.FindProperty(nameof(TraceAndOptimize.removeUnusedObjects));
            _mmdWorldCompatibility = serializedObject.FindProperty(nameof(TraceAndOptimize.mmdWorldCompatibility));
            _advancedAnimatorParser = serializedObject.FindProperty(nameof(TraceAndOptimize.advancedAnimatorParser));
            _advancedSettings = serializedObject.FindProperty(nameof(TraceAndOptimize.advancedSettings));
            _exclusions = _advancedSettings.FindPropertyRelative(nameof(TraceAndOptimize.AdvancedSettings.exclusions));
        }

        protected override void OnInspectorGUIInner()
        {
            base.OnInspectorGUIInner();
            serializedObject.UpdateIfRequiredOrScript();

            GUILayout.Label("General Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_mmdWorldCompatibility);
            GUILayout.Label("Features", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_freezeBlendShape);
            EditorGUILayout.PropertyField(_removeUnusedObjects);

            _advancedSettingsLabel.text = CL4EE.Tr("TraceAndOptimize:prop:advancedSettings");
            if (EditorGUILayout.PropertyField(_advancedSettings, _advancedSettingsLabel, false))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(CL4EE.Tr("TraceAndOptimize:warn:advancedSettings"), MessageType.Warning);
                EditorGUILayout.PropertyField(_advancedAnimatorParser);
                EditorGUILayout.PropertyField(_exclusions);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}