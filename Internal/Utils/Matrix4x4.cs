using System;
using UnityEngine;
using UnityMatrix4x4 = UnityEngine.Matrix4x4;

namespace Anatawa12.AvatarOptimizer
{
    /// <summary>
    /// AAO internal replacement for UnityEngine.Matrix4x4 for more math operations
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public struct Matrix4x4 : IEquatable<Matrix4x4>
    {
        // ReSharper disable InconsistentNaming
        public static readonly Matrix4x4 zero = new Matrix4x4(UnityMatrix4x4.zero);
        public static readonly Matrix4x4 identity = new Matrix4x4(UnityMatrix4x4.identity);

        public Matrix4x4 inverse => ToUnity().inverse;

        // @formatter:off
        public float m00;
        public float m10;
        public float m20;
        public float m30;
        public float m01;
        public float m11;
        public float m21;
        public float m31;
        public float m02;
        public float m12;
        public float m22;
        public float m32;
        public float m03;
        public float m13;
        public float m23;
        public float m33;
        // @formatter:on

        public Vector3 offset => new Vector3(m03, m13, m23);

        public Vector3 MultiplyPoint3x4(Vector3 point)
        {
            Vector3 vector3;
            vector3.x = (float)((double)m00 * point.x + (double)m01 * point.y + (double)m02 * point.z) + m03;
            vector3.y = (float)((double)m10 * point.x + (double)m11 * point.y + (double)m12 * point.z) + m13;
            vector3.z = (float)((double)m20 * point.x + (double)m21 * point.y + (double)m22 * point.z) + m23;
            return vector3;
        }

        public Vector3 MultiplyPoint3x3(Vector3 point)
        {
            Vector3 vector3;
            vector3.x = (float)((double)m00 * point.x + (double)m01 * point.y + (double)m02 * point.z);
            vector3.y = (float)((double)m10 * point.x + (double)m11 * point.y + (double)m12 * point.z);
            vector3.z = (float)((double)m20 * point.x + (double)m21 * point.y + (double)m22 * point.z);
            return vector3;
        }

        public static Matrix4x4 operator *(Matrix4x4 m, float w)
        {
            m.m00 *= w;
            m.m01 *= w;
            m.m02 *= w;
            m.m03 *= w;
            m.m10 *= w;
            m.m11 *= w;
            m.m12 *= w;
            m.m13 *= w;
            m.m20 *= w;
            m.m21 *= w;
            m.m22 *= w;
            m.m23 *= w;
            m.m30 *= w;
            m.m31 *= w;
            m.m32 *= w;
            m.m33 *= w;
            return m;
        }

        public static Matrix4x4 operator *(float w, Matrix4x4 m) => m * w;

        public static Matrix4x4 operator +(Matrix4x4 a, Matrix4x4 b)
        {
            a.m00 += b.m00;
            a.m01 += b.m01;
            a.m02 += b.m02;
            a.m03 += b.m03;
            a.m10 += b.m10;
            a.m11 += b.m11;
            a.m12 += b.m12;
            a.m13 += b.m13;
            a.m20 += b.m20;
            a.m21 += b.m21;
            a.m22 += b.m22;
            a.m23 += b.m23;
            a.m30 += b.m30;
            a.m31 += b.m31;
            a.m32 += b.m32;
            a.m33 += b.m33;
            return a;
        }

        // ReSharper restore InconsistentNaming

        public Matrix4x4(UnityMatrix4x4 value)
        {
            m00 = value.m00;
            m01 = value.m01;
            m02 = value.m02;
            m03 = value.m03;
            m10 = value.m10;
            m11 = value.m11;
            m12 = value.m12;
            m13 = value.m13;
            m20 = value.m20;
            m21 = value.m21;
            m22 = value.m22;
            m23 = value.m23;
            m30 = value.m30;
            m31 = value.m31;
            m32 = value.m32;
            m33 = value.m33;
        }

        public UnityMatrix4x4 ToUnity()
        {
            UnityMatrix4x4 result = default;
            result.m00 = m00;
            result.m01 = m01;
            result.m02 = m02;
            result.m03 = m03;
            result.m10 = m10;
            result.m11 = m11;
            result.m12 = m12;
            result.m13 = m13;
            result.m20 = m20;
            result.m21 = m21;
            result.m22 = m22;
            result.m23 = m23;
            result.m30 = m30;
            result.m31 = m31;
            result.m32 = m32;
            result.m33 = m33;
            return result;
        }

        // ReSharper disable once InconsistentNaming
        public Matrix3x3 To3x3() => new Matrix3x3(this);

        public Matrix4x4(Vector4 column0, Vector4 column1, Vector4 column2, Vector4 column3) =>
            this = new UnityMatrix4x4(column0, column1, column2, column3);

        public static implicit operator UnityMatrix4x4(Matrix4x4 value) => value.ToUnity();
        public static implicit operator Matrix4x4(UnityMatrix4x4 value) => new Matrix4x4(value);
        public static Matrix4x4 operator *(Matrix4x4 a, Matrix4x4 b) => a.ToUnity() * b.ToUnity();
        public static Matrix4x4 operator *(Matrix4x4 a, UnityMatrix4x4 b) => a.ToUnity() * b;
        public static Matrix4x4 operator *(UnityMatrix4x4 a, Matrix4x4 b) => a * b.ToUnity();
        public static bool operator ==(Matrix4x4 a, Matrix4x4 b) => a.Equals(b);
        public static bool operator !=(Matrix4x4 a, Matrix4x4 b) => !a.Equals(b);
        public static Vector4 operator *(Matrix4x4 a, Vector4 b) => a.ToUnity() * b;
        public bool Equals(Matrix4x4 other) => ToUnity().Equals(other.ToUnity());
        public override bool Equals(object obj) => obj is Matrix4x4 other && Equals(other);
        public override int GetHashCode() => ToUnity().GetHashCode();
        public override string ToString() => ToUnity().ToString();

        public static Matrix4x4 TRS(Vector3 pos, Quaternion rot, Vector3 scale) => UnityMatrix4x4.TRS(pos, rot, scale);
        public static Matrix4x4 TRS(Transform t) => UnityMatrix4x4.TRS(t.localPosition, t.localRotation, t.localScale);
    }
}
