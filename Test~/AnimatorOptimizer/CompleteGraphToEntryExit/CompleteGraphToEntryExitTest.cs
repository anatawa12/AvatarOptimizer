using System.Linq;
using Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorOptimizer
{
    public class CompleteGraphToEntryExitTest : AnimatorOptimizerTestBase
    {
        private AnimatorOptimizerState _state = new AnimatorOptimizerState();

        [Test]
        public void SimpleCompleteGraph_TwoStates_Converts()
        {
            // Create a simple 2-state complete graph
            var controller = CreateSimpleCompleteGraphController();
            var originalLayerCount = controller.layers.Length;
            
            CompleteGraphToEntryExit.Execute(_state, new AOAnimatorController(controller));
            
            // Verify layer still exists
            Assert.AreEqual(originalLayerCount, controller.layers.Length);
            
            var layer = controller.layers[0];
            var stateMachine = layer.stateMachine;
            
            // Verify entry transitions were created (one per state)
            Assert.AreEqual(2, stateMachine.entryTransitions.Length, "Should have 2 entry transitions");
            
            // Verify each state has exit transitions
            foreach (var childState in stateMachine.states)
            {
                var exitTransitions = childState.state.transitions.Where(t => t.isExit).ToArray();
                Assert.GreaterOrEqual(exitTransitions.Length, 1, $"State {childState.state.name} should have exit transitions");
            }
        }

        [Test]
        public void CompleteGraphWithSelfTransitions_PreservesSelfTransitions()
        {
            var controller = CreateCompleteGraphWithSelfTransitionsController();
            
            CompleteGraphToEntryExit.Execute(_state, new AOAnimatorController(controller));
            
            var layer = controller.layers[0];
            var stateMachine = layer.stateMachine;
            var states = stateMachine.states;
            
            // Each state should have self-transitions preserved
            foreach (var childState in states)
            {
                var selfTransitions = childState.state.transitions
                    .Where(t => !t.isExit && t.destinationState == childState.state)
                    .ToArray();
                Assert.AreEqual(1, selfTransitions.Length, 
                    $"State {childState.state.name} should preserve its self-transition");
            }
        }

        [Test]
        public void IncompleteGraph_DoesNotConvert()
        {
            var controller = CreateIncompleteGraphController();
            var layer = controller.layers[0];
            var originalTransitionCount = layer.stateMachine.states[0].state.transitions.Length;
            
            CompleteGraphToEntryExit.Execute(_state, new AOAnimatorController(controller));
            
            // Should not convert - verify no entry transitions were added
            Assert.AreEqual(0, layer.stateMachine.entryTransitions.Length, 
                "Incomplete graph should not be converted");
            
            // Original transitions should remain unchanged
            Assert.AreEqual(originalTransitionCount, layer.stateMachine.states[0].state.transitions.Length);
        }

        [Test]
        public void DifferentTransitionSettings_DoesNotConvert()
        {
            var controller = CreateDifferentTransitionSettingsController();
            
            CompleteGraphToEntryExit.Execute(_state, new AOAnimatorController(controller));
            
            // Should not convert
            var layer = controller.layers[0];
            Assert.AreEqual(0, layer.stateMachine.entryTransitions.Length, 
                "Graph with different transition settings should not be converted");
        }

        [Test]
        public void HasChildStateMachine_DoesNotConvert()
        {
            var controller = CreateControllerWithChildStateMachine();
            
            CompleteGraphToEntryExit.Execute(_state, new AOAnimatorController(controller));
            
            // Should not convert
            var layer = controller.layers[0];
            Assert.AreEqual(0, layer.stateMachine.entryTransitions.Length, 
                "Layer with child state machine should not be converted");
        }

        [Test]
        public void DifferentConditionsForSameTarget_DoesNotConvert()
        {
            var controller = CreateDifferentConditionsForSameTargetController();
            
            CompleteGraphToEntryExit.Execute(_state, new AOAnimatorController(controller));
            
            // Should not convert
            var layer = controller.layers[0];
            Assert.AreEqual(0, layer.stateMachine.entryTransitions.Length, 
                "Graph with different conditions for same target should not be converted");
        }

        [Test]
        public void ThreeStateCompleteGraph_Converts()
        {
            var controller = CreateThreeStateCompleteGraphController();
            
            CompleteGraphToEntryExit.Execute(_state, new AOAnimatorController(controller));
            
            var layer = controller.layers[0];
            var stateMachine = layer.stateMachine;
            
            // Should have 3 entry transitions (one per state)
            Assert.AreEqual(3, stateMachine.entryTransitions.Length);
            
            // Each state should have exit transitions
            foreach (var childState in stateMachine.states)
            {
                var exitCount = childState.state.transitions.Count(t => t.isExit);
                Assert.GreaterOrEqual(exitCount, 1, 
                    $"State {childState.state.name} should have exit transitions");
            }
        }

        // Helper methods to create test controllers programmatically

        private AnimatorController CreateSimpleCompleteGraphController()
        {
            var controller = new AnimatorController { name = "SimpleCompleteGraph" };
            controller.AddParameter("StateA", AnimatorControllerParameterType.Bool);
            controller.AddParameter("StateB", AnimatorControllerParameterType.Bool);

            var layer = new AnimatorControllerLayer
            {
                name = "Base Layer",
                stateMachine = new AnimatorStateMachine { name = "Base Layer" }
            };

            var clipA = new AnimationClip { name = "ClipA" };
            var clipB = new AnimationClip { name = "ClipB" };

            var stateA = layer.stateMachine.AddState("StateA");
            stateA.motion = clipA;
            
            var stateB = layer.stateMachine.AddState("StateB");
            stateB.motion = clipB;

            // Create complete graph: A->B, B->A, A->A, B->B
            var transAB = stateA.AddTransition(stateB);
            transAB.hasExitTime = false;
            transAB.duration = 0.25f;
            transAB.AddCondition(AnimatorConditionMode.If, 0, "StateB");

            var transAA = stateA.AddTransition(stateA);
            transAA.hasExitTime = false;
            transAA.duration = 0.25f;
            transAA.AddCondition(AnimatorConditionMode.If, 0, "StateA");

            var transBA = stateB.AddTransition(stateA);
            transBA.hasExitTime = false;
            transBA.duration = 0.25f;
            transBA.AddCondition(AnimatorConditionMode.If, 0, "StateA");

            var transBB = stateB.AddTransition(stateB);
            transBB.hasExitTime = false;
            transBB.duration = 0.25f;
            transBB.AddCondition(AnimatorConditionMode.If, 0, "StateB");

            controller.AddLayer(layer);
            return controller;
        }

        private AnimatorController CreateCompleteGraphWithSelfTransitionsController()
        {
            var controller = new AnimatorController { name = "WithSelfTransitions" };
            controller.AddParameter("Param1", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Param2", AnimatorControllerParameterType.Bool);

            var layer = new AnimatorControllerLayer
            {
                name = "Base Layer",
                stateMachine = new AnimatorStateMachine { name = "Base Layer" }
            };

            var state1 = layer.stateMachine.AddState("State1");
            state1.motion = new AnimationClip { name = "Clip1" };
            
            var state2 = layer.stateMachine.AddState("State2");
            state2.motion = new AnimationClip { name = "Clip2" };

            // Complete graph with self-transitions
            var trans12 = state1.AddTransition(state2);
            trans12.hasExitTime = false;
            trans12.duration = 0.1f;
            trans12.AddCondition(AnimatorConditionMode.If, 0, "Param2");

            var self1 = state1.AddTransition(state1);
            self1.hasExitTime = false;
            self1.duration = 0.1f;
            self1.AddCondition(AnimatorConditionMode.If, 0, "Param1");

            var trans21 = state2.AddTransition(state1);
            trans21.hasExitTime = false;
            trans21.duration = 0.1f;
            trans21.AddCondition(AnimatorConditionMode.If, 0, "Param1");

            var self2 = state2.AddTransition(state2);
            self2.hasExitTime = false;
            self2.duration = 0.1f;
            self2.AddCondition(AnimatorConditionMode.If, 0, "Param2");

            controller.AddLayer(layer);
            return controller;
        }

        private AnimatorController CreateIncompleteGraphController()
        {
            var controller = new AnimatorController { name = "IncompleteGraph" };
            controller.AddParameter("Param1", AnimatorControllerParameterType.Bool);

            var layer = new AnimatorControllerLayer
            {
                name = "Base Layer",
                stateMachine = new AnimatorStateMachine { name = "Base Layer" }
            };

            var state1 = layer.stateMachine.AddState("State1");
            var state2 = layer.stateMachine.AddState("State2");

            // Incomplete: state1 only has self-transition, no transition to state2
            var self1 = state1.AddTransition(state1);
            self1.AddCondition(AnimatorConditionMode.If, 0, "Param1");

            controller.AddLayer(layer);
            return controller;
        }

        private AnimatorController CreateDifferentTransitionSettingsController()
        {
            var controller = new AnimatorController { name = "DifferentSettings" };
            controller.AddParameter("Param1", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Param2", AnimatorControllerParameterType.Bool);

            var layer = new AnimatorControllerLayer
            {
                name = "Base Layer",
                stateMachine = new AnimatorStateMachine { name = "Base Layer" }
            };

            var state1 = layer.stateMachine.AddState("State1");
            var state2 = layer.stateMachine.AddState("State2");

            // Complete graph but with different durations from state1
            var trans12 = state1.AddTransition(state2);
            trans12.hasExitTime = false;
            trans12.duration = 0.5f; // Different
            trans12.AddCondition(AnimatorConditionMode.If, 0, "Param2");

            var self1 = state1.AddTransition(state1);
            self1.hasExitTime = false;
            self1.duration = 0.1f; // Different
            self1.AddCondition(AnimatorConditionMode.If, 0, "Param1");

            var trans21 = state2.AddTransition(state1);
            trans21.hasExitTime = false;
            trans21.duration = 0.25f;
            trans21.AddCondition(AnimatorConditionMode.If, 0, "Param1");

            var self2 = state2.AddTransition(state2);
            self2.hasExitTime = false;
            self2.duration = 0.25f;
            self2.AddCondition(AnimatorConditionMode.If, 0, "Param2");

            controller.AddLayer(layer);
            return controller;
        }

        private AnimatorController CreateControllerWithChildStateMachine()
        {
            var controller = new AnimatorController { name = "HasChildStateMachine" };
            
            var layer = new AnimatorControllerLayer
            {
                name = "Base Layer",
                stateMachine = new AnimatorStateMachine { name = "Base Layer" }
            };

            layer.stateMachine.AddState("State1");
            layer.stateMachine.AddStateMachine("SubStateMachine"); // This makes it non-convertible

            controller.AddLayer(layer);
            return controller;
        }

        private AnimatorController CreateDifferentConditionsForSameTargetController()
        {
            var controller = new AnimatorController { name = "DifferentConditions" };
            controller.AddParameter("Param1", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Param2", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Param3", AnimatorControllerParameterType.Bool);

            var layer = new AnimatorControllerLayer
            {
                name = "Base Layer",
                stateMachine = new AnimatorStateMachine { name = "Base Layer" }
            };

            var state1 = layer.stateMachine.AddState("State1");
            var state2 = layer.stateMachine.AddState("State2");

            // State1 -> State2 with Param2
            var trans12 = state1.AddTransition(state2);
            trans12.hasExitTime = false;
            trans12.duration = 0.25f;
            trans12.AddCondition(AnimatorConditionMode.If, 0, "Param2");

            var self1 = state1.AddTransition(state1);
            self1.hasExitTime = false;
            self1.duration = 0.25f;
            self1.AddCondition(AnimatorConditionMode.If, 0, "Param1");

            // State2 -> State1 with Param3 (different condition for same target!)
            var trans21 = state2.AddTransition(state1);
            trans21.hasExitTime = false;
            trans21.duration = 0.25f;
            trans21.AddCondition(AnimatorConditionMode.If, 0, "Param3");

            var self2 = state2.AddTransition(state2);
            self2.hasExitTime = false;
            self2.duration = 0.25f;
            self2.AddCondition(AnimatorConditionMode.If, 0, "Param2");

            controller.AddLayer(layer);
            return controller;
        }

        private AnimatorController CreateThreeStateCompleteGraphController()
        {
            var controller = new AnimatorController { name = "ThreeStateGraph" };
            controller.AddParameter("ToA", AnimatorControllerParameterType.Bool);
            controller.AddParameter("ToB", AnimatorControllerParameterType.Bool);
            controller.AddParameter("ToC", AnimatorControllerParameterType.Bool);

            var layer = new AnimatorControllerLayer
            {
                name = "Base Layer",
                stateMachine = new AnimatorStateMachine { name = "Base Layer" }
            };

            var stateA = layer.stateMachine.AddState("StateA");
            stateA.motion = new AnimationClip { name = "ClipA" };
            
            var stateB = layer.stateMachine.AddState("StateB");
            stateB.motion = new AnimationClip { name = "ClipB" };

            var stateC = layer.stateMachine.AddState("StateC");
            stateC.motion = new AnimationClip { name = "ClipC" };

            // Create complete 3-state graph
            float duration = 0.2f;
            
            // From A
            AddTransition(stateA, stateA, "ToA", duration);
            AddTransition(stateA, stateB, "ToB", duration);
            AddTransition(stateA, stateC, "ToC", duration);

            // From B
            AddTransition(stateB, stateA, "ToA", duration);
            AddTransition(stateB, stateB, "ToB", duration);
            AddTransition(stateB, stateC, "ToC", duration);

            // From C
            AddTransition(stateC, stateA, "ToA", duration);
            AddTransition(stateC, stateB, "ToB", duration);
            AddTransition(stateC, stateC, "ToC", duration);

            controller.AddLayer(layer);
            return controller;
        }

        private void AddTransition(AnimatorState from, AnimatorState to, string param, float duration)
        {
            var trans = from.AddTransition(to);
            trans.hasExitTime = false;
            trans.duration = duration;
            trans.AddCondition(AnimatorConditionMode.If, 0, param);
        }
    }
}
