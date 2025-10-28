using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(TraceAndOptimize))]
    internal class TraceAndOptimizeEditor : AvatarGlobalComponentEditorBase
    {
        private SerializedProperty _freezeBlendShape = null!; // Initialized in OnEnable
        private SerializedProperty _removeUnusedObjects = null!; // Initialized in OnEnable
        private SerializedProperty _preserveEndBone = null!; // Initialized in OnEnable
        private SerializedProperty _removeZeroSizedPolygons = null!; // Initialized in OnEnable
        private SerializedProperty _optimizePhysBone = null!; // Initialized in OnEnable
        private SerializedProperty _optimizeAnimator = null!; // Initialized in OnEnable
        private SerializedProperty _mergeSkinnedMesh = null!; // Initialized in OnEnable
        private SerializedProperty _allowShuffleMaterialSlots = null!; // Initialized in OnEnable
        private SerializedProperty _optimizeTexture = null!; // Initialized in OnEnable
        private SerializedProperty _mmdWorldCompatibility = null!; // Initialized in OnEnable
        private SerializedProperty _debugOptions = null!; // Initialized in OnEnable
        private GUIContent _advancedSettingsLabel = new GUIContent();
        private GUIContent _debugOptionsLabel = new GUIContent();

        private void OnEnable()
        {
            _freezeBlendShape = serializedObject.FindProperty(nameof(TraceAndOptimize.optimizeBlendShape));
            _removeUnusedObjects = serializedObject.FindProperty(nameof(TraceAndOptimize.removeUnusedObjects));
            _preserveEndBone = serializedObject.FindProperty(nameof(TraceAndOptimize.preserveEndBone));
            _removeZeroSizedPolygons = serializedObject.FindProperty(nameof(TraceAndOptimize.removeZeroSizedPolygons));
            _optimizePhysBone = serializedObject.FindProperty(nameof(TraceAndOptimize.optimizePhysBone));
            _optimizeAnimator = serializedObject.FindProperty(nameof(TraceAndOptimize.optimizeAnimator));
            _mergeSkinnedMesh = serializedObject.FindProperty(nameof(TraceAndOptimize.mergeSkinnedMesh));
            _allowShuffleMaterialSlots = serializedObject.FindProperty(nameof(TraceAndOptimize.allowShuffleMaterialSlots));
            _optimizeTexture = serializedObject.FindProperty(nameof(TraceAndOptimize.optimizeTexture));
            _mmdWorldCompatibility = serializedObject.FindProperty(nameof(TraceAndOptimize.mmdWorldCompatibility));
            _debugOptions = serializedObject.FindProperty(nameof(TraceAndOptimize.debugOptions));
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
            EditorGUILayout.PropertyField(_optimizePhysBone);
            EditorGUILayout.PropertyField(_optimizeAnimator);
            EditorGUILayout.PropertyField(_mergeSkinnedMesh);
            if (_mergeSkinnedMesh.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_allowShuffleMaterialSlots);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(_optimizeTexture);

            _advancedSettingsLabel.text = AAOL10N.Tr("TraceAndOptimize:prop:advancedOptimization");
            AdvancedOpened = EditorGUILayout.Foldout(AdvancedOpened, _advancedSettingsLabel);
            if (AdvancedOpened)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(AAOL10N.Tr("TraceAndOptimize:note:advancedOptimization"), MessageType.Info);
                EditorGUILayout.PropertyField(_removeZeroSizedPolygons);
                EditorGUI.indentLevel--;
            }

            _debugOptionsLabel.text = AAOL10N.Tr("TraceAndOptimize:prop:debugOptions");
            if (EditorGUILayout.PropertyField(_debugOptions, _debugOptionsLabel, false))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(AAOL10N.Tr("TraceAndOptimize:warn:debugOptions"), MessageType.Warning);
                var iterator = _debugOptions.Copy();
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

        private static bool AdvancedOpened
        {
            get => EditorPrefs.GetBool("AvatarOptimizer.TraceAndOptimizeEditor.AdvancedOpened", false);
            set => EditorPrefs.SetBool("AvatarOptimizer.TraceAndOptimizeEditor.AdvancedOpened", value);
        }
    }
}
