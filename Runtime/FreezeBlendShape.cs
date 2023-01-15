using System.Collections.Generic;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Optimizer/Freeze BlendShapes")]
    [DisallowMultipleComponent]
    public class FreezeBlendShape : EditSkinnedMeshComponent
    {
        public string[] shapeKeys;
    }
}
