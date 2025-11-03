using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor.Animations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer;

using IntRangeImpl = ClosedRange<int, RangeIntTrait>;
using FloatOpenRange = ClosedRange<float, RangeFloatTrait>;

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

    public readonly TValue MinInclusive; // inclusive
    public readonly TValue MaxInclusive; // inclusive

    // exclusive view
    public TValue? MinExclusive => IsEmpty() ? MinInclusive : default(TTrait).Previous(MinInclusive);
    public TValue? MaxExclusive => IsEmpty() ? MaxInclusive : default(TTrait).Next(MaxInclusive);

    private ClosedRange(TValue minInclusive, TValue maxInclusive)
    {
        MinInclusive = minInclusive;
        MaxInclusive = maxInclusive;
    }

    public static ClosedRange<TValue, TTrait> Empty => new(POS_INF, NEG_INF); // Min > Max => empty
    public static ClosedRange<TValue, TTrait> Entire => new(NEG_INF, POS_INF);
    public static ClosedRange<TValue, TTrait> FromInclusiveBounds(TValue minInclusive, TValue maxInclusive) => new(minInclusive, maxInclusive);
    public static ClosedRange<TValue, TTrait> FromExclusiveBounds(TValue? minExclusive, TValue? maxExclusive)
    {
        var minInclusiveOpt = minExclusive is {} minEx ? default(TTrait).Next(minEx) : NEG_INF;
        var maxInclusiveOpt = maxExclusive is {} maxEx ? default(TTrait).Previous(maxEx) : POS_INF;
        return (minInclusiveOpt, maxInclusiveOpt) switch
        {
            (null, _) => Empty, // no next value for minExclusive => empty
            (_, null) => Empty, // no previous value for maxExclusive => empty
            ({} minInclusive, {} maxInclusive) => new ClosedRange<TValue, TTrait>(minInclusive, maxInclusive),
        };
    }

    public static ClosedRange<TValue, TTrait> GreaterThanInclusive(TValue min) => new(min, POS_INF);
    public static ClosedRange<TValue, TTrait> LessThanInclusive(TValue max) => new(NEG_INF, max);
    public static ClosedRange<TValue, TTrait> GreaterThanExclusive(TValue min) => default(TTrait).Next(min) is {} minInclusive ? GreaterThanInclusive(minInclusive) : Empty;
    public static ClosedRange<TValue, TTrait> LessThanExclusive(TValue max) => default(TTrait).Previous(max) is {} maxInclusive ? LessThanInclusive(maxInclusive) : Empty;
    public static ClosedRange<TValue, TTrait> Point(TValue v) => new(v, v);

    public bool IsEmpty() => default(TTrait).Compare(MinInclusive, MaxInclusive) > 0;

    public ClosedRange<TValue, TTrait> Intersect(ClosedRange<TValue, TTrait> other) => new(default(TTrait).Max(MinInclusive, other.MinInclusive), default(TTrait).Min(MaxInclusive, other.MaxInclusive));

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
                return new[] { new ClosedRange<TValue, TTrait>(default(TTrait).Max(vNext, MinInclusive), MaxInclusive) }.Where(r => !r.IsEmpty());
            case ({ } vPrev, null):
                // no next: v is max sentinel
                return new[] { new ClosedRange<TValue, TTrait>(MinInclusive, default(TTrait).Min(MaxInclusive, vPrev)) }.Where(r => !r.IsEmpty());
            case ({} vPrev, {} vNext):
                // normal value
                return new[]
                {
                    new ClosedRange<TValue, TTrait>(MinInclusive, default(TTrait).Min(MaxInclusive, vPrev)),
                    new ClosedRange<TValue, TTrait>(default(TTrait).Max(MinInclusive,  vNext), MaxInclusive),
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
        if (default(TTrait).Compare(MaxInclusive, other.MinInclusive) < 0)
        {
            //          zero or more gap
            //                |
            //       this.Max | other.Min
            //              V V V
            // [ this range ]   [ other range ]
            //-----------------------------------> x

            // There must be next value for this.Max because this.Max < other.Min so at least other.Min is next.
            var maxNext = default(TTrait).Next(MaxInclusive)!.Value;
            if (default(TTrait).Compare(maxNext, other.MinInclusive) < 0) return null;
        }
        else if (default(TTrait).Compare(other.MaxInclusive, MinInclusive) < 0)
        {
            //           zero or more gap
            //                 |
            //       other.Max | this.Min
            //               V V V
            // [ other range ]   [ this range ]
            //-----------------------------------> x

            // There must be next value for other.Max because other.Max < this.Min so at least this.Min is next.
            var otherMaxNext = default(TTrait).Next(other.MaxInclusive)!.Value;
            if (default(TTrait).Compare(otherMaxNext, MinInclusive) < 0) return null;
        }

        var min = default(TTrait).Min(MinInclusive, other.MinInclusive);
        var max = default(TTrait).Max(MaxInclusive, other.MaxInclusive);
        return new ClosedRange<TValue, TTrait>(min, max);
    }

    public bool Equals(ClosedRange<TValue, TTrait> other) =>
        IsEmpty() && other.IsEmpty() || MinInclusive.Equals(other.MinInclusive) && MaxInclusive.Equals(other.MaxInclusive);

    public override bool Equals(object? obj) => obj is ClosedRange<TValue, TTrait> other && Equals(other);
    public override int GetHashCode() => IsEmpty() ? 0 : HashCode.Combine(MinInclusive, MaxInclusive);
    public static bool operator ==(ClosedRange<TValue, TTrait> left, ClosedRange<TValue, TTrait> right) => left.Equals(right);
    public static bool operator !=(ClosedRange<TValue, TTrait> left, ClosedRange<TValue, TTrait> right) => !left.Equals(right);

    public override string ToString() => IsEmpty() ? "Empty" : $"[{MinInclusive}, {MaxInclusive}]";
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

public struct RangeFloatTrait : IRangeTrait<float>
{
    public float MinValue => float.NegativeInfinity;
    public float MaxValue => float.PositiveInfinity;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float? Next(float element) => float.IsPositiveInfinity(element) ? null : Utils.NextFloat(element);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float? Previous(float element) => float.IsNegativeInfinity(element) ? null : Utils.PreviousFloat(element);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Compare(float a, float b) => a.CompareTo(b);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Min(float a, float b) => Math.Min(a, b);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Max(float a, float b) => Math.Max(a, b);
}

public static class RangesUtil
{
    // convert to animator conditions for a given parameter name
    public static AnimatorCondition[] ToConditions(this IntRangeImpl range, string parameter)
    {
        return (range.MinExclusive, range.MaxExclusive) switch
        {
            // if both bounds are same, use Equals condition
            (_, _) when range.MinInclusive == range.MaxInclusive => new[] { EqualsCondition(parameter, range.MinInclusive) },

            // otherwise, use Greater/Less conditions
            (null, null) => Array.Empty<AnimatorCondition>(),
            ({ } min, null) => new[] { GreaterCondition(parameter, min) },
            (null, { } max) => new[] { LessCondition(parameter, max) },
            ({ } min, { } max) => new[] { GreaterCondition(parameter, min), LessCondition(parameter, max), }
        };
    }

    public static AnimatorCondition[] ToConditions(this FloatOpenRange range, string parameter) => (Min: range.MinExclusive, Max: range.MaxExclusive) switch
    {
        (null, null) => Array.Empty<AnimatorCondition>(),
        ({ } min, null) => new[] { GreaterCondition(parameter, min) },
        (null, { } max) => new[] { LessCondition(parameter, max) },
        ({ } min, { } max) => new[] { GreaterCondition(parameter, min), LessCondition(parameter, max) },
    };

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
