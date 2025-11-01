namespace Anatawa12.AvatarOptimizer.Test.AnimatorOptimizer.CompleteGraphToEntryExits
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using IntClosedRange = Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer.CompleteGraphToEntryExit.IntClosedRange;

    [TestFixture]
    public class IntClosedRangeTest
    {
        // Intersect tests
        private static IEnumerable<TestCaseData> IntersectCases()
        {
            yield return new TestCaseData(new IntClosedRange(1, 5), new IntClosedRange(3, 7), new IntClosedRange(3, 5));
            yield return new TestCaseData(new IntClosedRange(1, 2), new IntClosedRange(3, 4), IntClosedRange.Empty);
            yield return new TestCaseData(new IntClosedRange(1, 3), new IntClosedRange(3, 5), new IntClosedRange(3, 3));
            yield return new TestCaseData(IntClosedRange.Entire, new IntClosedRange(4, 10), new IntClosedRange(4, 10));
            yield return new TestCaseData(IntClosedRange.FromMin(3), IntClosedRange.FromMax(5), new IntClosedRange(3, 5));
            yield return new TestCaseData(IntClosedRange.Point(int.MinValue), IntClosedRange.FromMax(int.MinValue), IntClosedRange.Point(int.MinValue));
            // extremes non-overlapping
            yield return new TestCaseData(new IntClosedRange(int.MinValue, -1), new IntClosedRange(0, int.MaxValue), IntClosedRange.Empty);
            // full overlap
            yield return new TestCaseData(new IntClosedRange(int.MinValue, int.MaxValue), IntClosedRange.Point(int.MaxValue), IntClosedRange.Point(int.MaxValue));
        }

        [TestCaseSource(nameof(IntersectCases))]
        public void Intersect_ReturnsExpected(IntClosedRange a, IntClosedRange b, IntClosedRange expected)
        {
            var actual = a.Intersect(b);
            Assert.That(actual, Is.EqualTo(expected));
            // symmetry
            Assert.That(b.Intersect(a), Is.EqualTo(expected));
        }

        // ExcludeValue tests
        private static IEnumerable<TestCaseData> ExcludeValueCases()
        {
            yield return new TestCaseData(new IntClosedRange(1, 5), 3, new[] { new IntClosedRange(1, 2), new IntClosedRange(4, 5) });
            yield return new TestCaseData(new IntClosedRange(1, 5), 1, new[] { new IntClosedRange(2, 5) });
            yield return new TestCaseData(new IntClosedRange(1, 5), 5, new[] { new IntClosedRange(1, 4) });
            yield return new TestCaseData(new IntClosedRange(3, 3), 3, new IntClosedRange[] { });
            yield return new TestCaseData(new IntClosedRange(1, 5), 7, new[] { new IntClosedRange(1, 5) }); // outside value -> unchanged
            yield return new TestCaseData(IntClosedRange.FromMin(0), 0, new[] { IntClosedRange.FromMin(1) }); // infinite top
            yield return new TestCaseData(IntClosedRange.FromMax(0), 0, new[] { IntClosedRange.FromMax(-1) }); // infinite bottom
            // exclude at extreme values
            yield return new TestCaseData(new IntClosedRange(int.MinValue, int.MaxValue), int.MinValue, new[] { new IntClosedRange(int.MinValue + 1, int.MaxValue) });
            yield return new TestCaseData(new IntClosedRange(int.MinValue, int.MaxValue), int.MaxValue, new[] { new IntClosedRange(int.MinValue, int.MaxValue - 1) });
        }

        [TestCaseSource(nameof(ExcludeValueCases))]
        public void ExcludeValue_ReturnsExpected(IntClosedRange range, int value, IntClosedRange[] expected)
        {
            var actual = range.ExcludeValue(value).ToArray();
            Assert.That(actual, Is.EqualTo(expected));
        }

        // Union tests: returns union when overlapping or adjacent, otherwise null
        private static IEnumerable<TestCaseData> UnionCases()
        {
            yield return new TestCaseData(new IntClosedRange(1, 4), new IntClosedRange(3, 6), (IntClosedRange?)new IntClosedRange(1, 6));
            yield return new TestCaseData(new IntClosedRange(1, 2), new IntClosedRange(3, 5), (IntClosedRange?)new IntClosedRange(1, 5)); // adjacent
            yield return new TestCaseData(new IntClosedRange(3, 5), new IntClosedRange(1, 2), (IntClosedRange?)new IntClosedRange(1, 5)); // reversed adjacent
            yield return new TestCaseData(new IntClosedRange(1, 2), new IntClosedRange(4, 5), (IntClosedRange?)null); // disjoint
            yield return new TestCaseData(new IntClosedRange(int.MinValue, -1), new IntClosedRange(0, int.MaxValue), (IntClosedRange?)new IntClosedRange(int.MinValue, int.MaxValue)); // adjacent to form entire
            yield return new TestCaseData(IntClosedRange.Entire, new IntClosedRange(10, 20), (IntClosedRange?)IntClosedRange.Entire); // entire absorbs
        }

        [TestCaseSource(nameof(UnionCases))]
        public void Union_MaybeReturnsUnionOrNull(IntClosedRange a, IntClosedRange b, IntClosedRange? expected)
        {
            var actual = a.Union(b);
            Assert.That(actual, Is.EqualTo(expected));
            // symmetry if expected != null
            var actual2 = b.Union(a);
            Assert.That(actual2, Is.EqualTo(expected));
        }

        // Equals tests: test Equals(IntClosedRange) specifically (do not use Is.EqualTo)
        private static IEnumerable<TestCaseData> EqualsCases()
        {
            // existing canonical cases
            yield return new TestCaseData(new IntClosedRange(1, 5), new IntClosedRange(1, 5), true);
            yield return new TestCaseData(new IntClosedRange(1, 5), new IntClosedRange(1, 4), false);
            yield return new TestCaseData(IntClosedRange.Empty, IntClosedRange.Empty, true);
            yield return new TestCaseData(IntClosedRange.FromMin(0), IntClosedRange.FromMin(0), true);
            yield return new TestCaseData(IntClosedRange.FromMax(0), IntClosedRange.FromMax(1), false);
            yield return new TestCaseData(new IntClosedRange(int.MinValue, int.MaxValue), IntClosedRange.Entire, true); // if Entire maps to full bounds

            // many different internal empty representations should be equal to Empty and to each other
            yield return new TestCaseData(new IntClosedRange(1, 0), IntClosedRange.Empty, true);
            yield return new TestCaseData(new IntClosedRange(5, 4), IntClosedRange.Empty, true);
            yield return new TestCaseData(new IntClosedRange(0, -1), IntClosedRange.Empty, true);
            yield return new TestCaseData(new IntClosedRange(int.MaxValue, int.MinValue), IntClosedRange.Empty, true);
            yield return new TestCaseData(new IntClosedRange(int.MinValue + 1, int.MinValue), IntClosedRange.Empty, true);

            // empties compared against other empty representations
            yield return new TestCaseData(new IntClosedRange(1, 0), new IntClosedRange(5, 4), true);
            yield return new TestCaseData(new IntClosedRange(0, -1), new IntClosedRange(int.MaxValue, int.MinValue), true);
            yield return new TestCaseData(new IntClosedRange(int.MinValue + 1, int.MinValue), new IntClosedRange(1, 0), true);

            // ensure empty != non-empty
            yield return new TestCaseData(new IntClosedRange(1, 0), new IntClosedRange(0, 0), false);
            yield return new TestCaseData(new IntClosedRange(5, 4), new IntClosedRange(4, 5), false);
        }

        [TestCaseSource(nameof(EqualsCases))]
        public void Equals_IntClosedRange_Works(IntClosedRange a, IntClosedRange b, bool expected)
        {
            Assert.That(a.Equals(b), Is.EqualTo(expected));
        }

        // GetHashCode tests: equal ranges produce equal hash codes
        private static IEnumerable<TestCaseData> HashCodeCases()
        {
            // existing canonical pairs
            yield return new TestCaseData(new IntClosedRange(1, 5), new IntClosedRange(1, 5));
            yield return new TestCaseData(IntClosedRange.Empty, IntClosedRange.Empty);
            yield return new TestCaseData(IntClosedRange.FromMin(0), IntClosedRange.FromMin(0));
            yield return new TestCaseData(new IntClosedRange(int.MinValue, int.MaxValue), IntClosedRange.Entire);

            // multiple empty internal representations should hash-equal the canonical Empty
            yield return new TestCaseData(IntClosedRange.Empty, new IntClosedRange(1, 0));
            yield return new TestCaseData(IntClosedRange.Empty, new IntClosedRange(5, 4));
            yield return new TestCaseData(IntClosedRange.Empty, new IntClosedRange(0, -1));
            yield return new TestCaseData(IntClosedRange.Empty, new IntClosedRange(int.MaxValue, int.MinValue));
            yield return new TestCaseData(IntClosedRange.Empty, new IntClosedRange(int.MinValue + 1, int.MinValue));
        }

        [TestCaseSource(nameof(HashCodeCases))]
        public void GetHashCode_EqualRanges_HaveSameHashCode(IntClosedRange a, IntClosedRange b)
        {
            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }
    }
}