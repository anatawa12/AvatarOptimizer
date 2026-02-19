using System.Collections;
using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.PrefabSafeSet;
using JetBrains.Annotations;

namespace Anatawa12.AvatarOptimizer.API
{
    [PublicAPI]
#pragma warning disable CA1710
    public readonly struct PrefabSafeSetAccessor<T> : ICollection<T> where T : notnull
#pragma warning restore CA1710
    {
        private readonly PrefabSafeSet<T> _set;

        internal PrefabSafeSetAccessor(PrefabSafeSet<T> set) => _set = set;

        [PublicAPI]
        public Enumerator GetEnumerator() => new Enumerator(_set.GetAsList().GetEnumerator());

        [PublicAPI]
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        [PublicAPI]
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        [PublicAPI]
        public void Add(T item) => _set.AddRange(new[] {item});
        [PublicAPI]
        public bool Remove(T item) => _set.RemoveRange(new[] { item });
        [PublicAPI]
        public void UnionWith(IEnumerable<T> other) => _set.AddRange(other);
        [PublicAPI]
        public void ExceptWith(IEnumerable<T> other) => _set.RemoveRange(other);
        [PublicAPI]
        public void Clear() => _set.Clear();
        [PublicAPI]
        public void CopyTo(T[] array, int arrayIndex) => _set.GetAsList().CopyTo(array, arrayIndex);
        [PublicAPI]
        public bool IsReadOnly => false;
        [PublicAPI]
        int ICollection<T>.Count => _set.GetAsList().Count;
        [PublicAPI]
        bool ICollection<T>.Contains(T item) => _set.GetAsSet().Contains(item);

        [PublicAPI]
        public struct Enumerator : IEnumerator<T>
        {
            private List<T>.Enumerator _set;
            internal Enumerator(List<T>.Enumerator set) => _set = set;
            [PublicAPI]
            public void Dispose() => _set.Dispose();
            [PublicAPI]
            public bool MoveNext() => _set.MoveNext();
            [PublicAPI]
            public void Reset() => ((IEnumerator)_set).Reset();
            [PublicAPI]
            public T Current => _set.Current;
            [PublicAPI]
            object IEnumerator.Current => Current;
        }
    }
}
