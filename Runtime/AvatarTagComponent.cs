using System;
using Anatawa12.AvatarOptimizer.ErrorReporting;
using UnityEngine;

#if AAO_VRCSDK3_AVATARS
using VRC.SDKBase;
#endif

namespace Anatawa12.AvatarOptimizer
{
    // https://github.com/bdunderscore/modular-avatar/blob/db49e2e210bc070671af963ff89df853ae4514a5/Packages/nadena.dev.modular-avatar/Runtime/AvatarTagComponent.cs
    // Originally under MIT License
    // Copyright (c) 2022 bd_
    /**
     * This abstract base class is injected into the VRCSDK avatar component allowlist to avoid
     */
    [DefaultExecutionOrder(-9990)] // run before av3emu
    [ExecuteAlways]
    internal abstract class AvatarTagComponent : MonoBehaviour, IEditorOnly
    {
        private void OnValidate()
        {
            if (RuntimeUtil.isPlaying) return;
            ErrorReporterRuntime.TriggerChange();
        }

        private void OnDestroy()
        {
            ErrorReporterRuntime.TriggerChange();
        }
    }
    
#if !AAO_VRCSDK3_AVATARS

    /**
     * Placeholder of VRC.SDKBase.IEditorOnly for environments without VRCSDK
     */
    interface IEditorOnly
    {
    }

#endif

}
