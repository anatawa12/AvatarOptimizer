using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorParserTest.Issue967
{
    public class TestIssue967
    {
        [Test]
        public void Test()
        {
            var controller = TestUtils.GetAssetAt<AnimatorController>("AnimatorParser/Issue967/FX.controller");
            var testGameObject = CreateTestGameObject();
            var renderer = testGameObject.transform.Find("Body").GetComponent<SkinnedMeshRenderer>();

            var parser = new AnimatorParsersV2.AnimatorParser(false);
            var container = parser.ParseAnimatorController(testGameObject, controller);

            Assert.That(container != null, nameof(container) + " != null");

            var node = container.FloatNodes[(renderer, "blendShape.eye_smile_1")];
                
            Assert.That(node != null, nameof(node) + " != null");

            Assert.That(node.AppliedAlways, Is.True);
            Assert.That(node.Value.IsConstant, Is.False);
            Assert.That(node.Value.PossibleValues, Is.EquivalentTo(new[] {0f, 100f}));
        }

        private static GameObject CreateTestGameObject()
        {
            var testGameObject = new GameObject("TestGameObject");
            var body = new GameObject("Body");
            body.transform.SetParent(testGameObject.transform);
            body.AddComponent<SkinnedMeshRenderer>();
            return testGameObject;
        }
    }
}