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

        public Mesh previewMesh;
        public Mesh originalMesh;
        public GameObject gameObject;

        protected MeshPreviewController()
        {
            EditorApplication.delayCall += Initialize;
        }

        private void Initialize()
        {
            if (previewing)
            {
                PreviewController = new RemoveMeshPreviewController(gameObject, originalMesh, previewMesh);
                EditorApplication.update -= UpdatePreviewing;
                EditorApplication.update += UpdatePreviewing;
            }
        }

        private void UpdatePreviewing()
        {
            if (!previewing)
            {
                EditorApplication.update -= UpdatePreviewing;
            }

            // Showing Inspector changed
            if (ActiveEditorTracker.sharedTracker.activeEditors[0].target != gameObject)
            {
                StopPreview();
                return;
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
                var targetGameObject = ActiveEditorTracker.sharedTracker.activeEditors[0].target as GameObject;
                if (targetGameObject == null)
                    throw new Exception("Already In Animation Mode");
                if (expectedGameObject != null && expectedGameObject != targetGameObject)
                    throw new Exception("Unexpected GameObject");

                PreviewController = new RemoveMeshPreviewController(targetGameObject);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }

            gameObject = PreviewController.TargetGameObject;
            previewMesh = PreviewController.PreviewMesh;
            originalMesh = PreviewController.OriginalMesh;

            previewing = true;
            EditorApplication.update -= UpdatePreviewing;
            EditorApplication.update += UpdatePreviewing;
            AnimationMode.StartAnimationMode();
            try
            {
                AnimationMode.BeginSampling();

                AnimationMode.AddPropertyModification(
                    EditorCurveBinding.PPtrCurve("", typeof(SkinnedMeshRenderer), "m_Mesh"),
                    new PropertyModification
                    {
                        target = PreviewController.TargetRenderer,
                        propertyPath = "m_Mesh",
                        objectReference = PreviewController.OriginalMesh,
                    }, 
                    true);

                PreviewController.TargetRenderer.sharedMesh = PreviewController.PreviewMesh;
            }
            finally
            {
                AnimationMode.EndSampling();   
            }
            return true;
        }

        private void OnDisable()
        {
            StopPreview();
        }

        public void StopPreview()
        {
            previewing = false;
            AnimationMode.StopAnimationMode();
            PreviewController.Dispose();
            PreviewController = null;
            EditorApplication.update -= UpdatePreviewing;
        }
    }
}
