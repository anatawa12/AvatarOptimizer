using CustomLocalization4EditorExtension;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(RemoveMeshByBlendShape))]
    internal class RemoveMeshByBlendShapeEditor : AvatarTagComponentEditorBase
    {
        private PrefabSafeSet.EditorUtil<string> _shapeKeysSet;
        private SerializedProperty _toleranceProp;
        private SkinnedMeshRenderer _renderer;
        public bool automaticallySetWeightWhenToggle;

        private void OnEnable()
        {
            _renderer = targets.Length == 1 ? ((Component)target).GetComponent<SkinnedMeshRenderer>() : null;
            var nestCount = PrefabSafeSet.PrefabSafeSetUtil.PrefabNestCount(serializedObject.targetObject);
            _shapeKeysSet = PrefabSafeSet.EditorUtil<string>.Create(
                serializedObject.FindProperty("shapeKeysSet"),
                nestCount,
                x => x.stringValue,
                (x, v) => x.stringValue = v);
            _toleranceProp = serializedObject.FindProperty(nameof(RemoveMeshByBlendShape.tolerance));
        }

        protected override void OnInspectorGUIInner()
        {
            var component = (RemoveMeshByBlendShape)target;

            // TODO: replace with better GUI
            if (EditModePreview.MeshPreviewController.Previewing)
            {
                if (GUILayout.Button("End Preview"))
                {
                    EditModePreview.MeshPreviewController.instance.StopPreview();
                }
            }
            else
            {
                if (GUILayout.Button("Start Preview"))
                {
                    EditModePreview.MeshPreviewController.instance.StartPreview(component.gameObject);
                }
            }

            if (!_renderer)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ToggleLeft(
                    new GUIContent(
                        CL4EE.Tr("RemoveMeshByBlendShape:editor:automaticallySetWeightWhenToggle"),
                        CL4EE.Tr("RemoveMeshByBlendShape:tooltip:automaticallySetWeightWhenToggle:noRenderer")
                    ),
                    false);
                automaticallySetWeightWhenToggle = false;
                EditorGUI.EndDisabledGroup();
            } else if (!_renderer.sharedMesh)
            {
                EditorGUI.BeginDisabledGroup(!_renderer || !_renderer.sharedMesh);
                EditorGUILayout.ToggleLeft(
                        new GUIContent(
                            CL4EE.Tr("RemoveMeshByBlendShape:editor:automaticallySetWeightWhenToggle"),
                            CL4EE.Tr("RemoveMeshByBlendShape:tooltip:automaticallySetWeightWhenToggle:noMesh")
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
                            CL4EE.Tr("RemoveMeshByBlendShape:editor:automaticallySetWeightWhenToggle"),
                            CL4EE.Tr("RemoveMeshByBlendShape:tooltip:automaticallySetWeightWhenToggle")
                        ),
                        automaticallySetWeightWhenToggle);
            }

            serializedObject.Update();
            EditorGUILayout.PropertyField(_toleranceProp);

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
                        if (automaticallySetWeightWhenToggle)
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
                if (GUILayout.Button(CL4EE.Tr("RemoveMeshByBlendShape:button:Check All")))
                {
                    foreach (var (shapeKeyName, _) in shapes)
                        _shapeKeysSet.GetElementOf(shapeKeyName).EnsureAdded();
                }
                
                if (GUILayout.Button(CL4EE.Tr("RemoveMeshByBlendShape:button:Invert All")))
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
