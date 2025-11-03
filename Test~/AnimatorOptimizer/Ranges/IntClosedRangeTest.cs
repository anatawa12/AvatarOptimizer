using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer;
using NUnit.Framework;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorOptimizer
{
    using IntRange = Range<int, RangeIntTrait>;

    [TestFixture]
    public class IntClosedRangeTest
    {
        // Intersect tests
        private static IEnumerable<TestCaseData> IntersectCases()
        {
            yield return new TestCaseData(IntRange.FromInclusiveBounds(1, 5), IntRange.FromInclusiveBounds(3, 7), IntRange.FromInclusiveBounds(3, 5));
            yield return new TestCaseData(IntRange.FromInclusiveBounds(1, 2), IntRange.FromInclusiveBounds(3, 4), IntRange.Empty);
            yield return new TestCaseData(IntRange.FromInclusiveBounds(1, 3), IntRange.FromInclusiveBounds(3, 5), IntRange.FromInclusiveBounds(3, 3));
            yield return new TestCaseData(IntRange.Entire, IntRange.FromInclusiveBounds(4, 10), IntRange.FromInclusiveBounds(4, 10));
            yield return new TestCaseData(IntRange.GreaterThanInclusive(3), IntRange.LessThanInclusive(5),
                IntRange.FromInclusiveBounds(3, 5));
            yield return new TestCaseData(IntRange.Point(int.MinValue), IntRange.LessThanInclusive(int.MinValue),
                IntRange.Point(int.MinValue));
            // extremes non-overlapping
            yield return new TestCaseData(IntRange.FromInclusiveBounds(int.MinValue, -1), IntRange.FromInclusiveBounds(0, int.MaxValue),
                IntRange.Empty);
            // full overlap
            yield return new TestCaseData(IntRange.FromInclusiveBounds(int.MinValue, int.MaxValue),
                IntRange.Point(int.MaxValue), IntRange.Point(int.MaxValue));
        }

        [TestCaseSource(nameof(IntersectCases))]
        public void Intersect_ReturnsExpected(IntRange a, IntRange b, IntRange expected)
        {
            var actual = a.Intersect(b);
            Assert.That(actual, Is.EqualTo(expected));
            // symmetry
            Assert.That(b.Intersect(a), Is.EqualTo(expected));
        }

        // ExcludeValue tests
        private static IEnumerable<TestCaseData> ExcludeValueCases()
        {
            yield return new TestCaseData(IntRange.FromInclusiveBounds(1, 5), 3,
                new[] { IntRange.FromInclusiveBounds(1, 2), IntRange.FromInclusiveBounds(4, 5) });
            yield return new TestCaseData(IntRange.FromInclusiveBounds(1, 5), 1, new[] { IntRange.FromInclusiveBounds(2, 5) });
            yield return new TestCaseData(IntRange.FromInclusiveBounds(1, 5), 5, new[] { IntRange.FromInclusiveBounds(1, 4) });
            yield return new TestCaseData(IntRange.FromInclusiveBounds(3, 3), 3, new IntRange[] { });
            yield return
                new TestCaseData(IntRange.FromInclusiveBounds(1, 5), 7,
                    new[] { IntRange.FromInclusiveBounds(1, 5) }); // outside value -> unchanged
            yield return
                new TestCaseData(IntRange.GreaterThanInclusive(0), 0, new[] { IntRange.GreaterThanInclusive(1) }); // infinite top
            yield return
                new TestCaseData(IntRange.LessThanInclusive(0), 0, new[] { IntRange.LessThanInclusive(-1) }); // infinite bottom
            // exclude at extreme values
            yield return new TestCaseData(IntRange.FromInclusiveBounds(int.MinValue, int.MaxValue), int.MinValue,
                new[] { IntRange.FromInclusiveBounds(int.MinValue + 1, int.MaxValue) });
            yield return new TestCaseData(IntRange.FromInclusiveBounds(int.MinValue, int.MaxValue), int.MaxValue,
                new[] { IntRange.FromInclusiveBounds(int.MinValue, int.MaxValue - 1) });
        }

        [TestCaseSource(nameof(ExcludeValueCases))]
        public void ExcludeValue_ReturnsExpected(IntRange range, int value, IntRange[] expected)
        {
            var actual = range.ExcludeValue(value).ToArray();
            Assert.That(actual, Is.EqualTo(expected));
        }

        // Union tests: returns union when overlapping or adjacent, otherwise null
        private static IEnumerable<TestCaseData> UnionCases()
        {
            yield return new TestCaseData(IntRange.FromInclusiveBounds(1, 4), IntRange.FromInclusiveBounds(3, 6),
                (IntRange?)IntRange.FromInclusiveBounds(1, 6));
            yield return new TestCaseData(IntRange.FromInclusiveBounds(1, 2), IntRange.FromInclusiveBounds(3, 5),
                (IntRange?)IntRange.FromInclusiveBounds(1, 5)); // adjacent
            yield return new TestCaseData(IntRange.FromInclusiveBounds(3, 5), IntRange.FromInclusiveBounds(1, 2),
                (IntRange?)IntRange.FromInclusiveBounds(1, 5)); // reversed adjacent
            yield return new TestCaseData(IntRange.FromInclusiveBounds(1, 2), IntRange.FromInclusiveBounds(4, 5),
                (IntRange?)null); // disjoint
            yield return new TestCaseData(IntRange.FromInclusiveBounds(int.MinValue, -1), IntRange.FromInclusiveBounds(0, int.MaxValue),
                (IntRange?)IntRange.FromInclusiveBounds(int.MinValue, int.MaxValue)); // adjacent to form entire
            yield return new TestCaseData(IntRange.Entire, IntRange.FromInclusiveBounds(10, 20),
                (IntRange?)IntRange.Entire); // entire absorbs
        }

        [TestCaseSource(nameof(UnionCases))]
        public void Union_MaybeReturnsUnionOrNull(IntRange a, IntRange b, IntRange? expected)
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
            yield return new TestCaseData(IntRange.FromInclusiveBounds(1, 5), IntRange.FromInclusiveBounds(1, 5), true);
            yield return new TestCaseData(IntRange.FromInclusiveBounds(1, 5), IntRange.FromInclusiveBounds(1, 4), false);
            yield return new TestCaseData(IntRange.Empty, IntRange.Empty, true);
            yield return new TestCaseData(IntRange.GreaterThanInclusive(0), IntRange.GreaterThanInclusive(0), true);
            yield return new TestCaseData(IntRange.LessThanInclusive(0), IntRange.LessThanInclusive(1), false);
            yield return
                new TestCaseData(IntRange.FromInclusiveBounds(int.MinValue, int.MaxValue), IntRange.Entire,
                    true); // if Entire maps to full bounds

            // many different internal empty representations should be equal to Empty and to each other
            yield return new TestCaseData(IntRange.FromInclusiveBounds(1, 0), IntRange.Empty, true);
            yield return new TestCaseData(IntRange.FromInclusiveBounds(5, 4), IntRange.Empty, true);
            yield return new TestCaseData(IntRange.FromInclusiveBounds(0, -1), IntRange.Empty, true);
            yield return new TestCaseData(IntRange.FromInclusiveBounds(int.MaxValue, int.MinValue), IntRange.Empty, true);
            yield return new TestCaseData(IntRange.FromInclusiveBounds(int.MinValue + 1, int.MinValue), IntRange.Empty,
                true);

            // empties compared against other empty representations
            yield return new TestCaseData(IntRange.FromInclusiveBounds(1, 0), IntRange.FromInclusiveBounds(5, 4), true);
            yield return new TestCaseData(IntRange.FromInclusiveBounds(0, -1), IntRange.FromInclusiveBounds(int.MaxValue, int.MinValue),
                true);
            yield return new TestCaseData(IntRange.FromInclusiveBounds(int.MinValue + 1, int.MinValue), IntRange.FromInclusiveBounds(1, 0),
                true);

            // ensure empty != non-empty
            yield return new TestCaseData(IntRange.FromInclusiveBounds(1, 0), IntRange.FromInclusiveBounds(0, 0), false);
            yield return new TestCaseData(IntRange.FromInclusiveBounds(5, 4), IntRange.FromInclusiveBounds(4, 5), false);
        }

        [TestCaseSource(nameof(EqualsCases))]
        public void Equals_IntClosedRange_Works(IntRange a, IntRange b, bool expected)
        {
            Assert.That(a.Equals(b), Is.EqualTo(expected));
        }

        // GetHashCode tests: equal ranges produce equal hash codes
        private static IEnumerable<TestCaseData> HashCodeCases()
        {
            // existing canonical pairs
            yield return new TestCaseData(IntRange.FromInclusiveBounds(1, 5), IntRange.FromInclusiveBounds(1, 5));
            yield return new TestCaseData(IntRange.Empty, IntRange.Empty);
            yield return new TestCaseData(IntRange.GreaterThanInclusive(0), IntRange.GreaterThanInclusive(0));
            yield return new TestCaseData(IntRange.FromInclusiveBounds(int.MinValue, int.MaxValue), IntRange.Entire);

            // multiple empty internal representations should hash-equal the canonical Empty
            yield return new TestCaseData(IntRange.Empty, IntRange.FromInclusiveBounds(1, 0));
            yield return new TestCaseData(IntRange.Empty, IntRange.FromInclusiveBounds(5, 4));
            yield return new TestCaseData(IntRange.Empty, IntRange.FromInclusiveBounds(0, -1));
            yield return new TestCaseData(IntRange.Empty, IntRange.FromInclusiveBounds(int.MaxValue, int.MinValue));
            yield return new TestCaseData(IntRange.Empty, IntRange.FromInclusiveBounds(int.MinValue + 1, int.MinValue));
        }

        [TestCaseSource(nameof(HashCodeCases))]
        public void GetHashCode_EqualRanges_HaveSameHashCode(IntRange a, IntRange b)
        {
            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }
    }
}
