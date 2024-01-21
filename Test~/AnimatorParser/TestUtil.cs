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
            public readonly bool Constant;
            public readonly float Value;

            public Expected(bool always, bool constant, float value)
            {
                Always = always;
                Constant = constant;
                Value = value;
            }
        }

        public static Expected ConstantAlways(float value) => new Expected(true, true, value);
        public static Expected ConstantPartially(float value) => new Expected(false, true, value);
        public static Expected Variable(bool always = true) => new Expected(always, false, 0);

        public static void AssertPropertyNode(PropModNode<float> propertyNode, Expected property)
        {
            Assert.That(propertyNode.IsConstant, Is.EqualTo(property.Constant));
            Assert.That(propertyNode.AppliedAlways, Is.EqualTo(property.Always));
            if (property.Constant)
                Assert.That(propertyNode.ConstantValue, Is.EqualTo(property.Value));
        }
    }
}