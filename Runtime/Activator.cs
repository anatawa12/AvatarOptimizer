#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    /// <summary>
    /// This component was to apply Avatar Optimizer.
    /// But this is completely replaced with Apply on Play Framework in `Internal/ApplyOnPlay`.
    /// That framework will be published as distinct framework in the near feature. 
    /// </summary>
    [AddComponentMenu("")]
    [ExecuteAlways]
    [DefaultExecutionOrder(-9989)]
    internal class Activator : MonoBehaviour
    {
        private void OnValidate()
        {
            EditorApplication.delayCall += RemoveSelf;
        }

        private void Update() => RemoveSelf();

        private void RemoveSelf()
        {
            if (this == null) return;
            var scene = gameObject.scene;
            DestroyImmediate(gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }

    [AddComponentMenu("")]
    [ExecuteAlways]
    [DefaultExecutionOrder(-9997)]
    internal class AvatarActivator : MonoBehaviour
    {
        private void Update()
        {
            DestroyImmediate(this);
        }
    }
}
#endif
