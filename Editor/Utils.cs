using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Anatawa12.Merger
{
    internal static class Utils
    {
        public static ZipWithNextEnumerable<T> ZipWithNext<T>(this IEnumerable<T> enumerable) =>
            new ZipWithNextEnumerable<T>(enumerable);

        public static ArraySerializedPropertyEnumerable AsEnumerable(this SerializedProperty property) =>
            new ArraySerializedPropertyEnumerable(property);
    }

    internal struct ZipWithNextEnumerable<T> : IEnumerable<(T, T)>
    {
        private readonly IEnumerable<T> _enumerable;

        public ZipWithNextEnumerable(IEnumerable<T> enumerable)
        {
            _enumerable = enumerable;
        }

        Enumerator GetEnumerator() => new Enumerator(_enumerable.GetEnumerator());
        IEnumerator<(T, T)> IEnumerable<(T, T)>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<(T, T)>
        {
            private readonly IEnumerator<T> _enumerator;
            private (T, T) _current;
            private bool _first;

            public Enumerator(IEnumerator<T> enumerator)
            {
                _enumerator = enumerator;
                _current = default;
                _first = true;
            }

            public bool MoveNext()
            {
                if (_first)
                {
                    if (!_enumerator.MoveNext()) return false;
                    _current = (default, _enumerator.Current);
                    _first = false;
                }
                if (!_enumerator.MoveNext()) return false;
                _current = (_current.Item2, _enumerator.Current);
                return true;
            }

            public void Reset()
            {
                _enumerator.Reset();
                _first = false;
            }

            public (T, T) Current => _current;
            object IEnumerator.Current => Current;
            public void Dispose() => _enumerator.Dispose();
        }
    }
    
    internal struct ArraySerializedPropertyEnumerable : IEnumerable<SerializedProperty>
    {
        private readonly SerializedProperty _property;

        public ArraySerializedPropertyEnumerable(SerializedProperty property)
        {
            this._property = property;
        }

        Enumerator GetEnumerator() => new Enumerator(_property);

        private struct Enumerator : IEnumerator<SerializedProperty>
        {
            private int _index;
            private readonly SerializedProperty _property;

            public Enumerator(SerializedProperty property)
            {
                _index = -1;
                _property = property;
            }

            public bool MoveNext() => ++_index < _property.arraySize;

            public void Reset() => _index = -1;

            public SerializedProperty Current => _property.GetArrayElementAtIndex(_index);

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }

        IEnumerator<SerializedProperty> IEnumerable<SerializedProperty>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
