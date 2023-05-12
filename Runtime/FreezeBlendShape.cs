using System;
using System.Collections.Generic;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Optimizer/Freeze BlendShapes")]
    [DisallowMultipleComponent]
    internal class FreezeBlendShape : EditSkinnedMeshComponent
    {
        #region v1

        // Traditional Way: list of frozen ShapeKeys
        // New Way: list of all ShapeKeys and flags.
        [Obsolete("legacy v1", true)]
        public string[] shapeKeys = Array.Empty<string>();
        [Obsolete("legacy v1", true)]
        public bool[] freezeFlags;

        [Obsolete("legacy v1", true)]
        public bool IsTraditionalForm => freezeFlags == null || shapeKeys.Length != freezeFlags.Length;

        #endregion

        #region v2

        // Traditional Way: list of frozen ShapeKeys
        // New Way: list of all ShapeKeys and flags.
        public PrefabSafeSet.StringSet shapeKeysSet;

        #endregion

        public FreezeBlendShape()
        {
            shapeKeysSet = new PrefabSafeSet.StringSet(this);
        }

        public HashSet<string> FreezingShapeKeys => shapeKeysSet.GetAsSet();
    }
}
