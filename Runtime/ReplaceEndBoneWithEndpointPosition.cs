#if AAO_VRCSDK3_AVATARS

using UnityEngine;
using UnityEngine.Animations;
using VRC.Dynamics;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/AAO Replace EndBone With Endpoint Position")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(VRCPhysBoneBase))]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/replace-endbone-with-endpoint-position/")]
    internal class ReplaceEndBoneWithEndpointPosition : AvatarTagComponent
    {
        [SerializeField]
        [AAOLocalized("ReplaceEndBoneWithEndpointPosition:prop:replacementPosition")]
        [NotKeyable]
        internal Vector3 replacementPosition = Vector3.zero;
    }
}

#endif