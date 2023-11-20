using System;
using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.AnimatorParsers;
using NUnit.Framework;
using static Anatawa12.AvatarOptimizer.AnimatorParsers.AnimationFloatProperty;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorParserTest
{
    class AnimationPropertyTest
    {
        private static readonly TestSourceImpl Source = TestSourceImpl.Instance;

        [TestCaseSource(nameof(TrueCause))]
        public void TestEquals(AnimationFloatProperty propA, AnimationFloatProperty propB)
        {
            Assert.That(propA.Equals(propB), Is.True);
        }

        public static IEnumerable<TestCaseData> TrueCause()
        {
            yield return new TestCaseData(ConstAlways(0, Source), ConstAlways(0, Source));
            yield return new TestCaseData(ConstAlways(1, Source), ConstAlways(1, Source));
            yield return new TestCaseData(ConstAlways(float.NaN, Source), ConstAlways(float.NaN, Source));
            yield return new TestCaseData(ConstAlways(float.NegativeInfinity, Source), ConstAlways(float.NegativeInfinity, Source));

            yield return new TestCaseData(ConstPartially(0, Source), ConstPartially(0, Source));
            yield return new TestCaseData(ConstPartially(1, Source), ConstPartially(1, Source));
            yield return new TestCaseData(ConstPartially(float.NaN, Source), ConstPartially(float.NaN, Source));
            yield return new TestCaseData(ConstPartially(float.NegativeInfinity, Source), ConstPartially(float.NegativeInfinity, Source));

            yield return new TestCaseData(Variable(Source), Variable(Source));
            yield return new TestCaseData(default, default);
        }

        [TestCaseSource(nameof(FalseCause))]
        public void TestNotEquals(AnimationFloatProperty propA, AnimationFloatProperty propB)
        {
            Assert.That(propA.Equals(propB), Is.False);
        }

        public static IEnumerable<TestCaseData> FalseCause()
        {
            yield return new TestCaseData(ConstAlways(0, Source), ConstAlways(1, Source));
            yield return new TestCaseData(ConstAlways(1, Source), ConstPartially(1, Source));
            yield return new TestCaseData(ConstAlways(0, Source), Variable(Source));
            yield return new TestCaseData(default, Variable(Source));
        }
    }
}