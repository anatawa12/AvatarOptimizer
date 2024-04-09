using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(RemoveMeshByMask))]
    internal class RemoveMeshByMaskEditor : AvatarTagComponentEditorBase
    {
        private SerializedProperty _materials;
        private SkinnedMeshRenderer _renderer;
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
            if ((Component)((Component)target).GetComponent<ISourceSkinnedMeshComponent>())
            {
                EditorGUILayout.HelpBox(AAOL10N.Tr("NoSourceEditSkinnedMeshComponent:HasSourceSkinnedMeshComponent"), MessageType.Error);
                return;
            }

            if (!_renderer || !_renderer.sharedMesh)
            {
                EditorGUILayout.HelpBox(AAOL10N.Tr("RemoveMeshByMask:warning:NoMesh"), MessageType.Warning);
                return;
            }

            EditModePreview.MeshPreviewController.ShowPreviewControl((Component)target);

            var mesh = _renderer.sharedMesh;
            var template = AAOL10N.Tr("RemoveMeshByMask:prop:enabled");

            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                var slotConfig = _materials.GetArrayElementAtIndex(i);

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

                        EditorGUILayout.PropertyField(mask);
                        EditorGUILayout.PropertyField(mode);
                        var texture = mask.objectReferenceValue as Texture2D;
                        if (texture == null)
                        {
                            EditorGUILayout.HelpBox(AAOL10N.Tr("RemoveMeshByMask:error:maskIsNone"), MessageType.Error);
                        }
                        else if (texture.isReadable == false)
                        {
                            EditorGUILayout.HelpBox(AAOL10N.Tr("RemoveMeshByMask:error:maskIsNotReadable"), MessageType.Error);
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
    }
}
