using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    internal class NewGameObjectAndAddComponent
    {
        private const string BASE_PATH = "GameObject/Avatar Optimizer/";
        
        private const string MERGE_SKINNED_MESH = "Merge Skinned Mesh";
        private const string MERGE_PHYSBONE = "Merge PhysBone";

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


        [MenuItem(BASE_PATH + MERGE_SKINNED_MESH, true)]
        private static bool ValidateCreateMergeSkinnedMesh() => Selection.activeGameObject != null;

        [MenuItem(BASE_PATH + MERGE_SKINNED_MESH)]
        private static void CreateMergeSkinnedMesh() => CreateAndAddComponent<MergeSkinnedMesh>(MERGE_SKINNED_MESH);


        [MenuItem(BASE_PATH + MERGE_PHYSBONE, true)]
        private static bool ValidateCreateMergePhysBone() => Selection.activeGameObject != null;

        [MenuItem(BASE_PATH + MERGE_PHYSBONE)]
        private static void CreateMergePhysBone() => CreateAndAddComponent<MergePhysBone>(MERGE_PHYSBONE);
    }
}
