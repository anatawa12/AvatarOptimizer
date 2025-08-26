using System;
using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;
using static Anatawa12.AvatarOptimizer.Test.AnimatorParserTest.TestUtil;
using Is = NUnit.Framework.Is;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorParserTest
{
    public class AnimatorTest
    {
        private GameObject _prefab;
        private SkinnedMeshRenderer _skinnedRenderer;
        private AnimatorController _controller;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _prefab = TestUtils.GetAssetAt<GameObject>($"AnimatorParser/AnimatorParserAnimated.prefab");
            _skinnedRenderer = _prefab.GetComponent<SkinnedMeshRenderer>() ?? throw new InvalidOperationException();
            _controller = TestUtils.GetAssetAt<AnimatorController>("AnimatorParser/TestController.controller");
        }

        [Test]
        public void TestLayer00_BaseAnimate0ToConst100() =>
            LayerTest(0, "BaseAnimate0ToConst100",
                "blendShape.shape0", ConstantAlways(100));

        [Test]
        public void TestLayer01_Animate1ToVariable() =>
            LayerTest(1, "Animate1ToVariable",
                "blendShape.shape1", Variable());

        [Test]
        public void TestLayer02_Animate2ToVariable() =>
            LayerTest(2, "Animate2ToVariable",
                "blendShape.shape2", Variable());

        [Test]
        public void TestLayer03_Animate3ToConst100Non0_1Weight() =>
            LayerTest(3, "Animate3ToConst100Non0/1Weight",
                "blendShape.shape3", ConstantAlways(100));

        [Test]
        public void TestLayer04_Animate4ToConst100WithMultipleState() =>
            LayerTest(4, "Animate4ToConst100WithMultipleState",
                "blendShape.shape4", ConstantAlways(100));

        [Test]
        public void TestLayer05_Animate5To100_0WithMultipleState() =>
            LayerTest(5, "Animate5To100/0WithMultipleState",
                "blendShape.shape5", MultipleAlways(0, 100));

        [Test]
        public void TestLayer06_Animate6To100_0WithSubStateMachine() =>
            LayerTest(6, "Animate6To100/0WithSubStateMachine",
                "blendShape.shape6", MultipleAlways(0, 100));

        [Test]
        public void TestLayer07_Animate7To100With1DBlendTree() =>
            LayerTest(7, "Animate7To100With1DBlendTree",
                "blendShape.shape7", ConstantAlways(100));

        [Test]
        public void TestLayer08_Animate8To100WithSimpleDirectional2DBlendTree() =>
            LayerTest(8, "Animate8To100WithSimpleDirectional2DBlendTree",
                "blendShape.shape8", ConstantAlways(100));

        [Test]
        public void TestLayer09_Animate9To100WithFreedomDirection2DBlendTree() =>
            LayerTest(9, "Animate9To100WithFreedomDirection2DBlendTree",
                "blendShape.shape9", ConstantAlways(100));

        [Test]
        public void TestLayer10_Animate10To100WithFreeformCartesian2DBlendTree() =>
            LayerTest(10, "Animate10To100WithFreeformCartesian2DBlendTree",
                "blendShape.shape10", ConstantAlways(100));

        [Test]
        public void TestLayer11_Animate11To100Partially() =>
            LayerTest(11, "Animate11To100Partially",
                "blendShape.shape11", ConstantPartially(100));

        [Test]
        public void TestLayer12_AnimateOverride1To100() =>
            LayerTest(12, "AnimateOverride1To100",
                "blendShape.shape1", ConstantAlways(100));

        [Test]
        public void TestLayer13_Animate12ToConst10() =>
            LayerTest(13, "Animate12ToConst10",
                "blendShape.shape12", ConstantAlways(10));

        [Test]
        public void TestLayer14_Animate12ToConst10Additive() =>
            LayerTest(14, "Animate12ToConst10Additive",
                "blendShape.shape12", ConstantAlways(10),
                blendingMode: AnimatorLayerBlendingMode.Additive);

        [Test]
        public void TestLayer15_Animate13ToConst10() =>
            LayerTest(15, "Animate13ToConst10",
                "blendShape.shape13", ConstantAlways(10));

        [Test]
        public void TestLayer16_Animate13ToConst10AdditivePartially() =>
            LayerTest(16, "Animate13ToConst10AdditivePartially",
                "blendShape.shape13", ConstantPartially(10),
                blendingMode: AnimatorLayerBlendingMode.Additive);

        [Test]
        public void TestLayer17_Animate14ToConst10AdditivePartially() =>
            LayerTest(17, "Animate14ToConst10AdditivePartially",
                "blendShape.shape14", ConstantPartially(10),
                blendingMode: AnimatorLayerBlendingMode.Additive);

        [Test]
        public void TestLayer18_Animate15ToConst10() =>
            LayerTest(18, "Animate15ToConst10",
                "blendShape.shape15", ConstantAlways(10));

        [Test]
        public void TestLayer19_Animate15ToVariableAdditive() =>
            LayerTest(19, "Animate15ToVariableAdditive",
                "blendShape.shape15", Variable(),
                blendingMode: AnimatorLayerBlendingMode.Additive);

        [Test]
        public void TestLayer20_Animate16ToConst100Weight0() =>
            LayerTest(20, "Animate16ToConst100Weight0",
                "blendShape.shape16", ConstantAlways(100));

        [Test]
        public void TestLayer21_Animate16ToConst100Weight0() =>
            LayerTest(21, "Animate17ToConst100Weight0",
                "blendShape.shape17", ConstantAlways(100));

        [Test]
        public void TestParseWhole()
        {
            var parser = new AnimatorParser(true);

            // execute
            var parsed = parser.AdvancedParseAnimatorController(_prefab, _controller,
                Utils.EmptyDictionary<AnimationClip, AnimationClip>(), null);

            var rendererTarget = (ComponentOrGameObject)_skinnedRenderer;

            // test
            Assert.That(parsed.FloatNodes.Keys, Is.EquivalentTo(new[]
            {
                (rendererTarget, "blendShape.shape0"),
                (rendererTarget, "blendShape.shape1"),
                (rendererTarget, "blendShape.shape2"),
                (rendererTarget, "blendShape.shape3"),
                (rendererTarget, "blendShape.shape4"),
                (rendererTarget, "blendShape.shape5"),
                (rendererTarget, "blendShape.shape6"),
                (rendererTarget, "blendShape.shape7"),
                (rendererTarget, "blendShape.shape8"),
                (rendererTarget, "blendShape.shape9"),
                (rendererTarget, "blendShape.shape10"),
                (rendererTarget, "blendShape.shape11"),
                (rendererTarget, "blendShape.shape12"),
                (rendererTarget, "blendShape.shape13"),
                (rendererTarget, "blendShape.shape14"), // 14 is additive constant so no motion
                (rendererTarget, "blendShape.shape15"),
                (rendererTarget, "blendShape.shape16"), // weight is 0
                (rendererTarget, "blendShape.shape17"), // weight is 0
            }));

            AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape0")], ConstantAlways(100));
            AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape1")], ConstantAlways(100));
            AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape2")], Variable());
            AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape3")], PartialConstant(100, ApplyState.Partially));
            AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape4")], ConstantAlways(100));
            AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape5")], MultipleAlways(0, 100));
            AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape6")], MultipleAlways(0, 100));
            AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape7")], ConstantAlways(100));
            AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape8")], ConstantAlways(100));
            AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape9")], ConstantAlways(100));
            AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape10")], ConstantAlways(100));
            AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape11")], PartialConstant(100, ApplyState.Partially));
            AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape12")], ConstantAlways(10));
            AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape13")], ConstantAlways(10));
            AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape14")], Never());
            AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape15")], Variable());
            AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape16")], Never());
            AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape17")], Never());
        }

        [Test]
        public void TestParseWholeWithExternalWeightChanges()
        {
            var parser = new AnimatorParser(true);

            var externallyWeightChanged = new AnimatorWeightChangesList(_controller.layers.Length)
            {
                // this should be ignored.
                [0] = AnimatorWeightChange.NonZeroOneChange,
                // variable even if external change is always 1
                [3] = AnimatorWeightChange.AlwaysOne,
                //
                [7] = AnimatorWeightChange.AlwaysZero,
                [8] = AnimatorWeightChange.NonZeroOneChange,
                [9] = AnimatorWeightChange.NonZeroOneChange,
                // if original have 1, no meaning
                [10] = AnimatorWeightChange.AlwaysOne,
                // original is 0 and override is 1
                [20] = AnimatorWeightChange.AlwaysOne,
                // original is 0 and override is 0
                [21] = AnimatorWeightChange.AlwaysOne,
            };

            var rendererTarget = (ComponentOrGameObject)_skinnedRenderer;

            // execute
            var parsed = parser.AdvancedParseAnimatorController(_prefab, _controller,
                Utils.EmptyDictionary<AnimationClip, AnimationClip>(), externallyWeightChanged);

            // test
            Assert.That(parsed.FloatNodes.Keys, Has.Member((rendererTarget, "blendShape.shape16")));
            Assert.That(parsed.FloatNodes.Keys, Has.No.EqualTo((rendererTarget, "blendShape.shape17")));

            AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape0")], ConstantAlways(100));
            //AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape1")], ConstantAlways(100));
            //AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape2")], Variable());
            AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape3")], PartialConstant(100, ApplyState.Partially));
            //AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape4")], ConstantAlways(100));
            //AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape5")], Variable());
            //AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape6")], Variable());
            AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape7")], PartialConstant(100, ApplyState.Partially));
            AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape8")], PartialConstant(100, ApplyState.Partially));
            AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape9")], PartialConstant(100, ApplyState.Partially));
            AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape10")], ConstantAlways(100));
            //AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape11")], ConstantPartially(100));
            //AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape12")], ConstantAlways(10));
            //AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape13")], ConstantAlways(10));
            //AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape14")], ConstantAlways());
            //AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape15")], Variable());
            AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape16")], PartialConstant(100, ApplyState.Partially));
            //AssertPropertyNode(parsed.FloatNodes[(rendererTarget, "blendShape.shape17")], ConstantAlways(100));
        }

        [Test]
        public void TestOneLayerOverrides()
        {
            var controller = TestUtils.GetAssetAt<RuntimeAnimatorController>("AnimatorParser/OneLayerOverrideController.overrideController");
            var animate0To100 = TestUtils.GetAssetAt<AnimationClip>("AnimatorParser/Animate0To100.anim");
            var animate1To100 = TestUtils.GetAssetAt<AnimationClip>("AnimatorParser/Animate1To100.anim");
            var (original, mapping) = ACUtils.GetControllerAndOverrides(controller);

            Assert.That(original, Is.EqualTo(_controller));
            Assert.That(mapping, Is.EquivalentTo(new []
            {
                new KeyValuePair<AnimationClip, AnimationClip>(animate0To100, animate1To100),
            }));
        }

        [Test]
        public void TestTwoLayerOverrides()
        {
            var controller = TestUtils.GetAssetAt<RuntimeAnimatorController>("AnimatorParser/TwoLayerOverrideController.overrideController");
            var animate0To100 = TestUtils.GetAssetAt<AnimationClip>("AnimatorParser/Animate0To100.anim");
            var animate1To100 = TestUtils.GetAssetAt<AnimationClip>("AnimatorParser/Animate1To100.anim");
            var animate1ToVariable = TestUtils.GetAssetAt<AnimationClip>("AnimatorParser/Animate1ToVariable.anim");
            var animate2ToVariable = TestUtils.GetAssetAt<AnimationClip>("AnimatorParser/Animate2ToVariable.anim");
            var (original, mapping) = ACUtils.GetControllerAndOverrides(controller);

            Assert.That(original, Is.EqualTo(_controller));
            Assert.That(mapping, Is.EquivalentTo(new []
            {
                new KeyValuePair<AnimationClip, AnimationClip>(animate0To100, animate2ToVariable),
                new KeyValuePair<AnimationClip, AnimationClip>(animate1ToVariable, animate2ToVariable),
                new KeyValuePair<AnimationClip, AnimationClip>(animate1To100, animate2ToVariable),
            }));
        }

        private void LayerTest(int layerIndex, string layerName,
            string propertyName, Expected property,
            AnimatorLayerBlendingMode blendingMode = AnimatorLayerBlendingMode.Override)
        {
            var parser = new AnimatorParser(true);

            // preconditions
            Assert.That(_controller.layers[layerIndex].name, Is.EqualTo(layerName));
            Assert.That(_controller.layers[layerIndex].blendingMode, Is.EqualTo(blendingMode));

            // execute
            var parsed = parser.ParseAnimatorControllerLayer(_prefab,
                _controller, Utils.EmptyDictionary<AnimationClip, AnimationClip>(), layerIndex);

            // check
            AssertContainer(parsed, propertyName, property);
        }

        private void AssertContainer(INodeContainer parsed, string prop, Expected property)
        {
            var pair = ((ComponentOrGameObject)_skinnedRenderer, prop);
            Assert.That(parsed.FloatNodes.Keys, Is.EquivalentTo(new[] { pair }));
            AssertPropertyNode(parsed.FloatNodes[pair], property);
        }
    }
}