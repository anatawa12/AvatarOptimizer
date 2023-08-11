using UnityEngine;
using VRC.Dynamics;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/AAO Clear Endpoint Position")]
    [RequireComponent(typeof(VRCPhysBoneBase))]
    [DisallowMultipleComponent]
    internal class ClearEndpointPosition : AvatarTagComponent
    {
    }
}
