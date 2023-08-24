using System;
using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;
using Is = NUnit.Framework.Is;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorParserTest
{
    public class AnimatorTest
    {
        private GameObject _prefab;
        private SkinnedMeshRenderer _skinnedRenderer;
        private MockedAnimationParser _mocked;
        private AnimatorController _controller;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _prefab = TestUtils.GetAssetAt<GameObject>($"AnimatorParser/AnimatorParserAnimated.prefab");
            _skinnedRenderer = _prefab.GetComponent<SkinnedMeshRenderer>() ?? throw new InvalidOperationException();
            _mocked = new MockedAnimationParser(_prefab);
            _controller = TestUtils.GetAssetAt<AnimatorController>("AnimatorParser/TestController.controller");
        }

        [Test]
        public void TestLayer00_BaseAnimate0ToConst100() =>
            LayerTest(0, "BaseAnimate0ToConst100",
                "blendShape.shape0", AnimationProperty.ConstAlways(100));

        [Test]
        public void TestLayer01_Animate1ToVariable() =>
            LayerTest(1, "Animate1ToVariable",
                "blendShape.shape1", AnimationProperty.Variable());

        [Test]
        public void TestLayer02_Animate2ToVariable() =>
            LayerTest(2, "Animate2ToVariable",
                "blendShape.shape2", AnimationProperty.Variable());

        [Test]
        public void TestLayer03_Animate3ToConst100Non0_1Weight() =>
            LayerTest(3, "Animate3ToConst100Non0/1Weight",
                "blendShape.shape3", AnimationProperty.ConstAlways(100));

        [Test]
        public void TestLayer04_Animate4ToConst100WithMultipleState() =>
            LayerTest(4, "Animate4ToConst100WithMultipleState",
                "blendShape.shape4", AnimationProperty.ConstAlways(100));

        [Test]
        public void TestLayer05_Animate5To100_0WithMultipleState() =>
            LayerTest(5, "Animate5To100/0WithMultipleState",
                "blendShape.shape5", AnimationProperty.Variable());

        [Test]
        public void TestLayer06_Animate6To100_0WithSubStateMachine() =>
            LayerTest(6, "Animate6To100/0WithSubStateMachine",
                "blendShape.shape6", AnimationProperty.Variable());

        [Test]
        public void TestLayer07_Animate7To100With1DBlendTree() =>
            LayerTest(7, "Animate7To100With1DBlendTree",
                "blendShape.shape7", AnimationProperty.ConstAlways(100));

        [Test]
        public void TestLayer08_Animate8To100WithSimpleDirectional2DBlendTree() =>
            LayerTest(8, "Animate8To100WithSimpleDirectional2DBlendTree",
                "blendShape.shape8", AnimationProperty.ConstAlways(100));

        [Test]
        public void TestLayer09_Animate9To100WithFreedomDirection2DBlendTree() =>
            LayerTest(9, "Animate9To100WithFreedomDirection2DBlendTree",
                "blendShape.shape9", AnimationProperty.ConstAlways(100));

        [Test]
        public void TestLayer10_Animate10To100WithFreeformCartesian2DBlendTree() =>
            LayerTest(10, "Animate10To100WithFreeformCartesian2DBlendTree",
                "blendShape.shape10", AnimationProperty.ConstAlways(100));

        [Test]
        public void TestLayer11_Animate11To100Partially() =>
            LayerTest(11, "Animate11To100Partially",
                "blendShape.shape11", AnimationProperty.ConstPartially(100));

        [Test]
        public void TestLayer12_AnimateOverride1To100() =>
            LayerTest(12, "AnimateOverride1To100",
                "blendShape.shape1", AnimationProperty.ConstAlways(100));

        [Test]
        public void TestLayer13_Animate12ToConst10() =>
            LayerTest(13, "Animate12ToConst10",
                "blendShape.shape12", AnimationProperty.ConstAlways(10));

        [Test]
        public void TestLayer14_Animate12ToConst10Additive() =>
            LayerTest(14, "Animate12ToConst10Additive",
                "blendShape.shape12", AnimationProperty.ConstAlways(10),
                blendingMode: AnimatorLayerBlendingMode.Additive);

        [Test]
        public void TestLayer15_Animate13ToConst10() =>
            LayerTest(15, "Animate13ToConst10",
                "blendShape.shape13", AnimationProperty.ConstAlways(10));

        [Test]
        public void TestLayer16_Animate13ToConst10AdditivePartially() =>
            LayerTest(16, "Animate13ToConst10AdditivePartially",
                "blendShape.shape13", AnimationProperty.ConstPartially(10),
                blendingMode: AnimatorLayerBlendingMode.Additive);

        [Test]
        public void TestLayer17_Animate14ToConst10AdditivePartially() =>
            LayerTest(17, "Animate14ToConst10AdditivePartially",
                "blendShape.shape14", AnimationProperty.ConstPartially(10),
                blendingMode: AnimatorLayerBlendingMode.Additive);

        [Test]
        public void TestLayer18_Animate15ToConst10() =>
            LayerTest(18, "Animate15ToConst10",
                "blendShape.shape15", AnimationProperty.ConstAlways(10));
        
        [Test]
        public void TestLayer19_Animate15ToVariableAdditive() =>
            LayerTest(19, "Animate15ToVariableAdditive",
                "blendShape.shape15", AnimationProperty.Variable(),
                blendingMode: AnimatorLayerBlendingMode.Additive);

        [Test]
        public void TestParseWhole()
        {
            var parser = new AnimatorParser(true, true);

            // execute
            var parsed = parser.AdvancedParseAnimatorController(_prefab, _controller,
                Utils.EmptyDictionary<AnimationClip, AnimationClip>(), null);

            // test
            Assert.That(parsed.ModifiedProperties.Keys,
                Is.EquivalentTo(new[] { (ComponentOrGameObject)_skinnedRenderer }));

            var properties = parsed.ModifiedProperties[_skinnedRenderer];
            Assert.That(properties.Keys, Is.EquivalentTo(new[]
            {
                "blendShape.shape0",
                "blendShape.shape1",
                "blendShape.shape2",
                "blendShape.shape3",
                "blendShape.shape4",
                "blendShape.shape5",
                "blendShape.shape6",
                "blendShape.shape7",
                "blendShape.shape8",
                "blendShape.shape9",
                "blendShape.shape10",
                "blendShape.shape11",
                "blendShape.shape12",
                "blendShape.shape13",
                // "blendShape.shape14", // 14 is additive constant so no motion
                "blendShape.shape15",
            }));

            Assert.That(properties["blendShape.shape0"], Is.EqualTo(AnimationProperty.ConstAlways(100)));
            Assert.That(properties["blendShape.shape1"], Is.EqualTo(AnimationProperty.ConstAlways(100)));
            Assert.That(properties["blendShape.shape2"], Is.EqualTo(AnimationProperty.Variable()));
            Assert.That(properties["blendShape.shape3"], Is.EqualTo(AnimationProperty.Variable()));
            Assert.That(properties["blendShape.shape4"], Is.EqualTo(AnimationProperty.ConstAlways(100)));
            Assert.That(properties["blendShape.shape5"], Is.EqualTo(AnimationProperty.Variable()));
            Assert.That(properties["blendShape.shape6"], Is.EqualTo(AnimationProperty.Variable()));
            Assert.That(properties["blendShape.shape7"], Is.EqualTo(AnimationProperty.ConstAlways(100)));
            Assert.That(properties["blendShape.shape8"], Is.EqualTo(AnimationProperty.ConstAlways(100)));
            Assert.That(properties["blendShape.shape9"], Is.EqualTo(AnimationProperty.ConstAlways(100)));
            Assert.That(properties["blendShape.shape10"], Is.EqualTo(AnimationProperty.ConstAlways(100)));
            Assert.That(properties["blendShape.shape11"], Is.EqualTo(AnimationProperty.ConstPartially(100)));
            Assert.That(properties["blendShape.shape12"], Is.EqualTo(AnimationProperty.ConstAlways(10)));
            Assert.That(properties["blendShape.shape13"], Is.EqualTo(AnimationProperty.ConstAlways(10)));
            //Assert.That(properties["blendShape.shape14"], Is.EqualTo(AnimationProperty.ConstAlways()));
            Assert.That(properties["blendShape.shape15"], Is.EqualTo(AnimationProperty.Variable()));

        }

        private void LayerTest(int layerIndex, string layerName,
            string propertyName, AnimationProperty property,
            AnimatorLayerBlendingMode blendingMode = AnimatorLayerBlendingMode.Override)
        {
            var parser = new AnimatorParser(true, true);

            // preconditions
            Assert.That(_controller.layers[layerIndex].name, Is.EqualTo(layerName));
            Assert.That(_controller.layers[layerIndex].blendingMode, Is.EqualTo(blendingMode));

            // execute
            var parsed = parser.ParseAnimatorControllerLayer(_prefab,
                _controller, Utils.EmptyDictionary<AnimationClip, AnimationClip>(), layerIndex);

            // check
            AssertContainer(parsed, propertyName, property);
        }

        private void AssertContainer(IModificationsContainer parsed, string prop, AnimationProperty property)
        {
            Assert.That(parsed.ModifiedProperties.Keys,
                Is.EquivalentTo(new[] { (ComponentOrGameObject)_skinnedRenderer }));
            var properties = parsed.ModifiedProperties[_skinnedRenderer];
            Assert.That(properties.Keys, Is.EquivalentTo(new[] { prop }));
            Assert.That(properties[prop], Is.EqualTo(property));
        }

        class MockedAnimationParser
        {
            private readonly GameObject _prefab;
            private Dictionary<AnimationClip, ImmutableModificationsContainer> _clips;

            public MockedAnimationParser(GameObject prefab)
            {
                _prefab = prefab;
                _clips = new Dictionary<AnimationClip, ImmutableModificationsContainer>();

                var skinnedRenderer =
                    _prefab.GetComponent<SkinnedMeshRenderer>() ?? throw new InvalidOperationException();

                foreach (var (animName, property, constantValue) in AnimationTest.ConstSourceAnimations())
                {
                    var clip = TestUtils.GetAssetAt<AnimationClip>($"AnimatorParser/{animName}.anim");
                    var mods = new ModificationsContainer();
                    mods.ModifyObject(skinnedRenderer)
                        .AddModificationAsNewLayer(property, AnimationProperty.ConstAlways(constantValue));
                    _clips[clip] = mods.ToImmutable();
                }

                foreach (var (animName, property) in AnimationTest.VariableSourceAnimations())
                {
                    var clip = TestUtils.GetAssetAt<AnimationClip>($"AnimatorParser/{animName}.anim");
                    var mods = new ModificationsContainer();
                    mods.ModifyObject(skinnedRenderer)
                        .AddModificationAsNewLayer(property, AnimationProperty.Variable());
                    _clips[clip] = mods.ToImmutable();
                }
            }

            public ImmutableModificationsContainer GetParsedAnimationMocked(GameObject gameObject, AnimationClip clip)
            {
                Assert.That(gameObject, Is.EqualTo(_prefab));
                return _clips[clip];
            }
        }
    }
}