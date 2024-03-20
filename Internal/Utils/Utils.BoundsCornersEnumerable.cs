using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    partial class Utils
    {
        public static BoundsCornersEnumerable Corners(this Bounds bounds)
        {
            return new BoundsCornersEnumerable(bounds);
        }

        public readonly struct BoundsCornersEnumerable : IEnumerable<Vector3>
        {
            private readonly Bounds _bounds;

            public BoundsCornersEnumerable(Bounds bounds) => _bounds = bounds;

            public Enumerator GetEnumerator() => new Enumerator(_bounds);

            IEnumerator<Vector3> IEnumerable<Vector3>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public struct Enumerator : IEnumerator<Vector3>
            {
                private readonly Bounds _bounds;

                // 0: before first
                // 1..=8: corner
                // 9: end
                private int _index;

                public Enumerator(Bounds bounds) => (_bounds, _index) = (bounds, 0);

                public bool MoveNext() => ++_index <= 8;

                public void Reset() => _index = 0;

                public Vector3 Current
                {
                    get
                    {
                        switch (_index)
                        {
                            case 1:
                                return new Vector3(_bounds.min.x, _bounds.min.y, _bounds.min.z);
                            case 2:
                                return new Vector3(_bounds.min.x, _bounds.min.y, _bounds.max.z);
                            case 3:
                                return new Vector3(_bounds.min.x, _bounds.max.y, _bounds.min.z);
                            case 4:
                                return new Vector3(_bounds.min.x, _bounds.max.y, _bounds.max.z);
                            case 5:
                                return new Vector3(_bounds.max.x, _bounds.min.y, _bounds.min.z);
                            case 6:
                                return new Vector3(_bounds.max.x, _bounds.min.y, _bounds.max.z);
                            case 7:
                                return new Vector3(_bounds.max.x, _bounds.max.y, _bounds.min.z);
                            case 8:
                                return new Vector3(_bounds.max.x, _bounds.max.y, _bounds.max.z);
                            default:
                                throw new InvalidOperationException();
                        }
                    }
                }

                object IEnumerator.Current => Current;

                public void Dispose()
                {
                }
            }
        }
    }
}
