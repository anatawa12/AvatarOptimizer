using System.Collections.Generic;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/AAO Freeze BlendShapes")]
    [DisallowMultipleComponent]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/freeze-blendshape/")]
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
