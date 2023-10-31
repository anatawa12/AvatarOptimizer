using System;
using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;
using NUnit.Framework;
using static Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes.AnimationProperty;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorParserTest
{
    class AnimationPropertyTest
    {
        [TestCaseSource(nameof(TrueCause))]
        public void TestEquals(AnimationProperty propA, AnimationProperty propB)
        {
            Assert.That(propA.Equals(propB), Is.True);
        }

        public static IEnumerable<TestCaseData> TrueCause()
        {
            yield return new TestCaseData(ConstAlways(0, null), ConstAlways(0, null));
            yield return new TestCaseData(ConstAlways(1, null), ConstAlways(1, null));
            yield return new TestCaseData(ConstAlways(float.NaN, null), ConstAlways(float.NaN, null));
            yield return new TestCaseData(ConstAlways(float.NegativeInfinity, null), ConstAlways(float.NegativeInfinity, null));

            yield return new TestCaseData(ConstPartially(0, null), ConstPartially(0, null));
            yield return new TestCaseData(ConstPartially(1, null), ConstPartially(1, null));
            yield return new TestCaseData(ConstPartially(float.NaN, null), ConstPartially(float.NaN, null));
            yield return new TestCaseData(ConstPartially(float.NegativeInfinity, null), ConstPartially(float.NegativeInfinity, null));

            yield return new TestCaseData(Variable(null), Variable(null));
            yield return new TestCaseData(default, default);
        }

        [TestCaseSource(nameof(FalseCause))]
        public void TestNotEquals(AnimationProperty propA, AnimationProperty propB)
        {
            Assert.That(propA.Equals(propB), Is.False);
        }

        public static IEnumerable<TestCaseData> FalseCause()
        {
            yield return new TestCaseData(ConstAlways(0, null), ConstAlways(1, null));
            yield return new TestCaseData(ConstAlways(1, null), ConstPartially(1, null));
            yield return new TestCaseData(ConstAlways(0, null), Variable(null));
            yield return new TestCaseData(default, Variable(null));
        }
    }
}