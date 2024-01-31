using System;
using System.Linq;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    static partial class Utils
    {
        // Prior to 2022.3.13f1 or 2023.3.0a7, AnimationCurve.GetHashCode() is implemented incorrectly
        // so we use our own implementation
        public static int GetHashCode2(this AnimationCurve curve)
        {
            // according to https://docs.unity3d.com/2022.3/Documentation/ScriptReference/AnimationCurve.GetHashCode.html,
            // hashcode is calculated by KeyFrames
            var array = curve.keys;
            return array.Aggregate(array.Length, (current, val) => unchecked(current * 314159 + val.GetHashCode2()));
        }

        // KeyFrame doesn't implement GetHashCode so it will be extremely slow so provide our implementation
        public static int GetHashCode2(this Keyframe curve)
        {
            var hash = 0;
            hash = unchecked(hash * 314159 + curve.time.GetHashCode());
            hash = unchecked(hash * 314159 + curve.value.GetHashCode());
            hash = unchecked(hash * 314159 + curve.inTangent.GetHashCode());
            hash = unchecked(hash * 314159 + curve.outTangent.GetHashCode());
#pragma warning disable CS0618 // Type or member is obsolete
            hash = unchecked(hash * 314159 + curve.tangentMode.GetHashCode());
#pragma warning restore CS0618 // Type or member is obsolete
            hash = unchecked(hash * 314159 + curve.weightedMode.GetHashCode());
            hash = unchecked(hash * 314159 + curve.inWeight.GetHashCode());
            hash = unchecked(hash * 314159 + curve.outWeight.GetHashCode());
            return hash;
        }
    }
}