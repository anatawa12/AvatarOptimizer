using System;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using NUnit.Framework;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorParserTest
{
    internal static class TestUtil
    {
        // (bool always, bool constant, float value)
        public struct Expected
        {
            public readonly ApplyState ApplyState;
            public readonly FloatValueInfo Value;

            public Expected(ApplyState applyApplyState, FloatValueInfo value)
            {
                ApplyState = applyApplyState;
                Value = value;
            }
        }

        public static Expected Never() => new(ApplyState.Never, new FloatValueInfo(Array.Empty<float>()));
        public static Expected ConstantAlways(float value) => new(ApplyState.Always, new FloatValueInfo(value));
        public static Expected ConstantPartially(float value) => new(ApplyState.Partially, new FloatValueInfo(value));
        public static Expected PartialConstant(float value, ApplyState applyState) => new(applyState, new FloatValueInfo(value, partialApplication: true));
        public static Expected Variable(ApplyState applyState = ApplyState.Always) => new(applyState, FloatValueInfo.Variable);

        public static Expected MultipleAlways(params float[] values) =>
            new(ApplyState.Always, new FloatValueInfo(values));

        public static void AssertPropertyNode(PropModNode<FloatValueInfo> propertyNode, Expected property)
        {
            Assert.That(propertyNode.ApplyState, Is.EqualTo(property.ApplyState));
            Assert.That(propertyNode.Value, Is.EqualTo(property.Value));
        }
    }
}
