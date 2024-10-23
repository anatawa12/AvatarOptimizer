using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    internal class NewGameObjectAndAddComponent
    {
        private const string BASE_PATH = "GameObject/Avatar Optimizer/";
        private const string MSM = "Merge Skinned Mesh";
        private const string MPB = "Merge PhysBone";

        private static void CreateAndAddComponent<T>(string componentName) where T : MonoBehaviour
        {
            var selectedObject = Selection.activeGameObject;
            if (selectedObject == null) return;

            var newGameObject = new GameObject(componentName);
            var transform = newGameObject.transform;
            transform.SetParent(selectedObject.transform, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            
            newGameObject.AddComponent<T>();

            Undo.RegisterCreatedObjectUndo(newGameObject, $"Create {componentName}");
            Selection.activeGameObject = newGameObject;
            EditorGUIUtility.PingObject(newGameObject);
        }


        [MenuItem(BASE_PATH + MSM, true)]
        private static bool ValidateCreateMergeSkinnedMesh() => Selection.activeGameObject != null;

        [MenuItem(BASE_PATH + MSM)]
        private static void CreateMergeSkinnedMesh() => CreateAndAddComponent<MergeSkinnedMesh>(MSM);


        [MenuItem(BASE_PATH + MPB, true)]
        private static bool ValidateCreateMergePhysBone() => Selection.activeGameObject != null;

        [MenuItem(BASE_PATH + MPB)]
        private static void CreateMergePhysBone() => CreateAndAddComponent<MergePhysBone>(MPB);
    }
}
