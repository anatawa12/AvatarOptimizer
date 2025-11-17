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
        public const string MaskTextureEditorToken = "com.anatawa12.avatar-optimizer.remove-mesh-by-mask-editor";
        private static readonly Vector2Int DefaultTextureSize = new(1024, 1024);

        private SerializedProperty _materials = null!; // Initialized in OnEnable
        private Renderer _renderer = null!; // Initialized in OnEnable
        public bool automaticallySetWeightWhenToggle;

        private void OnEnable()
        {
            NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace;
            _renderer = ((Component)target).GetComponent<Renderer>();
            _materials = serializedObject.FindProperty(nameof(RemoveMeshByMask.materials));
        }

        protected override void OnInspectorGUIInner()
        {
            GenericEditSkinnedMeshComponentsEditor.DrawUnexpectedRendererError(targets);

            // if there is source skinned mesh component, show error
            if (((Component)target).TryGetComponent<ISourceSkinnedMeshComponent>(out _))
            {
                EditorGUILayout.HelpBox(AAOL10N.Tr("NoSourceEditSkinnedMeshComponent:HasSourceSkinnedMeshComponent"), MessageType.Error);
                return;
            }

            Mesh? mesh = null;
            if (_renderer is SkinnedMeshRenderer skinnedMeshRenderer)
                mesh = skinnedMeshRenderer.sharedMesh;
            else if (_renderer is MeshRenderer && _renderer.TryGetComponent<MeshFilter>(out var filter))
                mesh = filter.sharedMesh;

            if (!_renderer || mesh == null)
            {
                EditorGUILayout.HelpBox(AAOL10N.Tr("RemoveMeshByMask:warning:NoMesh"), MessageType.Warning);
                return;
            }

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
            Renderer renderer, int slot)
        {
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
                                texture = CreateTexture(DefaultTextureSize, Color.white);
                                break;
                            case RemoveMeshByMask.RemoveMode.RemoveWhite:
                                texture = CreateTexture(DefaultTextureSize, Color.black);
                                break;
                        }

                        mask.serializedObject.Update();
                        mask.objectReferenceValue = texture;
                        mask.serializedObject.ApplyModifiedProperties();

#if AAO_MASK_TEXTURE_EDITOR
                        if (texture != null)
                        {
                            MaskTextureEditor.Window.TryOpen(texture, renderer, slot, MaskTextureEditorToken);
                        }
#endif

                        // Exit GUI to avoid "EndLayoutGroup: BeginLayoutGroup must be called first."
                        GUIUtility.ExitGUI();
                    }
                }
                else
                {
#if AAO_MASK_TEXTURE_EDITOR
                    var isOpenWindow = MaskTextureEditor.Window.IsOpenFor(renderer, slot, MaskTextureEditorToken);
#else
                    var isOpenWindow = false;
#endif
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
#if AAO_MASK_TEXTURE_EDITOR
                            if (shouldOpenWindow)
                            {
                                MaskTextureEditor.Window.TryOpen(texture, renderer, slot, MaskTextureEditorToken);
                            }
                            else
                            {
                                MaskTextureEditor.Window.TryClose();
                            }
#else
                            switch (EditorUtility.DisplayDialogComplex(
                                AAOL10N.Tr("RemoveMeshByMask:dialog:info"),
                                AAOL10N.Tr("RemoveMeshByMask:dialog:maskTextureEditorNotFound"),
                                AAOL10N.Tr("RemoveMeshByMask:dialog:addRepo"),
                                AAOL10N.Tr("RemoveMeshByMask:dialog:cancel"),
                                AAOL10N.Tr("RemoveMeshByMask:dialog:openWeb")))
                            {
                                case 0:
                                    Application.OpenURL("vcc://vpm/addRepo?url=https://vpm.nekobako.net/index.json");
                                    break;
                                case 2:
                                    Application.OpenURL("https://github.com/nekobako/MaskTextureEditor");
                                    break;
                            }
#endif

                            // Exit GUI to avoid "EndLayoutGroup: BeginLayoutGroup must be called first."
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }

            EditorGUILayout.PropertyField(mode);
        }

        private static Texture2D? CreateTexture(Vector2Int size, Color color)
        {
            var path = EditorUtility.SaveFilePanelInProject(
                AAOL10N.Tr("RemoveMeshByMask:dialog:create"),
                string.Empty, "png", string.Empty);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            var texture = new Texture2D(size.x, size.y);
            for (var x = 0; x < size.x; x++)
            {
                for (var y = 0; y < size.y; y++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();

            try
            {
                System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());

                AssetDatabase.ImportAsset(path);

                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.isReadable = true;
                importer.SaveAndReimport();

                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog(
                    AAOL10N.Tr("RemoveMeshByMask:dialog:error"),
                    AAOL10N.Tr("RemoveMeshByMask:dialog:createMaskFailed"),
                    "OK");

                Debug.LogError(e);
            }
            finally
            {
                DestroyImmediate(texture);
            }

            return null;
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
