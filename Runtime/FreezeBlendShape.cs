using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Optimizer/Freeze BlendShapes")]
    [DisallowMultipleComponent]
    public class FreezeBlendShape : EditSkinnedMeshComponent
    {
        // Traditional Way: list of frozen ShapeKeys
        // New Way: list of all ShapeKeys and flags.
        public string[] shapeKeys = Array.Empty<string>();
        public bool[] freezeFlags;

        public bool IsTraditionalForm => freezeFlags == null || shapeKeys.Length != freezeFlags.Length;
        public HashSet<string> FreezingShapeKeys =>
            IsTraditionalForm
                ? new HashSet<string>(shapeKeys)
                : new HashSet<string>(shapeKeys.Where((_, i) => freezeFlags[i]));
    }
}
