using System;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    partial class Utils
    {
        // the float in C# is 32bit IEC 60559 / IEEE 754 floating point number.
        // The value is finite possible values so there is next / previous value.
        // Those utility methods computes the next / previous value of the given float.
        //
        // The binary32 format is like the following:
        //   seeeeeee efffffff ffffffff ffffffff
        //   s: sign bit
        //   e: exponent
        //   f: fraction
        // 
        // This table shows the values for each bit pattern:
        // exponent | zero fraction | non-zero fraction
        // all 1s   | infinity      | NaN                   // Note: this don't handle this case
        // all 0s   | zero          | subnormal number
        // others   | normal number | normal number
        //
        // For NaN or infinity, there is no reasonable next / previous value so it throws ArgumentOutOfRangeException.
        //
        // For zero, there is special case because the next / previous value can step sign bit.
        // +0.0: 0x00000000
        // next: 0x00000000 + 1 = 0x00000001 = +1.401298E-45 (+epsilon)
        // prev:                  0x80000001 = -1.401298E-45 (-epsilon) // Special Edge Case
        // -0.0: 0x80000000
        // next:                  0x80000001 = +1.401298E-45 (+epsilon) // Special Edge Case
        // prev: 0x80000000 + 1 = 0x80000001 = -1.401298E-45 (-epsilon)
        // 
        // For normal or subnormal numbers, next / previous value can be computed
        //    by adding / subtracting 1 to the bit representation.
        //
        // Here is edge case for subnormal / normal numbers:
        //  (negatives are also same except sign bit and plus/minus for next/prev)
        // min subnormal: 0x00000001 = +1.401298E-45 = float.Epsilon
        //          next: 0x00000001 + 1 = 0x00000002 = 1.401298E-45
        //          prev: 0x00000001 - 1 = 0x00000000 = 0x00000000 = +0.0
        // max subnormal: 0x007FFFFF = 1.17549421E-38
        //          next: 0x007FFFFF + 1 = 0x00800000 = 1.17549435E-38 (min normal)
        //          prev: 0x007FFFFE - 1 = 0x007FFFFD = 1.175494E-38
        //    min normal: 0x00800000 = 1.17549435E-38
        //          next: 0x00800000 + 1 = 0x00800001 = 1.17549449E-38
        //          prev: 0x00800000 - 1 = 0x007FFFFF = 1.17549421E-38 (max subnormal)
        //    max normal: 0x7F7FFFFF = 3.40282347E+38 = float.MaxValue
        //          next: 0x7F7FFFFF + 1 = 0x7F800000 = +Infinity // becomes infinity but I accept this as next value
        //          prev: 0x7F7FFFFE - 1 = 0x7F7FFFFD = 3.402823E+38
        // normal fraction overflow:
        // just before overflow: 0x00FFFFFF = 2.35098856E-38
        //                 next: 0x00FFFFFF + 1 = 0x01000000 = 2.3509887E-38

        public static float NextFloat(float x)
        {
            // NaN or Infinity : there is no next value
            if (float.IsNaN(x) || float.IsInfinity(x))
                throw new ArgumentOutOfRangeException(nameof(x), "x must be finite number");
            // zero: special case
            if (x == 0) return float.Epsilon;

            // rest is normal or subnormal number
            var asInt = BitConverter.SingleToInt32Bits(x);
            asInt += asInt < 0 ? -1 : 1;
            return BitConverter.Int32BitsToSingle(asInt);
        }

        public static float PreviousFloat(float x)
        {
            // NaN or Infinity : there is no previous value
            if (float.IsNaN(x) || float.IsInfinity(x))
                throw new ArgumentOutOfRangeException(nameof(x), "x must be finite number");
            // zero: special case
            if (x == 0) return -float.Epsilon;

            // rest is normal or subnormal number
            var asInt = BitConverter.SingleToInt32Bits(x);
            asInt -= asInt < 0 ? -1 : 1;
            return BitConverter.Int32BitsToSingle(asInt);
        }

        public static bool IsFinite(float x) => !float.IsNaN(x) && !float.IsInfinity(x);
        public static float Modulo(float x, float y) => x - y * Mathf.Floor(x / y);
    }
}
