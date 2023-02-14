using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Anatawa12.AvatarOptimizer
{
    [InitializeOnLoad]
    public static class MaterialSaverUtility
    {
        static MaterialSaverUtility()
        {
            EditorApplication.update += Update;
        }

        public static Material[] GetMaterials(Renderer renderer) => 
            renderer.GetComponent<MaterialSaver>()?.sharedMaterials ?? renderer.sharedMaterials;

        private static void Update()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
            {
                foreach (var renderer in Enumerable.Range(0, SceneManager.sceneCount)
                             .Select(SceneManager.GetSceneAt)
                             .SelectMany(x => x.GetRootGameObjects())
                             .SelectMany(x => x.GetComponentsInChildren<Renderer>()))
                {
                    var saver = renderer.gameObject.GetOrAddComponent<MaterialSaver>();
                    saver.sharedMaterials = renderer.sharedMaterials;
                    saver.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                }
            }
        }
    }
}
