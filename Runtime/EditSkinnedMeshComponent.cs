using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    [ExecuteAlways]
    internal abstract class EditSkinnedMeshComponent : AvatarTagComponent
    {
        private void Awake() => RuntimeUtil.OnAwakeEditSkinnedMesh?.Invoke(this);
        
        private void OnDestroy() => RuntimeUtil.OnDestroyEditSkinnedMesh?.Invoke(this);
    }
}
