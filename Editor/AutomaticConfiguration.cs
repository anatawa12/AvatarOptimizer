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
        private SerializedProperty _preserveEndBone;
        private SerializedProperty _mmdWorldCompatibility;
        private SerializedProperty _advancedAnimatorParser;
        private SerializedProperty _advancedSettings;
        private GUIContent _advancedSettingsLabel = new GUIContent();

        private void OnEnable()
        {
            _freezeBlendShape = serializedObject.FindProperty(nameof(TraceAndOptimize.freezeBlendShape));
            _removeUnusedObjects = serializedObject.FindProperty(nameof(TraceAndOptimize.removeUnusedObjects));
            _preserveEndBone = serializedObject.FindProperty(nameof(TraceAndOptimize.preserveEndBone));
            _mmdWorldCompatibility = serializedObject.FindProperty(nameof(TraceAndOptimize.mmdWorldCompatibility));
            _advancedAnimatorParser = serializedObject.FindProperty(nameof(TraceAndOptimize.advancedAnimatorParser));
            _advancedSettings = serializedObject.FindProperty(nameof(TraceAndOptimize.advancedSettings));
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
            if (_removeUnusedObjects.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_preserveEndBone);
                EditorGUI.indentLevel--;
            }

            _advancedSettingsLabel.text = CL4EE.Tr("TraceAndOptimize:prop:advancedSettings");
            if (EditorGUILayout.PropertyField(_advancedSettings, _advancedSettingsLabel, false))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(CL4EE.Tr("TraceAndOptimize:warn:advancedSettings"), MessageType.Warning);
                EditorGUILayout.PropertyField(_advancedAnimatorParser);
                var iterator = _advancedSettings.Copy();
                var enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    EditorGUILayout.PropertyField(iterator);
                }
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}