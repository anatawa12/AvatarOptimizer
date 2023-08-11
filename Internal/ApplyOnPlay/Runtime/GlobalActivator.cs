#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Anatawa12.ApplyOnPlay
{
    [DefaultExecutionOrder(-100000)]
    [ExecuteAlways]
    internal class GlobalActivator : MonoBehaviour
    {
#if UNITY_EDITOR
        internal static Action<GlobalActivator> activate;

        private void Awake()
        {
            if (!EditorApplication.isPlaying || this == null) return;
            activate?.Invoke(this);
        }

        internal static void CreateIfNotPresent(Scene scene)
        {
            if (!scene.IsValid() || EditorSceneManager.IsPreviewScene(scene)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            bool rootPresent = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponent<GlobalActivator>() != null)
                {
                    root.hideFlags = HIDE_FLAGS;
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
                gameObject.hideFlags = HIDE_FLAGS;
            }
            finally
            {
                SceneManager.SetActiveScene(oldActiveScene);
            }
        }
        
        private const HideFlags HIDE_FLAGS = HideFlags.HideInHierarchy;
#endif
    }
}