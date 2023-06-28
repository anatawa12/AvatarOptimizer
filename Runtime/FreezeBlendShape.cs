using System.Collections.Generic;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/Freeze BlendShapes")]
    [DisallowMultipleComponent]
    internal class FreezeBlendShape : EditSkinnedMeshComponent
    {
        public PrefabSafeSet.StringSet shapeKeysSet;

        public FreezeBlendShape()
        {
            shapeKeysSet = new PrefabSafeSet.StringSet(this);
        }

        public HashSet<string> FreezingShapeKeys => shapeKeysSet.GetAsSet();
    }
}
