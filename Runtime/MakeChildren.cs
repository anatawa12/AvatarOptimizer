using CustomLocalization4EditorExtension;
using UnityEngine;
using UnityEngine.Animations;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/AAO Make Children")]
    [DisallowMultipleComponent]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/make-children/")]
    internal class MakeChildren : AvatarTagComponent
    {
        [NotKeyable, CL4EELocalized("MakeChildren:prop:executeEarly", "MakeChildren:tooltip:executeEarly")]
        public bool executeEarly;
        [NotKeyable, CL4EELocalized("MakeChildren:prop:children")]
        public PrefabSafeSet.TransformSet children;
    }
}
