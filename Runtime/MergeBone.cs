using UnityEngine;
using UnityEngine.Animations;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/AAO Merge Bone")]
    [DisallowMultipleComponent]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/merge-bone/")]
    internal class MergeBone : AvatarTagComponent
    {
        [NotKeyable, AAOLocalized("MergeBone:prop:avoidNameConflict", "MergeBone:tooltip:avoidNameConflict")]
        [ToggleLeft]
        public bool avoidNameConflict;

        private void Reset()
        {
            avoidNameConflict = true;
        }
    }
}
