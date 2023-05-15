using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Optimizer/DO NOT USE/Delete Game Object")]
    [DisallowMultipleComponent]
    public class DeleteGameObject : AvatarTagComponent
    {
#if UNITY_EDITOR
        private void Reset()
        {
            EditorUtility.DisplayDialog(
                "Removed", 
                "DeleteGameObject is removed in AvatarOptimizer v0.4. use EditorOnly tag instead.", 
                "OK");
            DestroyImmediate(this);
        }
#endif
    }
}
