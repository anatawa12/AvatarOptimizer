using Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorOptimizer
{
    public class MergeBlendTreeLayerTest : AnimatorOptimizerTestBase
    {
        [Test]
        public void Merge()
        {
            var controller = LoadCloneAnimatorController("ConvertibleSimple");
            controller.name = "ConvertibleSimple.converted";
            MergeBlendTreeLayer.Execute(new AnimatorOptimizerState(), new AOAnimatorController(controller));
            var except = LoadAnimatorController("ConvertibleSimple.converted");
            RecursiveCheckEquals(except, controller);
        }
        
        [Test]
        public void BlockedCompletely()
        {
            var controller = LoadCloneAnimatorController("BlockedCompletely");
            controller.name = "BlockedCompletely.converted";
            MergeBlendTreeLayer.Execute(new AnimatorOptimizerState(), new AOAnimatorController(controller));
            var except = LoadAnimatorController("BlockedCompletely.converted");
            RecursiveCheckEquals(except, controller);
        }
        
        [Test]
        public void BlockedPartially()
        {
            var controller = LoadCloneAnimatorController("BlockedPartially");
            controller.name = "BlockedPartially.converted";
            MergeBlendTreeLayer.Execute(new AnimatorOptimizerState(), new AOAnimatorController(controller));
            var except = LoadAnimatorController("BlockedPartially.converted");
            RecursiveCheckEquals(except, controller);
        }
        
        [Test]
        public void NormalizeBlendValues()
        {
            var controller = LoadCloneAnimatorController("NormalizeBlendValues");
            controller.name = "NormalizeBlendValues.converted";
            MergeBlendTreeLayer.Execute(new AnimatorOptimizerState(), new AOAnimatorController(controller), "AlwaysOneTestParam");
            var except = LoadAnimatorController("NormalizeBlendValues.converted");
            RecursiveCheckEquals(except, controller);
        }

        #region state settings and time dependent clips
        // https://github.com/anatawa12/AvatarOptimizer/issues/1829

        // Those settings are per-state settings and cannot be kept in merged state so such layers must not be merged
        [TestCase("mirror")]
        [TestCase("mirrorParameterActive")]
        [TestCase("iKOnFeet")]
        // Merging to Direct BlendTree changes playback speed of time dependent clips
        [TestCase("timeDependent")]
        [TestCase("timeDependent+speedParameterActive")]
        public void NotMergedLayer(string setting)
        {
            var controller = CreateThreeDirectBlendTreeLayers(out var states,
                timeDependentMiddleLayer: setting.StartsWith("timeDependent"));
            var middleState = states[1];
            ApplySetting(middleState, setting);

            var aoController = new AOAnimatorController(controller);
            MergeBlendTreeLayer.Execute(new AnimatorOptimizerState(), aoController);

            // the middle layer is kept as is
            var middleLayer = aoController.layers[1];
            Assert.That(middleLayer.name, Is.EqualTo("Layer 1"));
            Assert.That(middleLayer.stateMachine.states.Length, Is.EqualTo(1));
            Assert.That(middleLayer.stateMachine.states[0].state, Is.SameAs(middleState));
            Assert.That(middleLayer.stateMachine.defaultState, Is.SameAs(middleState));

            // other layers are merged
            Assert.That(aoController.layers[0].stateMachine.states, Is.Empty);
            var mergedLayer = aoController.layers[2];
            Assert.That(mergedLayer.name, Is.EqualTo("AAO BlendTree MergedLayer"));
            var mergedTree = (BlendTree)mergedLayer.stateMachine.states[0].state.motion;
            Assert.That(mergedTree.children.Length, Is.EqualTo(2));
        }

        [TestCase("default")]
        // speed and cycle offset do not affect time independent clips so we can merge them
        [TestCase("speed")]
        [TestCase("speedParameterActive")]
        [TestCase("cycleOffset")]
        [TestCase("cycleOffsetParameterActive")]
        public void MergedLayer(string setting)
        {
            var controller = CreateThreeDirectBlendTreeLayers(out var states, timeDependentMiddleLayer: false);
            ApplySetting(states[1], setting);

            var aoController = new AOAnimatorController(controller);
            MergeBlendTreeLayer.Execute(new AnimatorOptimizerState(), aoController);

            Assert.That(aoController.layers[0].stateMachine.states, Is.Empty);
            Assert.That(aoController.layers[1].stateMachine.states, Is.Empty);
            var mergedLayer = aoController.layers[2];
            Assert.That(mergedLayer.name, Is.EqualTo("AAO BlendTree MergedLayer"));
            var mergedTree = (BlendTree)mergedLayer.stateMachine.states[0].state.motion;
            Assert.That(mergedTree.children.Length, Is.EqualTo(3));
        }

        private static void ApplySetting(AnimatorState state, string setting)
        {
            foreach (var part in setting.Split('+'))
            {
                switch (part)
                {
                    case "default":
                    case "timeDependent":
                        break;
                    case "speed":
                        state.speed = 2f;
                        break;
                    case "speedParameterActive":
                        state.speedParameterActive = true;
                        state.speedParameter = "Param";
                        break;
                    case "cycleOffset":
                        state.cycleOffset = 0.5f;
                        break;
                    case "cycleOffsetParameterActive":
                        state.cycleOffsetParameterActive = true;
                        state.cycleOffsetParameter = "Param";
                        break;
                    case "mirror":
                        state.mirror = true;
                        break;
                    case "mirrorParameterActive":
                        state.mirrorParameterActive = true;
                        state.mirrorParameter = "BoolParam";
                        break;
                    case "iKOnFeet":
                        state.iKOnFeet = true;
                        break;
                    default:
                        Assert.Fail($"unknown setting: {part}");
                        break;
                }
            }
        }

        /// <summary>
        /// Creates controller with three layers which have single state with Direct BlendTree.
        /// Each layer animates different property.
        /// </summary>
        private static AnimatorController CreateThreeDirectBlendTreeLayers(out AnimatorState[] states,
            bool timeDependentMiddleLayer)
        {
            var controller = new AnimatorController { name = "StateSettings" };
            controller.AddParameter("Param", AnimatorControllerParameterType.Float);
            controller.AddParameter("BoolParam", AnimatorControllerParameterType.Bool);

            states = new AnimatorState[3];
            for (var i = 0; i < states.Length; i++)
            {
                var clip = new AnimationClip { name = $"Clip {i}" };
                AnimationUtility.SetEditorCurve(clip,
                    EditorCurveBinding.FloatCurve($"Object{i}", typeof(GameObject), "m_IsActive"),
                    i == 1 && timeDependentMiddleLayer
                        ? AnimationCurve.Linear(0, 0, 1, 1)
                        : AnimationCurve.Constant(0, 1, 1));

                var blendTree = new BlendTree
                {
                    name = $"BlendTree {i}",
                    blendType = BlendTreeType.Direct,
                    children = new[]
                    {
                        new ChildMotion { motion = clip, directBlendParameter = "Param", timeScale = 1 },
                    },
                };

                var layer = new AnimatorControllerLayer
                {
                    name = $"Layer {i}",
                    defaultWeight = 1.0f,
                    stateMachine = new AnimatorStateMachine
                    {
                        name = $"Layer {i}",
                        hideFlags = HideFlags.HideInHierarchy,
                    },
                };

                var state = layer.stateMachine.AddState("State");
                state.motion = blendTree;
                state.writeDefaultValues = true;
                layer.stateMachine.defaultState = state;
                states[i] = state;

                controller.AddLayer(layer);
            }

            return controller;
        }

        #endregion
    }
}
