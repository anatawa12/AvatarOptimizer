using System;
using System.Collections.Generic;
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
            HashCode code = default;
            foreach (var keyframe in array)
                code.Add(keyframe.GetHashCode2());
            return code.ToHashCode();
        }

        // KeyFrame doesn't implement GetHashCode so it will be extremely slow so provide our implementation
        public static int GetHashCode2(this Keyframe curve)
        {
            HashCode code = default;
            code.Add(curve.time);
            code.Add(curve.value);
            code.Add(curve.inTangent);
            code.Add(curve.outTangent);
#pragma warning disable CS0618 // Type or member is obsolete
            code.Add(curve.tangentMode);
#pragma warning restore CS0618 // Type or member is obsolete
            code.Add(curve.weightedMode);
            code.Add(curve.inWeight);
            code.Add(curve.outWeight);
            return code.ToHashCode();
        }

        // The HashSet doesn't implement HashCode
        public static int GetHashCode2<T>(this HashSet<T> set)
        {
            // we use XOR to make the hashcode order-independent
            var hash = set.Count;
            foreach (var item in set)
                hash ^= HashCode.Combine(item);
            return hash;
        }
    }
}
