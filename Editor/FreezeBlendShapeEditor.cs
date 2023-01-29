using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(FreezeBlendShape))]
    public class FreezeBlendShapeEditor : Editor
    {
        private SerializedProperty _freezeFlags;

        private void OnEnable()
        {
            _freezeFlags = serializedObject.FindProperty("freezeFlags");
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

            void SetShapeKeys(HashSet<string> frozenKeys)
            {
                component.shapeKeys = shapes;
                component.freezeFlags = new bool[shapes.Length];
                for (var i = 0; i < component.shapeKeys.Length; i++)
                    component.freezeFlags[i] = frozenKeys.Contains(component.shapeKeys[i]);
                EditorUtility.SetDirty(component);
            }

            // Update ShapeKeys 
            if (component.freezeFlags == null || component.shapeKeys.Length != component.freezeFlags.Length)
                SetShapeKeys(new HashSet<string>(component.shapeKeys));
            else if (!component.shapeKeys.SequenceEqual(shapes))
                SetShapeKeys(new HashSet<string>(component.shapeKeys.Where((_, i) => component.freezeFlags[i])));

            serializedObject.Update();
            for (var i = 0; i < component.shapeKeys.Length; i++)
            {
                var rect = EditorGUILayout.GetControlRect();
                var label = new GUIContent(component.shapeKeys[i]);
                var prop = _freezeFlags.GetArrayElementAtIndex(i);
                label = EditorGUI.BeginProperty(rect, label, prop);
                EditorGUI.BeginChangeCheck();
                var set = EditorGUI.ToggleLeft(rect, label, prop.boolValue);
                if (EditorGUI.EndChangeCheck()) prop.boolValue = set;
                EditorGUI.EndProperty();
            }

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Check All"))
                {
                    for (var i = 0; i < _freezeFlags.arraySize; i++)
                    {
                        var prop = _freezeFlags.GetArrayElementAtIndex(i);
                        prop.boolValue = true;
                    }
                }
                
                if (GUILayout.Button("Invert All"))
                {
                    for (var i = 0; i < _freezeFlags.arraySize; i++)
                    {
                        var prop = _freezeFlags.GetArrayElementAtIndex(i);
                        prop.boolValue = !prop.boolValue;
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
