using UnityEngine;
using UnityEngine.SceneManagement;

namespace Anatawa12.ApplyOnPlay
{
    [ExecuteAlways]
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public class SceneChangeReceiver : MonoBehaviour
    {
#if UNITY_EDITOR
        public Scene scene;

        void Update()
        {
            if (scene.IsValid())
            {
                if (GlobalActivator.HasAvatarInScene(scene))
                {
                    GlobalActivator.CreateIfNotNeeded(scene);
                    DestroyImmediate(this);
                }
            }
            else
            {
                DestroyImmediate(this);
            }
        }
        
        internal static void CreateIfNotNeeded(Scene scene)
        {
            if (!scene.IsValid() || UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(scene)) return;
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (GlobalActivator.HasAvatarInScene(scene)) return;
            CreateIfNotExists(scene);
        }

        internal static void CreateIfNotExists(Scene scene)
        {
            bool rootPresent = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponent<SceneChangeReceiver>() != null)
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
                var gameObject = new GameObject("ApplyOnPlaySceneChangeReceiver");
                var component = gameObject.AddComponent<SceneChangeReceiver>();
                component.scene = scene;
                gameObject.hideFlags = HideFlags;
                component.hideFlags = HideFlags;
            }
            finally
            {
                SceneManager.SetActiveScene(oldActiveScene);
            }
        }

        private const HideFlags HideFlags = UnityEngine.HideFlags.HideAndDontSave;
#endif
    }
}