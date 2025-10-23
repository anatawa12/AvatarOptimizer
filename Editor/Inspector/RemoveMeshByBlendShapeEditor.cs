using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(RemoveMeshByBlendShape))]
    internal class RemoveMeshByBlendShapeEditor : AvatarTagComponentEditorBase
    {
        private PrefabSafeSet.PSSEditorUtil<string> _shapeKeysSet = null!; // initialized in OnEnable
        private SerializedProperty _toleranceProp = null!; // initialized in OnEnable
        private SerializedProperty _invertSelection = null!; // initialized in OnEnable
        private SkinnedMeshRenderer? _renderer;
        public bool automaticallySetWeightWhenToggle;

        private void OnEnable()
        {
            NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace;
            _renderer = targets.Length == 1 ? ((Component)target).GetComponent<SkinnedMeshRenderer>() : null;
            _shapeKeysSet = PrefabSafeSet.PSSEditorUtil<string>.Create(
                serializedObject.FindProperty("shapeKeysSet"),
                x => x.stringValue,
                (x, v) => x.stringValue = v);
            _toleranceProp = serializedObject.FindProperty(nameof(RemoveMeshByBlendShape.tolerance));
            _invertSelection = serializedObject.FindProperty(nameof(RemoveMeshByBlendShape.invertSelection));
        }

        protected override void OnInspectorGUIInner()
        {
            var component = (RemoveMeshByBlendShape)target;

            if (_renderer == null)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ToggleLeft(
                    new GUIContent(
                        AAOL10N.Tr("RemoveMeshByBlendShape:editor:automaticallySetWeightWhenToggle"),
                        AAOL10N.Tr("RemoveMeshByBlendShape:tooltip:automaticallySetWeightWhenToggle:noRenderer")
                    ),
                    false);
                automaticallySetWeightWhenToggle = false;
                EditorGUI.EndDisabledGroup();
            } else if (!_renderer.sharedMesh)
            {
                EditorGUI.BeginDisabledGroup(!_renderer || !_renderer.sharedMesh);
                EditorGUILayout.ToggleLeft(
                        new GUIContent(
                            AAOL10N.Tr("RemoveMeshByBlendShape:editor:automaticallySetWeightWhenToggle"),
                            AAOL10N.Tr("RemoveMeshByBlendShape:tooltip:automaticallySetWeightWhenToggle:noMesh")
                            ),
                        false);
                automaticallySetWeightWhenToggle = false;
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                automaticallySetWeightWhenToggle =
                    EditorGUILayout.ToggleLeft(
                        new GUIContent(
                            AAOL10N.Tr("RemoveMeshByBlendShape:editor:automaticallySetWeightWhenToggle"),
                            AAOL10N.Tr("RemoveMeshByBlendShape:tooltip:automaticallySetWeightWhenToggle")
                        ),
                        automaticallySetWeightWhenToggle);
            }

            serializedObject.Update();
            EditorGUILayout.PropertyField(_toleranceProp);
            EditorGUILayout.PropertyField(_invertSelection);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("BlendShapes", EditorStyles.boldLabel);

            var shapes = EditSkinnedMeshComponentUtil.GetBlendShapes(component.GetComponent<SkinnedMeshRenderer>(), component);

            var label = new GUIContent();

            foreach (var (shapeKeyName, _) in shapes)
            {
                var rect = EditorGUILayout.GetControlRect();
                label.text = shapeKeyName;
                var element = _shapeKeysSet.GetElementOf(shapeKeyName);
                using (new PrefabSafeSet.PropertyScope<string>(element, rect, label))
                {
                    var existence = EditorGUI.ToggleLeft(rect, label, element.Contains);
                    if (existence != element.Contains)
                    {
                        element.SetExistence(existence);
                        if (automaticallySetWeightWhenToggle && _renderer != null)
                        {
                            var shapeIndex = _renderer.sharedMesh.GetBlendShapeIndex(shapeKeyName);
                            if (shapeIndex != -1)
                            {
                                using (var serializedRenderer = new SerializedObject(_renderer))
                                {
                                    var size = serializedRenderer.FindProperty("m_BlendShapeWeights.Array.size");
                                    if (size.intValue <= shapeIndex)
                                        size.intValue = shapeIndex + 1;
                                    var weight = serializedRenderer.FindProperty($"m_BlendShapeWeights.Array.data[{shapeIndex}]");
                                    weight.floatValue = existence ? 100 : 0;
                                    serializedRenderer.ApplyModifiedProperties();
                                }
                            }
                        }
                    }
                    
                }
            }

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button(AAOL10N.Tr("RemoveMeshByBlendShape:button:Check All")))
                {
                    foreach (var (shapeKeyName, _) in shapes)
                        _shapeKeysSet.GetElementOf(shapeKeyName).EnsureAdded();
                }
                
                if (GUILayout.Button(AAOL10N.Tr("RemoveMeshByBlendShape:button:Invert All")))
                {
                    foreach (var (shapeKeyName, _) in shapes)
                    {
                        var element = _shapeKeysSet.GetElementOf(shapeKeyName);
                        if (element.Contains) element.EnsureRemoved();
                        else element.EnsureAdded();
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
