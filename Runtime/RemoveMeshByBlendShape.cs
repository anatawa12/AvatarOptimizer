using System.Collections.Generic;
using CustomLocalization4EditorExtension;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/Remove Mesh By Blend Shape")]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    [DisallowMultipleComponent]
    internal class RemoveMeshByBlendShape : EditSkinnedMeshComponent
    {
        public PrefabSafeSet.StringSet shapeKeysSet;
        [CL4EELocalized("RemoveMeshByBlendShape:prop:Tolerance")]
        public double Tolerance;

        public RemoveMeshByBlendShape()
        {
            shapeKeysSet = new PrefabSafeSet.StringSet(this);
            Tolerance = 0.001;
        }

        public HashSet<string> RemovingShapeKeys => shapeKeysSet.GetAsSet();
    }
}
