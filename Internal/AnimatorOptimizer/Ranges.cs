using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor.Animations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer;

using IntRangeImpl = ClosedRange<int, RangeIntTrait>;

// This file includes 'range' related utilities for animator optimization.

public interface IRangeTrait<TValue> where TValue : struct
{
    public TValue MinValue { get; }
    public TValue MaxValue { get; }

    // sibling values
    // returns null if no next/previous value exists (e.g., for max/min sentinel)
    public TValue? Next(TValue element);
    public TValue? Previous(TValue element);

    // comparison
    public int Compare(TValue a, TValue b);
    public TValue Min(TValue a, TValue b);
    public TValue Max(TValue a, TValue b);
}

public readonly struct ClosedRange<TValue, TTrait> : IEquatable<ClosedRange<TValue, TTrait>>
    where TValue : struct, IEquatable<TValue>
    where TTrait: struct, IRangeTrait<TValue> {

    // inclusive bounds; use sentinels to represent unbounded
    private static readonly TValue NEG_INF = default(TTrait).MinValue;
    private static readonly TValue POS_INF = default(TTrait).MaxValue;

    public readonly TValue Min; // inclusive
    public readonly TValue Max; // inclusive

    public ClosedRange(TValue min, TValue max)
    {
        Min = min;
        Max = max;
    }

    public static ClosedRange<TValue, TTrait> Empty => new(POS_INF, NEG_INF); // Min > Max => empty
    public static ClosedRange<TValue, TTrait> Entire => new(NEG_INF, POS_INF);
    public static ClosedRange<TValue, TTrait> FromMinInclusive(TValue min) => new(min, POS_INF);
    public static ClosedRange<TValue, TTrait> FromMaxInclusive(TValue max) => new(NEG_INF, max);
    public static ClosedRange<TValue, TTrait> Point(TValue v) => new(v, v);

    public bool IsEmpty() => default(TTrait).Compare(Min, Max) > 0;

    public ClosedRange<TValue, TTrait> Intersect(ClosedRange<TValue, TTrait> other) => new(default(TTrait).Max(Min, other.Min), default(TTrait).Min(Max, other.Max));

    // subtract single value v; may split range into up to two ranges
    public IEnumerable<ClosedRange<TValue, TTrait>> ExcludeValue(TValue v)
    {
        var vPrevOpt = default(TTrait).Previous(v);
        var vNextOpt = default(TTrait).Next(v);
        switch (vPrevOpt, vNextOpt)
        {
            case (null, null):
                // no previous or next: v is both min and max sentinel
                return Array.Empty<ClosedRange<TValue, TTrait>>();
            case (null, { } vNext):
                // no previous: v is min sentinel
                return new[] { new ClosedRange<TValue, TTrait>(default(TTrait).Max(vNext, Min), Max) }.Where(r => !r.IsEmpty());
            case ({ } vPrev, null):
                // no next: v is max sentinel
                return new[] { new ClosedRange<TValue, TTrait>(Min, default(TTrait).Min(Max, vPrev)) }.Where(r => !r.IsEmpty());
            case ({} vPrev, {} vNext):
                // normal value
                return new[]
                {
                    new ClosedRange<TValue, TTrait>(Min, default(TTrait).Min(Max, vPrev)),
                    new ClosedRange<TValue, TTrait>(default(TTrait).Max(Min,  vNext), Max),
                }.Where(r => !r.IsEmpty());
        }
    }

    // try union: if adjacent or overlapping, return union; otherwise null
    public ClosedRange<TValue, TTrait>? Union(ClosedRange<TValue, TTrait> other)
    {
        if (IsEmpty()) return other;
        if (other.IsEmpty()) return this;

        // return null if disjoint with gap > 0

        // ranges are disjoint if one's max + 1 < other's min or vice versa
        if (default(TTrait).Compare(Max, other.Min) < 0)
        {
            //          zero or more gap
            //                |
            //       this.Max | other.Min
            //              V V V
            // [ this range ]   [ other range ]
            //-----------------------------------> x

            // There must be next value for this.Max because this.Max < other.Min so at least other.Min is next.
            var maxNext = default(TTrait).Next(Max)!.Value;
            if (default(TTrait).Compare(maxNext, other.Min) < 0) return null;
        }
        else if (default(TTrait).Compare(other.Max, Min) < 0)
        {
            //           zero or more gap
            //                 |
            //       other.Max | this.Min
            //               V V V
            // [ other range ]   [ this range ]
            //-----------------------------------> x

            // There must be next value for other.Max because other.Max < this.Min so at least this.Min is next.
            var otherMaxNext = default(TTrait).Next(other.Max)!.Value;
            if (default(TTrait).Compare(otherMaxNext, Min) < 0) return null;
        }

        var min = default(TTrait).Min(Min, other.Min);
        var max = default(TTrait).Max(Max, other.Max);
        return new ClosedRange<TValue, TTrait>(min, max);
    }

    public bool Equals(ClosedRange<TValue, TTrait> other) =>
        IsEmpty() && other.IsEmpty() || Min.Equals(other.Min) && Max.Equals(other.Max);

    public override bool Equals(object? obj) => obj is ClosedRange<TValue, TTrait> other && Equals(other);
    public override int GetHashCode() => IsEmpty() ? 0 : HashCode.Combine(Min, Max);
    public static bool operator ==(ClosedRange<TValue, TTrait> left, ClosedRange<TValue, TTrait> right) => left.Equals(right);
    public static bool operator !=(ClosedRange<TValue, TTrait> left, ClosedRange<TValue, TTrait> right) => !left.Equals(right);

    public override string ToString() => IsEmpty() ? "Empty" : $"[{Min}, {Max}]";
}

public struct RangeIntTrait : IRangeTrait<int>
{
    public int MinValue => int.MinValue;
    public int MaxValue => int.MaxValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int? Next(int element) => element == int.MaxValue ? null : element + 1;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int? Previous(int element) => element == int.MinValue ? null : element - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Compare(int a, int b) => a.CompareTo(b);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Min(int a, int b) => Math.Min(a, b);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Max(int a, int b) => Math.Max(a, b);
}

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

public static class RangesUtil
{
    // convert to animator conditions for a given parameter name
    public static AnimatorCondition[] ToConditions(this IntRangeImpl range, string parameter)
    {
        var hasMin = range.MinInclusive != int.MinValue;
        var hasMax = range.MaxInclusive != int.MaxValue;

        return (hasMin, hasMax) switch
        {
            (false, false) => Array.Empty<AnimatorCondition>(),
            (true, false) => new[] { GreaterCondition(parameter, range.MinInclusive - 1) },
            (false, true) => new[] { LessCondition(parameter, range.MaxInclusive + 1f) },
            (true, true) => range.MinInclusive == range.MaxInclusive
                ? new[] { EqualsCondition(parameter, range.MinInclusive) }
                : new[]
                {
                    GreaterCondition(parameter, range.MinInclusive - 1f), LessCondition(parameter, range.MaxInclusive + 1f)
                }
        };
    }

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
