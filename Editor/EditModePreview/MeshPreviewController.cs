using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.EditModePreview
{
    class MeshPreviewController : ScriptableSingleton<MeshPreviewController>
    {
        public RemoveMeshPreviewController PreviewController;
        public bool previewing;
        public static bool Previewing => instance.previewing;

        [SerializeField] private Mesh previewMesh;
        [SerializeField] private Mesh originalMesh;
        [SerializeField] private GameObject gameObject;
        [SerializeField] private Object driverCached;

        private Object DriverCached => driverCached ? driverCached : driverCached = AnimationMode.CreateDriver();

        private void OnEnable()
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

        private void OnDisable()
        {
            PreviewController?.Dispose();
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
            AnimationMode.StartAnimationMode(DriverCached);
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

        public void StopPreview()
        {
            previewing = false;
            AnimationMode.StopAnimationMode(DriverCached);
            PreviewController.Dispose();
            PreviewController = null;
            EditorApplication.update -= UpdatePreviewing;
        }

        // TODO: in Unity 2022, this class must be removed and replaced with UnityEditor.AnimationMode
        public static class AnimationMode
        {
            public static void BeginSampling() => UnityEditor.AnimationMode.BeginSampling();
            public static void EndSampling() => UnityEditor.AnimationMode.EndSampling();
            public static bool InAnimationMode() => UnityEditor.AnimationMode.InAnimationMode();
            public static void StartAnimationMode(Object o) => StartAnimationMode("StartAnimationMode", o);
            public static void StopAnimationMode(Object o) => StartAnimationMode("StopAnimationMode", o);

            public static void AddPropertyModification(EditorCurveBinding binding, PropertyModification modification,
                bool keepPrefabOverride) =>
                UnityEditor.AnimationMode.AddPropertyModification(binding, modification, keepPrefabOverride);

            public static Object CreateDriver() =>
                ScriptableObject.CreateInstance(
                    typeof(UnityEditor.AnimationMode).Assembly.GetType("UnityEditor.AnimationModeDriver"));

            private static void StartAnimationMode(string name, Object o)
            {
                var method = typeof(UnityEditor.AnimationMode).GetMethod(name,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(Object) },
                    null);
                System.Diagnostics.Debug.Assert(method != null, nameof(method) + " != null");
                method.Invoke(null, new object[] { o });
            }
        }
    }
}
