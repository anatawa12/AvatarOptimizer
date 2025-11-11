#if AAO_VRCSDK3_AVATARS

using UnityEngine;
using UnityEngine.Animations;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace Anatawa12.AvatarOptimizer
{
    internal enum ReplaceEndBoneWithEndpointPositionKind
    {
        Average,
        Manual,
    }

    [AddComponentMenu("Avatar Optimizer/AAO Replace EndBone With Endpoint Position")]
    [RequireComponent(typeof(VRCPhysBone))]
    [DisallowMultipleComponent]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/replace-endbone-with-endpoint-position/")]
    internal class ReplaceEndBoneWithEndpointPosition : AvatarTagComponent
    {
        [SerializeField]
        [AAOLocalized("ReplaceEndBoneWithEndpointPosition:prop:kind")]
        [NotKeyable]
        internal ReplaceEndBoneWithEndpointPositionKind kind = ReplaceEndBoneWithEndpointPositionKind.Average;

        [SerializeField]
        [AAOLocalized("ReplaceEndBoneWithEndpointPosition:prop:manualReplacementPosition")]
        [NotKeyable]
        internal Vector3 manualReplacementPosition = Vector3.zero;
    }
}

#endif
