using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer;
using NUnit.Framework;


namespace Anatawa12.AvatarOptimizer.Test.AnimatorOptimizer
{
    using FloatOpenRange = ClosedRange<float, RangeFloatTrait>;

    [TestFixture]
    public class FloatOpenRangeTest
    {
        // Test data for IsEmpty cases
        private static IEnumerable<TestCaseData> IsEmptyCases()
        {
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(null, null), false).SetName("Entire_NotEmpty");
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(0f, 1f), false).SetName("BoundedNonEmpty");
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(1f, 1f), true).SetName("EqualEndpoints_Empty");
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(2f, 1f), true).SetName("Inverted_Empty");
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(null, 1f), false).SetName("LeftUnbounded_NotEmpty");
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(1f, null), false).SetName("RightUnbounded_NotEmpty");

            // general (non-edge) cases
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(-5f, -1f), false).SetName("NegativeRange_NonEmpty");
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(1f, 1.00001f), false).SetName("SmallDecimalRange_NonEmpty");
        }

        [Test, TestCaseSource(nameof(IsEmptyCases))]
        public void IsEmpty_Param(FloatOpenRange range, bool expected)
        {
            Assert.That(range.IsEmpty(), Is.EqualTo(expected));
        }

        // Test data for Intersect cases
        // parameters: a, b, expectedRange (use FloatOpenRange.Empty for empty results)
        private static IEnumerable<TestCaseData> IntersectCases()
        {
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(1f, 3f), FloatOpenRange.FromExclusiveBounds(2f, 4f), FloatOpenRange.FromExclusiveBounds(2f, 3f))
                .SetName("Intersect_Overlapping");
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(1f, 2f), FloatOpenRange.FromExclusiveBounds(3f, 4f), FloatOpenRange.Empty)
                .SetName("Intersect_Disjoint_Empty");
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(1f, 2f), FloatOpenRange.FromExclusiveBounds(2f, 3f), FloatOpenRange.Empty)
                .SetName("Intersect_Touching_Empty");
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(null, 2f), FloatOpenRange.FromExclusiveBounds(1f, null), FloatOpenRange.FromExclusiveBounds(1f, 2f))
                .SetName("Intersect_Unbounded");
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(1f, 1f), FloatOpenRange.FromExclusiveBounds(0f, 2f), FloatOpenRange.Empty)
                .SetName("Intersect_Empty_Left");
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(0f, 2f), FloatOpenRange.FromExclusiveBounds(1f, 1f), FloatOpenRange.Empty)
                .SetName("Intersect_Empty_Right");

            // general (non-edge) cases
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(1f, 10f), FloatOpenRange.FromExclusiveBounds(3f, 4f), FloatOpenRange.FromExclusiveBounds(3f, 4f))
                .SetName("Intersect_Contained");
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(0.1f, 0.5f), FloatOpenRange.FromExclusiveBounds(0.3f, 0.8f), FloatOpenRange.FromExclusiveBounds(0.3f, 0.5f))
                .SetName("Intersect_DecimalOverlap");
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(-2f, 1f), FloatOpenRange.FromExclusiveBounds(0f, 3f), FloatOpenRange.FromExclusiveBounds(0f, 1f))
                .SetName("Intersect_CrossZero");
        }

        [Test, TestCaseSource(nameof(IntersectCases))]
        public void Intersect_Param(FloatOpenRange a, FloatOpenRange b, FloatOpenRange expected)
        {
            Assert.That(a.Intersect(b), Is.EqualTo(expected));
        }

        // Test data for Union cases
        // parameters: a, b, expectedRangeNullable (null means Union returns null)
        private static IEnumerable<TestCaseData> UnionCases()
        {
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(1f, 3f), FloatOpenRange.FromExclusiveBounds(2f, 4f), FloatOpenRange.FromExclusiveBounds(1f, 4f))
                .SetName("Union_Overlapping_Merge");
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(1f, 5f), FloatOpenRange.FromExclusiveBounds(2f, 3f), FloatOpenRange.FromExclusiveBounds(1f, 5f))
                .SetName("Union_Contained_ReturnOuter");
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(1f, 2f), FloatOpenRange.FromExclusiveBounds(3f, 4f), (FloatOpenRange?)null)
                .SetName("Union_Disjoint_Null");
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(1f, 2f), FloatOpenRange.FromExclusiveBounds(2f, 3f), (FloatOpenRange?)null)
                .SetName("Union_Touching_Null");
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(null, 2f), FloatOpenRange.FromExclusiveBounds(1f, null), FloatOpenRange.FromExclusiveBounds(null, null))
                .SetName("Union_Unbounded_ToEntire");
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(1f, 1f), FloatOpenRange.FromExclusiveBounds(1f, 2f), FloatOpenRange.FromExclusiveBounds(1f, 2f))
                .SetName("Union_Empty_LeftReturnsOther");
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(1f, 2f), FloatOpenRange.FromExclusiveBounds(1f, 1f), FloatOpenRange.FromExclusiveBounds(1f, 2f))
                .SetName("Union_Empty_RightReturnsOther");

            // general (non-edge) cases
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(0.1f, 0.5f), FloatOpenRange.FromExclusiveBounds(0.3f, 0.8f), FloatOpenRange.FromExclusiveBounds(0.1f, 0.8f))
                .SetName("Union_DecimalMerge");
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(-3f, -1f), FloatOpenRange.FromExclusiveBounds(-2f, 2f), FloatOpenRange.FromExclusiveBounds(-3f, 2f))
                .SetName("Union_NegativeMerge");
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(1f, 10f), FloatOpenRange.FromExclusiveBounds(3f, 4f), FloatOpenRange.FromExclusiveBounds(1f, 10f))
                .SetName("Union_Contained_General");
        }

        [Test, TestCaseSource(nameof(UnionCases))]
        public void Union_Param(FloatOpenRange a, FloatOpenRange b, FloatOpenRange? expected)
        {
            Assert.That(a.Union(b), Is.EqualTo(expected));
        }

        [Test]
        public void Union_BothEmpty_ReturnsEmpty()
        {
            var e1 = FloatOpenRange.FromExclusiveBounds(1f, 1f);
            var e2 = FloatOpenRange.FromExclusiveBounds(2f, 2f);
            var u = e1.Union(e2);
            Assert.That(u, Is.Not.Null);
            // equality treats all empty ranges as equal, so asserting equality with e1 is sufficient
            Assert.That(u.Value, Is.EqualTo(e1));
        }

        // Equality test cases: (a, b, expectedEquals)
        private static IEnumerable<TestCaseData> EqualityCases()
        {
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(1f, 2f), FloatOpenRange.FromExclusiveBounds(1f, 2f), true).SetName("Equals_SameValues");
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(1f, 2f), FloatOpenRange.FromExclusiveBounds(2f, 3f), false).SetName("Equals_DifferentValues");
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(1f, 1f), FloatOpenRange.FromExclusiveBounds(2f, 2f), true).SetName("Equals_BothEmpty");
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(null, null), FloatOpenRange.FromExclusiveBounds(null, null), true).SetName("Equals_Entire");

            // half-open (one side unbounded) equality cases
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(null, 1f), FloatOpenRange.FromExclusiveBounds(null, 1f), true).SetName("Equals_LeftUnbounded_Same");
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(1f, null), FloatOpenRange.FromExclusiveBounds(1f, null), true).SetName("Equals_RightUnbounded_Same");
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(null, 1f), FloatOpenRange.FromExclusiveBounds(1f, null), false).SetName("Equals_LeftUnbounded_Vs_RightUnbounded");
        }

        [Test, TestCaseSource(nameof(EqualityCases))]
        public void Equals_Param(FloatOpenRange a, FloatOpenRange b, bool expected)
        {
            Assert.That(a.Equals(b), Is.EqualTo(expected));
        }

        // HashCode test cases: (a, b, expectEqualHash)
        private static IEnumerable<TestCaseData> HashCodeCases()
        {
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(1f, 2f), FloatOpenRange.FromExclusiveBounds(1f, 2f), true)
                .SetName("Hash_EqualRanges");
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(1f, 1f), FloatOpenRange.FromExclusiveBounds(2f, 2f), true)
                .SetName("Hash_BothEmpty");
            // include one distinct case to catch obvious incorrect implementations
            yield return new TestCaseData(FloatOpenRange.FromExclusiveBounds(1f, 2f), FloatOpenRange.FromExclusiveBounds(2f, 3f), false)
                .SetName("Hash_DifferentRanges");
        }

        [Test, TestCaseSource(nameof(HashCodeCases))]
        public void GetHashCode_Param(FloatOpenRange a, FloatOpenRange b, bool expectEqualHash)
        {
            var ha = a.GetHashCode();
            var hb = b.GetHashCode();
            if (expectEqualHash)
                Assert.That(ha, Is.EqualTo(hb));
            else
                Assert.That(ha, Is.Not.EqualTo(hb));
        }
    }
}