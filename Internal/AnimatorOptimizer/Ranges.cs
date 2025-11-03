using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer;

// This file includes 'range' related utilities for animator optimization.

/// <summary>
/// Represents an open range of float values: (Min, Max), where each bound is exclusive.
/// Null bound means unbounded (infinity + 1).
/// </summary>
public struct FloatOpenRange : IEquatable<FloatOpenRange>
{
    // each value is exclusive, null means unbounded (infinity + 1)
    public float? Min;
    public float? Max;

    public FloatOpenRange(float? min = null, float? max = null)
    {
        Min = min;
        Max = max;
    }

    public static FloatOpenRange Empty => new(0f, 0f);
    public static FloatOpenRange Entire => default;
    public static FloatOpenRange LessThan(float value) => new(max: value);
    public static FloatOpenRange GreaterThan(float value) => new(min: value);

    public readonly bool IsEmpty() => Min.HasValue && Max.HasValue && Min.Value >= Max.Value;

    public readonly FloatOpenRange Intersect(FloatOpenRange other)
    {
        var result = this;
        if (other.Min is { } minOther) result.Min = Min is { } minSelf ? Mathf.Max(minSelf, minOther) : minOther;
        if (other.Max is { } maxOther) result.Max = Max is { } maxSelf ? Mathf.Min(maxSelf, maxOther) : maxOther;
        return result;
    }

    public readonly FloatOpenRange? Union(FloatOpenRange other)
    {
        // union-ing empty range and another will result another
        if (IsEmpty()) return other;
        if (other.IsEmpty()) return this;

        // check if two ranges are adjacent or overlapping
        if (Intersect(other).IsEmpty()) return null;

        var result = this;
        result.Min = (this.Min, other.Min) is ({ } minSelf, { } minOther) ? Mathf.Min(minSelf, minOther) : null;
        result.Max = (this.Max, other.Max) is ({ } maxSelf, { } maxOther) ? Mathf.Max(maxSelf, maxOther) : null;
        return result;
    }

    public readonly bool Equals(FloatOpenRange other) =>
        IsEmpty() && other.IsEmpty() || Nullable.Equals(Min, other.Min) && Nullable.Equals(Max, other.Max);

    public readonly override bool Equals(object? obj) => obj is FloatOpenRange other && Equals(other);
    public readonly override int GetHashCode() => IsEmpty() ? 0 : HashCode.Combine(Min, Max);
    public static bool operator ==(FloatOpenRange left, FloatOpenRange right) => left.Equals(right);
    public static bool operator !=(FloatOpenRange left, FloatOpenRange right) => !left.Equals(right);

    public override string ToString() =>
        IsEmpty() ? "Empty" : $"({Min?.ToString() ?? "none"}, {Max?.ToString() ?? "none"})";

    public readonly AnimatorCondition[] ToConditions(string parameter) => (Min, Max) switch
    {
        (null, null) => Array.Empty<AnimatorCondition>(),
        ({ } min, null) => new[] { RangesUtil.GreaterCondition(parameter, min) },
        (null, { } max) => new[] { RangesUtil.LessCondition(parameter, max) },
        ({ } min, { } max) => new[]
        {
            RangesUtil.GreaterCondition(parameter, min),
            RangesUtil.LessCondition(parameter, max),
        },
    };
}

/// <summary>
/// Represents a closed range of integer values: [Min, Max], where each bound is inclusive.
/// </summary>
public struct IntClosedRange : IEquatable<IntClosedRange>
{
    // inclusive bounds; use sentinels to represent unbounded
    private const int NEG_INF = int.MinValue;
    private const int POS_INF = int.MaxValue;

    public int Min; // inclusive
    public int Max; // inclusive

    public IntClosedRange(int min, int max)
    {
        Min = min;
        Max = max;
    }

    public static IntClosedRange Empty => new(1, 0); // Min > Max => empty
    public static IntClosedRange Entire => new(NEG_INF, POS_INF);
    public static IntClosedRange FromMin(int min) => new(min, POS_INF);
    public static IntClosedRange FromMax(int max) => new(NEG_INF, max);
    public static IntClosedRange Point(int v) => new(v, v);

    public bool IsEmpty() => Min > Max;

    public IntClosedRange Intersect(IntClosedRange other) => new(Math.Max(Min, other.Min), Math.Min(Max, other.Max));

    // subtract single value v; may split range into up to two ranges
    public IEnumerable<IntClosedRange> ExcludeValue(int v)
    {
        if (v == NEG_INF) return new[] { new IntClosedRange(Math.Max(Min, v + 1), Max) }.Where(r => !r.IsEmpty());
        if (v == POS_INF) return new[] { new IntClosedRange(Min, Math.Min(Max, v - 1)) }.Where(r => !r.IsEmpty());
        return new[]
        {
            new IntClosedRange(Min, Math.Min(Max, v - 1)),
            new IntClosedRange(Math.Max(Min, v + 1), Max),
        }.Where(r => !r.IsEmpty());
    }

    // try union: if adjacent or overlapping, return union; otherwise null
    public IntClosedRange? Union(IntClosedRange other)
    {
        if (IsEmpty()) return other;
        if (other.IsEmpty()) return this;

        // disjoint with gap > 0
        if (Max != POS_INF && other.Min != NEG_INF && (long)Max + 1 < other.Min) return null;
        if (other.Max != POS_INF && Min != NEG_INF && (long)other.Max + 1 < Min) return null;

        var min = Math.Min(Min, other.Min);
        var max = Math.Max(Max, other.Max);
        return new IntClosedRange(min, max);
    }

    public bool Equals(IntClosedRange other) =>
        IsEmpty() && other.IsEmpty() || Min == other.Min && Max == other.Max;

    public override bool Equals(object? obj) => obj is IntClosedRange other && Equals(other);
    public override int GetHashCode() => IsEmpty() ? 0 : HashCode.Combine(Min, Max);
    public static bool operator ==(IntClosedRange left, IntClosedRange right) => left.Equals(right);
    public static bool operator !=(IntClosedRange left, IntClosedRange right) => !left.Equals(right);

    public override string ToString() => IsEmpty() ? "Empty" : $"[{Min}, {Max}]";

    // convert to animator conditions for a given parameter name
    public AnimatorCondition[] ToConditions(string parameter)
    {
        var hasMin = Min != NEG_INF;
        var hasMax = Max != POS_INF;

        return (hasMin, hasMax) switch
        {
            (false, false) => Array.Empty<AnimatorCondition>(),
            (true, false) => new[] { RangesUtil.GreaterCondition(parameter, Min - 1) },
            (false, true) => new[] { RangesUtil.LessCondition(parameter, Max + 1f) },
            (true, true) => Min == Max
                ? new[] { RangesUtil.EqualsCondition(parameter, Min) }
                : new[]
                {
                    RangesUtil.GreaterCondition(parameter, Min - 1f), RangesUtil.LessCondition(parameter, Max + 1f)
                }
        };
    }
}

static class RangesUtil
{
    // utilities
    static AnimatorCondition AnimatorCondition(string parameter, AnimatorConditionMode mode, float threshold = 0) =>
        new()
        {
            parameter = parameter,
            mode = mode,
            threshold = threshold,
        };

    public static AnimatorCondition GreaterCondition(string parameter, float threshold) =>
        AnimatorCondition(parameter, AnimatorConditionMode.Greater, threshold);

    public static AnimatorCondition LessCondition(string parameter, float threshold) =>
        AnimatorCondition(parameter, AnimatorConditionMode.Less, threshold);

    public static AnimatorCondition EqualsCondition(string parameter, float threshold) =>
        AnimatorCondition(parameter, AnimatorConditionMode.Equals, threshold);

    public static AnimatorCondition NotEqualsCondition(string parameter, float threshold) =>
        AnimatorCondition(parameter, AnimatorConditionMode.NotEqual, threshold);
}
