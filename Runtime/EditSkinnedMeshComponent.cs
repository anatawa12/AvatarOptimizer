using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    [ExecuteAlways]
    internal abstract class EditSkinnedMeshComponent : AvatarTagComponent
    {
        private void OnEnable() => RuntimeUtil.OnAwakeEditSkinnedMesh?.Invoke(this);
        
        private void OnDisable() => RuntimeUtil.OnDestroyEditSkinnedMesh?.Invoke(this);
    }
}
