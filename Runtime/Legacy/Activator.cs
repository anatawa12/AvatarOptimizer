#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;

namespace Anatawa12.AvatarOptimizer
{
    /// <summary>
    /// This component was to apply Avatar Optimizer.
    /// But this is completely replaced with NDMF. 
    /// </summary>
    [AddComponentMenu("")]
    [ExecuteAlways]
    [DefaultExecutionOrder(-9989)]
    [DisallowMultipleComponent]
    [NotKeyable]
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
    [DisallowMultipleComponent]
    internal class AvatarActivator : MonoBehaviour
    {
        private void Update()
        {
            DestroyImmediate(this);
        }
    }
}
#endif
