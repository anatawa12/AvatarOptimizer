using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/AAO Freeze BlendShapes")]
    [DisallowMultipleComponent]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/freeze-blendshape/")]
    internal class FreezeBlendShape : EditSkinnedMeshComponent
    {
        public PrefabSafeSet.PrefabSafeSet<string> shapeKeysSet;

        public FreezeBlendShape()
        {
            shapeKeysSet = new PrefabSafeSet.PrefabSafeSet<string>(this);
        }

        public HashSet<string> FreezingShapeKeys => shapeKeysSet.GetAsSet();

        private void OnValidate()
        {
            PrefabSafeSet.PrefabSafeSet.OnValidate(this, x => x.shapeKeysSet);
        }
    }
}
