using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(FreezeBlendShape))]
    public class FreezeBlendShapeEditor : Editor
    {
        private readonly SaveVersionDrawer _saveVersion = new SaveVersionDrawer();
        private PrefabSafeSet.EditorUtil<string> _shapeKeysSet;

        private void OnEnable()
        {
            var nestCount = PrefabSafeSet.PrefabSafeSetUtil.PrefabNestCount(serializedObject.targetObject);
            _shapeKeysSet = PrefabSafeSet.EditorUtil<string>.Create(
                serializedObject.FindProperty("shapeKeysSet"),
                nestCount,
                x => x.stringValue,
                (x, v) => x.stringValue = v);
        }

        public override void OnInspectorGUI()
        {
            if (targets.Length != 1)
            {
                EditorGUILayout.LabelField("MultiTarget Editing is not supported");
                return;
            }

            _saveVersion.Draw(serializedObject);

            var component = (FreezeBlendShape)target;

            var shapes = EditSkinnedMeshComponentUtil.GetBlendShapes(component.GetComponent<SkinnedMeshRenderer>(), component);

            var label = new GUIContent();

            serializedObject.Update();
            foreach (var shapeKeyName in shapes)
            {
                var rect = EditorGUILayout.GetControlRect();
                label.text = shapeKeyName;
                var element = _shapeKeysSet.GetElementOf(shapeKeyName);
                using (new PrefabSafeSet.PropertyScope<string>(element, rect, label))
                    element.SetExistence(EditorGUI.ToggleLeft(rect, label, element.Contains));
            }

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Check All"))
                {
                    foreach (var shapeKeyName in shapes)
                        _shapeKeysSet.GetElementOf(shapeKeyName).EnsureAdded();
                }
                
                if (GUILayout.Button("Invert All"))
                {
                    foreach (var shapeKeyName in shapes)
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
