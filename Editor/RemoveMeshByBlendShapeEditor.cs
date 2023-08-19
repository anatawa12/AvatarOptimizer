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

        private void OnEnable()
        {
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

            serializedObject.Update();
            EditorGUILayout.PropertyField(_toleranceProp);

            var shapes = EditSkinnedMeshComponentUtil.GetBlendShapes(component.GetComponent<SkinnedMeshRenderer>(), component);

            var label = new GUIContent();

            foreach (var (shapeKeyName, _) in shapes)
            {
                var rect = EditorGUILayout.GetControlRect();
                label.text = shapeKeyName;
                var element = _shapeKeysSet.GetElementOf(shapeKeyName);
                using (new PrefabSafeSet.PropertyScope<string>(element, rect, label))
                    element.SetExistence(EditorGUI.ToggleLeft(rect, label, element.Contains));
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
