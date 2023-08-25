using CustomLocalization4EditorExtension;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/AAO UnusedBonesByReferencesTool")]
    [DisallowMultipleComponent]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/unused-bones-by-references-tool/")]
    internal class UnusedBonesByReferencesTool : AvatarGlobalComponent
    {
        [CL4EELocalized("UnusedBonesByReferencesTool:prop:preserveEndBone", 
        "UnusedBonesByReferencesTool:tooltip:preserveEndBone")]
        [ToggleLeft]
        public bool preserveEndBone = true;

        [CL4EELocalized("UnusedBonesByReferencesTool:prop:detectExtraChild", 
        "UnusedBonesByReferencesTool:tooltip:detectExtraChild")]
        [ToggleLeft]
        public bool detectExtraChild;
    }
}
