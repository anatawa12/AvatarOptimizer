using System.Collections.Generic;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Anatawa12/Freeze BlendShapes")]
    [DisallowMultipleComponent]
    public class FreezeBlendShape : EditSkinnedMeshComponent
    {
        public string[] shapeKeys;
    }
}
