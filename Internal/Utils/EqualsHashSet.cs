using System;
using System.Collections;
using System.Collections.Generic;

namespace Anatawa12.AvatarOptimizer
{
    public sealed class EqualsHashSet<T> : IEquatable<EqualsHashSet<T>>, IEnumerable<T>
    {
        public readonly HashSet<T> backedSet;

        public EqualsHashSet(HashSet<T> backedSet)
        {
            if (backedSet == null) throw new ArgumentNullException(nameof(backedSet));
            this.backedSet = backedSet;
        }

        public EqualsHashSet(IEnumerable<T> collection) : this(new HashSet<T>(collection))
        {
        }

        public int Count => backedSet.Count;

        public override int GetHashCode() => backedSet.GetSetHashCode();

        public bool Equals(EqualsHashSet<T> other) =>
            !ReferenceEquals(null, other) && (ReferenceEquals(this, other) || backedSet.SetEquals(other.backedSet) && backedSet.Comparer.Equals(backedSet.Comparer));

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        public HashSet<T>.Enumerator GetEnumerator() => backedSet.GetEnumerator();

        public override bool Equals(object? obj) =>
            ReferenceEquals(this, obj) || obj is EqualsHashSet<T> other && Equals(other);

        public static bool operator ==(EqualsHashSet<T>? left, EqualsHashSet<T>? right) => Equals(left, right);
        public static bool operator !=(EqualsHashSet<T>? left, EqualsHashSet<T>? right) => !Equals(left, right);

        // empty
        public static readonly EqualsHashSet<T> Empty = new(new HashSet<T>());
    }
}
