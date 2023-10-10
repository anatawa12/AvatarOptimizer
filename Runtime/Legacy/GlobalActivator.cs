using UnityEngine;
using UnityEngine.Animations;

namespace Anatawa12.ApplyOnPlay
{
    [DefaultExecutionOrder(-100000)]
    [ExecuteAlways]
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    [NotKeyable]
    internal class GlobalActivator : MonoBehaviour
    {
#if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += RemoveSelf;
        }

        private void Update() => RemoveSelf();

        private void RemoveSelf()
        {
            if (this == null) return;
            var scene = gameObject.scene;
            DestroyImmediate(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        }

#endif
    }
}