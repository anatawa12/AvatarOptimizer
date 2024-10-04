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
            public readonly FloatValueInfo Value;

            public Expected(bool always, FloatValueInfo value)
            {
                Always = always;
                Value = value;
            }
        }

        public static Expected ConstantAlways(float value) => new Expected(true, new FloatValueInfo(value));
        public static Expected ConstantPartially(float value) => new Expected(false, new FloatValueInfo(value));
        public static Expected Variable(bool always = true) => new Expected(always, FloatValueInfo.Variable);

        public static Expected MultipleAlways(params float[] values) =>
            new Expected(true, new FloatValueInfo(values));

        public static void AssertPropertyNode(PropModNode<FloatValueInfo> propertyNode, Expected property)
        {
            Assert.That(propertyNode.AppliedAlways, Is.EqualTo(property.Always));
            Assert.That(propertyNode.Value, Is.EqualTo(property.Value));
        }
    }
}
