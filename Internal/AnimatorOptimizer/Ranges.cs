using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor.Animations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer;

using IntRange = Range<int, RangeIntTrait>;
using IntRangeSet = RangeSet<int, RangeIntTrait>;
using FloatRange = Range<float, RangeFloatTrait>;
using FloatRangeSet = RangeSet<float, RangeFloatTrait>;

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

public readonly struct Range<TValue, TTrait> : IEquatable<Range<TValue, TTrait>>
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

    private Range(TValue minInclusive, TValue maxInclusive)
    {
        MinInclusive = minInclusive;
        MaxInclusive = maxInclusive;
    }

    public static Range<TValue, TTrait> Empty => new(POS_INF, NEG_INF); // Min > Max => empty
    public static Range<TValue, TTrait> Entire => new(NEG_INF, POS_INF);
    public static Range<TValue, TTrait> FromInclusiveBounds(TValue minInclusive, TValue maxInclusive) => new(minInclusive, maxInclusive);
    public static Range<TValue, TTrait> FromExclusiveBounds(TValue? minExclusive, TValue? maxExclusive)
    {
        var minInclusiveOpt = minExclusive is {} minEx ? default(TTrait).Next(minEx) : NEG_INF;
        var maxInclusiveOpt = maxExclusive is {} maxEx ? default(TTrait).Previous(maxEx) : POS_INF;
        return (minInclusiveOpt, maxInclusiveOpt) switch
        {
            (null, _) => Empty, // no next value for minExclusive => empty
            (_, null) => Empty, // no previous value for maxExclusive => empty
            ({} minInclusive, {} maxInclusive) => new Range<TValue, TTrait>(minInclusive, maxInclusive),
        };
    }

    public static Range<TValue, TTrait> GreaterThanInclusive(TValue min) => new(min, POS_INF);
    public static Range<TValue, TTrait> LessThanInclusive(TValue max) => new(NEG_INF, max);
    public static Range<TValue, TTrait> GreaterThanExclusive(TValue min) => default(TTrait).Next(min) is {} minInclusive ? GreaterThanInclusive(minInclusive) : Empty;
    public static Range<TValue, TTrait> LessThanExclusive(TValue max) => default(TTrait).Previous(max) is {} maxInclusive ? LessThanInclusive(maxInclusive) : Empty;
    public static Range<TValue, TTrait> Point(TValue v) => new(v, v);

    public bool IsEmpty() => default(TTrait).Compare(MinInclusive, MaxInclusive) > 0;

    public Range<TValue, TTrait> Intersect(Range<TValue, TTrait> other) => new(default(TTrait).Max(MinInclusive, other.MinInclusive), default(TTrait).Min(MaxInclusive, other.MaxInclusive));

    // subtract single value v; may split range into up to two ranges
    public IEnumerable<Range<TValue, TTrait>> ExcludeValue(TValue v)
    {
        var vPrevOpt = default(TTrait).Previous(v);
        var vNextOpt = default(TTrait).Next(v);
        switch (vPrevOpt, vNextOpt)
        {
            case (null, null):
                // no previous or next: v is both min and max sentinel
                return Array.Empty<Range<TValue, TTrait>>();
            case (null, { } vNext):
                // no previous: v is min sentinel
                return new[] { new Range<TValue, TTrait>(default(TTrait).Max(vNext, MinInclusive), MaxInclusive) }.Where(r => !r.IsEmpty());
            case ({ } vPrev, null):
                // no next: v is max sentinel
                return new[] { new Range<TValue, TTrait>(MinInclusive, default(TTrait).Min(MaxInclusive, vPrev)) }.Where(r => !r.IsEmpty());
            case ({} vPrev, {} vNext):
                // normal value
                return new[]
                {
                    new Range<TValue, TTrait>(MinInclusive, default(TTrait).Min(MaxInclusive, vPrev)),
                    new Range<TValue, TTrait>(default(TTrait).Max(MinInclusive,  vNext), MaxInclusive),
                }.Where(r => !r.IsEmpty());
        }
    }

    // try union: if adjacent or overlapping, return union; otherwise null
    public Range<TValue, TTrait>? Union(Range<TValue, TTrait> other)
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
        return new Range<TValue, TTrait>(min, max);
    }

    public bool Equals(Range<TValue, TTrait> other) =>
        IsEmpty() && other.IsEmpty() || MinInclusive.Equals(other.MinInclusive) && MaxInclusive.Equals(other.MaxInclusive);

    public override bool Equals(object? obj) => obj is Range<TValue, TTrait> other && Equals(other);
    public override int GetHashCode() => IsEmpty() ? 0 : HashCode.Combine(MinInclusive, MaxInclusive);
    public static bool operator ==(Range<TValue, TTrait> left, Range<TValue, TTrait> right) => left.Equals(right);
    public static bool operator !=(Range<TValue, TTrait> left, Range<TValue, TTrait> right) => !left.Equals(right);

    public override string ToString() => IsEmpty() ? "Empty" : $"[{MinInclusive}, {MaxInclusive}]";
}

public interface IRangeSet<TSelf> : IEquatable<TSelf>
    where TSelf : IRangeSet<TSelf>
{
    [Pure]
    public TSelf Union(TSelf other);
    [Pure]
    public TSelf Intersect(TSelf other);
    [Pure]
    public TSelf Exclude(TSelf other);
    [Pure]
    public TSelf Complement();

    public bool IsEmpty();
}

public readonly struct RangeSet<TValue, TTrait> : IRangeSet<RangeSet<TValue, TTrait>>
    where TValue : struct, IEquatable<TValue>
    where TTrait : struct, IRangeTrait<TValue>
{
    // This list must not include empty ranges and must be sorted and non-overlapping.
    // In most case this Rages is small so we use ImmutableArray instead of ImmutableList which uses b-tree internally.
    private readonly ImmutableArray<Range<TValue, TTrait>> _ranges;

    private RangeSet(ImmutableArray<Range<TValue, TTrait>> ranges)
    {
        _ranges = ranges;
    }

    public static RangeSet<TValue, TTrait> Empty => default;
    public static RangeSet<TValue, TTrait> Entire => new(ImmutableArray.Create(Range<TValue, TTrait>.Entire));
    public static RangeSet<TValue, TTrait> FromRange(Range<TValue, TTrait> range) => range.IsEmpty() ? Empty : new(ImmutableArray.Create(range));

    public bool IsEmpty() => _ranges == null || _ranges.Length == 0;

    public RangeSet<TValue, TTrait> Intersect(Range<TValue, TTrait> other) => _ranges == null ? Empty 
        : new(_ranges.Select(r => r.Intersect(other)).Where(r => !r.IsEmpty()).ToImmutableArray());

    public RangeSet<TValue, TTrait> Intersect(RangeSet<TValue, TTrait> other)
    {
        if (_ranges == null) return Empty;
        if (other._ranges == null) return Empty;

        // since both are sorted non-overlapping ranges, we can do merge intersection
        return new(_ranges.SelectMany(r1 => IntersectRangeWithSet(r1, other)).ToImmutableArray());

        IEnumerable<Range<TValue, TTrait>> IntersectRangeWithSet(Range<TValue, TTrait> r1, RangeSet<TValue, TTrait> set2)
        {
            // In most case range only has a small number of ranges, so we do simple linear scan rather than binary search
            var index = 0;
            while (index < set2._ranges.Length && default(TTrait).Compare(set2._ranges[index].MaxInclusive, r1.MinInclusive) < 0)
                index++;

            for (; index < set2._ranges.Length; index++)
            {
                var r2 = set2._ranges[index];
                // stop if r2.Min > r1.Max
                if (default(TTrait).Compare(r2.MinInclusive, r1.MaxInclusive) > 0)
                    break;

                var intersected = r1.Intersect(r2);
                if (!intersected.IsEmpty())
                    yield return intersected;
            }
        }
    }

    public RangeSet<TValue, TTrait> ExcludeValue(TValue v) => _ranges == null ? this 
        : new(_ranges.SelectMany(r => r.ExcludeValue(v)).ToImmutableArray());

    public RangeSet<TValue, TTrait> Union(Range<TValue, TTrait> other) => Union(FromRange(other));
    public RangeSet<TValue, TTrait> Union(RangeSet<TValue, TTrait> other) => Union(this, other);

    public static RangeSet<TValue, TTrait> Union(params Range<TValue, TTrait>[] other) => Union((IEnumerable<Range<TValue, TTrait>>)other);
    public static RangeSet<TValue, TTrait> Union(params RangeSet<TValue, TTrait>[] other) => Union((IEnumerable<RangeSet<TValue, TTrait>>)other);


    // TODO? we may optimize selectmany by creating custom selectmany that merges two sorted lists to sorted list directly
    public static RangeSet<TValue, TTrait> Union(IEnumerable<RangeSet<TValue, TTrait>> others) => Union(others.SelectMany(r => r.Ranges));

    public static RangeSet<TValue, TTrait> Union(IEnumerable<Range<TValue, TTrait>> others)
    {
        ImmutableArray<Range<TValue, TTrait>>.Builder ranges = ImmutableArray.CreateBuilder<Range<TValue, TTrait>>();

        ranges.AddRange(others);
        ranges.Sort((r1, r2) => default(TTrait).Compare(r1.MinInclusive, r2.MinInclusive));

        // merge sibling / overlapping ranges
        Range<TValue, TTrait>? currentRange = null;
        int currentIndex = 0;
        for (var i = 0; i < ranges.Count; i++)
        {
            var nextRange = ranges[i];
            if (nextRange.IsEmpty()) continue;

            if (currentRange == null)
            {
                currentRange = nextRange;
            }
            else
            {
                var unioned = currentRange.Value.Union(nextRange);
                if (unioned != null)
                {
                    currentRange = unioned;
                }
                else
                {
                    ranges[currentIndex++] = currentRange.Value;
                    currentRange = nextRange;
                }
            }
        }
        if (currentRange != null)
            ranges[currentIndex++] = currentRange.Value;

        ranges.RemoveRange(currentIndex, ranges.Count - currentIndex);

        return new RangeSet<TValue, TTrait>(ranges.ToImmutable());
    }

    public RangeSet<TValue, TTrait> Exclude(RangeSet<TValue, TTrait> other) => Intersect(other.Complement());

    public RangeSet<TValue, TTrait> Complement()
    {
        if (IsEmpty()) return Entire;
        var inversedRanges = ImmutableArray.CreateBuilder<Range<TValue, TTrait>>(_ranges.Length + 1);
        // add range before first range
        var prevRange = _ranges[0];
        if (prevRange.MinExclusive is {} firstMinEx)
            inversedRanges.Add(Range<TValue, TTrait>.LessThanInclusive(firstMinEx));
        // add ranges between ranges
        for (int i = 1; i < _ranges.Length; i++)
        {
            var currentRange = _ranges[i];
            // since both bounds have at least one next / previous range, we can safely use ! operator
            var prevMaxEx = default(TTrait).Next(prevRange.MaxInclusive)!.Value;
            var currentMinEx = default(TTrait).Previous(currentRange.MinInclusive)!.Value;
            inversedRanges.Add(Range<TValue, TTrait>.FromInclusiveBounds(prevMaxEx, currentMinEx));
            prevRange = currentRange;
        }
        // add range after last range
        if (prevRange.MaxExclusive is {} lastMaxEx)
            inversedRanges.Add(Range<TValue, TTrait>.GreaterThanInclusive(lastMaxEx));
        return new RangeSet<TValue, TTrait>(inversedRanges.ToImmutable());
    }

    public IEnumerable<Range<TValue, TTrait>> Ranges => _ranges == null ? Enumerable.Empty<Range<TValue, TTrait>>() : _ranges;

    public override string ToString() => $"{{{string.Join(", ", Ranges)}}}";

    public bool Equals(RangeSet<TValue, TTrait> other)
    {
        if (IsEmpty() && other.IsEmpty()) return true;
        if (_ranges == null || other._ranges == null) return false;
        return _ranges.SequenceEqual(other._ranges);
    }
    public override bool Equals(object? obj) => obj is RangeSet<TValue, TTrait> other && Equals(other);
    public override int GetHashCode() => IsEmpty() ? 0 : _ranges!.Aggregate(0, (hash, range) => HashCode.Combine(hash, range.GetHashCode()));
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

// BoolSet is not a range or range set, but it's similar conceptually so we put it here.
public readonly struct BoolSet : IRangeSet<BoolSet>
{
    private readonly byte _bits;
    public bool HasFalse => (_bits & 1) != 0;
    public bool HasTrue => (_bits & 2) != 0;

    public BoolSet(bool hasFalse, bool hasTrue) => _bits = (byte)((hasFalse ? 1 : 0) | (hasTrue ? 2 : 0));
    private BoolSet(byte bits) => _bits = bits;

    public static BoolSet Empty => new(false, false);
    public static BoolSet Entire => new(true, true);

    public bool IsEmpty() => _bits == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BoolSet FromValue(bool b) => b ? new BoolSet(false, true) : new BoolSet(true, false);

    // set operations
    public BoolSet Union(BoolSet other) => new((byte)(_bits | other._bits));
    public BoolSet Intersect(BoolSet other) => new((byte)(_bits & other._bits));
    public BoolSet Exclude(BoolSet other) => new((byte)(_bits & ~other._bits));
    public BoolSet Complement() => new((byte)((~_bits) & 3));

    public static BoolSet Union(IEnumerable<BoolSet> sets)
    {
        var bits = 0;
        foreach (var set in sets)
            bits |= set._bits;
        return new BoolSet((byte)bits);
    }

    public override string ToString() => (HasTrue, HasFalse) switch
    {
        (false, false) => "{}",
        (true, false) => "{True}",
        (false, true) => "{False}",
        (true, true) => "{True, False}",
    };

    public bool Equals(BoolSet other) => _bits == other._bits;
    public override bool Equals(object? obj) => obj is BoolSet other && Equals(other);
    public override int GetHashCode() => _bits.GetHashCode();
}

#region SetTraits

public interface ISetTrait<TRangeSet> where TRangeSet : IRangeSet<TRangeSet>
{
    public TRangeSet Empty { get; }
    public TRangeSet SetFromConditions(AnimatorCondition[] conditions);
    public List<AnimatorCondition[]> ToConditions(TRangeSet rangeSet, string parameter);
    public TRangeSet Union(IEnumerable<TRangeSet> ranges);
    public FloatRangeSet ConvertToFloatRangeSet(TRangeSet rangeSet);
}

public struct BoolSetTrait : ISetTrait<BoolSet>
{
    public BoolSet Empty => BoolSet.Empty;
    public BoolSet SetFromConditions(AnimatorCondition[] conditions) => RangesUtil.BoolSetFromConditions(conditions);
    public List<AnimatorCondition[]> ToConditions(BoolSet rangeSet, string parameter) => rangeSet.ToConditions(parameter);
    public BoolSet Union(IEnumerable<BoolSet> ranges) => BoolSet.Union(ranges);
    public FloatRangeSet ConvertToFloatRangeSet(BoolSet rangeSet) => RangesUtil.BoolSetToFloatRangeSet(rangeSet);
}

public struct IntSetTrait : ISetTrait<IntRangeSet>
{
    public IntRangeSet Empty => IntRangeSet.Empty;
    public IntRangeSet SetFromConditions(AnimatorCondition[] conditions) => RangesUtil.IntRangeSetFromConditions(conditions);
    public List<AnimatorCondition[]> ToConditions(IntRangeSet rangeSet, string parameter) => rangeSet.ToConditions(parameter);
    public IntRangeSet Union(IEnumerable<IntRangeSet> ranges) => IntRangeSet.Union(ranges);
    public FloatRangeSet ConvertToFloatRangeSet(IntRangeSet rangeSet) => RangesUtil.IntRangeSetToFloatRangeSet(rangeSet);
}

public struct FloatSetTrait : ISetTrait<FloatRangeSet>
{
    public FloatRangeSet Empty => FloatRangeSet.Empty;
    public FloatRangeSet SetFromConditions(AnimatorCondition[] conditions) => RangesUtil.FloatRangeSetFromConditions(conditions);
    public List<AnimatorCondition[]> ToConditions(FloatRangeSet rangeSet, string parameter) => rangeSet.ToConditions(parameter);
    public FloatRangeSet Union(IEnumerable<FloatRangeSet> ranges) => FloatRangeSet.Union(ranges);
    public FloatRangeSet ConvertToFloatRangeSet(FloatRangeSet rangeSet) => rangeSet;
}

#endregion

public static class RangesUtil
{
    // convert to animator conditions for a given parameter name
    public static AnimatorCondition[] ToConditions(this IntRange range, string parameter)
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

    public static List<AnimatorCondition[]> ToConditions(this IntRangeSet rangeSet, string parameter)
    {
        // We simply can convert each range to conditions, but that may produce many conditions.
        // To get better result, we try to merge ranges with holes.
        // a < x < b || b + 1 < x < c => a < x < c with hole b since it's likely smaller number of conditions.
        // We allow up to two connected values as a holes
        // In other words, a < x < b || b + 2 < x < c will be a < x < c with hole b and b + 1, but
        // a < x < b || b + 3 < x < c will remain as is.
        var finalRanges = new List<(IntRange, List<int> holes)>();
        foreach (var range in rangeSet.Ranges)
        {
            if (finalRanges.Count == 0)
            {
                finalRanges.Add((range, new List<int>()));
                continue;
            }

            var (lastRange, holes) = finalRanges[^1];
            // check if we can merge current range into lastRange with holes
            if (range.MinInclusive - lastRange.MaxInclusive > 0 && range.MinInclusive - lastRange.MaxInclusive <= 3)
            {
                // can merge
                // add holes for the gap
                for (int v = lastRange.MaxInclusive + 1; v < range.MinInclusive; v++)
                {
                    holes.Add(v);
                }

                // update last range to cover current range
                finalRanges[^1] = (IntRange.FromInclusiveBounds(lastRange.MinInclusive, range.MaxInclusive), holes);
            }
            else
            {
                // cannot merge, just add
                finalRanges.Add((range, new List<int>()));
            }
        }

        return finalRanges.Select(tuple =>
        {
            var (range, holes) = tuple;
            // create range and add NotEquals for holes
            return range.ToConditions(parameter).Concat(holes.Select(h => NotEqualsCondition(parameter, h))).ToArray();
        }).ToList();
    }

    public static AnimatorCondition[] ToConditions(this FloatRange range, string parameter) => (Min: range.MinExclusive, Max: range.MaxExclusive) switch
    {
        (null, null) => Array.Empty<AnimatorCondition>(),
        ({ } min, null) => new[] { GreaterCondition(parameter, min) },
        (null, { } max) => new[] { LessCondition(parameter, max) },
        ({ } min, { } max) => new[] { GreaterCondition(parameter, min), LessCondition(parameter, max) },
    };
    
    public static List<AnimatorCondition[]> ToConditions(this FloatRangeSet rangeSet, string parameter) =>
        rangeSet.Ranges.Select(range => range.ToConditions(parameter)).ToList();

    public static List<AnimatorCondition[]> ToConditions(this BoolSet range, string parameter) => (range.HasTrue, range.HasFalse) switch
    {
        (false, false) => new List<AnimatorCondition[]>(),
        (true, true) => new List<AnimatorCondition[]> { Array.Empty<AnimatorCondition>() },
        (true, false) => new List<AnimatorCondition[]> { new[] { AnimatorCondition(parameter, AnimatorConditionMode.If) } },
        (false, true) => new List<AnimatorCondition[]> { new[] { AnimatorCondition(parameter, AnimatorConditionMode.IfNot) } },
    };

    public static FloatRangeSet IntRangeSetToFloatRangeSet(IntRangeSet intRangeSet)
    {
        return FloatRangeSet.Union(intRangeSet.Ranges.Select(iRange =>
        {
            var lowerBound = iRange.MinInclusive - 0.5f;
            var upperBound = iRange.MaxInclusive + 0.5f;
            var lowerBoundRounded = Mathf.RoundToInt(lowerBound);
            var upperBoundRounded = Mathf.RoundToInt(upperBound);

            if (lowerBoundRounded != iRange.MinInclusive)
                lowerBound = Utils.NextFloat(lowerBound);
            if (upperBoundRounded != iRange.MaxInclusive)
                upperBound = Utils.PreviousFloat(upperBound);

            Utils.Assert(Mathf.RoundToInt(lowerBound) == iRange.MinInclusive, "lower bound conversion failed");
            Utils.Assert(Mathf.RoundToInt(upperBound) == iRange.MaxInclusive, "upper bound conversion failed");

            return FloatRange.FromInclusiveBounds(lowerBound, upperBound);
        }));
    }

    public static FloatRangeSet BoolSetToFloatRangeSet(BoolSet boolSet) => (boolSet.HasTrue, boolSet.HasFalse) switch
    {
        (false, false) => FloatRangeSet.Empty,
        (true, true) => FloatRangeSet.Entire,
        (true, false) => FloatRangeSet.Entire.ExcludeValue(0),
        (false, true) => FloatRangeSet.FromRange(FloatRange.Point(0)),
    };

    public static IntRangeSet IntRangeSetFromConditions(IEnumerable<AnimatorCondition> conditions) => conditions
        .Aggregate(IntRangeSet.Entire, (current, c) => c.mode switch
        {
            AnimatorConditionMode.Equals => current.Intersect(IntRange.Point(Mathf.FloorToInt(c.threshold))),
            AnimatorConditionMode.NotEqual => current.ExcludeValue(Mathf.FloorToInt(c.threshold)),
            AnimatorConditionMode.Greater => current.Intersect(IntRange.GreaterThanExclusive(Mathf.FloorToInt(c.threshold))),
            AnimatorConditionMode.Less => current.Intersect(IntRange.LessThanExclusive(Mathf.FloorToInt(c.threshold))),
            _ => throw new ArgumentOutOfRangeException(),
        });

    public static FloatRangeSet FloatRangeSetFromConditions(IEnumerable<AnimatorCondition> conditions) => conditions
        .Aggregate(FloatRangeSet.Entire, (current, c) => c.mode switch
        {
            AnimatorConditionMode.Greater => current.Intersect(FloatRange.GreaterThanExclusive(c.threshold)),
            AnimatorConditionMode.Less => current.Intersect(FloatRange.LessThanExclusive(c.threshold)),
            _ => throw new ArgumentOutOfRangeException(),
        });

    public static BoolSet BoolSetFromConditions(IEnumerable<AnimatorCondition> conditions) => conditions
        .Aggregate(BoolSet.Entire, (current, c) => c.mode switch
        {
            AnimatorConditionMode.If => current.Intersect(BoolSet.FromValue(true)),
            AnimatorConditionMode.IfNot => current.Intersect(BoolSet.FromValue(false)),
            _ => throw new ArgumentOutOfRangeException(),
        });

    // utilities
    static AnimatorCondition AnimatorCondition(string parameter, AnimatorConditionMode mode, float threshold = 0) =>
        new()
        {
            parameter = parameter,
            mode = mode,
            threshold = threshold,
        };

    private static AnimatorCondition GreaterCondition(string parameter, float threshold) =>
        AnimatorCondition(parameter, AnimatorConditionMode.Greater, threshold);

    private static AnimatorCondition LessCondition(string parameter, float threshold) =>
        AnimatorCondition(parameter, AnimatorConditionMode.Less, threshold);

    private static AnimatorCondition EqualsCondition(string parameter, float threshold) =>
        AnimatorCondition(parameter, AnimatorConditionMode.Equals, threshold);

    private static AnimatorCondition NotEqualsCondition(string parameter, float threshold) =>
        AnimatorCondition(parameter, AnimatorConditionMode.NotEqual, threshold);
}
