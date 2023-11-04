using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    internal partial class Utils
    {
        public static TransformParentEnumerable ParentEnumerable(this Transform transform) =>
            new TransformParentEnumerable(transform);

        public readonly struct TransformParentEnumerable : IEnumerable<Transform>
        {
            private readonly Transform _transform;

            public TransformParentEnumerable(Transform transform) => _transform = transform;
            public Enumerator GetEnumerator() => new Enumerator(_transform);
            IEnumerator<Transform> IEnumerable<Transform>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public struct Enumerator : IEnumerator<Transform>
            {
                object IEnumerator.Current => Current;
                public Transform Current { get; private set; }
                private readonly Transform _initial;

                public Enumerator(Transform transform) => _initial = Current = transform;

                public bool MoveNext()
                {
                    Current = Current != null ? Current.parent : null;
                    return Current != null;
                }

                public void Reset() => Current = _initial;

                public void Dispose()
                {
                }
            }
        }
    }
}