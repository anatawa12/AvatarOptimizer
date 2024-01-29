using System.Collections.Generic;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/AAO Remove Mesh By BlendShape")]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    [DisallowMultipleComponent]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/remove-mesh-by-blendshape/")]
    internal class RemoveMeshByBlendShape : EditSkinnedMeshComponent
    {
        public PrefabSafeSet.StringSet shapeKeysSet;
        [AAOLocalized("RemoveMeshByBlendShape:prop:Tolerance",
            "RemoveMeshByBlendShape:tooltip:Tolerance")]
        public double tolerance = 0.001;

        public RemoveMeshByBlendShape()
        {
            shapeKeysSet = new PrefabSafeSet.StringSet(this);
        }

        public HashSet<string> RemovingShapeKeys => shapeKeysSet.GetAsSet();
    }
}
