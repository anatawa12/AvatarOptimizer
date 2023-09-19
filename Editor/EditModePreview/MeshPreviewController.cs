using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
#if !UNITY_2020_1_OR_NEWER
using AnimationModeDriver = UnityEngine.Object;
#endif

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
        [SerializeField] private AnimationModeDriver driverCached;

        private AnimationModeDriver DriverCached => driverCached ? driverCached : driverCached = CreateDriver();

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

        private Object ActiveEditor()
        {
            var editors = ActiveEditorTracker.sharedTracker.activeEditors;
            return editors.Length == 0 ? null : editors[0].target;
        } 

        private void UpdatePreviewing()
        {
            if (!previewing)
            {
                EditorApplication.update -= UpdatePreviewing;
            }

            // Showing Inspector changed
            if (ActiveEditor() != gameObject)
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
                var targetGameObject = ActiveEditor() as GameObject;
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

#if !UNITY_2020_1_OR_NEWER
        private static AnimationModeDriver CreateDriver() =>
            ScriptableObject.CreateInstance(
                typeof(UnityEditor.AnimationMode).Assembly.GetType("UnityEditor.AnimationModeDriver"));
#else
        private static AnimationModeDriver CreateDriver() => ScriptableObject.CreateInstance<AnimationModeDriver>();
#endif

#if !UNITY_2020_1_OR_NEWER
        public static class AnimationMode
        {
            public static void BeginSampling() => UnityEditor.AnimationMode.BeginSampling();
            public static void EndSampling() => UnityEditor.AnimationMode.EndSampling();
            public static bool InAnimationMode() => UnityEditor.AnimationMode.InAnimationMode();
            public static void StartAnimationMode(AnimationModeDriver o) => StartAnimationMode("StartAnimationMode", o);
            public static void StopAnimationMode(AnimationModeDriver o) => StartAnimationMode("StopAnimationMode", o);

            public static void AddPropertyModification(EditorCurveBinding binding, PropertyModification modification,
                bool keepPrefabOverride) =>
                UnityEditor.AnimationMode.AddPropertyModification(binding, modification, keepPrefabOverride);

            private static void StartAnimationMode(string name, AnimationModeDriver o)
            {
                var method = typeof(UnityEditor.AnimationMode).GetMethod(name,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(AnimationModeDriver) },
                    null);
                System.Diagnostics.Debug.Assert(method != null, nameof(method) + " != null");
                method.Invoke(null, new object[] { o });
            }
        }
#endif
    }
}
