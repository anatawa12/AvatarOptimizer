using Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

#if AAO_VRCSDK3_AVATARS
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
#endif

namespace Anatawa12.AvatarOptimizer.Test.AnimatorOptimizer
{
    public class EntryExitToBlendTreeTest : AnimatorOptimizerTestBase
    {
        private AnimatorOptimizerState _state = new AnimatorOptimizerState();

        [Test]
        public void GestureConvertibleSimple()
        {
            var controller = LoadCloneAnimatorController("GestureConvertibleSimple");
            controller.name = "GestureConvertibleSimple.converted";
            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));
            var except = LoadAnimatorController("GestureConvertibleSimple.converted");
            RecursiveCheckEquals(except, controller);
        }

        [Test]
        public void GestureConvertibleWithIntOrBoolCondition()
        {
            var controller = LoadCloneAnimatorController("GestureConvertibleWithIntOrBoolCondition");
            controller.name = "GestureConvertibleWithIntOrBoolCondition.converted";
            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));
            var except = LoadAnimatorController("GestureConvertibleWithIntOrBoolCondition.converted");
            RecursiveCheckEquals(except, controller);
        }

        [Test]
        public void GestureNonConvertibleBecauseCurve()
        {
            var controller = LoadCloneAnimatorController("GestureNonConvertibleBecauseCurve");
            controller.name = "GestureNonConvertibleBecauseCurve";
            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));
            var except = LoadAnimatorController("GestureNonConvertibleBecauseCurve");
            RecursiveCheckEquals(except, controller);
        }

        [Test]
        public void LinearToggleConvertible()
        {
            var controller = LoadCloneAnimatorController("LinearToggleConvertible");
            controller.name = "LinearToggleConvertible.converted";
            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));
            var except = LoadAnimatorController("LinearToggleConvertible.converted");
            RecursiveCheckEquals(except, controller);
        }

        [Test]
        public void LinearToggleNonConvertibleBecauseCurve()
        {
            var controller = LoadCloneAnimatorController("LinearToggleNonConvertibleBecauseCurve");
            controller.name = "LinearToggleNonConvertibleBecauseCurve";
            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));
            var except = LoadAnimatorController("LinearToggleNonConvertibleBecauseCurve");
            RecursiveCheckEquals(except, controller);
        }

        // https://github.com/anatawa12/AvatarOptimizer/issues/1505
        // IndexOutOfRangeException at AnimatorOptimizer with empty layers
        [Test]
        public void EmptyLayerNoErrors()
        {
            var controller = LoadCloneAnimatorController("EmptyLayer");
            controller.name = "EmptyLayer";
            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));
            var except = LoadAnimatorController("EmptyLayer");
            RecursiveCheckEquals(except, controller);
        }

#if AAO_VRCSDK3_AVATARS
        // https://github.com/anatawa12/AvatarOptimizer/issues/1509 (from MA #1725, NDMF #693)
        // Parameter drivers need correction when parameter types change from bool/int to float
        [Test]
        public void ParameterDriverCorrectionForRandomBool()
        {
            var controller = new AnimatorController();
            controller.name = "ParameterDriverTest";
            
            // Add a bool parameter that will be converted to float
            controller.AddParameter("TestBool", AnimatorControllerParameterType.Bool);
            
            // Create a layer with a diamond pattern that will trigger conversion (no behaviors)
            var convertibleLayer = new AnimatorControllerLayer
            {
                name = "ConvertibleLayer",
                defaultWeight = 1.0f,
                stateMachine = new AnimatorStateMachine
                {
                    name = "ConvertibleLayer",
                    hideFlags = HideFlags.HideInHierarchy
                }
            };
            
            // Create states for the diamond pattern
            var defaultState = convertibleLayer.stateMachine.AddState("DefaultState");
            var trueState = convertibleLayer.stateMachine.AddState("TrueState");
            
            // Set up the diamond pattern: Entry -> states, states -> Exit
            convertibleLayer.stateMachine.defaultState = defaultState;
            
            // Entry transitions
            convertibleLayer.stateMachine.AddEntryTransition(trueState)
                .AddCondition(AnimatorConditionMode.If, 0, "TestBool");
            
            // Exit transitions
            var transition = defaultState.AddExitTransition(defaultExitTime: false);
            transition.duration = 0.0f;
            transition.AddCondition(AnimatorConditionMode.If, 0, "TestBool");

            transition = trueState.AddExitTransition(defaultExitTime: false);
            transition.duration = 0.0f;
            transition.AddCondition(AnimatorConditionMode.IfNot, 0, "TestBool");
            
            // Set motions to empty clips
            defaultState.motion = new AnimationClip { name = "DefaultClip" };
            trueState.motion = new AnimationClip { name = "TrueClip" };
            
            controller.AddLayer(convertibleLayer);
            
            // Create a separate layer with a parameter driver (will not be converted)
            var driverLayer = new AnimatorControllerLayer
            {
                name = "DriverLayer",
                defaultWeight = 1.0f,
                stateMachine = new AnimatorStateMachine
                {
                    name = "DriverLayer",
                    hideFlags = HideFlags.HideInHierarchy
                }
            };
            
            var driverState = driverLayer.stateMachine.AddState("StateWithDriver");
            driverLayer.stateMachine.defaultState = driverState;
            driverState.motion = new AnimationClip { name = "DriverClip" };
            
            // Add a parameter driver to the driver layer state
            var driver = driverState.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            driver.parameters = new System.Collections.Generic.List<VRC_AvatarParameterDriver.Parameter>
            {
                new VRC_AvatarParameterDriver.Parameter
                {
                    name = "TestBool",
                    type = VRC_AvatarParameterDriver.ChangeType.Random,
                    valueMin = 0,
                    valueMax = 1
                }
            };
            
            controller.AddLayer(driverLayer);
            
            // Execute the optimization
            var aoController = new AOAnimatorController(controller);
            EntryExitToBlendTree.Execute(_state, aoController);
            
            // Verify the parameter was converted to float
            var parameters = controller.parameters;
            Assert.IsTrue(parameters.Length >= 1, "Should have at least the original parameter");
            var testBoolParam = System.Array.Find(parameters, p => p.name == "TestBool");
            Assert.IsNotNull(testBoolParam, "TestBool parameter should still exist");
            Assert.AreEqual(AnimatorControllerParameterType.Float, testBoolParam.type, 
                "TestBool should be converted to Float");
            
            // Verify that a temporary parameter was added
            var tempParams = System.Array.FindAll(parameters, p => p.name.StartsWith("__AAO_tmp_TestBool_"));
            Assert.AreEqual(1, tempParams.Length, 
                "Should have exactly one temporary parameter for TestBool");
            Assert.AreEqual(AnimatorControllerParameterType.Bool, tempParams[0].type,
                "Temporary parameter should be Bool type");
            
            // Verify the parameter driver was modified
            Assert.AreEqual(2, driver.parameters.Count, 
                "Driver should have 2 parameters (temp + copy)");
            Assert.AreEqual(tempParams[0].name, driver.parameters[0].name,
                "First parameter should set the temp parameter");
            Assert.AreEqual(VRC_AvatarParameterDriver.ChangeType.Random, driver.parameters[0].type,
                "First parameter should still be Random type");
            Assert.AreEqual("TestBool", driver.parameters[1].name,
                "Second parameter should target TestBool");
            Assert.AreEqual(VRC_AvatarParameterDriver.ChangeType.Copy, driver.parameters[1].type,
                "Second parameter should be Copy type");
            Assert.AreEqual(tempParams[0].name, driver.parameters[1].source,
                "Second parameter should copy from temp parameter");
            
            // Cleanup
            Object.DestroyImmediate(controller);
        }
#endif

        #region Issue861

        // https://github.com/anatawa12/AvatarOptimizer/issues/861
        // Test conversion with time-dependent clips WITH motion time parameter (should convert)
        [Test]
        public void GestureConvertibleWithMotionTime()
        {
            var controller = new AnimatorController();
            controller.name = "GestureConvertibleWithMotionTime";
            
            // Add Gesture parameter
            controller.AddParameter("GestureLeft", AnimatorControllerParameterType.Int);
            controller.AddParameter("MotionTime", AnimatorControllerParameterType.Float);
            
            // Create time-dependent animation clips
            var timeDependentClip0 = CreateTimeDependentClip("TimeDependentClip0");
            var timeDependentClip1 = CreateTimeDependentClip("TimeDependentClip1");
            
            // Create a diamond pattern layer with motion time
            var layer = new AnimatorControllerLayer
            {
                name = "GestureLayer",
                defaultWeight = 1.0f,
                stateMachine = new AnimatorStateMachine
                {
                    name = "GestureLayer",
                    hideFlags = HideFlags.HideInHierarchy
                }
            };
            
            // Create states
            var defaultState = layer.stateMachine.AddState("Idle");
            defaultState.motion = timeDependentClip0;
            defaultState.timeParameterActive = true;
            defaultState.timeParameter = "MotionTime";
            defaultState.writeDefaultValues = true;
            
            var fistState = layer.stateMachine.AddState("Fist");
            fistState.motion = timeDependentClip1;
            fistState.timeParameterActive = true;
            fistState.timeParameter = "MotionTime";
            fistState.writeDefaultValues = true;
            
            layer.stateMachine.defaultState = defaultState;
            
            // Entry transitions
            layer.stateMachine.AddEntryTransition(fistState)
                .AddCondition(AnimatorConditionMode.Equals, 1, "GestureLeft");
            
            // Exit transitions
            var exitTransition = defaultState.AddExitTransition(defaultExitTime: false);
            exitTransition.duration = 0.0f;
            exitTransition.AddCondition(AnimatorConditionMode.Equals, 1, "GestureLeft");
            
            exitTransition = fistState.AddExitTransition(defaultExitTime: false);
            exitTransition.duration = 0.0f;
            exitTransition.AddCondition(AnimatorConditionMode.NotEqual, 1, "GestureLeft");
            
            controller.AddLayer(layer);
            
            // Execute optimization
            var aoController = new AOAnimatorController(controller);
            EntryExitToBlendTree.Execute(_state, aoController);
            
            // Verify conversion happened (layer should now have a blend tree)
            var convertedLayer = controller.layers[0];
            Assert.AreEqual(1, convertedLayer.stateMachine.states.Length, 
                "Should have one state after conversion");
            
            var blendTreeState = convertedLayer.stateMachine.states[0].state;
            Assert.IsTrue(blendTreeState.timeParameterActive, 
                "BlendTree state should have timeParameterActive = true");
            Assert.AreEqual("MotionTime", blendTreeState.timeParameter,
                "BlendTree state should have timeParameter = MotionTime");
            Assert.IsInstanceOf<BlendTree>(blendTreeState.motion,
                "State motion should be a BlendTree");
            
            // Cleanup
            Object.DestroyImmediate(timeDependentClip0);
            Object.DestroyImmediate(timeDependentClip1);
            Object.DestroyImmediate(controller);
        }

        // https://github.com/anatawa12/AvatarOptimizer/issues/861
        // Test conversion with time-dependent clips WITHOUT motion time parameter (should NOT convert)
        [Test]
        public void GestureNonConvertibleBecauseTimeDependentWithoutMotionTime()
        {
            var controller = new AnimatorController();
            controller.name = "GestureNonConvertibleBecauseTimeDependentWithoutMotionTime";
            
            // Add Gesture parameter
            controller.AddParameter("GestureLeft", AnimatorControllerParameterType.Int);
            
            // Create time-dependent animation clips
            var timeDependentClip0 = CreateTimeDependentClip("TimeDependentClip0");
            var timeDependentClip1 = CreateTimeDependentClip("TimeDependentClip1");
            
            // Create a diamond pattern layer WITHOUT motion time
            var layer = new AnimatorControllerLayer
            {
                name = "GestureLayer",
                defaultWeight = 1.0f,
                stateMachine = new AnimatorStateMachine
                {
                    name = "GestureLayer",
                    hideFlags = HideFlags.HideInHierarchy
                }
            };
            
            // Create states WITHOUT timeParameterActive
            var defaultState = layer.stateMachine.AddState("Idle");
            defaultState.motion = timeDependentClip0;
            defaultState.timeParameterActive = false;  // No motion time parameter
            defaultState.writeDefaultValues = true;
            
            var fistState = layer.stateMachine.AddState("Fist");
            fistState.motion = timeDependentClip1;
            fistState.timeParameterActive = false;  // No motion time parameter
            fistState.writeDefaultValues = true;
            
            layer.stateMachine.defaultState = defaultState;
            
            // Entry transitions
            layer.stateMachine.AddEntryTransition(fistState)
                .AddCondition(AnimatorConditionMode.Equals, 1, "GestureLeft");
            
            // Exit transitions
            var exitTransition = defaultState.AddExitTransition(defaultExitTime: false);
            exitTransition.duration = 0.0f;
            exitTransition.AddCondition(AnimatorConditionMode.Equals, 1, "GestureLeft");
            
            exitTransition = fistState.AddExitTransition(defaultExitTime: false);
            exitTransition.duration = 0.0f;
            exitTransition.AddCondition(AnimatorConditionMode.NotEqual, 1, "GestureLeft");
            
            controller.AddLayer(layer);
            
            // Store original structure for comparison
            var originalStateCount = controller.layers[0].stateMachine.states.Length;
            
            // Execute optimization
            var aoController = new AOAnimatorController(controller);
            EntryExitToBlendTree.Execute(_state, aoController);
            
            // Verify NO conversion happened (should still have 2 states, not blend tree)
            var convertedLayer = controller.layers[0];
            Assert.AreEqual(originalStateCount, convertedLayer.stateMachine.states.Length,
                "Should NOT convert - state count should remain the same");
            Assert.AreEqual(2, convertedLayer.stateMachine.states.Length,
                "Should still have 2 states (no conversion)");
            
            // Cleanup
            Object.DestroyImmediate(timeDependentClip0);
            Object.DestroyImmediate(timeDependentClip1);
            Object.DestroyImmediate(controller);
        }

        // https://github.com/anatawa12/AvatarOptimizer/issues/861
        // Test with linear toggle pattern WITH motion time parameter
        [Test]
        public void LinearToggleConvertibleWithMotionTime()
        {
            var controller = new AnimatorController();
            controller.name = "LinearToggleConvertibleWithMotionTime";
            
            // Add toggle parameter and motion time parameter
            controller.AddParameter("Toggle", AnimatorControllerParameterType.Bool);
            controller.AddParameter("MotionTime", AnimatorControllerParameterType.Float);
            
            // Create time-dependent animation clips
            var timeDependentClip0 = CreateTimeDependentClip("TimeDependentClip0");
            var timeDependentClip1 = CreateTimeDependentClip("TimeDependentClip1");
            
            // Create a linear pattern layer with motion time
            var layer = new AnimatorControllerLayer
            {
                name = "ToggleLayer",
                defaultWeight = 1.0f,
                stateMachine = new AnimatorStateMachine
                {
                    name = "ToggleLayer",
                    hideFlags = HideFlags.HideInHierarchy
                }
            };
            
            // Create states
            var offState = layer.stateMachine.AddState("Off");
            offState.motion = timeDependentClip0;
            offState.timeParameterActive = true;
            offState.timeParameter = "MotionTime";
            offState.writeDefaultValues = true;
            
            var onState = layer.stateMachine.AddState("On");
            onState.motion = timeDependentClip1;
            onState.timeParameterActive = true;
            onState.timeParameter = "MotionTime";
            onState.writeDefaultValues = true;
            
            layer.stateMachine.defaultState = offState;
            
            // Create linear transitions
            var toOnTransition = offState.AddTransition(onState);
            toOnTransition.hasExitTime = false;
            toOnTransition.duration = 0.0f;
            toOnTransition.AddCondition(AnimatorConditionMode.If, 0, "Toggle");

            // Exit transitions
            var exitTransition = onState.AddExitTransition(defaultExitTime: false);
            exitTransition.duration = 0.0f;
            exitTransition.AddCondition(AnimatorConditionMode.IfNot, 0, "Toggle");
            
            controller.AddLayer(layer);
            
            // Execute optimization
            var aoController = new AOAnimatorController(controller);
            EntryExitToBlendTree.Execute(_state, aoController);
            
            // Verify conversion happened
            var convertedLayer = controller.layers[0];
            Assert.AreEqual(1, convertedLayer.stateMachine.states.Length,
                "Should have one state after conversion");
            
            var blendTreeState = convertedLayer.stateMachine.states[0].state;
            Assert.IsTrue(blendTreeState.timeParameterActive,
                "BlendTree state should have timeParameterActive = true");
            Assert.AreEqual("MotionTime", blendTreeState.timeParameter,
                "BlendTree state should have timeParameter = MotionTime");
            Assert.IsInstanceOf<BlendTree>(blendTreeState.motion,
                "State motion should be a BlendTree");
            
            // Cleanup
            Object.DestroyImmediate(timeDependentClip0);
            Object.DestroyImmediate(timeDependentClip1);
            Object.DestroyImmediate(controller);
        }

        // https://github.com/anatawa12/AvatarOptimizer/issues/861
        // Test with mixed motion time parameters (should NOT convert)
        [Test]
        public void GestureNonConvertibleBecauseMixedMotionTimeParameters()
        {
            var controller = new AnimatorController();
            controller.name = "GestureNonConvertibleBecauseMixedMotionTimeParameters";
            
            // Add parameters
            controller.AddParameter("GestureLeft", AnimatorControllerParameterType.Int);
            controller.AddParameter("MotionTime1", AnimatorControllerParameterType.Float);
            controller.AddParameter("MotionTime2", AnimatorControllerParameterType.Float);
            
            // Create time-dependent animation clips
            var timeDependentClip0 = CreateTimeDependentClip("TimeDependentClip0");
            var timeDependentClip1 = CreateTimeDependentClip("TimeDependentClip1");
            
            // Create a diamond pattern layer with DIFFERENT motion time parameters
            var layer = new AnimatorControllerLayer
            {
                name = "GestureLayer",
                defaultWeight = 1.0f,
                stateMachine = new AnimatorStateMachine
                {
                    name = "GestureLayer",
                    hideFlags = HideFlags.HideInHierarchy
                }
            };
            
            // Create states with DIFFERENT timeParameter values
            var defaultState = layer.stateMachine.AddState("Idle");
            defaultState.motion = timeDependentClip0;
            defaultState.timeParameterActive = true;
            defaultState.timeParameter = "MotionTime1";  // Different parameter
            defaultState.writeDefaultValues = true;
            
            var fistState = layer.stateMachine.AddState("Fist");
            fistState.motion = timeDependentClip1;
            fistState.timeParameterActive = true;
            fistState.timeParameter = "MotionTime2";  // Different parameter
            fistState.writeDefaultValues = true;
            
            layer.stateMachine.defaultState = defaultState;
            
            // Entry transitions
            layer.stateMachine.AddEntryTransition(fistState)
                .AddCondition(AnimatorConditionMode.Equals, 1, "GestureLeft");
            
            // Exit transitions
            var exitTransition = defaultState.AddExitTransition(defaultExitTime: false);
            exitTransition.duration = 0.0f;
            exitTransition.AddCondition(AnimatorConditionMode.Equals, 1, "GestureLeft");
            
            exitTransition = fistState.AddExitTransition(defaultExitTime: false);
            exitTransition.duration = 0.0f;
            exitTransition.AddCondition(AnimatorConditionMode.Equals, 0, "GestureLeft");
            
            controller.AddLayer(layer);
            
            // Store original structure
            var originalStateCount = controller.layers[0].stateMachine.states.Length;
            
            // Execute optimization
            var aoController = new AOAnimatorController(controller);
            EntryExitToBlendTree.Execute(_state, aoController);
            
            // Verify NO conversion happened (mixed motion time parameters not allowed)
            var convertedLayer = controller.layers[0];
            Assert.AreEqual(originalStateCount, convertedLayer.stateMachine.states.Length,
                "Should NOT convert - state count should remain the same");
            Assert.AreEqual(2, convertedLayer.stateMachine.states.Length,
                "Should still have 2 states (no conversion due to mixed motion time parameters)");
            
            // Cleanup
            Object.DestroyImmediate(timeDependentClip0);
            Object.DestroyImmediate(timeDependentClip1);
            Object.DestroyImmediate(controller);
        }

        // Helper method to create a time-dependent animation clip
        private AnimationClip CreateTimeDependentClip(string name)
        {
            var clip = new AnimationClip { name = name };
            
            // Create a curve with time-dependent values (non-constant)
            var curve = new AnimationCurve();
            curve.AddKey(new Keyframe(0f, 0f, 0f, 1f));  // Value changes from 0 to 1
            curve.AddKey(new Keyframe(1f, 1f, 1f, 0f));
            
            // Add the curve to the clip
            clip.SetCurve("Body", typeof(GameObject), "m_IsActive", curve);
            
            return clip;
        }

        #endregion

        #region state settings
        // https://github.com/anatawa12/AvatarOptimizer/issues/1829

        // foot IK cannot be kept in the new state
        [TestCase("iKOnFeet", false)]
        [TestCase("iKOnFeet", true)]
        // mirror must be same for all states
        [TestCase("mirror", false)]
        [TestCase("mirrorParameterActive", false)]
        [TestCase("mirror", true)]
        [TestCase("mirrorParameterActive", true)]
        // cycle offset is added to motion time so it must be same for all states with motion time
        [TestCase("cycleOffset", true)]
        [TestCase("cycleOffsetParameterActive", true)]
        public void NonConvertibleBecauseStateSettings(string setting, bool motionTime)
        {
            var controller = CreateDiamondLayerForStateSettings(setting, motionTime);

            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));

            Assert.AreEqual(2, controller.layers[0].stateMachine.states.Length,
                "Should NOT convert - state count should remain the same");
        }

        [TestCase("default", false)]
        [TestCase("default", true)]
        // speed and cycle offset do not affect time independent clips
        [TestCase("speed", false)]
        [TestCase("speedParameterActive", false)]
        [TestCase("cycleOffset", false)]
        [TestCase("cycleOffsetParameterActive", false)]
        // speed is ignored when motion time is active
        [TestCase("speed", true)]
        [TestCase("speedParameterActive", true)]
        public void ConvertibleWithStateSettings(string setting, bool motionTime)
        {
            var controller = CreateDiamondLayerForStateSettings(setting, motionTime);

            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));

            var states = controller.layers[0].stateMachine.states;
            Assert.AreEqual(1, states.Length, "Should have one state after conversion");
            Assert.IsInstanceOf<BlendTree>(states[0].state.motion, "State motion should be a BlendTree");
            Assert.AreEqual(motionTime, states[0].state.timeParameterActive);
        }

        // mirror is copied to the new state if it is same for all states
        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void ConvertibleWithSameMirror(bool parameter, bool motionTime)
        {
            var controller = CreateDiamondLayerForStateSettings("default", motionTime);
            foreach (var childState in controller.layers[0].stateMachine.states)
            {
                if (parameter)
                {
                    childState.state.mirrorParameterActive = true;
                    childState.state.mirrorParameter = "BoolParam";
                }
                else
                {
                    childState.state.mirror = true;
                }
            }

            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));

            var states = controller.layers[0].stateMachine.states;
            Assert.AreEqual(1, states.Length, "Should have one state after conversion");
            var newState = states[0].state;
            Assert.IsInstanceOf<BlendTree>(newState.motion, "State motion should be a BlendTree");
            Assert.AreEqual(parameter, newState.mirrorParameterActive);
            if (parameter)
                Assert.AreEqual("BoolParam", newState.mirrorParameter);
            else
                Assert.IsTrue(newState.mirror);
        }

        [Test]
        public void NonConvertibleBecauseDifferentMirrorParameter()
        {
            var controller = CreateDiamondLayerForStateSettings("default", motionTime: false);
            controller.AddParameter("BoolParam2", AnimatorControllerParameterType.Bool);
            var states = controller.layers[0].stateMachine.states;
            states[0].state.mirrorParameterActive = true;
            states[0].state.mirrorParameter = "BoolParam";
            states[1].state.mirrorParameterActive = true;
            states[1].state.mirrorParameter = "BoolParam2";

            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));

            Assert.AreEqual(2, controller.layers[0].stateMachine.states.Length,
                "Should NOT convert - state count should remain the same");
        }

        // cycle offset is copied to the new state like motion time parameter if it is same for all states
        [TestCase(false)]
        [TestCase(true)]
        public void ConvertibleWithSameCycleOffsetWithMotionTime(bool parameter)
        {
            var controller = CreateDiamondLayerForStateSettings("default", motionTime: true);
            foreach (var childState in controller.layers[0].stateMachine.states)
            {
                if (parameter)
                {
                    childState.state.cycleOffsetParameterActive = true;
                    childState.state.cycleOffsetParameter = "FloatParam";
                }
                else
                {
                    childState.state.cycleOffset = 0.25f;
                }
            }

            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));

            var states = controller.layers[0].stateMachine.states;
            Assert.AreEqual(1, states.Length, "Should have one state after conversion");
            var newState = states[0].state;
            Assert.IsInstanceOf<BlendTree>(newState.motion, "State motion should be a BlendTree");
            Assert.IsTrue(newState.timeParameterActive);
            Assert.AreEqual("MotionTime", newState.timeParameter);
            Assert.AreEqual(parameter, newState.cycleOffsetParameterActive);
            if (parameter)
                Assert.AreEqual("FloatParam", newState.cycleOffsetParameter);
            else
                Assert.AreEqual(0.25f, newState.cycleOffset);
        }

        [TestCase("constant")]
        [TestCase("parameter")]
        [TestCase("constantAndParameter")]
        public void NonConvertibleBecauseDifferentCycleOffsetWithMotionTime(string kind)
        {
            var controller = CreateDiamondLayerForStateSettings("default", motionTime: true);
            var states = controller.layers[0].stateMachine.states;
            switch (kind)
            {
                case "constant":
                    states[0].state.cycleOffset = 0.25f;
                    states[1].state.cycleOffset = 0.5f;
                    break;
                case "parameter":
                    states[0].state.cycleOffsetParameterActive = true;
                    states[0].state.cycleOffsetParameter = "FloatParam";
                    states[1].state.cycleOffsetParameterActive = true;
                    states[1].state.cycleOffsetParameter = "MotionTime";
                    break;
                case "constantAndParameter":
                    states[0].state.cycleOffset = 0.25f;
                    states[1].state.cycleOffsetParameterActive = true;
                    states[1].state.cycleOffsetParameter = "FloatParam";
                    break;
            }

            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));

            Assert.AreEqual(2, controller.layers[0].stateMachine.states.Length,
                "Should NOT convert - state count should remain the same");
        }

        /// <summary>
        /// Creates diamond style layer, and applies the setting to the non-default state.
        /// With motion time, clips are time dependent. Without motion time, clips are time independent.
        /// </summary>
        private AnimatorController CreateDiamondLayerForStateSettings(string setting, bool motionTime)
        {
            var controller = new AnimatorController { name = "StateSettings" };
            controller.AddParameter("GestureLeft", AnimatorControllerParameterType.Int);
            controller.AddParameter("MotionTime", AnimatorControllerParameterType.Float);
            controller.AddParameter("FloatParam", AnimatorControllerParameterType.Float);
            controller.AddParameter("BoolParam", AnimatorControllerParameterType.Bool);

            AnimationClip CreateClip(string name)
            {
                if (motionTime) return CreateTimeDependentClip(name);
                var clip = new AnimationClip { name = name };
                clip.SetCurve("Body", typeof(GameObject), "m_IsActive", AnimationCurve.Constant(0, 1, 1));
                return clip;
            }

            var layer = new AnimatorControllerLayer
            {
                name = "GestureLayer",
                defaultWeight = 1.0f,
                stateMachine = new AnimatorStateMachine
                {
                    name = "GestureLayer",
                    hideFlags = HideFlags.HideInHierarchy
                }
            };

            var defaultState = layer.stateMachine.AddState("Idle");
            defaultState.motion = CreateClip("Clip0");
            defaultState.writeDefaultValues = true;

            var fistState = layer.stateMachine.AddState("Fist");
            fistState.motion = CreateClip("Clip1");
            fistState.writeDefaultValues = true;

            if (motionTime)
            {
                defaultState.timeParameterActive = true;
                defaultState.timeParameter = "MotionTime";
                fistState.timeParameterActive = true;
                fistState.timeParameter = "MotionTime";
            }

            switch (setting)
            {
                case "default":
                    break;
                case "speed":
                    fistState.speed = 2f;
                    break;
                case "speedParameterActive":
                    fistState.speedParameterActive = true;
                    fistState.speedParameter = "FloatParam";
                    break;
                case "cycleOffset":
                    fistState.cycleOffset = 0.5f;
                    break;
                case "cycleOffsetParameterActive":
                    fistState.cycleOffsetParameterActive = true;
                    fistState.cycleOffsetParameter = "FloatParam";
                    break;
                case "mirror":
                    fistState.mirror = true;
                    break;
                case "mirrorParameterActive":
                    fistState.mirrorParameterActive = true;
                    fistState.mirrorParameter = "BoolParam";
                    break;
                case "iKOnFeet":
                    fistState.iKOnFeet = true;
                    break;
                default:
                    Assert.Fail($"unknown setting: {setting}");
                    break;
            }

            layer.stateMachine.defaultState = defaultState;

            // Entry transitions
            layer.stateMachine.AddEntryTransition(fistState)
                .AddCondition(AnimatorConditionMode.Equals, 1, "GestureLeft");

            // Exit transitions
            var exitTransition = defaultState.AddExitTransition(defaultExitTime: false);
            exitTransition.duration = 0.0f;
            exitTransition.AddCondition(AnimatorConditionMode.Equals, 1, "GestureLeft");

            exitTransition = fistState.AddExitTransition(defaultExitTime: false);
            exitTransition.duration = 0.0f;
            exitTransition.AddCondition(AnimatorConditionMode.NotEqual, 1, "GestureLeft");

            controller.AddLayer(layer);
            return controller;
        }

        #endregion

        // https://github.com/anatawa12/AvatarOptimizer/issues/1697
        // Default value for animator must be preserved (converted) when converting non-float parameters to float
        // for entry/exit to blend tree, otherwise it can cause issues with animator parameters without 
        // VRC Expression Parameter (other default value sources).
        [Test]
        public void AnimatorDefaultValueMustBePreserved()
        {
            var controller = LoadCloneAnimatorController("AnimatorParameterDefaultValue");
            controller.name = "AnimatorParameterDefaultValue.converted";
            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));
            var except = LoadAnimatorController("AnimatorParameterDefaultValue.converted");
            RecursiveCheckEquals(except, controller);
        }
    }
}
