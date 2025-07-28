using Unity.Collections;
using UnityEditor;
using UnityEngine;
#if AAO_MASK_TEXTURE_EDITOR
using MaskTextureEditor = net.nekobako.MaskTextureEditor.Editor;
#endif

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(RemoveMeshByMask))]
    internal class RemoveMeshByMaskEditor : AvatarTagComponentEditorBase
    {
#if AAO_MASK_TEXTURE_EDITOR
        public const string MaskTextureEditorToken = "com.anatawa12.avatar-optimizer.remove-mesh-by-mask-editor";
        private static readonly Vector2Int DefaultTextureSize = new(1024, 1024);
#endif

        private SerializedProperty _materials = null!; // Initialized in OnEnable
        private SkinnedMeshRenderer _renderer = null!; // Initialized in OnEnable
        public bool automaticallySetWeightWhenToggle;

        private void OnEnable()
        {
            NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace;
            _renderer = ((Component)target).GetComponent<SkinnedMeshRenderer>();
            _materials = serializedObject.FindProperty(nameof(RemoveMeshByMask.materials));
        }

        protected override void OnInspectorGUIInner()
        {
            // if there is source skinned mesh component, show error
            if (((Component)target).TryGetComponent<ISourceSkinnedMeshComponent>(out _))
            {
                EditorGUILayout.HelpBox(AAOL10N.Tr("NoSourceEditSkinnedMeshComponent:HasSourceSkinnedMeshComponent"), MessageType.Error);
                return;
            }

            if (!_renderer || !_renderer.sharedMesh)
            {
                EditorGUILayout.HelpBox(AAOL10N.Tr("RemoveMeshByMask:warning:NoMesh"), MessageType.Warning);
                return;
            }

            var mesh = _renderer.sharedMesh;
            var template = AAOL10N.Tr("RemoveMeshByMask:prop:enabled");

            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                var slotConfig = i < _materials.arraySize ? _materials.GetArrayElementAtIndex(i) : null;

                var enabledName = string.Format(template, i);

                if (slotConfig != null)
                {
                    var enabled = slotConfig.FindPropertyRelative(nameof(RemoveMeshByMask.MaterialSlot.enabled));
                    EditorGUILayout.PropertyField(enabled, new GUIContent(enabledName));
                    if (enabled.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        var mask = slotConfig.FindPropertyRelative(nameof(RemoveMeshByMask.MaterialSlot.mask));
                        var mode = slotConfig.FindPropertyRelative(nameof(RemoveMeshByMask.MaterialSlot.mode));
                        DrawMaskAndMode(mask, mode, _renderer, i);
                        var texture = mask.objectReferenceValue as Texture2D;
                        if (texture == null)
                        {
                            EditorGUILayout.HelpBox(AAOL10N.Tr("RemoveMeshByMask:error:maskIsNone"), MessageType.Error);
                        }
                        else if (texture.isReadable == false)
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.HelpBox(AAOL10N.Tr("RemoveMeshByMask:error:maskIsNotReadable"), MessageType.Error);

                            var importer = GetTextureImporter(texture);
                            if (importer == null)
                            {
                                var fixContent = new GUIContent(AAOL10N.Tr("RemoveMeshByMask:button:makeReadable"));
                                fixContent.tooltip = AAOL10N.Tr("RemoveMeshByMask:tooltip:textureIsNotImported");
                                EditorGUI.BeginDisabledGroup(true);
                                GUILayout.Button("", GUILayout.Height(38));
                                EditorGUI.EndDisabledGroup();
                            }
                            else
                            {
                                var fixContent = new GUIContent(AAOL10N.Tr("RemoveMeshByMask:button:makeReadable"));
                                if (GUILayout.Button(fixContent, GUILayout.Height(38)))
                                {
                                    importer.isReadable = true;
                                    importer.SaveAndReimport();
                                }
                            }

                            GUILayout.EndHorizontal();
                        }
                        EditorGUI.indentLevel--;
                    }
                }
                else
                {
                    // if the component is relatively new, it may not have the property so we simulate it
                    var enabled = EditorGUILayout.ToggleLeft(enabledName, false);

                    if (enabled)
                    {
                        _materials.arraySize = mesh.subMeshCount;
                        slotConfig = _materials.GetArrayElementAtIndex(i);
                        slotConfig.FindPropertyRelative(nameof(RemoveMeshByMask.MaterialSlot.enabled)).boolValue = true;
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawMaskAndMode(
            SerializedProperty mask,
            SerializedProperty mode,
            SkinnedMeshRenderer renderer, int slot)
        {
#if AAO_MASK_TEXTURE_EDITOR
            using (new EditorGUILayout.HorizontalScope())
            {
                var texture = mask.objectReferenceValue as Texture2D;
                if (texture == null)
                {
                    EditorGUILayout.PropertyField(mask);

                    if (GUILayout.Button(AAOL10N.Tr("RemoveMeshByMask:button:create"), GUILayout.ExpandWidth(false)))
                    {
                        switch ((RemoveMeshByMask.RemoveMode)mode.intValue)
                        {
                            case RemoveMeshByMask.RemoveMode.RemoveBlack:
                                texture = MaskTextureEditor.Utility.CreateTexture(DefaultTextureSize, Color.white);
                                break;
                            case RemoveMeshByMask.RemoveMode.RemoveWhite:
                                texture = MaskTextureEditor.Utility.CreateTexture(DefaultTextureSize, Color.black);
                                break;
                        }

                        mask.serializedObject.Update();
                        mask.objectReferenceValue = texture;
                        mask.serializedObject.ApplyModifiedProperties();

                        if (texture != null)
                        {
                            MaskTextureEditor.Window.TryOpen(texture, renderer, slot, MaskTextureEditorToken);
                        }

                        // Exit GUI to avoid "EndLayoutGroup: BeginLayoutGroup must be called first."
                        GUIUtility.ExitGUI();
                    }
                }
                else
                {
                    var isOpenWindow = MaskTextureEditor.Window.IsOpenFor(renderer, slot, MaskTextureEditorToken);
                    using (new EditorGUI.DisabledScope(isOpenWindow))
                    {
                        EditorGUILayout.PropertyField(mask);
                    }

                    var extension = System.IO.Path.GetExtension(AssetDatabase.GetAssetPath(texture));
                    using (new EditorGUI.DisabledScope(extension != ".png"))
                    {
                        var shouldOpenWindow = GUILayout.Toggle(isOpenWindow,
                            AAOL10N.Tr("RemoveMeshByMask:button:edit"),
                            GUI.skin.button, GUILayout.ExpandWidth(false));
                        if (isOpenWindow != shouldOpenWindow)
                        {
                            if (shouldOpenWindow)
                            {
                                MaskTextureEditor.Window.TryOpen(texture, renderer, slot, MaskTextureEditorToken);
                            }
                            else
                            {
                                MaskTextureEditor.Window.TryClose();
                            }

                            // Exit GUI to avoid "EndLayoutGroup: BeginLayoutGroup must be called first."
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }
#else
            EditorGUILayout.PropertyField(mask);
#endif

            EditorGUILayout.PropertyField(mode);
        }

        TextureImporter? GetTextureImporter(Texture2D texture)
        {
            var path = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(path))
                return null;
            return AssetImporter.GetAtPath(path) as TextureImporter;
        }
    }
}
