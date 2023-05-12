using UnityEngine;
using VRC.Dynamics;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Optimizer/Clear Endpoint Position")]
    [RequireComponent(typeof(VRCPhysBoneBase))]
    internal class ClearEndpointPosition : AvatarTagComponent
    {
    }
}
