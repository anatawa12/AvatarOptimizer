using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using UnityEditor;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(MergeToonLitTexture))]
    internal class MergeToonLitTextureEditor : Editor
    {
        private Material[] _upstreamMaterials;
        private (Material mat, int index)[] _materials;
        
        private (Material mat, int index)[] _candidateMaterials;
        private string[] _candidateNames;

        private Texture[] _generatedPreviews;

        private readonly Func<MergeToonLitTexture.MergeSource> _createNewSource;
        private readonly Func<MergeToonLitTexture.MergeInfo> _createNewMergeInfo;

        public MergeToonLitTextureEditor()
        {
            // ReSharper disable once PossibleNullReferenceException
            _createNewSource = () => new MergeToonLitTexture.MergeSource
                { materialIndex = _candidateMaterials[0].index };
            _createNewMergeInfo = () => new MergeToonLitTexture.MergeInfo 
                { source = new [] {_createNewSource()} };
        }

        private static void DrawList<T>(
            ref T[] array,
            Action<T, int> drawer,
            Func<T> newElement,
            bool noEmpty = false,
            Action postButtons = null,
            Action onMoved = null,
            Action<T> onRemoved = null,
            Action<T> onAdded = null
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
                if (GUILayout.Button("Add Source"))
                {
                    Debug.Assert(newElement != null, nameof(newElement) + " != null");
                    T element;
                    ArrayUtility.Add(ref array, element = newElement());
                    onAdded(element);
                }

        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("The component to merge multiple VRChat ToonLit materials.");
            EditorGUILayout.LabelField("This is for quest avoid limitation");

            if (targets.Length != 1)
            {
                EditorGUILayout.LabelField("MultiTarget Editing is not supported");
                return;
            }

            EditorGUI.BeginChangeCheck();

            var component = (MergeToonLitTexture)target;

            DrawList(ref component.merges, (componentMerge, i) =>
                {
                    DrawList(ref componentMerge.source, (mergeSource, _2) =>
                        {
                            var found = _materials.FirstOrDefault(x => x.index == mergeSource.materialIndex);
                            _candidateNames[0] = found.mat != null ? found.mat.name : "(invalid)";
                            EditorGUI.BeginChangeCheck();
                            var newIndex = EditorGUILayout.Popup(0, _candidateNames);
                            if (EditorGUI.EndChangeCheck() && newIndex != 0)
                                mergeSource.materialIndex = _candidateMaterials[newIndex - 1].index;

                            EditorGUI.BeginChangeCheck();
                            mergeSource.targetRect = EditorGUILayout.RectField(mergeSource.targetRect);
                        },
                        _candidateMaterials.Length != 0 ? _createNewSource : null,
                        onMoved: OnChanged,
                        onAdded: _ => OnChanged(),
                        onRemoved: _ => OnChanged()
                    );

                    componentMerge.textureSize =
                        EditorGUILayout.Vector2IntField("Texture Size", componentMerge.textureSize);

                    var preview = _generatedPreviews != null ? _generatedPreviews[i] : Utils.PreviewHereTex;
                    EditorGUILayout.LabelField(new GUIContent(preview), GUILayout.MaxHeight(256), GUILayout.MaxHeight(256));

                    HorizontalLine();
                },
                _candidateMaterials.Length != 0 ? _createNewMergeInfo : null,
                postButtons: () => HorizontalLine(),
                onMoved: OnChanged,
                onAdded: _ => OnChanged(),
                onRemoved: _ => OnChanged()
            );

            if (EditorGUI.EndChangeCheck())
                OnChanged();

            if (GUILayout.Button("Generate Preview"))
            {
                _generatedPreviews = MergeToonLitTextureProcessor.GenerateTextures(component, _upstreamMaterials);
            }            
        }

        private void OnChanged()
        {
            _generatedPreviews = null;
            var component = (MergeToonLitTexture)target;
            EditorUtility.SetDirty(component);
            var usedIndices = new HashSet<int>(component.merges.SelectMany(x => x.source.Select(y => y.materialIndex)));
            _candidateMaterials = _materials.Where(x => !usedIndices.Contains(x.index)).ToArray();
            _candidateNames = new []{""}.Concat(_candidateMaterials.Select(x => x.mat.name)).ToArray();
        }

        private void HorizontalLine(bool marginTop = true, bool marginBottom = true)
        {
            const float margin = 17f / 2;
            var maxHeight = 1f;
            if (marginTop) maxHeight += margin;
            if (marginBottom) maxHeight += margin;

            var rect = GUILayoutUtility.GetRect(
                EditorGUIUtility.fieldWidth, float.MaxValue, 
                1, maxHeight, GUIStyle.none);
            if (marginTop && marginBottom)
                rect.y += rect.height / 2 - 0.5f;
            else if (marginTop)
                rect.y += rect.height - 1f;
            else if (marginBottom)
                rect.y += 0;
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }

        private void OnEnable()
        {
            var component = (MergeToonLitTexture)target;

            // find materials with toonlit
            _upstreamMaterials = EditSkinnedMeshComponentUtil
                .GetMaterials(component.GetComponent<SkinnedMeshRenderer>(), component);
            _materials = _upstreamMaterials
                .Select((mat, index) => (mat, index))
                .Where(x => x.mat.shader == Utils.ToonLitShader)
                .ToArray();
            OnChanged();
        }
    }
}

