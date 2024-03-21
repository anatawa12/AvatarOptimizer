using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using NUnit.Framework;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorParserTest
{
    internal static class TestUtil
    {
        // (bool always, bool constant, float value)
        public struct Expected
        {
            public readonly bool Always;
            public readonly ValueInfo<float> Value;

            public Expected(bool always, ValueInfo<float> value)
            {
                Always = always;
                Value = value;
            }
        }

        public static Expected ConstantAlways(float value) => new Expected(true, new ValueInfo<float>(value));
        public static Expected ConstantPartially(float value) => new Expected(false, new ValueInfo<float>(value));
        public static Expected Variable(bool always = true) => new Expected(always, ValueInfo<float>.Variable);

        public static Expected MultipleAlways(params float[] values) =>
            new Expected(true, new ValueInfo<float>(values));

        public static void AssertPropertyNode(PropModNode<float> propertyNode, Expected property)
        {
            Assert.That(propertyNode.AppliedAlways, Is.EqualTo(property.Always));
            Assert.That(propertyNode.Value, Is.EqualTo(property.Value));
        }
    }
}