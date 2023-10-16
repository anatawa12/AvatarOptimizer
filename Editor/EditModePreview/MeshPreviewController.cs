using System;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
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
        [CanBeNull] private RemoveMeshPreviewController _previewController;
        public static bool Previewing => instance.previewing;

        [SerializeField] private bool previewing;
        [SerializeField] private Mesh previewMesh;
        [SerializeField] private Mesh originalMesh;
        [SerializeField] private SkinnedMeshRenderer targetRenderer;
        [SerializeField] private AnimationModeDriver driverCached;

        private AnimationModeDriver DriverCached => driverCached ? driverCached : driverCached = CreateDriver();

        public bool Enabled
        {
            get => EditorPrefs.GetBool("com.anatawa12.avatar-optimizer.mesh-preview.enabled", true);
            set => EditorPrefs.SetBool("com.anatawa12.avatar-optimizer.mesh-preview.enabled", value);
        }

        private void OnEnable()
        {
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
        }

        private void OnDisable()
        {
            _previewController?.Dispose();
            EditorApplication.update -= Update;
        }

        private Object ActiveEditor()
        {
            var editors = ActiveEditorTracker.sharedTracker.activeEditors;
            return editors.Length == 0 ? null : editors[0].target;
        } 

        private void Update()
        {
            if (previewing)
            {
                if (_previewController == null)
                    _previewController = new RemoveMeshPreviewController(targetRenderer, originalMesh, previewMesh);

                if (targetRenderer == null || ActiveEditor() != targetRenderer.gameObject)
                {
                    StopPreview();
                    return;
                }

                if (_previewController.UpdatePreviewing())
                    StopPreview();
            }
            else
            {
                if (Enabled && StateForImpl(null) == PreviewState.PreviewAble)
                {
                    var editorObj = ActiveEditor();
                    if (editorObj is GameObject go &&
                        go.GetComponent<SkinnedMeshRenderer>().sharedMesh && 
                        RemoveMeshPreviewController.EditorTypes.Any(t => go.GetComponent(t)))
                    {
                        StartPreview(go);
                    }
                }
            }
        }

        public enum PreviewState
        {
            PreviewAble,
            PreviewingThat,

            NoMesh,
            PreviewingOther,
            ActiveEditorMismatch,
        }

        public static PreviewState StateFor([CanBeNull] Component component) => instance.StateForImpl(component);

        private PreviewState StateForImpl([CanBeNull] Component component)
        {
            var gameObject = component ? component.gameObject : null;

            if (previewing && targetRenderer && targetRenderer.gameObject == gameObject)
                return PreviewState.PreviewingThat;

            if (AnimationMode.InAnimationMode())
                return PreviewState.PreviewingOther;

            if (gameObject)
            {
                if (ActiveEditor() as GameObject != gameObject)
                    return PreviewState.ActiveEditorMismatch;

                var renderer = gameObject.GetComponent<SkinnedMeshRenderer>();
                if (!renderer || !renderer.sharedMesh)
                    return PreviewState.NoMesh;
            }

            return PreviewState.PreviewAble;
        }

        public static void ShowPreviewControl(Component component) => instance.ShowPreviewControlImpl(component);

        private void ShowPreviewControlImpl(Component component)
        {
            switch (StateForImpl(component))
            {
                case PreviewState.PreviewAble:
                    if (GUILayout.Button("Preview"))
                    {
                        Enabled = true;
                        StartPreview();
                    }
                    break;
                case PreviewState.PreviewingThat:
                    if (GUILayout.Button("Stop Preview"))
                    {
                        StopPreview();
                        Enabled = false;
                    }
                    break;
                case PreviewState.NoMesh:
                    EditorGUI.BeginDisabledGroup(true);
                    GUILayout.Button("Preview (no Mesh)");
                    EditorGUI.EndDisabledGroup();
                    break;
                case PreviewState.PreviewingOther:
                    EditorGUI.BeginDisabledGroup(true);
                    GUILayout.Button("Preview (other Previewing)");
                    EditorGUI.EndDisabledGroup();
                    break;
                case PreviewState.ActiveEditorMismatch:
                    EditorGUI.BeginDisabledGroup(true);
                    GUILayout.Button("Preview (not the active object)");
                    EditorGUI.EndDisabledGroup();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void StartPreview(GameObject expectedGameObject = null)
        {
            // Already in AnimationMode of other object
            if (AnimationMode.InAnimationMode())
                throw new Exception("Already in Animation Mode");

            var targetGameObject = ActiveEditor() as GameObject;
            if (targetGameObject == null)
                throw new Exception("Active Editor is not GameObject");
            if (expectedGameObject != null && expectedGameObject != targetGameObject)
                throw new Exception("Unexpected GameObject");

            targetRenderer = targetGameObject.GetComponent<SkinnedMeshRenderer>();
            _previewController = new RemoveMeshPreviewController(targetRenderer);
            targetRenderer = _previewController.TargetRenderer;
            previewMesh = _previewController.PreviewMesh;
            originalMesh = _previewController.OriginalMesh;

            previewing = true;
            AnimationMode.StartAnimationMode(DriverCached);
            try
            {
                AnimationMode.BeginSampling();

                AnimationMode.AddPropertyModification(
                    EditorCurveBinding.PPtrCurve("", typeof(SkinnedMeshRenderer), "m_Mesh"),
                    new PropertyModification
                    {
                        target = _previewController.TargetRenderer,
                        propertyPath = "m_Mesh",
                        objectReference = _previewController.OriginalMesh,
                    }, 
                    true);

                _previewController.TargetRenderer.sharedMesh = _previewController.PreviewMesh;
            }
            finally
            {
                AnimationMode.EndSampling();   
            }
        }

        public void StopPreview()
        {
            previewing = false;
            AnimationMode.StopAnimationMode(DriverCached);
            _previewController?.Dispose();
            _previewController = null;
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
