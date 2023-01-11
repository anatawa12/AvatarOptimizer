using System.Collections;
using System.Collections.Generic;

namespace Anatawa12.AvatarOptimizer
{
    internal static class Utils
    {
        public static ZipWithNextEnumerable<T> ZipWithNext<T>(this IEnumerable<T> enumerable) =>
            new ZipWithNextEnumerable<T>(enumerable);
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
}
