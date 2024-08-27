using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    partial class Utils
    {
        public static TransformDirectChildrenEnumerable DirectChildrenEnumerable(this Transform transform) =>
            new TransformDirectChildrenEnumerable(transform);

        public readonly struct TransformDirectChildrenEnumerable : IEnumerable<Transform>
        {
            private readonly Transform _parent;

            public TransformDirectChildrenEnumerable(Transform parent)
            {

                _parent = parent;
            }

            public Enumerator GetEnumerator() => new Enumerator(_parent);
            IEnumerator<Transform> IEnumerable<Transform>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public struct Enumerator : IEnumerator<Transform>
            {
                private int _index;
                private readonly Transform _parent;
                object IEnumerator.Current => Current;
                public Transform Current => _parent.GetChild(_index);

                public Enumerator(Transform parent) => (_index, _parent) = (-1, parent);

                public bool MoveNext() => ++_index < _parent.childCount;
                public void Reset() => _index = -1;

                public void Dispose()
                {
                }
            }
        }
    }
}
