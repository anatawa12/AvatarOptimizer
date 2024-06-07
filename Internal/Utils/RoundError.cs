using System;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    /// <summary>
    /// The class for rounding some numbers to remove floating point errors.
    /// </summary>
    public static class RoundError
    {
        // the number of digits to round for global position or size.
        // 10^-5 m = 0.00001m = 0.001cm = 0.01mm
        const int digits = 5;

        public static float Float(float value) => (float)Math.Round(value, digits);

        public static Vector3 Vector3(Vector3 vector3) =>
            new Vector3(Float(vector3.x), Float(vector3.y), Float(vector3.z));

        public static Bounds Bounds(Bounds bounds) => new Bounds(Vector3(bounds.center), Vector3(bounds.size));

        // TODO: implement round for Quaternion
        // I think just rounding each component would cause too much error so I need to find a better way.
        public static Quaternion Quaternion(Quaternion quaternion) => quaternion;
    }
}
