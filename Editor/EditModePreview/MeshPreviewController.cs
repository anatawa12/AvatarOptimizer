using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.EditModePreview
{
    class MeshPreviewController : ScriptableSingleton<MeshPreviewController>
    {
        private static readonly Type[] EditorTypes = { };

        public GameObject targetGameObject;
        public SkinnedMeshRenderer targetRenderer;
        public bool previewing;

        private void UpdatePreviewing()
        {
            if (!previewing) return;

            bool ShouldStopPreview()
            {
                // target GameObject disappears
                if (targetGameObject == null || targetRenderer == null) return true;
                // animation mode externally exited
                if (!AnimationMode.InAnimationMode()) return true;
                // Showing Inspector changed
                if (ActiveEditorTracker.sharedTracker.activeEditors[0].target != targetGameObject) return true;
                // Preview Component Not Found
                if (EditorTypes.All(t => targetGameObject.GetComponent(t) == null)) return true;

                return false;
            }

            if (ShouldStopPreview())
                StopPreview();
        }

        private bool StartPreview(GameObject expectedGameObject = null)
        {
            // Already in AnimationMode of other object
            if (AnimationMode.InAnimationMode()) return false;
            // Previewing object
            if (previewing) return false;

            var gameObject = ActiveEditorTracker.sharedTracker.activeEditors[0].target as GameObject;
            if (gameObject == null) return false;
            if (expectedGameObject != null && expectedGameObject != gameObject) return false;
            var renderer = gameObject.GetComponent<SkinnedMeshRenderer>();
            if (renderer == null) return false;
            // Editor Components does not exists
            if (EditorTypes.All(t => gameObject.GetComponent(t) == null)) return false;

            AnimationMode.StartAnimationMode();
            targetGameObject = gameObject;
            targetRenderer = renderer;
            previewing = true;
            EditorApplication.update -= UpdatePreviewing;
            EditorApplication.update += UpdatePreviewing;
            return true;
        }

        private void StopPreview()
        {
            AnimationMode.StopAnimationMode();
            targetGameObject = null;
            targetRenderer = null;
            previewing = false;
            EditorApplication.update -= UpdatePreviewing;
        }
    }
}