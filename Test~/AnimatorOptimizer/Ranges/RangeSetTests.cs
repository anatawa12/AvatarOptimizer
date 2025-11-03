using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer;
using NUnit.Framework;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorOptimizer
{
    using IntRange = Range<int, RangeIntTrait>;
    using IntRangeSet = RangeSet<int, RangeIntTrait>;

    [TestFixture]
    public class RangeSetTests
    {
        // ---------------- Constructors / ToString / IsEmpty ----------------

        private static IEnumerable<TestCaseData> Constructors_Source()
        {
            yield return new TestCaseData(
                IntRangeSet.Empty,
                true,
                new IntRange[0]
            ).SetName("Constructors_Empty");

            yield return new TestCaseData(
                IntRangeSet.FromRange(IntRange.Empty),
                true,
                new IntRange[0]
            ).SetName("Constructors_FromEmptyRange");

            yield return new TestCaseData(
                IntRangeSet.Entire,
                false,
                new[]
                {
                    IntRange.FromInclusiveBounds(default(RangeIntTrait).MinValue, default(RangeIntTrait).MaxValue)
                }
            ).SetName("Constructors_Entire");
        }

        [Test, TestCaseSource(nameof(Constructors_Source))]
        public void Constructors_All(IntRangeSet set, bool expectedIsEmpty, IntRange[] expectedRanges)
        {
            Assert.That(set.IsEmpty(), Is.EqualTo(expectedIsEmpty));
            Assert.That(set.Ranges.ToList(), Is.EqualTo(expectedRanges));
        }

        // ---------------- Union (range) ----------------

        private static IEnumerable<TestCaseData> Union_WithRangeSource()
        {
            yield return new TestCaseData(
                IntRangeSet.Empty.Union(IntRange.FromInclusiveBounds(1, 3)),
                IntRange.FromInclusiveBounds(4, 6),
                new[] { IntRange.FromInclusiveBounds(1, 6) }
            ).SetName("Union_Adjacent_Merge");

            yield return new TestCaseData(
                IntRangeSet.Empty.Union(IntRange.FromInclusiveBounds(1, 2)),
                IntRange.FromInclusiveBounds(4, 5),
                new[]
                {
                    IntRange.FromInclusiveBounds(1, 2),
                    IntRange.FromInclusiveBounds(4, 5)
                }
            ).SetName("Union_Disjoint_Remain");

            yield return new TestCaseData(
                IntRangeSet.Empty,
                IntRange.FromInclusiveBounds(5, 10),
                new[] { IntRange.FromInclusiveBounds(5, 10) }
            ).SetName("Union_WithRange_OnEmpty");
        }

        [Test, TestCaseSource(nameof(Union_WithRangeSource))]
        public void Union_WithRange(IntRangeSet set, IntRange range, IntRange[] expectedRanges)
        {
            var res = set.Union(range);
            Assert.That(res.Ranges.ToList(), Is.EqualTo(expectedRanges));
        }

        // ---------------- Union (set equality / hash) ----------------

        private static IEnumerable<TestCaseData> Union_SetEqualitySource()
        {
            yield return new TestCaseData(
                IntRangeSet.Empty
                    .Union(IntRange.FromInclusiveBounds(1, 2))
                    .Union(IntRange.FromInclusiveBounds(8, 9)),
                IntRangeSet.Empty
                    .Union(IntRange.FromInclusiveBounds(8, 9))
                    .Union(IntRange.FromInclusiveBounds(1, 2))
            );
            yield return new TestCaseData(
                IntRangeSet.Empty
                    .Union(IntRange.FromInclusiveBounds(1, 2))
                    .Union(IntRange.FromInclusiveBounds(2, 9)),
                IntRangeSet.Empty
                    .Union(IntRange.FromInclusiveBounds(1, 4))
                    .Union(IntRange.FromInclusiveBounds(3, 9))
            );
        }

        [Test, TestCaseSource(nameof(Union_SetEqualitySource))]
        public void Union_SetEquality(IntRangeSet a, IntRangeSet b)
        {
            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        // ---------------- Intersect ----------------

        private static IEnumerable<TestCaseData> IntersectSource()
        {
            yield return new TestCaseData(
                IntRangeSet.Empty
                    .Union(IntRange.FromInclusiveBounds(1, 3))
                    .Union(IntRange.FromInclusiveBounds(5, 7)),
                IntRange.FromInclusiveBounds(2, 6),
                new[]
                {
                    IntRange.FromInclusiveBounds(2, 3),
                    IntRange.FromInclusiveBounds(5, 6)
                }
            );

            yield return new TestCaseData(
                IntRangeSet.Empty.Union(IntRange.FromInclusiveBounds(1, 10)),
                IntRange.Empty,
                new IntRange[0]
            );
        }

        [Test, TestCaseSource(nameof(IntersectSource))]
        public void Intersect(IntRangeSet set, IntRange range, IntRange[] expectedRanges)
        {
            var res = set.Intersect(range);
            Assert.That(res.Ranges.ToList(), Is.EqualTo(expectedRanges));
        }

        // ---------------- ExcludeValue ----------------

        private static IEnumerable<TestCaseData> ExcludeValueSource()
        {
            yield return new TestCaseData(
                IntRangeSet.Empty.Union(IntRange.FromInclusiveBounds(1, 5)),
                3,
                new[]
                {
                    IntRange.FromInclusiveBounds(1, 2),
                    IntRange.FromInclusiveBounds(4, 5)
                }
            );

            yield return new TestCaseData(
                IntRangeSet.Empty.Union(IntRange.Point(10)),
                10,
                new IntRange[0]
            );

            yield return new TestCaseData(
                IntRangeSet.Empty,
                42,
                new IntRange[0]
            );
        }

        [Test, TestCaseSource(nameof(ExcludeValueSource))]
        public void ExcludeValue(IntRangeSet set, int value, IntRange[] expectedRanges)
        {
            var res = set.ExcludeValue(value);
            Assert.That(res.Ranges.ToList(), Is.EqualTo(expectedRanges));
        }

        // ---------------- Ranges property ordering / merging ----------------

        private static IEnumerable<TestCaseData> RangesOrderingSource()
        {
            yield return new TestCaseData(
                new []
                {
                    IntRange.FromInclusiveBounds(5, 6),
                    IntRange.FromInclusiveBounds(1, 2),
                    IntRange.FromInclusiveBounds(3, 3),
                },
                new[]
                {
                    IntRange.FromInclusiveBounds(1, 3),
                    IntRange.FromInclusiveBounds(5, 6),
                }
            );
        }

        [Test, TestCaseSource(nameof(RangesOrderingSource))]
        public void Ranges_Property_OrderingAndMerging(IntRange[] sourceSets, IntRange[] expectedOrderedRanges)
        {
            IntRangeSet set = IntRangeSet.Empty;
            foreach (var r in sourceSets) set = set.Union(r);
            Assert.That(set.Ranges.ToList(), Is.EqualTo(expectedOrderedRanges));
        }

        // ---------------- Equals / GetHashCode ----------------

        private static IEnumerable<TestCaseData> EqualsHashSource()
        {
            yield return new TestCaseData(
                IntRangeSet.Empty,
                IntRangeSet.Empty,
                true,
                true
            ).SetName("Equals_Empty");

            yield return new TestCaseData(
                IntRangeSet.FromRange(IntRange.FromInclusiveBounds(1, 2)),
                IntRangeSet.FromRange(IntRange.FromInclusiveBounds(1, 2)),
                true,
                true
            ).SetName("Equals_ContentsEqual");

            yield return new TestCaseData(
                IntRangeSet.FromRange(IntRange.FromInclusiveBounds(1, 2)),
                IntRangeSet.FromRange(IntRange.FromInclusiveBounds(1, 3)),
                false,
                false
            ).SetName("Equals_Different");
        }

        [Test, TestCaseSource(nameof(EqualsHashSource))]
        public void EqualsAndGetHashCode(IntRangeSet a, IntRangeSet b, bool expectedEquals, bool expectHashEqual)
        {
            Assert.That(a.Equals(b), Is.EqualTo(expectedEquals));
            if (expectHashEqual)
                Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            else
                Assert.That(a.GetHashCode(), Is.Not.EqualTo(b.GetHashCode()));
        }

        // ---------------- Union (multiple ranges) ----------------
        // Replace tests that union multiple individual ranges with tests that
        // merge two RangeSet instances (each may contain multiple ranges).

        private static IEnumerable<TestCaseData> Union_TwoRangeSetsSource()
        {
            // Interleaving: left has 1 and 5, right has 2 and 3 -> 1..3 and 5 remains
            yield return new TestCaseData(
                IntRangeSet.Empty
                    .Union(IntRange.FromInclusiveBounds(1, 1))
                    .Union(IntRange.FromInclusiveBounds(5, 5)),
                IntRangeSet.Empty
                    .Union(IntRange.FromInclusiveBounds(2, 2))
                    .Union(IntRange.FromInclusiveBounds(3, 3)),
                new[]
                {
                    IntRange.FromInclusiveBounds(1, 3),
                    IntRange.FromInclusiveBounds(5, 5)
                }
            ).SetName("Union_TwoRangeSets_Interleaving");

            // Bulk merging: overlapping/adjacent ranges across both sets merge into one
            yield return new TestCaseData(
                IntRangeSet.Empty
                    .Union(IntRange.FromInclusiveBounds(1, 2))
                    .Union(IntRange.FromInclusiveBounds(4, 5)),
                IntRangeSet.Empty
                    .Union(IntRange.FromInclusiveBounds(2, 3))
                    .Union(IntRange.FromInclusiveBounds(5, 6)),
                new[]
                {
                    IntRange.FromInclusiveBounds(1, 6)
                }
            ).SetName("Union_TwoRangeSets_BulkMerge");

            // Mixed: disjoint and merging parts produce mixed result
            yield return new TestCaseData(
                IntRangeSet.Empty
                    .Union(IntRange.FromInclusiveBounds(1, 2))
                    .Union(IntRange.FromInclusiveBounds(10, 11)),
                IntRangeSet.Empty
                    .Union(IntRange.FromInclusiveBounds(4, 6))
                    .Union(IntRange.FromInclusiveBounds(7, 9)),
                new[]
                {
                    IntRange.FromInclusiveBounds(1, 2),
                    IntRange.FromInclusiveBounds(4, 11)
                }
            ).SetName("Union_TwoRangeSets_Mixed");

            // With existing set: left contains a separate block that should remain
            yield return new TestCaseData(
                IntRangeSet.Empty
                    .Union(IntRange.FromInclusiveBounds(20, 22))
                    .Union(IntRange.FromInclusiveBounds(30, 30)),
                IntRangeSet.Empty
                    .Union(IntRange.FromInclusiveBounds(1, 3))
                    .Union(IntRange.FromInclusiveBounds(4, 7))
                    .Union(IntRange.FromInclusiveBounds(5, 5)),
                new[]
                {
                    IntRange.FromInclusiveBounds(1, 7),
                    IntRange.FromInclusiveBounds(20, 22),
                    IntRange.FromInclusiveBounds(30, 30)
                }
            ).SetName("Union_TwoRangeSets_WithExisting");

            // ---------------- Additional edge cases ----------------

            // Both empty -> empty
            yield return new TestCaseData(
                IntRangeSet.Empty,
                IntRangeSet.Empty,
                new IntRange[0]
            ).SetName("Union_TwoRangeSets_BothEmpty");

            // Left empty -> result equals right
            yield return new TestCaseData(
                IntRangeSet.Empty,
                IntRangeSet.Empty
                    .Union(IntRange.FromInclusiveBounds(1, 3))
                    .Union(IntRange.FromInclusiveBounds(5, 5)),
                new[]
                {
                    IntRange.FromInclusiveBounds(1, 3),
                    IntRange.FromInclusiveBounds(5, 5)
                }
            ).SetName("Union_TwoRangeSets_LeftEmpty");

            // Right empty -> result equals left
            yield return new TestCaseData(
                IntRangeSet.Empty
                    .Union(IntRange.FromInclusiveBounds(2, 4)),
                IntRangeSet.Empty,
                new[]
                {
                    IntRange.FromInclusiveBounds(2, 4)
                }
            ).SetName("Union_TwoRangeSets_RightEmpty");

            // Contained: right is subset of left -> left unchanged
            yield return new TestCaseData(
                IntRangeSet.Empty
                    .Union(IntRange.FromInclusiveBounds(1, 10)),
                IntRangeSet.Empty
                    .Union(IntRange.FromInclusiveBounds(3, 5)),
                new[]
                {
                    IntRange.FromInclusiveBounds(1, 10)
                }
            ).SetName("Union_TwoRangeSets_Contained");

            // Subset bridging two left blocks -> should merge into single larger block
            yield return new TestCaseData(
                IntRangeSet.Empty
                    .Union(IntRange.FromInclusiveBounds(1, 2))
                    .Union(IntRange.FromInclusiveBounds(8, 9)),
                IntRangeSet.Empty
                    .Union(IntRange.FromInclusiveBounds(2, 8)),
                new[]
                {
                    IntRange.FromInclusiveBounds(1, 9)
                }
            ).SetName("Union_TwoRangeSets_BridgeMerge");

            // Touching trait min/max from both sides -> Entire
            yield return new TestCaseData(
                IntRangeSet.Empty
                    .Union(IntRange.FromInclusiveBounds(default(RangeIntTrait).MinValue, 0)),
                IntRangeSet.Empty
                    .Union(IntRange.FromInclusiveBounds(1, default(RangeIntTrait).MaxValue)),
                new[]
                {
                    IntRange.FromInclusiveBounds(default(RangeIntTrait).MinValue, default(RangeIntTrait).MaxValue)
                }
            ).SetName("Union_TwoRangeSets_TouchMinMax_ToEntire");

            // Involvement of Entire -> Entire
            yield return new TestCaseData(
                IntRangeSet.Entire,
                IntRangeSet.FromRange(IntRange.FromInclusiveBounds(1, 2)),
                new[]
                {
                    IntRange.FromInclusiveBounds(default(RangeIntTrait).MinValue, default(RangeIntTrait).MaxValue)
                }
            ).SetName("Union_TwoRangeSets_LeftEntire");

            yield return new TestCaseData(
                IntRangeSet.FromRange(IntRange.FromInclusiveBounds(1, 2)),
                IntRangeSet.Entire,
                new[]
                {
                    IntRange.FromInclusiveBounds(default(RangeIntTrait).MinValue, default(RangeIntTrait).MaxValue)
                }
            ).SetName("Union_TwoRangeSets_RightEntire");

            // Duplicate ranges in inputs -> idempotent merge
            yield return new TestCaseData(
                IntRangeSet.Empty
                    .Union(IntRange.FromInclusiveBounds(1, 3))
                    .Union(IntRange.FromInclusiveBounds(1, 3)),
                IntRangeSet.Empty
                    .Union(IntRange.FromInclusiveBounds(1, 3)),
                new[]
                {
                    IntRange.FromInclusiveBounds(1, 3)
                }
            ).SetName("Union_TwoRangeSets_Duplicates_Idempotent");

            // Disjoint multiple ranges remain disjoint and ordered
            yield return new TestCaseData(
                IntRangeSet.Empty
                    .Union(IntRange.FromInclusiveBounds(10, 12))
                    .Union(IntRange.FromInclusiveBounds(1, 1)),
                IntRangeSet.Empty
                    .Union(IntRange.FromInclusiveBounds(5, 5))
                    .Union(IntRange.FromInclusiveBounds(3, 4)),
                new[]
                {
                    IntRange.FromInclusiveBounds(1, 1),
                    IntRange.FromInclusiveBounds(3, 5),
                    IntRange.FromInclusiveBounds(10, 12)
                }
            ).SetName("Union_TwoRangeSets_DisjointOrderAndMerge");
        }

        [Test, TestCaseSource(nameof(Union_TwoRangeSetsSource))]
        public void Union_TwoRangeSets(IntRangeSet left, IntRangeSet right, IntRange[] expectedRanges)
        {
            Assert.That(left.Union(right).Ranges.ToList(), Is.EqualTo(expectedRanges));
        }
    }
}