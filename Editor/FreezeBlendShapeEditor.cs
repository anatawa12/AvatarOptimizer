using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(FreezeBlendShape))]
    public class FreezeBlendShapeEditor : Editor
    {
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

            var component = (FreezeBlendShape)target;

            var shapes = EditSkinnedMeshComponentUtil.GetBlendShapes(component.GetComponent<SkinnedMeshRenderer>(), component);

            var label = new GUIContent();

            serializedObject.Update();
            foreach (var shapeKeyName in shapes)
            {
                var rect = EditorGUILayout.GetControlRect();
                label.text = shapeKeyName;
                var contains = _shapeKeysSet.Contains(shapeKeyName);
                EditorGUI.BeginChangeCheck();
                var set = EditorGUI.ToggleLeft(rect, label, contains);
                if (EditorGUI.EndChangeCheck())
                {
                    if (set) _shapeKeysSet.EnsureAdded(shapeKeyName);
                    else _shapeKeysSet.EnsureRemoved(shapeKeyName);
                }
            }

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Check All"))
                {
                    foreach (var shapeKeyName in shapes)
                        _shapeKeysSet.EnsureAdded(shapeKeyName);
                }
                
                if (GUILayout.Button("Invert All"))
                {
                    foreach (var shapeKeyName in shapes)
                        if (_shapeKeysSet.Contains(shapeKeyName))
                            _shapeKeysSet.EnsureRemoved(shapeKeyName);
                        else
                            _shapeKeysSet.EnsureAdded(shapeKeyName);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
