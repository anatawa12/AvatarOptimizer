using System;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(RemoveMeshByUVTile))]
    internal class RemoveMeshByUVTileEditor : AvatarTagComponentEditorBase
    {
        private SerializedProperty _materials = null!; // Initialized in OnEnable
        private SkinnedMeshRenderer? _renderer; // Initialized in OnEnable
        public bool automaticallySetWeightWhenToggle;

        private void OnEnable()
        {
            NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace;
            _renderer = ((Component)target).GetComponent<SkinnedMeshRenderer>();
            _materials = serializedObject.FindProperty(nameof(RemoveMeshByUVTile.materials));
        }

        protected override void OnInspectorGUIInner()
        {
            // if there is source skinned mesh component, show error
            if (((Component)target).TryGetComponent<ISourceSkinnedMeshComponent>(out _))
            {
                EditorGUILayout.HelpBox(AAOL10N.Tr("NoSourceEditSkinnedMeshComponent:HasSourceSkinnedMeshComponent"), MessageType.Error);
                return;
            }

            if (_renderer == null || _renderer.sharedMesh == null)
            {
                EditorGUILayout.HelpBox(AAOL10N.Tr("RemoveMeshByUVTile:warning:NoMesh"), MessageType.Warning);
                return;
            }

            var mesh = _renderer.sharedMesh;

            _materials.arraySize = mesh.subMeshCount;

            EditorGUILayout.LabelField("Remove Tiles", EditorStyles.boldLabel);

            var template = AAOL10N.Tr("RemoveMeshByUVTile:prop:MaterialSlot");
            var content = new GUIContent("Material Slot 0");
            for (var i = 0; i < mesh.subMeshCount; i++)
            {
                content.text = string.Format(template, i);
                EditorGUILayout.PropertyField(_materials.GetArrayElementAtIndex(i), content);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomPropertyDrawer(typeof(RemoveMeshByUVTile.MaterialSlot))]
    class MaterialSlotDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded) return EditorGUIUtility.singleLineHeight;
            // one line for foldout, 4 lines for each tile, one line for uv channel
            return EditorGUIUtility.singleLineHeight * 6 + EditorGUIUtility.standardVerticalSpacing * 5;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var lineOffset = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            position.height = EditorGUIUtility.singleLineHeight;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label);
            if (!property.isExpanded) return;

            position.y += lineOffset;
            var uvChannelPosition = position;
            EditorGUI.indentLevel++;
            EditorGUI.PropertyField(uvChannelPosition,
                property.FindPropertyRelative(nameof(RemoveMeshByUVTile.MaterialSlot.uvChannel)));
            EditorGUI.indentLevel--;

            position.xMin += 15f * (EditorGUI.indentLevel + 1);
            position.y += lineOffset;
            var line3 = position;
            position.y += lineOffset;
            var line2 = position;
            position.y += lineOffset;
            var line1 = position;
            position.y += lineOffset;
            var line0 = position;

            var (tile0, tile1, tile2, tile3) = SplitToFour(line0);
            var (tile4, tile5, tile6, tile7) = SplitToFour(line1);
            var (tile8, tile9, tile10, tile11) = SplitToFour(line2);
            var (tile12, tile13, tile14, tile15) = SplitToFour(line3);

            Span<Rect> tiles = stackalloc[]
            {
                tile0, tile1, tile2, tile3, 
                tile4, tile5, tile6, tile7, 
                tile8, tile9, tile10, tile11, 
                tile12, tile13, tile14, tile15,
            };

            var template = AAOL10N.Tr("RemoveMeshByUVTile:prop:Tile");
            for (var i = 0; i < 16; i++)
            {
                var tileProp = property.FindPropertyRelative($"removeTile{i}");
                var text = new GUIContent(string.Format(template, i));
                text = EditorGUI.BeginProperty(tiles[i], text, tileProp);
                EditorGUI.BeginChangeCheck();
                var value = GUI.Toggle(tiles[i], tileProp.boolValue, text);
                if (EditorGUI.EndChangeCheck())
                    tileProp.boolValue = value;
                EditorGUI.EndProperty();
            }
        }

        private (Rect, Rect, Rect, Rect) SplitToFour(Rect rect)
        {
            var space = EditorGUIUtility.standardVerticalSpacing;
            var width = (rect.width + space) / 4 - space;
            var offsetX = width + space;
            return (
                rect with { width = width, x = rect.x + offsetX * 0 },
                rect with { width = width, x = rect.x + offsetX * 1 },
                rect with { width = width, x = rect.x + offsetX * 2 },
                rect with { width = width, x = rect.x + offsetX * 3 }
            );
        }
    }
}
