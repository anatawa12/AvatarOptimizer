using System;
using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.AnimatorParsers;
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
                "blendShape.shape0", AnimationFloatProperty.ConstAlways(100, null));

        [Test]
        public void TestLayer01_Animate1ToVariable() =>
            LayerTest(1, "Animate1ToVariable",
                "blendShape.shape1", AnimationFloatProperty.Variable(null));

        [Test]
        public void TestLayer02_Animate2ToVariable() =>
            LayerTest(2, "Animate2ToVariable",
                "blendShape.shape2", AnimationFloatProperty.Variable(null));

        [Test]
        public void TestLayer03_Animate3ToConst100Non0_1Weight() =>
            LayerTest(3, "Animate3ToConst100Non0/1Weight",
                "blendShape.shape3", AnimationFloatProperty.ConstAlways(100, null));

        [Test]
        public void TestLayer04_Animate4ToConst100WithMultipleState() =>
            LayerTest(4, "Animate4ToConst100WithMultipleState",
                "blendShape.shape4", AnimationFloatProperty.ConstAlways(100, null));

        [Test]
        public void TestLayer05_Animate5To100_0WithMultipleState() =>
            LayerTest(5, "Animate5To100/0WithMultipleState",
                "blendShape.shape5", AnimationFloatProperty.Variable(null));

        [Test]
        public void TestLayer06_Animate6To100_0WithSubStateMachine() =>
            LayerTest(6, "Animate6To100/0WithSubStateMachine",
                "blendShape.shape6", AnimationFloatProperty.Variable(null));

        [Test]
        public void TestLayer07_Animate7To100With1DBlendTree() =>
            LayerTest(7, "Animate7To100With1DBlendTree",
                "blendShape.shape7", AnimationFloatProperty.ConstAlways(100, null));

        [Test]
        public void TestLayer08_Animate8To100WithSimpleDirectional2DBlendTree() =>
            LayerTest(8, "Animate8To100WithSimpleDirectional2DBlendTree",
                "blendShape.shape8", AnimationFloatProperty.ConstAlways(100, null));

        [Test]
        public void TestLayer09_Animate9To100WithFreedomDirection2DBlendTree() =>
            LayerTest(9, "Animate9To100WithFreedomDirection2DBlendTree",
                "blendShape.shape9", AnimationFloatProperty.ConstAlways(100, null));

        [Test]
        public void TestLayer10_Animate10To100WithFreeformCartesian2DBlendTree() =>
            LayerTest(10, "Animate10To100WithFreeformCartesian2DBlendTree",
                "blendShape.shape10", AnimationFloatProperty.ConstAlways(100, null));

        [Test]
        public void TestLayer11_Animate11To100Partially() =>
            LayerTest(11, "Animate11To100Partially",
                "blendShape.shape11", AnimationFloatProperty.ConstPartially(100, null));

        [Test]
        public void TestLayer12_AnimateOverride1To100() =>
            LayerTest(12, "AnimateOverride1To100",
                "blendShape.shape1", AnimationFloatProperty.ConstAlways(100, null));

        [Test]
        public void TestLayer13_Animate12ToConst10() =>
            LayerTest(13, "Animate12ToConst10",
                "blendShape.shape12", AnimationFloatProperty.ConstAlways(10, null));

        [Test]
        public void TestLayer14_Animate12ToConst10Additive() =>
            LayerTest(14, "Animate12ToConst10Additive",
                "blendShape.shape12", AnimationFloatProperty.ConstAlways(10, null),
                blendingMode: AnimatorLayerBlendingMode.Additive);

        [Test]
        public void TestLayer15_Animate13ToConst10() =>
            LayerTest(15, "Animate13ToConst10",
                "blendShape.shape13", AnimationFloatProperty.ConstAlways(10, null));

        [Test]
        public void TestLayer16_Animate13ToConst10AdditivePartially() =>
            LayerTest(16, "Animate13ToConst10AdditivePartially",
                "blendShape.shape13", AnimationFloatProperty.ConstPartially(10, null),
                blendingMode: AnimatorLayerBlendingMode.Additive);

        [Test]
        public void TestLayer17_Animate14ToConst10AdditivePartially() =>
            LayerTest(17, "Animate14ToConst10AdditivePartially",
                "blendShape.shape14", AnimationFloatProperty.ConstPartially(10, null),
                blendingMode: AnimatorLayerBlendingMode.Additive);

        [Test]
        public void TestLayer18_Animate15ToConst10() =>
            LayerTest(18, "Animate15ToConst10",
                "blendShape.shape15", AnimationFloatProperty.ConstAlways(10, null));

        [Test]
        public void TestLayer19_Animate15ToVariableAdditive() =>
            LayerTest(19, "Animate15ToVariableAdditive",
                "blendShape.shape15", AnimationFloatProperty.Variable(null),
                blendingMode: AnimatorLayerBlendingMode.Additive);

        [Test]
        public void TestLayer20_Animate16ToConst100Weight0() =>
            LayerTest(20, "Animate16ToConst100Weight0",
                "blendShape.shape16", AnimationFloatProperty.ConstAlways(100, null));

        [Test]
        public void TestLayer21_Animate16ToConst100Weight0() =>
            LayerTest(21, "Animate17ToConst100Weight0",
                "blendShape.shape17", AnimationFloatProperty.ConstAlways(100, null));

        [Test]
        public void TestParseWhole()
        {
            var parser = new AnimatorParser(true);

            // execute
            var parsed = parser.AdvancedParseAnimatorController(_prefab, _controller,
                Utils.EmptyDictionary<AnimationClip, AnimationClip>(), null);

            // test
            Assert.That(parsed.ModifiedProperties.Keys,
                Is.EquivalentTo(new[] { (ComponentOrGameObject)_skinnedRenderer }));

            var properties = parsed.ModifiedProperties[_skinnedRenderer].FloatProperties;
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
                // "blendShape.shape16", // weight is 0
                // "blendShape.shape17", // weight is 0
            }));

            Assert.That(properties["blendShape.shape0"], Is.EqualTo(AnimationFloatProperty.ConstAlways(100, null)));
            Assert.That(properties["blendShape.shape1"], Is.EqualTo(AnimationFloatProperty.ConstAlways(100, null)));
            Assert.That(properties["blendShape.shape2"], Is.EqualTo(AnimationFloatProperty.Variable(null)));
            Assert.That(properties["blendShape.shape3"], Is.EqualTo(AnimationFloatProperty.Variable(null)));
            Assert.That(properties["blendShape.shape4"], Is.EqualTo(AnimationFloatProperty.ConstAlways(100, null)));
            Assert.That(properties["blendShape.shape5"], Is.EqualTo(AnimationFloatProperty.Variable(null)));
            Assert.That(properties["blendShape.shape6"], Is.EqualTo(AnimationFloatProperty.Variable(null)));
            Assert.That(properties["blendShape.shape7"], Is.EqualTo(AnimationFloatProperty.ConstAlways(100, null)));
            Assert.That(properties["blendShape.shape8"], Is.EqualTo(AnimationFloatProperty.ConstAlways(100, null)));
            Assert.That(properties["blendShape.shape9"], Is.EqualTo(AnimationFloatProperty.ConstAlways(100, null)));
            Assert.That(properties["blendShape.shape10"], Is.EqualTo(AnimationFloatProperty.ConstAlways(100, null)));
            Assert.That(properties["blendShape.shape11"], Is.EqualTo(AnimationFloatProperty.ConstPartially(100, null)));
            Assert.That(properties["blendShape.shape12"], Is.EqualTo(AnimationFloatProperty.ConstAlways(10, null)));
            Assert.That(properties["blendShape.shape13"], Is.EqualTo(AnimationFloatProperty.ConstAlways(10, null)));
            //Assert.That(properties["blendShape.shape14"], Is.EqualTo(AnimationProperty.ConstAlways(, null)));
            Assert.That(properties["blendShape.shape15"], Is.EqualTo(AnimationFloatProperty.Variable(null)));
            //Assert.That(properties["blendShape.shape16"], Is.EqualTo(AnimationProperty.ConstAlways(100, null)));
            //Assert.That(properties["blendShape.shape17"], Is.EqualTo(AnimationProperty.ConstAlways(100, null)));
        }

        [Test]
        public void TestParseWholeWithExternalWeightChanges()
        {
            var parser = new AnimatorParser(true);

            var externallyWeightChanged = new AnimatorLayerWeightMap<int>
            {
                // this should be ignored.
                [0] = AnimatorWeightState.Variable,
                // variable even if external change is always 1
                [3] = AnimatorWeightState.AlwaysOne,
                //
                [7] = AnimatorWeightState.AlwaysZero,
                [8] = AnimatorWeightState.EitherZeroOrOne,
                [9] = AnimatorWeightState.Variable,
                // if original have 1, no meaning
                [10] = AnimatorWeightState.AlwaysOne,
                // original is 0 and override is 1
                [20] = AnimatorWeightState.AlwaysOne,
                // original is 0 and override is 0
                [21] = AnimatorWeightState.AlwaysOne,
            };

            // execute
            var parsed = parser.AdvancedParseAnimatorController(_prefab, _controller,
                Utils.EmptyDictionary<AnimationClip, AnimationClip>(), externallyWeightChanged);

            // test
            Assert.That(parsed.ModifiedProperties.Keys,
                Is.EquivalentTo(new[] { (ComponentOrGameObject)_skinnedRenderer }));

            var properties = parsed.ModifiedProperties[_skinnedRenderer].FloatProperties;
            Assert.That(properties.Keys, Has.Member("blendShape.shape16"));
            Assert.That(properties.Keys, Has.No.EqualTo("blendShape.shape17"));

            Assert.That(properties["blendShape.shape0"], Is.EqualTo(AnimationFloatProperty.ConstAlways(100, null)));
            //Assert.That(properties["blendShape.shape1"], Is.EqualTo(AnimationProperty.ConstAlways(100, null)));
            //Assert.That(properties["blendShape.shape2"], Is.EqualTo(AnimationProperty.Variable(null)));
            Assert.That(properties["blendShape.shape3"], Is.EqualTo(AnimationFloatProperty.Variable(null)));
            //Assert.That(properties["blendShape.shape4"], Is.EqualTo(AnimationProperty.ConstAlways(100, null)));
            //Assert.That(properties["blendShape.shape5"], Is.EqualTo(AnimationProperty.Variable(null)));
            //Assert.That(properties["blendShape.shape6"], Is.EqualTo(AnimationProperty.Variable(null)));
            Assert.That(properties["blendShape.shape7"], Is.EqualTo(AnimationFloatProperty.ConstPartially(100, null)));
            Assert.That(properties["blendShape.shape8"], Is.EqualTo(AnimationFloatProperty.ConstPartially(100, null)));
            Assert.That(properties["blendShape.shape9"], Is.EqualTo(AnimationFloatProperty.Variable(null)));
            Assert.That(properties["blendShape.shape10"], Is.EqualTo(AnimationFloatProperty.ConstAlways(100, null)));
            //Assert.That(properties["blendShape.shape11"], Is.EqualTo(AnimationProperty.ConstPartially(100)));
            //Assert.That(properties["blendShape.shape12"], Is.EqualTo(AnimationProperty.ConstAlways(10, null)));
            //Assert.That(properties["blendShape.shape13"], Is.EqualTo(AnimationProperty.ConstAlways(10, null)));
            ////Assert.That(properties["blendShape.shape14"], Is.EqualTo(AnimationProperty.ConstAlways(, null)));
            //Assert.That(properties["blendShape.shape15"], Is.EqualTo(AnimationProperty.Variable(null)));
            Assert.That(properties["blendShape.shape16"], Is.EqualTo(AnimationFloatProperty.ConstPartially(100, null)));
            //Assert.That(properties["blendShape.shape17"], Is.EqualTo(AnimationProperty.ConstAlways(100, null)));
        }

        [Test]
        public void TestOneLayerOverrides()
        {
            var parser = new AnimatorParser(true);
            var controller = TestUtils.GetAssetAt<RuntimeAnimatorController>("AnimatorParser/OneLayerOverrideController.overrideController");
            var animate0To100 = TestUtils.GetAssetAt<AnimationClip>("AnimatorParser/Animate0To100.anim");
            var animate1To100 = TestUtils.GetAssetAt<AnimationClip>("AnimatorParser/Animate1To100.anim");
            var (original, mapping) = parser.GetControllerAndOverrides(controller);

            Assert.That(original, Is.EqualTo(_controller));
            Assert.That(mapping, Is.EquivalentTo(new []
            {
                new KeyValuePair<AnimationClip, AnimationClip>(animate0To100, animate1To100),
            }));
        }

        [Test]
        public void TestTwoLayerOverrides()
        {
            var parser = new AnimatorParser(true);
            var controller = TestUtils.GetAssetAt<RuntimeAnimatorController>("AnimatorParser/TwoLayerOverrideController.overrideController");
            var animate0To100 = TestUtils.GetAssetAt<AnimationClip>("AnimatorParser/Animate0To100.anim");
            var animate1To100 = TestUtils.GetAssetAt<AnimationClip>("AnimatorParser/Animate1To100.anim");
            var animate1ToVariable = TestUtils.GetAssetAt<AnimationClip>("AnimatorParser/Animate1ToVariable.anim");
            var animate2ToVariable = TestUtils.GetAssetAt<AnimationClip>("AnimatorParser/Animate2ToVariable.anim");
            var (original, mapping) = parser.GetControllerAndOverrides(controller);

            Assert.That(original, Is.EqualTo(_controller));
            Assert.That(mapping, Is.EquivalentTo(new []
            {
                new KeyValuePair<AnimationClip, AnimationClip>(animate0To100, animate2ToVariable),
                new KeyValuePair<AnimationClip, AnimationClip>(animate1ToVariable, animate2ToVariable),
                new KeyValuePair<AnimationClip, AnimationClip>(animate1To100, animate2ToVariable),
            }));
        }

        private void LayerTest(int layerIndex, string layerName,
            string propertyName, AnimationFloatProperty property,
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

        private void AssertContainer(IModificationsContainer parsed, string prop, AnimationFloatProperty property)
        {
            Assert.That(parsed.ModifiedProperties.Keys,
                Is.EquivalentTo(new[] { (ComponentOrGameObject)_skinnedRenderer }));
            var properties = parsed.ModifiedProperties[_skinnedRenderer].FloatProperties;
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
                        .AddModificationAsNewLayer(property, AnimationFloatProperty.ConstAlways(constantValue, null));
                    _clips[clip] = mods.ToImmutable();
                }

                foreach (var (animName, property) in AnimationTest.VariableSourceAnimations())
                {
                    var clip = TestUtils.GetAssetAt<AnimationClip>($"AnimatorParser/{animName}.anim");
                    var mods = new ModificationsContainer();
                    mods.ModifyObject(skinnedRenderer)
                        .AddModificationAsNewLayer(property, AnimationFloatProperty.Variable(null));
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