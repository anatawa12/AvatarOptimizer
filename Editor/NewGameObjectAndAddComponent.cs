using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    internal class NewGameObjectAndAddComponent
    {
        private const string BASE_PATH = "GameObject/Avatar Optimizer/";

        private static void CreateAndAddComponent<T>() where T : MonoBehaviour
        {
            var selectedObject = Selection.activeGameObject;
            if (selectedObject == null) return;

            var newGameObject = new GameObject(typeof(T).Name);
            var transform = newGameObject.transform;
            transform.SetParent(selectedObject.transform, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            
            newGameObject.AddComponent<T>();

            Undo.RegisterCreatedObjectUndo(newGameObject, $"Create {typeof(T).Name}");
            Selection.activeGameObject = newGameObject;
            EditorGUIUtility.PingObject(newGameObject);
        }

        [MenuItem(BASE_PATH + nameof(MergeSkinnedMesh))]
        private static void CreateMergeSkinnedMesh() => CreateAndAddComponent<MergeSkinnedMesh>();

        [MenuItem(BASE_PATH + nameof(MergePhysBone))]
        private static void CreateMergePhysBone() => CreateAndAddComponent<MergePhysBone>();
    }
}
