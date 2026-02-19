using System;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    /// <summary>
    /// AAO internal replacement for UnityEngine.Matrix3x3 for more math operations
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public struct Matrix3x3 : IEquatable<Matrix3x3>
    {
        // ReSharper disable InconsistentNaming
        public static readonly Matrix3x3 zero = new Matrix3x3(0, 0, 0, 0, 0, 0, 0, 0, 0);
        public static readonly Matrix3x3 identity = new Matrix3x3(1, 0, 0, 0, 1, 0, 0, 0, 1);

        public float determinant =>
            (m00 * m11 * m22) + (m01 * m12 * m20) + (m02 * m10 * m21)
            - (m00 * m12 * m21) - (m01 * m10 * m22) - (m02 * m11 * m20);

        // @formatter:off
        public float m00;
        public float m10;
        public float m20;
        public float m01;
        public float m11;
        public float m21;
        public float m02;
        public float m12;
        public float m22;
        // @formatter:on

        public float this[int row, int col]
        {
            get
            {
                if ((uint)row >= 3) throw new IndexOutOfRangeException(nameof(row));
                if ((uint)col >= 3) throw new IndexOutOfRangeException(nameof(col));

                // @formatter:off
                switch (row + col * 3)
                {
                    case 0 + 0 * 3: return m00;
                    case 1 + 0 * 3: return m10;
                    case 2 + 0 * 3: return m20;
                    case 0 + 1 * 3: return m01;
                    case 1 + 1 * 3: return m11;
                    case 2 + 1 * 3: return m21;
                    case 0 + 2 * 3: return m02;
                    case 1 + 2 * 3: return m12;
                    case 2 + 2 * 3: return m22;
                    default: throw new Exception("unreachable");
                }
                // @formatter:on
            }
        }

        public static Matrix3x3 Rotate(Quaternion q)
        {
            float x2 = q.x * 2f;
            float y2 = q.y * 2f;
            float z2 = q.z * 2f;
            float xx2 = q.x * x2;
            float yy2 = q.y * y2;
            float zz2 = q.z * z2;
            float xy2 = q.x * y2;
            float xz2 = q.x * z2;
            float yz2 = q.y * z2;
            float xw2 = q.w * x2;
            float yw2 = q.w * y2;
            float zw2 = q.w * z2;
            Matrix3x3 result;
            result.m00 = 1f - (yy2 + zz2);
            result.m10 = xy2 + zw2;
            result.m20 = xz2 - yw2;

            result.m01 = xy2 - zw2;
            result.m11 = 1f - (xx2 + zz2);
            result.m21 = yz2 + xw2;

            result.m02 = xz2 + yw2;
            result.m12 = yz2 - xw2;
            result.m22 = 1f - (xx2 + yy2);
            return result;
        }


        public Vector3 MultiplyPoint3x3(Vector3 point)
        {
            Vector3 vector3;
            vector3.x = (float)((double)m00 * point.x + (double)m01 * point.y + (double)m02 * point.z);
            vector3.y = (float)((double)m10 * point.x + (double)m11 * point.y + (double)m12 * point.z);
            vector3.z = (float)((double)m20 * point.x + (double)m21 * point.y + (double)m22 * point.z);
            return vector3;
        }

        public static Matrix3x3 operator *(Matrix3x3 m, float w)
        {
            m.m00 *= w;
            m.m01 *= w;
            m.m02 *= w;
            m.m10 *= w;
            m.m11 *= w;
            m.m12 *= w;
            m.m20 *= w;
            m.m21 *= w;
            m.m22 *= w;
            return m;
        }

        public static Matrix3x3 operator *(float w, Matrix3x3 m) => m * w;

        public static Matrix3x3 operator *(Matrix3x3 lhs, Matrix3x3 rhs)
        {
            Matrix3x3 result;
            result.m00 = lhs.m00 * rhs.m00 + lhs.m01 * rhs.m10 + lhs.m02 * rhs.m20;
            result.m01 = lhs.m00 * rhs.m01 + lhs.m01 * rhs.m11 + lhs.m02 * rhs.m21;
            result.m02 = lhs.m00 * rhs.m02 + lhs.m01 * rhs.m12 + lhs.m02 * rhs.m22;

            result.m10 = lhs.m10 * rhs.m00 + lhs.m11 * rhs.m10 + lhs.m12 * rhs.m20;
            result.m11 = lhs.m10 * rhs.m01 + lhs.m11 * rhs.m11 + lhs.m12 * rhs.m21;
            result.m12 = lhs.m10 * rhs.m02 + lhs.m11 * rhs.m12 + lhs.m12 * rhs.m22;

            result.m20 = lhs.m20 * rhs.m00 + lhs.m21 * rhs.m10 + lhs.m22 * rhs.m20;
            result.m21 = lhs.m20 * rhs.m01 + lhs.m21 * rhs.m11 + lhs.m22 * rhs.m21;
            result.m22 = lhs.m20 * rhs.m02 + lhs.m21 * rhs.m12 + lhs.m22 * rhs.m22;
            return result;
        }

        public static Matrix3x3 operator /(Matrix3x3 m, float w)
        {
            m.m00 /= w;
            m.m01 /= w;
            m.m02 /= w;
            m.m10 /= w;
            m.m11 /= w;
            m.m12 /= w;
            m.m20 /= w;
            m.m21 /= w;
            m.m22 /= w;
            return m;
        }

        public static Matrix3x3 operator +(Matrix3x3 a, Matrix3x3 b)
        {
            a.m00 += b.m00;
            a.m01 += b.m01;
            a.m02 += b.m02;
            a.m10 += b.m10;
            a.m11 += b.m11;
            a.m12 += b.m12;
            a.m20 += b.m20;
            a.m21 += b.m21;
            a.m22 += b.m22;
            return a;
        }

        // ReSharper restore InconsistentNaming

        public Matrix3x3(Matrix4x4 matrix)
        {
            m00 = matrix.m00;
            m10 = matrix.m10;
            m20 = matrix.m20;
            m01 = matrix.m01;
            m11 = matrix.m11;
            m21 = matrix.m21;
            m02 = matrix.m02;
            m12 = matrix.m12;
            m22 = matrix.m22;
        }

        public Matrix3x3(
            float m00, float m10, float m20,
            float m01, float m11, float m21,
            float m02, float m12, float m22)
        {
            this.m00 = m00;
            this.m10 = m10;
            this.m20 = m20;
            this.m01 = m01;
            this.m11 = m11;
            this.m21 = m21;
            this.m02 = m02;
            this.m12 = m12;
            this.m22 = m22;
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(m00);
            hashCode.Add(m10);
            hashCode.Add(m20);
            hashCode.Add(m01);
            hashCode.Add(m11);
            hashCode.Add(m21);
            hashCode.Add(m02);
            hashCode.Add(m12);
            hashCode.Add(m22);
            return hashCode.ToHashCode();
        }

        public bool Equals(Matrix3x3 other) =>
            m00.Equals(other.m00) && m10.Equals(other.m10) && m20.Equals(other.m20) &&
            m01.Equals(other.m01) && m11.Equals(other.m11) && m21.Equals(other.m21) &&
            m02.Equals(other.m02) && m12.Equals(other.m12) && m22.Equals(other.m22);

        public override bool Equals(object? obj) => obj is Matrix3x3 other && Equals(other);
        public static bool operator ==(Matrix3x3 left, Matrix3x3 right) => left.Equals(right);
        public static bool operator !=(Matrix3x3 left, Matrix3x3 right) => !(left == right);
    }
}
