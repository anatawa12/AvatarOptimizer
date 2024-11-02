using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    internal class NewGameObjectAndAddComponent
    {
        private const string BASE_PATH = "GameObject/Avatar Optimizer/";
        private const int PRIORITY = 20;

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


        [MenuItem(BASE_PATH + MERGE_SKINNED_MESH, true, PRIORITY)]
        private static bool ValidateCreateMergeSkinnedMesh() => Selection.activeGameObject != null;

        [MenuItem(BASE_PATH + MERGE_SKINNED_MESH, false, PRIORITY)]
        private static void CreateMergeSkinnedMesh() => CreateAndAddComponent<MergeSkinnedMesh>(MERGE_SKINNED_MESH);


        [MenuItem(BASE_PATH + MERGE_PHYSBONE, true, PRIORITY)]
        private static bool ValidateCreateMergePhysBone() => Selection.activeGameObject != null;

#if AAO_VRCSDK3_AVATARS
        [MenuItem(BASE_PATH + MERGE_PHYSBONE, false, PRIORITY)]
        private static void CreateMergePhysBone() => CreateAndAddComponent<MergePhysBone>(MERGE_PHYSBONE);
#endif
    }
}
