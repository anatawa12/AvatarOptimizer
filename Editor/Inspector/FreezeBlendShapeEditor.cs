using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(FreezeBlendShape))]
    class FreezeBlendShapeEditor : AvatarTagComponentEditorBase
    {
        private PrefabSafeSet.PSSEditorUtil<string> _shapeKeysSet = null!; // initialized in OnEnable

        private void OnEnable()
        {
            _shapeKeysSet = PrefabSafeSet.PSSEditorUtil<string>.Create(
                serializedObject.FindProperty("shapeKeysSet"),
                x => x.stringValue,
                (x, v) => x.stringValue = v);
        }

        protected override void OnInspectorGUIInner()
        {
            var component = (FreezeBlendShape)target;

            var shapes = EditSkinnedMeshComponentUtil.GetBlendShapes(component.GetComponent<SkinnedMeshRenderer>(), component);

            var label = new GUIContent();

            serializedObject.Update();
            foreach (var (shapeKeyName, _) in shapes)
            {
                var rect = EditorGUILayout.GetControlRect();
                label.text = shapeKeyName;
                var element = _shapeKeysSet.GetElementOf(shapeKeyName);
                using (new PrefabSafeSet.PropertyScope<string>(element, rect, label))
                {
                    EditorGUI.BeginChangeCheck();
                    var selected = EditorGUI.ToggleLeft(rect, label, element.Contains);
                    if (EditorGUI.EndChangeCheck())
                        element.SetExistence(selected);
                }
            }

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button(AAOL10N.Tr("FreezeBlendShape:button:Check All")))
                {
                    foreach (var (shapeKeyName, _) in shapes)
                        _shapeKeysSet.GetElementOf(shapeKeyName).EnsureAdded();
                }
                
                if (GUILayout.Button(AAOL10N.Tr("FreezeBlendShape:button:Invert All")))
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
