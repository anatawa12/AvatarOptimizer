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

        // Cache for each mergeInfo
        private MaterialEditorCache[] _materialEditorCaches = Array.Empty<MaterialEditorCache>();

        struct MaterialEditorCache
        {
            public MergeMaterialProcessor.ValidatedMergeInfo? ValidatedInfo;
            public List<MergeMaterialProcessor.RootValidationError>? ValidationErrors;
            public Texture? Texture;
        }

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
            Action<int, int>? onMoved = null,
            Action<int, T>? onRemoved = null,
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
                        onMoved?.Invoke(i, i - 1);
                    }
                }

                using (new EditorGUI.DisabledScope(i == array.Length - 1))
                {
                    if (GUILayout.Button("▼"))
                    {
                        (array[i], array[i + 1]) = (array[i + 1], array[i]);
                        onMoved?.Invoke(i, i + 1);
                    }
                }

                using (new EditorGUI.DisabledScope(noEmpty && array.Length == 1))
                {
                    if (GUILayout.Button("✗"))
                    {
                        var removing = array[i];
                        ArrayUtility.RemoveAt(ref array, i);
                        onRemoved?.Invoke(i, removing);
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

            if (_materialEditorCaches.Length != component.merges.Length)
            {
                if (_materialEditorCaches.Length > component.merges.Length)
                {
                    // destroy temporary caches
                    for (var i = component.merges.Length; i < _materialEditorCaches.Length; i++)
                    {
                        DestroyImmediate(_materialEditorCaches[i].Texture);
                    }
                }
                Array.Resize(ref _materialEditorCaches, component.merges.Length);
            }

            DrawList(ref component.merges, AAOL10N.Tr("MergeMaterial:button:Add Merged Material"),
                (componentMerge, i) =>
                {
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    EditorGUI.BeginChangeCheck();
                    componentMerge.referenceMaterial =
                        EditorGUILayout.ObjectField(AAOL10N.Tr("MergeMaterial:Reference Material"),
                            componentMerge.referenceMaterial, typeof(Material), false) as Material;

                    DrawList(ref componentMerge.source, AAOL10N.Tr("MergeMaterial:button:Add Source"),
                        (mergeSource, _) =>
                        {
                            var found = _materials.FirstOrDefault(mat => mat == mergeSource.material);
                            _candidateNames[0] = found != null ? found.name : "(invalid)";
                            EditorGUI.BeginChangeCheck();
                            var newIndex = EditorGUILayout.Popup(0, _candidateNames);
                            if (EditorGUI.EndChangeCheck() && newIndex != 0)
                                mergeSource.material = _candidateMaterials[newIndex - 1];

                            mergeSource.targetRect = EditorGUILayout.RectField(mergeSource.targetRect);
                        },
                        _candidateMaterials.Length != 0 ? _createNewSource : null
                    );

                    componentMerge.textureSize =
                        EditorGUILayout.Vector2IntField(AAOL10N.Tr("MergeMaterial:label:Texture Size"),
                            componentMerge.textureSize);

                    componentMerge.mergedFormat =
                        (MergeMaterial.MergedTextureFormat)EditorGUILayout.EnumPopup("Format",
                            componentMerge.mergedFormat);

                    ref var cache = ref _materialEditorCaches[i];

                    if (EditorGUI.EndChangeCheck() || cache.ValidationErrors == null)
                    {
                        cache.ValidatedInfo = MergeMaterialProcessor.ValidateOneSetting(componentMerge,
                            (mat) => (mat, 0), out cache.ValidationErrors);
                        cache.Texture = null;
                        if (cache.ValidatedInfo != null)
                        {
                            cache.Texture = MergeMaterialProcessor.CreateTexture(cache.ValidatedInfo, compress: false,
                                propertyName: "_MainTex");
                        }
                    }

                    foreach (var error in cache.ValidationErrors ??
                                          new List<MergeMaterialProcessor.RootValidationError>())
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        switch (error)
                        {
                            case MergeMaterialProcessor.UnsupportedShaderInMergeSetting setting:
                                EditorGUILayout.SelectableLabel(
                                    AAOL10N.Tr("MergeMaterial:UnsupportedShaderInMergeSetting"),
                                    EditorStyles.wordWrappedLabel);
                                EditorGUILayout.ObjectField("Reference Material", setting.ReferenceMaterial,
                                    typeof(Material), false);
                                EditorGUILayout.ObjectField("Shader", setting.ReferenceMaterial.shader, typeof(Shader),
                                    false);
                                break;
                            case MergeMaterialProcessor.UnsupportedUVTransformInReferenceMaterial setting:
                                EditorGUILayout.SelectableLabel(
                                    AAOL10N.Tr("MergeMaterial:UnsupportedUVTransformInReferenceMaterial")
                                        .Replace("{0}", string.Join(", ", setting.BadProperties)),
                                    EditorStyles.wordWrappedLabel);
                                EditorGUILayout.ObjectField("Reference Material", setting.ReferenceMaterial,
                                    typeof(Material), false);
                                break;
                            case MergeMaterialProcessor.UnknownUVTransform setting:
                                EditorGUILayout.SelectableLabel(
                                    AAOL10N.Tr("MergeMaterial:UnknownUVTransform")
                                        .Replace("{0}", string.Join(", ", setting.BadProperties)),
                                    EditorStyles.wordWrappedLabel);
                                EditorGUILayout.ObjectField("Reference Material", setting.Material, typeof(Material),
                                    false);
                                break;
                            case MergeMaterialProcessor.DifferentShaderInMergeSetting:
                                EditorGUILayout.SelectableLabel(
                                    AAOL10N.Tr("MergeMaterial:DifferentShaderInMergeSetting"),
                                    EditorStyles.wordWrappedLabel);
                                break;
                            case MergeMaterialProcessor.NotAllTexturesUsed settings:
                                EditorGUILayout.SelectableLabel(
                                    AAOL10N.Tr("MergeMaterial:ReferenceMaterial:NotAllTexturesUsed").Replace("{0}",
                                        string.Join(", ", settings.NonUsedProperties)), EditorStyles.wordWrappedLabel);
                                EditorGUILayout.ObjectField("Reference Material", settings.ReferenceMaterial,
                                    typeof(Material), false);
                                break;
                        }

                        EditorGUILayout.EndVertical();
                    }

                    var preview = cache.Texture;
                    if (!preview) preview = Assets.PreviewHereTex; // TODO: replace with error image
                    EditorGUILayout.LabelField(new GUIContent(preview), GUILayout.MaxHeight(256),
                        GUILayout.MaxHeight(256));

                    EditorGUILayout.EndVertical();

                    Utils.HorizontalLine();
                },
                _candidateMaterials.Length != 0 ? _createNewMergeInfo : null,
                postButtons: () => Utils.HorizontalLine(),
                onMoved: (aIndex, bIndex) =>
                {
                    if (aIndex < _materialEditorCaches.Length && bIndex < _materialEditorCaches.Length)
                        (_materialEditorCaches[aIndex], _materialEditorCaches[bIndex]) = (_materialEditorCaches[bIndex], _materialEditorCaches[aIndex]);
                },
                onRemoved: (index, _) =>
                {
                    if (index < _materialEditorCaches.Length)
                    {
                        DestroyImmediate(_materialEditorCaches[index].Texture);
                        ArrayUtility.RemoveAt(ref _materialEditorCaches, index);
                    }
                }
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
