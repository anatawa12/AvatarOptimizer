using System;
using UnityEngine;
using UnityEngine.Animations;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/AAO UnusedBonesByReferencesTool")]
    [DisallowMultipleComponent]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/unused-bones-by-references-tool/")]
    [Obsolete("Obsoleted by Trace and Optimize")]
    internal class UnusedBonesByReferencesTool : AvatarGlobalComponent
    {
        [NotKeyable]
        [AAOLocalized("UnusedBonesByReferencesTool:prop:preserveEndBone", 
        "UnusedBonesByReferencesTool:tooltip:preserveEndBone")]
        [ToggleLeft]
        public bool preserveEndBone = true;

        [NotKeyable]
        [AAOLocalized("UnusedBonesByReferencesTool:prop:detectExtraChild", 
        "UnusedBonesByReferencesTool:tooltip:detectExtraChild")]
        [ToggleLeft]
        public bool detectExtraChild;
    }
}
