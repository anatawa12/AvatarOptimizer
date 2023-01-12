using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    [ExecuteInEditMode]
    public abstract class EditSkinnedMeshComponent : AvatarTagComponent
    {
        private void OnEnable() => RuntimeUtil.OnAwakeEditSkinnedMesh?.Invoke(this);
        
        private void OnDisable() => RuntimeUtil.OnDestroyEditSkinnedMesh?.Invoke(this);
    }
}
