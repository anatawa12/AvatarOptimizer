using System;
using System.Linq;
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

            EditorGUILayout.LabelField("Merge Materials:", EditorStyles.boldLabel);
            if (targets.Length != 1)
            {
                EditorGUILayout.LabelField("MergeMaterial is not supported with Multi Target Editor");
            }
            else
            {
                var merge = (MergeSkinnedMesh)target;
                foreach (var group in merge.renderers
                             .SelectMany((x, renderer) =>
                                 x.sharedMaterials.Select((mat, material) => (mat, renderer, material)))
                             .GroupBy(x => x.mat))
                {
                    if (group.Count() == 1)
                    {
                        var found = Array.FindIndex(merge.merges, x => x.target == group.Key);
                        if (found >= 0)
                        {
                            EditorUtility.SetDirty(merge);
                            ArrayUtility.RemoveAt(ref merge.merges, found);
                        }
                        continue;
                    }

                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(group.Key, typeof(Material), true);
                    EditorGUI.EndDisabledGroup();

                    EditorGUI.indentLevel++;
                    var foundConfigIndex = Array.FindIndex(merge.merges, x => x.target == group.Key);
                    var oldMerges = foundConfigIndex >= 0;
                    var newMerges = EditorGUILayout.ToggleLeft("Merge", oldMerges);
                    EditorUtility.SetDirty(merge);
                    if (newMerges)
                    {
                        var mergesList = group.Select(x => ((ulong)x.renderer << 32) | (uint)x.material).ToArray();
                        Array.Sort(mergesList);
                        if (foundConfigIndex >= 0)
                        {
                            if (!merge.merges[foundConfigIndex].merges.SequenceEqual(mergesList))
                            {
                                EditorUtility.SetDirty(merge);
                                merge.merges[foundConfigIndex].merges = mergesList;
                            }
                        }
                        else
                        {
                            EditorUtility.SetDirty(merge);
                            ArrayUtility.Add(ref merge.merges,
                                new MergeSkinnedMesh.MergeConfig { target = group.Key, merges = mergesList });
                        }
                    }
                    else
                    {
                        if (foundConfigIndex >= 0)
                        {
                            EditorUtility.SetDirty(merge);
                            ArrayUtility.RemoveAt(ref merge.merges, foundConfigIndex);
                        }
                    }

                    EditorGUILayout.LabelField("Renderers:");
                    EditorGUI.indentLevel++;
                    EditorGUI.BeginDisabledGroup(true);
                    foreach (var (_, rendererIndex, _) in group)
                        EditorGUILayout.ObjectField(merge.renderers[rendererIndex], typeof(SkinnedMeshRenderer), true);
                    EditorGUI.EndDisabledGroup();
                    EditorGUI.indentLevel--;
                    EditorGUI.indentLevel--;
                }
            }
        }
    }
}
