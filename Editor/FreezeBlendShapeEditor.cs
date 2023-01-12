using System;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(FreezeBlendShape))]
    public class FreezeBlendShapeEditor : Editor
    {
        private ReorderableList _shapeKeys;
        private SerializedProperty _shapeKeysProp;

        private void OnEnable()
        {
            var component = (FreezeBlendShape)target;

            var shapes = EditSkinnedMeshComponentUtil.GetBlendShapes(component.GetComponent<SkinnedMeshRenderer>(), component);
            _shapeKeysProp = serializedObject.FindProperty(nameof(FreezeBlendShape.shapeKeys));
            _shapeKeys = new ReorderableList(serializedObject, _shapeKeysProp, true, false, true, true);
            _shapeKeys.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var arrayProp = _shapeKeysProp.GetArrayElementAtIndex(index);
                var shapeKey = arrayProp.stringValue;
                var candidates = shapes;
                var foundIndex = Array.IndexOf(candidates, shapeKey);
                if (foundIndex == -1)
                {
                    ArrayUtility.Insert(ref candidates, 0, shapeKey);
                    foundIndex = 0;
                }

                var selected = EditorGUI.Popup(rect, foundIndex, candidates);
                if (selected != foundIndex)
                    arrayProp.stringValue = candidates[selected];
            };
        
            _shapeKeys.onAddCallback = list =>
            {
                list.serializedProperty.arraySize += 1;
                list.serializedProperty.GetArrayElementAtIndex(list.serializedProperty.arraySize - 1).stringValue = shapes.FirstOrDefault() ?? "";
            };
                
            _shapeKeys.elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            _shapeKeys.headerHeight = 3;
        }

        public override void OnInspectorGUI()
        {
            if (targets.Length != 1)
            {
                EditorGUILayout.LabelField("MultiTarget Editing is not supported");
                return;
            }

            serializedObject.Update();
            _shapeKeys.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
