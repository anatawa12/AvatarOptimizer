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
    }
}
