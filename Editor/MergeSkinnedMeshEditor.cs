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

        SerializedProperty _renderersSetProp;
        SerializedProperty _staticRenderersSetProp;
        PrefabSafeSet.EditorUtil<Material> _doNotMergeMaterials;

        private void OnEnable()
        {
            _renderersSetProp = serializedObject.FindProperty("renderersSet");
            _staticRenderersSetProp = serializedObject.FindProperty("staticRenderersSet");
            var nestCount = PrefabSafeSet.PrefabSafeSetUtil.PrefabNestCount(serializedObject.targetObject);
            _doNotMergeMaterials = PrefabSafeSet.EditorUtil<Material>.Create(
                serializedObject.FindProperty("doNotMergeMaterials"),
                nestCount,
                x => x.objectReferenceValue as Material,
                (x, v) => x.objectReferenceValue = v);
        }

        public override void OnInspectorGUI()
        {
            if (((MergeSkinnedMesh)target).GetComponent<SkinnedMeshRenderer>().sharedMesh)
            {
                GUILayout.Label($"Mesh of SkinnedMeshRenderer is not None!", Style.WarningStyle);
                GUILayout.Label($"You should add MergeSkinnedMesh onto new GameObject with new SkinnedMeshRenderer!",
                    Style.WarningStyle);
            }

            EditorGUILayout.PropertyField(_renderersSetProp);
            EditorGUILayout.PropertyField(_staticRenderersSetProp);

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
            var renderersSetAsList = merge.renderersSet.GetAsList();
            var staticRenderersSetAsList = merge.staticRenderersSet.GetAsList();
            var ofRenderers = renderersSetAsList.Select(EditSkinnedMeshComponentUtil.GetMaterials);
            var ofStatics = staticRenderersSetAsList.Select(x => x.sharedMaterials);
            foreach (var group in ofRenderers.Concat(ofStatics)
                         .SelectMany((x, renderer) => x.Select((mat, material) => (mat, renderer, material)))
                         .GroupBy(x => x.mat))
            {
                materials.Add(group.Key);
                if (group.Count() == 1)
                {
                    continue;
                }

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(group.Key, typeof(Material), true);
                EditorGUI.EndDisabledGroup();

                EditorGUI.indentLevel++;
                var doNotMerge = _doNotMergeMaterials.Contains(group.Key);
                var newMerges = EditorGUILayout.ToggleLeft("Merge", !doNotMerge);
                if (newMerges)
                {
                    _doNotMergeMaterials.EnsureRemoved(group.Key);
                }
                else
                {
                    _doNotMergeMaterials.EnsureAdded(group.Key);
                }

                EditorGUILayout.LabelField("Renderers:");
                EditorGUI.indentLevel++;
                EditorGUI.BeginDisabledGroup(true);
                foreach (var (_, rendererIndex, _) in group)
                {
                    if (rendererIndex < renderersSetAsList.Count)
                        EditorGUILayout.ObjectField(renderersSetAsList[rendererIndex], typeof(SkinnedMeshRenderer),
                            true);
                    else
                        EditorGUILayout.ObjectField(staticRenderersSetAsList[rendererIndex - renderersSetAsList.Count],
                            typeof(MeshRenderer), true);
                }

                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
