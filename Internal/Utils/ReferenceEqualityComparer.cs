using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Anatawa12.AvatarOptimizer;

/// <summary>
/// The <see cref="ReferenceEqualityComparer"/> is a singleton that provides reference equality comparison.
///
/// Same class exists in .NET 5 or later as `System.Collections.Generic.ReferenceEqualityComparer` but
/// unity does not support .NET 5 yet.  
/// </summary>
public sealed class ReferenceEqualityComparer : IEqualityComparer<object?>, IEqualityComparer
{
    private ReferenceEqualityComparer()
    {
    }

    public static ReferenceEqualityComparer Instance { get; } = new();

    public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
    public int GetHashCode(object? obj) => RuntimeHelpers.GetHashCode(obj);
}
