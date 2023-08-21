#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDKBase;

namespace Anatawa12.ApplyOnPlay
{
    [DefaultExecutionOrder(-100000)]
    [ExecuteAlways]
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    internal class GlobalActivator : MonoBehaviour
    {
#if UNITY_EDITOR
        internal static Action<GlobalActivator> activate;

        private void Awake()
        {
            if (!EditorApplication.isPlaying || this == null) return;
            activate?.Invoke(this);
            DestroyImmediate(this);
        }

        private void Start()
        {
            if (!EditorApplication.isPlaying || this == null) return;
            activate?.Invoke(this);
            DestroyImmediate(this);
        }

        internal static bool HasAvatarInScene(Scene scene)
        {
            return scene.GetRootGameObjects().Any(x => x.GetComponentInChildren<VRC_AvatarDescriptor>(true));
        }

        internal static void CreateIfNotNeeded(Scene scene)
        {
            if (!scene.IsValid() || EditorSceneManager.IsPreviewScene(scene)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (HasAvatarInScene(scene))
            {
                CreateIfNotExists(scene);
            }
            else
            {
                SceneChangeReceiver.CreateIfNotExists(scene);
                foreach (var root in scene.GetRootGameObjects())
                {
                    if (root.GetComponent<GlobalActivator>() != null)
                    {
                        DestroyImmediate(root);
                        EditorSceneManager.MarkSceneDirty(scene);
                    }
                }
            }
        }

        private static void CreateIfNotExists(Scene scene)
        {
            bool rootPresent = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponent<GlobalActivator>() != null)
                {
                    root.hideFlags = HideFlags;
                    root.SetActive(true);
                    if (rootPresent) DestroyImmediate(root);
                    rootPresent = true;
                }
            }

            if (rootPresent) return;

            var oldActiveScene = SceneManager.GetActiveScene();
            try
            {
                SceneManager.SetActiveScene(scene);
                var gameObject = new GameObject("ApplyOnPlayGlobalActivator");
                gameObject.AddComponent<GlobalActivator>();
                gameObject.hideFlags = HideFlags;
            }
            finally
            {
                SceneManager.SetActiveScene(oldActiveScene);
            }
        }

        private void OnValidate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            EditorApplication.delayCall += () =>
            {
                if (this == null) return;

                gameObject.hideFlags = HideFlags;
                if (!HasAvatarInScene(gameObject.scene))
                {
                    var scene = gameObject.scene;
                    DestroyImmediate(gameObject);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            };
        }
        
        private const HideFlags HideFlags = UnityEngine.HideFlags.HideInHierarchy;
#endif
    }
}