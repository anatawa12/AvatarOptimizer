using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    // https://github.com/bdunderscore/modular-avatar/blob/db49e2e210bc070671af963ff89df853ae4514a5/Packages/nadena.dev.modular-avatar/Runtime/AvatarTagComponent.cs
    // Originally under MIT License
    // Copyright (c) 2022 bd_
    /**
     * This abstract base class is injected into the VRCSDK avatar component allowlist to avoid
     */
    [DefaultExecutionOrder(-9990)] // run before av3emu
    public abstract class AvatarTagComponent : MonoBehaviour
    {
        // saved format versions. saveVersions[0] is original asset and saveVersions[1] is prefab instance
        // this is used for migration in 0.x v versions. in 1.x versions, this should be removed.
        public int[] saveVersions;

        private void Awake()
        {
            if (!RuntimeUtil.isPlaying || this == null) return;
            RuntimeUtil.OnDemandProcessAvatar(RuntimeUtil.OnDemandSource.Awake, this);
        }

        private void Start()
        {
            if (!RuntimeUtil.isPlaying || this == null) return;
            RuntimeUtil.OnDemandProcessAvatar(RuntimeUtil.OnDemandSource.Start, this);
        }

        private void OnValidate()
        {
            if (RuntimeUtil.isPlaying) return;

            RuntimeUtil.delayCall(() =>
            {
                if (this == null) return;
#if UNITY_EDITOR
                Activator.CreateIfNotPresent(gameObject.scene);
#endif
            });
        }
    }
}
