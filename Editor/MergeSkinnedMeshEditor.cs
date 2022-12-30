using UnityEditor;
using UnityEngine;

namespace Anatawa12.Merger
{
    [CustomEditor(typeof(MergeSkinnedMesh))]
    internal class MergeSkinnedMeshEditor : Editor
    {
        private static class Style
        {
            public static readonly GUIStyle ErrorStyle = new GUIStyle
            {
                normal = { textColor = Color.red },
                wordWrap = false,
            };

            public static readonly GUIStyle WarningStyle = new GUIStyle
            {
                normal = { textColor = Color.yellow },
                wordWrap = false,
            };
        }

        public override void OnInspectorGUI()
        {
            //var mergedComponentProp = serializedObject.FindProperty("merged");
            //EditorGUI.BeginDisabledGroup(mergedComponentProp.objectReferenceValue != null);
            //EditorGUILayout.PropertyField(mergedComponentProp);
            //EditorGUI.EndDisabledGroup();

            var renderersProp = serializedObject.FindProperty("renderers");

            EditorGUILayout.LabelField("Renderers:", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            for (var i = 0; i < renderersProp.arraySize; i++)
            {
                var elementProp = renderersProp.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(elementProp);

                if (elementProp.objectReferenceValue == null)
                {
                    renderersProp.DeleteArrayElementAtIndex(i);
                    i--;
                }
                else if (elementProp.objectReferenceValue is SkinnedMeshRenderer renderer)
                {
                    var mesh = renderer.sharedMesh;

                    for (var j = 0; j < mesh.blendShapeCount; j++)
                    {
                        if (mesh.GetBlendShapeFrameCount(j) != 1)
                            GUILayout.Label($"BlendShapeCount of {mesh.GetBlendShapeName(j)} is not One",
                                Style.ErrorStyle);
                        // ReSharper disable once CompareOfFloatsByEqualityOperator
                        if (mesh.GetBlendShapeFrameWeight(j, 0) != 100.0f)
                            GUILayout.Label($"GetBlendShapeFrameWeight of {mesh.GetBlendShapeName(j)} is not 100",
                                Style.ErrorStyle);
                    }
                }
            }

            var toAdd = (SkinnedMeshRenderer)EditorGUILayout.ObjectField($"Element {renderersProp.arraySize}", null,
                typeof(SkinnedMeshRenderer), true);
            EditorGUI.indentLevel--;
            if (toAdd != null)
            {
                renderersProp.arraySize += 1;
                renderersProp.GetArrayElementAtIndex(renderersProp.arraySize - 1).objectReferenceValue = toAdd;
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
