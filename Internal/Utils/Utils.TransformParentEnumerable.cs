using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    partial class Utils
    {
        public static TransformParentEnumerable ParentEnumerable(this Transform transform) =>
            transform.ParentEnumerable(null);

        public static TransformParentEnumerable ParentEnumerable(this Transform transform, bool includeMe) =>
            transform.ParentEnumerable(null, includeMe);

        // root is exclusive
        public static TransformParentEnumerable ParentEnumerable(this Transform transform,
            Transform? root, bool includeMe = false) =>
            new TransformParentEnumerable(transform, root, includeMe);

        public readonly struct TransformParentEnumerable : IEnumerable<Transform>
        {
            private readonly Transform _transform;
            private readonly Transform? _root;
            private readonly bool _includeMe;

            public TransformParentEnumerable(Transform transform, Transform? root, bool includeMe) => 
                (_transform, _root, _includeMe) = (transform, root, includeMe);

            public Enumerator GetEnumerator() => new Enumerator(_transform, _root, _includeMe);
            IEnumerator<Transform> IEnumerable<Transform>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public struct Enumerator : IEnumerator<Transform>
            {
                object IEnumerator.Current => Current;
                public Transform Current => _current != null ? _current : throw new Exception("invalid state");

                private readonly Transform _initial;
                private readonly Transform? _root;
                private readonly bool _includeMe;

                private bool _beforeFirst;
                private Transform? _current;

                public Enumerator(Transform transform, Transform? root, bool includeMe)
                {
                    _current = null;
                    _initial = transform;
                    _root = root;
                    _includeMe = includeMe;
                    _beforeFirst = true;
                }

                public bool MoveNext()
                {
                    if (_beforeFirst)
                        _current = _includeMe ? _initial : _initial != null ? _initial.parent : null;
                    else
                        _current = _current != null ? _current.parent : null;
                    _beforeFirst = false;
                    if (_current == _root) _current = null;
                    return _current != null;
                }

                public void Reset()
                {
                    _beforeFirst = true;
                    _current = null;
                }

                public void Dispose()
                {
                }
            }
        }
    }
}
