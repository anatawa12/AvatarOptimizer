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
            yield return new TestCaseData(ConstAlways(0), ConstAlways(0));
            yield return new TestCaseData(ConstAlways(1), ConstAlways(1));
            yield return new TestCaseData(ConstAlways(float.NaN), ConstAlways(float.NaN));
            yield return new TestCaseData(ConstAlways(float.NegativeInfinity), ConstAlways(float.NegativeInfinity));

            yield return new TestCaseData(ConstPartially(0), ConstPartially(0));
            yield return new TestCaseData(ConstPartially(1), ConstPartially(1));
            yield return new TestCaseData(ConstPartially(float.NaN), ConstPartially(float.NaN));
            yield return new TestCaseData(ConstPartially(float.NegativeInfinity), ConstPartially(float.NegativeInfinity));

            yield return new TestCaseData(Variable(), Variable());
            yield return new TestCaseData(default, default);
        }

        [TestCaseSource(nameof(FalseCause))]
        public void TestNotEquals(AnimationProperty propA, AnimationProperty propB)
        {
            Assert.That(propA.Equals(propB), Is.False);
        }

        public static IEnumerable<TestCaseData> FalseCause()
        {
            yield return new TestCaseData(ConstAlways(0), ConstAlways(1));
            yield return new TestCaseData(ConstAlways(1), ConstPartially(1));
            yield return new TestCaseData(ConstAlways(0), Variable());
            yield return new TestCaseData(default, Variable());
        }
    }
}