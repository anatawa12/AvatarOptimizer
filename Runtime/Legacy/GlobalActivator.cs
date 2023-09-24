using UnityEngine;

namespace Anatawa12.ApplyOnPlay
{
    [DefaultExecutionOrder(-100000)]
    [ExecuteAlways]
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
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