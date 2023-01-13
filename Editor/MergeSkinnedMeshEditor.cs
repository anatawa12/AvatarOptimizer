using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer
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
            var renderersProp = serializedObject.FindProperty("renderers");
            var staticRenderersProp = serializedObject.FindProperty("staticRenderers");

            ShowRenderers("Skinned Renderers", renderersProp, (SkinnedMeshRenderer renderer) =>
            {
                var mesh = renderer.sharedMesh;
                if (!mesh) return;

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
            });

            ShowRenderers("Static Renderers", staticRenderersProp, (MeshRenderer renderer) => { });

            {
                var prop = serializedObject.FindProperty("removeEmptyRendererObject");
                var rect = EditorGUILayout.GetControlRect(true);
                var label = new GUIContent("Remove Empty Renderer GameObject");
                label = EditorGUI.BeginProperty(rect, label, prop);
                EditorGUI.BeginChangeCheck();
                var flag = EditorGUI.ToggleLeft(rect, label, prop.boolValue);
                if (EditorGUI.EndChangeCheck())
                    prop.boolValue = flag;
                EditorGUI.EndProperty();
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.LabelField("Merge Materials:", EditorStyles.boldLabel);
            if (targets.Length != 1)
                EditorGUILayout.LabelField("MergeMaterial is not supported with Multi Target Editor");
            else
                MergeMaterials((MergeSkinnedMesh)target);
        }

        public void ShowRenderers<T>(string property, SerializedProperty array, Action<T> validation)
            where T : Object
        {
            EditorGUILayout.LabelField(property, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            for (var i = 0; i < array.arraySize; i++)
            {
                var elementProp = array.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(elementProp);

                if (elementProp.objectReferenceValue == null)
                {
                    array.DeleteArrayElementAtIndex(i);
                    i--;
                }
                else if (elementProp.objectReferenceValue is T renderer)
                {
                    validation(renderer);
                }
            }

            var toAdd = (T)EditorGUILayout.ObjectField($"Element {array.arraySize}", null, typeof(T), true);
            if (toAdd != null)
            {
                array.arraySize += 1;
                array.GetArrayElementAtIndex(array.arraySize - 1).objectReferenceValue = toAdd;
            }

            EditorGUI.indentLevel--;
        }

        public void MergeMaterials(MergeSkinnedMesh merge)
        {
            var materials = new HashSet<Material>();
            var ofRenderers = merge.renderers.Select(EditSkinnedMeshComponentUtil.GetMaterials);
            var ofStatics = merge.staticRenderers.Select(x => x.sharedMaterials);
            foreach (var group in ofRenderers.Concat(ofStatics)
                         .SelectMany((x, renderer) => x.Select((mat, material) => (mat, renderer, material)))
                         .GroupBy(x => x.mat))
            {
                materials.Add(group.Key);
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
                {
                    if (rendererIndex < merge.renderers.Length)
                        EditorGUILayout.ObjectField(merge.renderers[rendererIndex], typeof(SkinnedMeshRenderer),
                            true);
                    else
                        EditorGUILayout.ObjectField(merge.staticRenderers[rendererIndex - merge.renderers.Length],
                            typeof(MeshRenderer), true);
                }

                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
                EditorGUI.indentLevel--;
            }

            // remove unused mapping
            for (var i = 0; i < merge.merges.Length; i++)
            {
                if (!materials.Contains(merge.merges[i].target))
                {
                    EditorUtility.SetDirty(merge);
                    ArrayUtility.RemoveAt(ref merge.merges, i);
                    i--;
                }
            }
        }
    }
}
