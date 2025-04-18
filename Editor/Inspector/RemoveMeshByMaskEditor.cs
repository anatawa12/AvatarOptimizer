using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(RemoveMeshByMask))]
    internal class RemoveMeshByMaskEditor : AvatarTagComponentEditorBase
    {
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
                        MaskTextureEditor.Inspector.DrawFields(_renderer, i, mask, mode);
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

        TextureImporter? GetTextureImporter(Texture2D texture)
        {
            var path = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(path))
                return null;
            return AssetImporter.GetAtPath(path) as TextureImporter;
        }
    }
}
