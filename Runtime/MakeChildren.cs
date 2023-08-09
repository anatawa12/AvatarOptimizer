using CustomLocalization4EditorExtension;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/AAO Make Children")]
    internal class MakeChildren : AvatarTagComponent
    {
        [CL4EELocalized("MakeChildren:prop:executeEarly", "MakeChildren:tooltip:executeEarly")]
        public bool executeEarly;
        [CL4EELocalized("MakeChildren:prop:children")]
        public PrefabSafeSet.TransformSet children;
    }
}
