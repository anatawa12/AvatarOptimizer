using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Optimizer/Freeze BlendShapes")]
    [DisallowMultipleComponent]
    public class FreezeBlendShape : EditSkinnedMeshComponent
    {
        // Traditional Way: list of frozen ShapeKeys
        // New Way: list of all ShapeKeys and flags.
        public string[] shapeKeys;
        public bool[] freezeFlags;
    }
}
