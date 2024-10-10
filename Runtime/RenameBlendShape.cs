using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/AAO Rename BlendShape")]
    [DisallowMultipleComponent]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/rename-blend-shape/")]
    internal class RenameBlendShape : EditSkinnedMeshComponent
    {
        [SerializeField] internal PrefabSafeMap.PrefabSafeMap<string, string> nameMap;

        public RenameBlendShape()
        {
            nameMap = new PrefabSafeMap.PrefabSafeMap<string, string>(this);
        }
    }
}
