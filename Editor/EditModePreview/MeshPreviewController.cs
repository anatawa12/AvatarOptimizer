using System;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.EditModePreview
{
    class MeshPreviewController : ScriptableSingleton<MeshPreviewController>
    {
        public RemoveMeshPreviewController PreviewController;
        public bool previewing;
        public static bool Previewing => instance.previewing;

        private void UpdatePreviewing()
        {
            if (!previewing)
            {
                EditorApplication.update -= UpdatePreviewing;
            }

            if (PreviewController.UpdatePreviewing())
                StopPreview();
        }

        public bool StartPreview(GameObject expectedGameObject = null)
        {
            // Already in AnimationMode of other object
            if (AnimationMode.InAnimationMode()) return false;
            // Previewing object
            if (previewing) return false;

            try
            {
                PreviewController = new RemoveMeshPreviewController(expectedGameObject);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }

            previewing = true;
            EditorApplication.update -= UpdatePreviewing;
            EditorApplication.update += UpdatePreviewing;
            PreviewController.BeginPreview();
            return true;
        }

        public void StopPreview()
        {
            previewing = false;
            PreviewController.Dispose();
            PreviewController = null;
            EditorApplication.update -= UpdatePreviewing;
        }
    }
}
