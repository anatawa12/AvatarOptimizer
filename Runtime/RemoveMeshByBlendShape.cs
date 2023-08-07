using System.Collections.Generic;
using CustomLocalization4EditorExtension;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/Remove Mesh By BlendShape")]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    [DisallowMultipleComponent]
    internal class RemoveMeshByBlendShape : EditSkinnedMeshComponent
    {
        public PrefabSafeSet.StringSet shapeKeysSet;
        [CL4EELocalized("RemoveMeshByBlendShape:prop:Tolerance",
            "RemoveMeshByBlendShape:tooltip:Tolerance")]
        public double tolerance = 0.001;

        public RemoveMeshByBlendShape()
        {
            shapeKeysSet = new PrefabSafeSet.StringSet(this);
        }

        public HashSet<string> RemovingShapeKeys => shapeKeysSet.GetAsSet();
    }
}
