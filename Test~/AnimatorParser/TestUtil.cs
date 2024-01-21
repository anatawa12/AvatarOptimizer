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
            public readonly ConstantInfo<float> Constant;

            public Expected(bool always, ConstantInfo<float> constant)
            {
                Always = always;
                Constant = constant;
            }
        }

        public static Expected ConstantAlways(float value) => new Expected(true, new ConstantInfo<float>(value));
        public static Expected ConstantPartially(float value) => new Expected(false, new ConstantInfo<float>(value));
        public static Expected Variable(bool always = true) => new Expected(always, ConstantInfo<float>.Variable);

        public static void AssertPropertyNode(PropModNode<float> propertyNode, Expected property)
        {
            Assert.That(propertyNode.AppliedAlways, Is.EqualTo(property.Always));
            Assert.That(propertyNode.Constant, Is.EqualTo(property.Constant));
        }
    }
}