using System;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    /// <summary>
    /// AAO internal replacement for UnityEngine.Matrix3x3 for more math operations
    /// </summary>
    // ReSharper disable once InconsistentNaming
    struct Matrix3x3 : IEquatable<Matrix3x3>
    {
        // ReSharper disable InconsistentNaming
        public static Matrix3x3 zero = new Matrix3x3(0, 0, 0, 0, 0, 0, 0, 0, 0);
        public static Matrix3x3 identity = new Matrix3x3(1, 0, 0, 0, 1, 0, 0, 0, 1);

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

        public Matrix3x3 transpose => new Matrix3x3(
            m00, m01, m02,
            m10, m11, m12,
            m20, m21, m22);

        public float determinant => m00 * m11 * m22 + m01 * m12 * m20 + m02 * m10 * m21
                                    - m02 * m11 * m20 - m00 * m12 * m21 - m01 * m10 * m22;

        public Quaternion rotation => (this / determinant).ComputeRotationNormalized();

        private Quaternion ComputeRotationNormalized()
        {
            // https://github.com/davheld/tf/blob/824bb0bf2c26308e41c1add4ded31d0bf2775730/include/tf/LinearMath/Matrix3x3.h#L243-L275
            var trace = m00 + m11 + m22;
            Quaternion result = default;

            if (trace > 0) 
            {
                // non-zero scale
                var s = Mathf.Sqrt(trace + 1.0f);
                result.w = s * 0.5f;
                s = 0.5f / s;

                result.x = (m21 - m12) * s;
                result.y = (m02 - m20) * s;
                result.z = (m10 - m01) * s;
            } 
            else 
            {
                int i = m00 < m11 ? 
                    (m11 < m22 ? 2 : 1) :
                    (m00 < m22 ? 2 : 0); 
                int j = (i + 1) % 3;  
                int k = (i + 2) % 3;

                var s = Mathf.Sqrt(this[i, i] - this[j, j] - this[k, k] + 1.0f);
                result[i] = s * 0.5f;
                s = 0.5f / s;

                result[3] = (this[k, j] - this[j, k]) * s;
                result[j] = (this[j, i] + this[i, j]) * s;
                result[k] = (this[k, i] + this[i, k]) * s;
            }

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
            unchecked
            {
                var hashCode = m00.GetHashCode();
                hashCode = (hashCode * 397) ^ m10.GetHashCode();
                hashCode = (hashCode * 397) ^ m20.GetHashCode();
                hashCode = (hashCode * 397) ^ m01.GetHashCode();
                hashCode = (hashCode * 397) ^ m11.GetHashCode();
                hashCode = (hashCode * 397) ^ m21.GetHashCode();
                hashCode = (hashCode * 397) ^ m02.GetHashCode();
                hashCode = (hashCode * 397) ^ m12.GetHashCode();
                hashCode = (hashCode * 397) ^ m22.GetHashCode();
                return hashCode;
            }
        }

        public bool Equals(Matrix3x3 other) =>
            m00.Equals(other.m00) && m10.Equals(other.m10) && m20.Equals(other.m20) &&
            m01.Equals(other.m01) && m11.Equals(other.m11) && m21.Equals(other.m21) &&
            m02.Equals(other.m02) && m12.Equals(other.m12) && m22.Equals(other.m22);

        public override bool Equals(object obj) => obj is Matrix3x3 other && Equals(other);
    }
}