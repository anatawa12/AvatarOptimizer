using Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorOptimizer
{
    /// <summary>
    /// Tests for Greater/Less condition support in Entry/Exit to BlendTree optimization
    /// </summary>
    public class GreaterLessConditionTest : AnimatorOptimizerTestBase
    {
        private AnimatorOptimizerState _state = new AnimatorOptimizerState();

        /// <summary>
        /// Test that a state machine with Greater/Less exit conditions can be converted
        /// </summary>
        [Test]
        public void TestGreaterLessExitConditions()
        {
            // Create a simple animator controller programmatically
            var controller = new AnimatorController();
            controller.name = "TestGreaterLessController";

            // Add int parameter
            controller.AddParameter("TestParam", AnimatorControllerParameterType.Int);

            // Create layer
            var layer = controller.layers[0];
            var stateMachine = layer.stateMachine;

            // Create animations
            var anim1 = new AnimationClip { name = "Anim1" };
            var anim2 = new AnimationClip { name = "Anim2" };
            var anim3 = new AnimationClip { name = "Anim3" };
            var defaultAnim = new AnimationClip { name = "DefaultAnim" };

            // Create states
            var defaultState = stateMachine.AddState("Default");
            defaultState.motion = defaultAnim;
            defaultState.writeDefaultValues = true;

            var state1 = stateMachine.AddState("State1");
            state1.motion = anim1;
            state1.writeDefaultValues = true;

            var state2 = stateMachine.AddState("State2");
            state2.motion = anim2;
            state2.writeDefaultValues = true;

            var state3 = stateMachine.AddState("State3");
            state3.motion = anim3;
            state3.writeDefaultValues = true;

            stateMachine.defaultState = defaultState;

            // Add entry transitions
            var entry1 = stateMachine.AddEntryTransition(state1);
            entry1.AddCondition(AnimatorConditionMode.Equals, 1, "TestParam");

            var entry2 = stateMachine.AddEntryTransition(state2);
            entry2.AddCondition(AnimatorConditionMode.Equals, 2, "TestParam");

            var entry3 = stateMachine.AddEntryTransition(state3);
            entry3.AddCondition(AnimatorConditionMode.Equals, 3, "TestParam");

            // Add exit transitions using Greater/Less (new feature being tested)
            // State1 should exit when TestParam < 1 or TestParam > 1
            // Use separate transitions for OR logic
            var exit1Less = state1.AddExitTransition();
            exit1Less.hasExitTime = false;
            exit1Less.duration = 0;
            exit1Less.AddCondition(AnimatorConditionMode.Less, 1, "TestParam");

            var exit1Greater = state1.AddExitTransition();
            exit1Greater.hasExitTime = false;
            exit1Greater.duration = 0;
            exit1Greater.AddCondition(AnimatorConditionMode.Greater, 1, "TestParam");

            // State2 should exit when TestParam < 2 or TestParam > 2
            var exit2Less = state2.AddExitTransition();
            exit2Less.hasExitTime = false;
            exit2Less.duration = 0;
            exit2Less.AddCondition(AnimatorConditionMode.Less, 2, "TestParam");

            var exit2Greater = state2.AddExitTransition();
            exit2Greater.hasExitTime = false;
            exit2Greater.duration = 0;
            exit2Greater.AddCondition(AnimatorConditionMode.Greater, 2, "TestParam");

            // State3 should exit when TestParam < 3 or TestParam > 3
            var exit3Less = state3.AddExitTransition();
            exit3Less.hasExitTime = false;
            exit3Less.duration = 0;
            exit3Less.AddCondition(AnimatorConditionMode.Less, 3, "TestParam");

            var exit3Greater = state3.AddExitTransition();
            exit3Greater.hasExitTime = false;
            exit3Greater.duration = 0;
            exit3Greater.AddCondition(AnimatorConditionMode.Greater, 3, "TestParam");

            // Add default state exit transitions
            var defaultExit = defaultState.AddExitTransition();
            defaultExit.hasExitTime = false;
            defaultExit.duration = 0;
            defaultExit.AddCondition(AnimatorConditionMode.Equals, 1, "TestParam");
            defaultExit.AddCondition(AnimatorConditionMode.Equals, 2, "TestParam");
            defaultExit.AddCondition(AnimatorConditionMode.Equals, 3, "TestParam");

            // Execute the optimization
            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));

            // Verify that the optimization was applied (state machine reduced to 1 state with blend tree)
            Assert.That(stateMachine.states.Length, Is.EqualTo(1), 
                "State machine should be optimized to a single state with blend tree");
            Assert.That(stateMachine.states[0].state.motion, Is.InstanceOf<BlendTree>(),
                "The single state should contain a BlendTree");
        }

        /// <summary>
        /// Test that a state machine with only Greater condition can be converted
        /// </summary>
        [Test]
        public void TestOnlyGreaterExitCondition()
        {
            // Create a simple animator controller
            var controller = new AnimatorController();
            controller.name = "TestGreaterController";

            // Add int parameter
            controller.AddParameter("TestParam", AnimatorControllerParameterType.Int);

            // Create layer
            var layer = controller.layers[0];
            var stateMachine = layer.stateMachine;

            // Create animations
            var anim1 = new AnimationClip { name = "Anim1" };
            var defaultAnim = new AnimationClip { name = "DefaultAnim" };

            // Create states
            var defaultState = stateMachine.AddState("Default");
            defaultState.motion = defaultAnim;
            defaultState.writeDefaultValues = true;

            var state1 = stateMachine.AddState("State1");
            state1.motion = anim1;
            state1.writeDefaultValues = true;

            stateMachine.defaultState = defaultState;

            // Add entry transition
            var entry1 = stateMachine.AddEntryTransition(state1);
            entry1.AddCondition(AnimatorConditionMode.Equals, 1, "TestParam");

            // Add exit transition using only Greater
            var exit1 = state1.AddExitTransition();
            exit1.hasExitTime = false;
            exit1.duration = 0;
            exit1.AddCondition(AnimatorConditionMode.Greater, 1, "TestParam");

            // Add default state exit transition
            var defaultExit = defaultState.AddExitTransition();
            defaultExit.hasExitTime = false;
            defaultExit.duration = 0;
            defaultExit.AddCondition(AnimatorConditionMode.Equals, 1, "TestParam");

            // Execute the optimization
            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));

            // Verify that the optimization was applied
            Assert.That(stateMachine.states.Length, Is.EqualTo(1),
                "State machine should be optimized to a single state with blend tree");
        }

        /// <summary>
        /// Test that a state machine with only Less condition can be converted
        /// </summary>
        [Test]
        public void TestOnlyLessExitCondition()
        {
            // Create a simple animator controller
            var controller = new AnimatorController();
            controller.name = "TestLessController";

            // Add int parameter
            controller.AddParameter("TestParam", AnimatorControllerParameterType.Int);

            // Create layer
            var layer = controller.layers[0];
            var stateMachine = layer.stateMachine;

            // Create animations
            var anim1 = new AnimationClip { name = "Anim1" };
            var defaultAnim = new AnimationClip { name = "DefaultAnim" };

            // Create states
            var defaultState = stateMachine.AddState("Default");
            defaultState.motion = defaultAnim;
            defaultState.writeDefaultValues = true;

            var state1 = stateMachine.AddState("State1");
            state1.motion = anim1;
            state1.writeDefaultValues = true;

            stateMachine.defaultState = defaultState;

            // Add entry transition
            var entry1 = stateMachine.AddEntryTransition(state1);
            entry1.AddCondition(AnimatorConditionMode.Equals, 1, "TestParam");

            // Add exit transition using only Less
            var exit1 = state1.AddExitTransition();
            exit1.hasExitTime = false;
            exit1.duration = 0;
            exit1.AddCondition(AnimatorConditionMode.Less, 1, "TestParam");

            // Add default state exit transition
            var defaultExit = defaultState.AddExitTransition();
            defaultExit.hasExitTime = false;
            defaultExit.duration = 0;
            defaultExit.AddCondition(AnimatorConditionMode.Equals, 1, "TestParam");

            // Execute the optimization
            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));

            // Verify that the optimization was applied
            Assert.That(stateMachine.states.Length, Is.EqualTo(1),
                "State machine should be optimized to a single state with blend tree");
        }
    }
}
