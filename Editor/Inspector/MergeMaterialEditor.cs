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
                { material = null };
            _createNewMergeInfo = () => new MergeMaterial.MergeInfo
                { source = new[] { _createNewSource() } };
        }

        delegate void DrawerCallback<T>(ref T arg1, int arg2);

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
        ) where T : class
        {
            DrawList(ref array, addButton, delegate(ref T v, int i) { drawer(v, i); }, newElement, noEmpty, postButtons, onMoved, onRemoved, onAdded);
        }

        private static void DrawList<T>(
            [AllowNull] ref T[] array,
            string addButton,
            DrawerCallback<T> drawer,
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
                drawer(ref array[i], i);

                var indentedRect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
                var spacing = 1;
                var buttonRect = new Rect(
                    indentedRect.x,
                    indentedRect.y,
                    (indentedRect.width + spacing) / 3 - spacing,
                    indentedRect.height
                );
                GUILayout.BeginHorizontal();
                using (new EditorGUI.DisabledScope(i == 0))
                {
                    if (GUI.Button(buttonRect, "▲"))
                    {
                        (array[i], array[i - 1]) = (array[i - 1], array[i]);
                        onMoved?.Invoke(i, i - 1);
                    }
                }

                buttonRect.x += buttonRect.width + spacing;
                using (new EditorGUI.DisabledScope(i == array.Length - 1))
                {
                    if (GUI.Button(buttonRect, "▼"))
                    {
                        (array[i], array[i + 1]) = (array[i + 1], array[i]);
                        onMoved?.Invoke(i, i + 1);
                    }
                }

                buttonRect.x += buttonRect.width + spacing;
                using (new EditorGUI.DisabledScope(noEmpty && array.Length == 1))
                {
                    if (GUI.Button(buttonRect, "✗"))
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
                if (GUI.Button(EditorGUI.IndentedRect(EditorGUILayout.GetControlRect()), addButton))
                {
                    if (newElement == null) throw new InvalidOperationException();
                    T element;
                    ArrayUtility.Add(ref array, element = newElement());
                    onAdded?.Invoke(element);
                }

        }

        private static class Styles
        {
            public static readonly GUIContent ReferenceMaterial =
                new GUIContent(AAOL10N.Tr("MergeMaterial:Reference Material"))
                {
                    tooltip = AAOL10N.Tr("MergeMaterial:Reference Material:tooltip"),
                };
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
                    Styles.ReferenceMaterial.text = AAOL10N.Tr("MergeMaterial:label:Reference Material");
                    Styles.ReferenceMaterial.tooltip = AAOL10N.Tr("MergeMaterial:Reference Material:tooltip");
                    componentMerge.referenceMaterial =
                        EditorGUILayout.ObjectField(Styles.ReferenceMaterial,
                            componentMerge.referenceMaterial, typeof(Material), false) as Material;

                    DrawList(ref componentMerge.source, AAOL10N.Tr("MergeMaterial:button:Add Source"),
                        (mergeSource, _) =>
                        {
                            EditorGUILayout.BeginHorizontal();
                            mergeSource.material =
                                EditorGUILayout.ObjectField(
                                    mergeSource.material,
                                    typeof(Material), 
                                    false)
                                    as Material;
                            var selectedMaterial = Utils.PopupSuggestion(GetCandidateMaterials, x => x.name, GUILayout.Width(20));
                            if (selectedMaterial != null) mergeSource.material = selectedMaterial;
                            EditorGUILayout.EndHorizontal();

                            mergeSource.targetRect = EditorGUILayout.RectField(mergeSource.targetRect);
                        },
                        _createNewSource
                    );

                    componentMerge.textureSize =
                        EditorGUILayout.Vector2IntField(AAOL10N.Tr("MergeMaterial:label:Texture Size"),
                            componentMerge.textureSize);

                    componentMerge.mergedFormat =
                        (MergeMaterial.MergedTextureFormat)EditorGUILayout.EnumPopup("Format",
                            componentMerge.mergedFormat);

                    // texture settings overrides
                    EditorGUILayout.LabelField(AAOL10N.Tr("MergeMaterial:label:Texture Config Overrides"));
                    EditorGUI.indentLevel++;
                    DrawList(ref componentMerge.textureConfigOverrides, AAOL10N.Tr("MergeMaterial:button:Add Texture Config Override"),
                        delegate(ref MergeMaterial.TextureConfigOverride @override, int _)
                        {
                            EditorGUILayout.BeginHorizontal();
                            @override.textureName =
                                EditorGUILayout.TextField(AAOL10N.Tr("MergeMaterial:label:Texture Name"),
                                    @override.textureName);
                            var texture = Utils.PopupSuggestion(() =>
                                    _materialEditorCaches[i].ValidatedInfo?.ReferenceInformation?.DefaultResult
                                        ?.TextureUsageInformationList?.Select(x => x.MaterialPropertyName)?.ToArray() ??
                                    Array.Empty<string>(),
                                x => x,
                                GUILayout.Width(20));
                            if (texture != null) @override.textureName = texture;
                            EditorGUILayout.EndHorizontal();

                            EditorGUI.indentLevel++;
                            @override.sizeOverride = EditorGUILayout.Vector2IntField(
                                AAOL10N.Tr("MergeMaterial:label:Texture Size Override"), @override.sizeOverride);
                            @override.formatOverride =
                                (MergeMaterial.MergedTextureFormat)EditorGUILayout.EnumPopup(
                                    AAOL10N.Tr("MergeMaterial:label:Texture Format Override"),
                                    @override.formatOverride);
                            EditorGUI.indentLevel--;
                        },
                        () => new MergeMaterial.TextureConfigOverride()
                        {
                            sizeOverride = componentMerge.textureSize,
                            formatOverride = componentMerge.mergedFormat,
                        });
                    EditorGUI.indentLevel--;

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
                                    AAOL10N.Tr("MergeMaterial:error:UnsupportedShaderInMergeSetting"),
                                    EditorStyles.wordWrappedLabel);
                                EditorGUILayout.ObjectField("Reference Material", setting.ReferenceMaterial,
                                    typeof(Material), false);
                                EditorGUILayout.ObjectField("Shader", setting.ReferenceMaterial.shader, typeof(Shader),
                                    false);
                                break;
                            case MergeMaterialProcessor.UnsupportedUVTransformInReferenceMaterial setting:
                                EditorGUILayout.SelectableLabel(
                                    AAOL10N.Tr("MergeMaterial:error:UnsupportedUVTransformInReferenceMaterial")
                                        .Replace("{0}", string.Join(", ", setting.BadProperties)),
                                    EditorStyles.wordWrappedLabel);
                                EditorGUILayout.ObjectField("Reference Material", setting.ReferenceMaterial,
                                    typeof(Material), false);
                                break;
                            case MergeMaterialProcessor.UnknownUVTransform setting:
                                EditorGUILayout.SelectableLabel(
                                    AAOL10N.Tr("MergeMaterial:error:UnknownUVTransform")
                                        .Replace("{0}", string.Join(", ", setting.BadProperties)),
                                    EditorStyles.wordWrappedLabel);
                                EditorGUILayout.ObjectField("Reference Material", setting.Material, typeof(Material),
                                    false);
                                break;
                            case MergeMaterialProcessor.DifferentShaderInMergeSetting:
                                EditorGUILayout.SelectableLabel(
                                    AAOL10N.Tr("MergeMaterial:error:DifferentShaderInMergeSetting"),
                                    EditorStyles.wordWrappedLabel);
                                break;
                            case MergeMaterialProcessor.NotAllTexturesUsed settings:
                                EditorGUILayout.SelectableLabel(
                                    AAOL10N.Tr("MergeMaterial:error:ReferenceMaterial:NotAllTexturesUsed").Replace("{0}",
                                        string.Join(", ", settings.NonUsedProperties)), EditorStyles.wordWrappedLabel);
                                EditorGUILayout.ObjectField("Reference Material", settings.ReferenceMaterial,
                                    typeof(Material), false);
                                break;
                        }

                        EditorGUILayout.EndVertical();
                    }

                    var preview = cache.Texture;
                    if (preview)
                    {
                        EditorGUILayout.LabelField(new GUIContent(preview), GUILayout.MaxHeight(256),
                            GUILayout.MaxHeight(256));
                    }


                    EditorGUILayout.EndVertical();

                    Utils.HorizontalLine();
                },
                _createNewMergeInfo,
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
                EditorUtility.SetDirty(target);
        }

        private Material[] GetCandidateMaterials()
        {
            var component = (MergeMaterial)target;
            return EditSkinnedMeshComponentUtil.GetMaterials(component.GetComponent<SkinnedMeshRenderer>(), component)
                .Where(x => x)
                .ToArray()!;
        }
    }
}
