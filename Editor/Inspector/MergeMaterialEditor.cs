using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(MergeMaterial))]
    internal class MergeMaterialEditor : AvatarTagComponentEditorBase
    {
        private Material?[] _upstreamMaterials = null!; // initialized in OnEnable
        private Material[] _materials = null!; // initialized in OnEnable

        private Material[] _candidateMaterials = null!; // initialized in OnEnable
        private string[] _candidateNames = null!; // initialized in OnEnable

        private Texture[]? _generatedPreviews;

        private readonly Func<MergeMaterial.MergeSource> _createNewSource;
        private readonly Func<MergeMaterial.MergeInfo> _createNewMergeInfo;

        public MergeMaterialEditor()
        {
            _createNewSource = () => new MergeMaterial.MergeSource
                { material = null }; // TODO: consider create material selection
            _createNewMergeInfo = () => new MergeMaterial.MergeInfo
                { source = new[] { _createNewSource() } };
        }

        private static void DrawList<T>(
            [AllowNull] ref T[] array,
            string addButton,
            Action<T, int> drawer,
            Func<T>? newElement,
            bool noEmpty = false,
            Action? postButtons = null,
            Action? onMoved = null,
            Action<T>? onRemoved = null,
            Action<T>? onAdded = null
        )
        {
            if (array == null) array = Array.Empty<T>();
            for (var i = 0; i < array.Length; i++)
            {
                drawer(array[i], i);

                GUILayout.BeginHorizontal();
                using (new EditorGUI.DisabledScope(i == 0))
                {
                    if (GUILayout.Button("▲"))
                    {
                        (array[i], array[i - 1]) = (array[i - 1], array[i]);
                        onMoved?.Invoke();
                    }
                }

                using (new EditorGUI.DisabledScope(i == array.Length - 1))
                {
                    if (GUILayout.Button("▼"))
                    {
                        (array[i], array[i + 1]) = (array[i + 1], array[i]);
                        onMoved?.Invoke();
                    }
                }

                using (new EditorGUI.DisabledScope(noEmpty && array.Length == 1))
                {
                    if (GUILayout.Button("✗"))
                    {
                        var removing = array[i];
                        ArrayUtility.RemoveAt(ref array, i);
                        onRemoved?.Invoke(removing);
                        i--;
                    }
                }

                GUILayout.EndHorizontal();
                postButtons?.Invoke();
            }

            using (new EditorGUI.DisabledScope(newElement == null))
                if (GUILayout.Button(addButton))
                {
                    if (newElement == null) throw new InvalidOperationException();
                    T element;
                    ArrayUtility.Add(ref array, element = newElement());
                    onAdded?.Invoke(element);
                }

        }

        protected override void OnInspectorGUIInner()
        {
            Undo.RecordObject(target, "Inspector");
            EditorGUI.BeginChangeCheck();

            var component = (MergeMaterial)target;

            DrawList(ref component.merges, AAOL10N.Tr("MergeMaterial:button:Add Merged Material"),
                (componentMerge, i) =>
                {
                    componentMerge.referenceMaterial = EditorGUILayout.ObjectField(AAOL10N.Tr("MergeMaterial:Reference Material"), componentMerge.referenceMaterial, typeof(Material), false) as Material;

                    DrawList(ref componentMerge.source, AAOL10N.Tr("MergeMaterial:button:Add Source"),
                        (mergeSource, _) =>
                        {
                            var found = _materials.FirstOrDefault(mat => mat == mergeSource.material);
                            _candidateNames[0] = found != null ? found.name : "(invalid)";
                            EditorGUI.BeginChangeCheck();
                            var newIndex = EditorGUILayout.Popup(0, _candidateNames);
                            if (EditorGUI.EndChangeCheck() && newIndex != 0)
                                mergeSource.material = _candidateMaterials[newIndex - 1];

                            EditorGUI.BeginChangeCheck();
                            mergeSource.targetRect = EditorGUILayout.RectField(mergeSource.targetRect);
                        },
                        _candidateMaterials.Length != 0 ? _createNewSource : null,
                        onMoved: OnChanged,
                        onAdded: _ => OnChanged(),
                        onRemoved: _ => OnChanged()
                    );

                    componentMerge.textureSize =
                        EditorGUILayout.Vector2IntField(AAOL10N.Tr("MergeMaterial:label:Texture Size"),
                            componentMerge.textureSize);

                    componentMerge.mergedFormat =
                        (MergeMaterial.MergedTextureFormat)EditorGUILayout.EnumPopup("Format",
                            componentMerge.mergedFormat);

                    var preview = _generatedPreviews != null ? _generatedPreviews[i] : Assets.PreviewHereTex;
                    EditorGUILayout.LabelField(new GUIContent(preview), GUILayout.MaxHeight(256),
                        GUILayout.MaxHeight(256));

                    Utils.HorizontalLine();
                },
                _candidateMaterials.Length != 0 ? _createNewMergeInfo : null,
                postButtons: () => Utils.HorizontalLine(),
                onMoved: OnChanged,
                onAdded: _ => OnChanged(),
                onRemoved: _ => OnChanged()
            );

            if (EditorGUI.EndChangeCheck())
                OnChanged();

            if (GUILayout.Button(AAOL10N.Tr("MergeMaterial:button:Generate Preview")))
            {
                // preview: we only show _MainTex
                // TODO: implement preview generation
                //_generatedPreviews = MergeMaterialProcessor.GenerateTextures(component, _upstreamMaterials, false);
            }
        }

        private void OnChanged() => OnChanged(true);

        private void OnChanged(bool dirty)
        {
            _generatedPreviews = null;
            var component = (MergeMaterial)target;
            if (dirty) EditorUtility.SetDirty(component);
            var usedMaterials =
                new HashSet<Material>(component.merges.SelectMany(x =>
                    x.source.Select(y => y.material).OfType<Material>()));
            _candidateMaterials = _materials.Where(mat => !usedMaterials.Contains(mat)).ToArray();
            _candidateNames = new[] { "" }.Concat(_candidateMaterials.Select(mat => mat.name)).ToArray();
        }

        private void OnEnable()
        {
            var component = (MergeMaterial)target;

            _upstreamMaterials = EditSkinnedMeshComponentUtil
                .GetMaterials(component.GetComponent<SkinnedMeshRenderer>(), component);
            _materials = _upstreamMaterials.ToArray()!;
            OnChanged(dirty: false);
        }
    }
}
