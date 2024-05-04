using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.MaskTextureEditor
{
    internal static class Inspector
    {
        private readonly static Vector2Int DefaultTextureSize = new Vector2Int(1024, 1024);

        public static void DrawFields(
            SkinnedMeshRenderer renderer, int subMesh,
            SerializedProperty mask,
            SerializedProperty mode)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(mask);

                var texture = mask.objectReferenceValue as Texture2D;
                if (texture == null)
                {
                    if (GUILayout.Button(AAOL10N.Tr("MaskTextureEditor:create"), GUILayout.ExpandWidth(false)))
                    {
                        mask.serializedObject.Update();

                        switch ((RemoveMeshByMask.RemoveMode)mode.intValue)
                        {
                            case RemoveMeshByMask.RemoveMode.RemoveBlack:
                                mask.objectReferenceValue = CreateTexture(DefaultTextureSize, Color.white);
                                break;
                            case RemoveMeshByMask.RemoveMode.RemoveWhite:
                                mask.objectReferenceValue = CreateTexture(DefaultTextureSize, Color.black);
                                break;
                        }

                        mask.serializedObject.ApplyModifiedProperties();

                        // Exit GUI to avoid "EndLayoutGroup: BeginLayoutGroup must be called first."
                        GUIUtility.ExitGUI();
                    }
                }
                else
                {
                    var extension = Path.GetExtension(AssetDatabase.GetAssetPath(texture));
                    using (new EditorGUI.DisabledScope(extension != ".png"))
                    {
                        var isOpen = Window.IsOpen(renderer, subMesh, texture);
                        var shouldOpen = GUILayout.Toggle(isOpen,
                            AAOL10N.Tr("MaskTextureEditor:edit"),
                            GUI.skin.button, GUILayout.ExpandWidth(false));
                        if (isOpen != shouldOpen)
                        {
                            if (EditorWindow.HasOpenInstances<Window>())
                            {
                                EditorWindow.GetWindow<Window>().Close();
                            }
                            if (shouldOpen)
                            {
                                EditorWindow.GetWindow<Window>().Open(renderer, subMesh, texture);
                            }
                        }
                    }
                }
            }

            EditorGUILayout.PropertyField(mode);
        }

        private static Texture2D CreateTexture(Vector2Int size, Color color)
        {
            var path = EditorUtility.SaveFilePanelInProject(
                AAOL10N.Tr("MaskTextureEditor:create"),
                string.Empty, "png", string.Empty);

            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            var painter = ScriptableObject.CreateInstance<TexturePainter>();
            painter.Init(size, color);

            var texture = new Texture2D(0, 0);
            painter.Save(texture);

            try
            {
                File.WriteAllBytes(path, texture.EncodeToPNG());

                AssetDatabase.ImportAsset(path);

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                importer.isReadable = true;
                importer.SaveAndReimport();

                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog(
                    AAOL10N.Tr("MaskTextureEditor:errorTitle"),
                    AAOL10N.Tr("MaskTextureEditor:errorMessageCreateFailed"),
                    AAOL10N.Tr("MaskTextureEditor:errorButton"));

                Debug.LogError(e);
            }
            finally
            {
                Object.DestroyImmediate(painter);
                Object.DestroyImmediate(texture);
            }

            return null;
        }
    }
}
