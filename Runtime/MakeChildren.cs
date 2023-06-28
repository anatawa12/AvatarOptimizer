using CustomLocalization4EditorExtension;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/Make Children")]
    internal class MakeChildren : AvatarTagComponent
    {
        [CL4EELocalized("MakeChildren:prop:children")]
        public PrefabSafeSet.TransformSet children;
    }
}
