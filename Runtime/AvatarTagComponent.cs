using JetBrains.Annotations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    /**
     * This class is the base class for most components in Avatar Optimizer.
     * When you found instance of this class, you can assume that it's a part of Avatar Optimizer.
     */
    [DefaultExecutionOrder(-9990)] // run before av3emu
    [ExecuteAlways]
    [PublicAPI]
    public abstract class AvatarTagComponent : MonoBehaviour
#if AAO_VRCSDK3_AVATARS
        , VRC.SDKBase.IEditorOnly
#endif
    {
        private protected AvatarTagComponent()
        {
        }
    }
}