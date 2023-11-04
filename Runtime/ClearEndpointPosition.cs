#if AAO_VRCSDK3_AVATARS

using UnityEngine;
using UnityEngine.Animations;
using VRC.Dynamics;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/AAO Clear Endpoint Position")]
    [RequireComponent(typeof(VRCPhysBoneBase))]
    [DisallowMultipleComponent]
    [NotKeyable]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/clear-endpoint-position/")]
    internal class ClearEndpointPosition : AvatarTagComponent
    {
    }
}

#endif