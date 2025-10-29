using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorOptimizer
{
    /// <summary>
    /// Editor script to generate test animator controllers for CompleteGraphToEntryExit tests.
    /// Run this from Unity Editor menu: Avatar Optimizer > Tests > Generate CompleteGraphToEntryExit Test Controllers
    /// </summary>
    public static class CompleteGraphToEntryExitTestGenerator
    {
        private const string BasePath = "Assets/Test~/AnimatorOptimizer/CompleteGraphToEntryExit/";

        [MenuItem("Avatar Optimizer/Tests/Generate CompleteGraphToEntryExit Test Controllers")]
        public static void GenerateAllTestControllers()
        {
            GenerateSimpleCompleteGraph();
            GenerateCompleteGraphWithSelfTransitions();
            GenerateCompleteGraphWithDifferentConditions();
            GenerateNonConvertibleIncompleteGraph();
            GenerateNonConvertibleDifferentTransitionSettings();
            GenerateNonConvertibleHasStateMachine();
            GenerateNonConvertibleDifferentConditionsForSameTarget();
            
            AssetDatabase.SaveAssets();
            Debug.Log("Generated all CompleteGraphToEntryExit test controllers");
        }

        private static void GenerateSimpleCompleteGraph()
        {
            var controller = new AnimatorController();
            controller.name = "SimpleCompleteGraph";
            
            // Add parameters
            controller.AddParameter("StateA", AnimatorControllerParameterType.Bool);
            controller.AddParameter("StateB", AnimatorControllerParameterType.Bool);

            // Create animation clips
            var clipA = new AnimationClip { name = "ClipA" };
            var clipB = new AnimationClip { name = "ClipB" };

            // Create layer
            var layer = new AnimatorControllerLayer
            {
                name = "Base Layer",
                stateMachine = new AnimatorStateMachine { name = "Base Layer" }
            };

            // Create states
            var stateA = layer.stateMachine.AddState("StateA");
            stateA.motion = clipA;
            
            var stateB = layer.stateMachine.AddState("StateB");
            stateB.motion = clipB;

            // Create complete graph transitions: StateA -> StateB
            var transitionAtoB = stateA.AddTransition(stateB);
            transitionAtoB.hasExitTime = false;
            transitionAtoB.duration = 0.25f;
            transitionAtoB.AddCondition(AnimatorConditionMode.If, 0, "StateB");

            // StateB -> StateA
            var transitionBtoA = stateB.AddTransition(stateA);
            transitionBtoA.hasExitTime = false;
            transitionBtoA.duration = 0.25f;
            transitionBtoA.AddCondition(AnimatorConditionMode.If, 0, "StateA");

            // Self-transitions (part of complete graph)
            var selfA = stateA.AddTransition(stateA);
            selfA.hasExitTime = false;
            selfA.duration = 0.25f;
            selfA.AddCondition(AnimatorConditionMode.If, 0, "StateA");

            var selfB = stateB.AddTransition(stateB);
            selfB.hasExitTime = false;
            selfB.duration = 0.25f;
            selfB.AddCondition(AnimatorConditionMode.If, 0, "StateB");

            controller.AddLayer(layer);
            
            AssetDatabase.CreateAsset(controller, BasePath + "SimpleCompleteGraph.controller");
            AssetDatabase.AddObjectToAsset(clipA, controller);
            AssetDatabase.AddObjectToAsset(clipB, controller);
            
            // Generate expected output
            GenerateSimpleCompleteGraphConverted();
        }

        private static void GenerateSimpleCompleteGraphConverted()
        {
            var controller = new AnimatorController();
            controller.name = "SimpleCompleteGraph.converted";
            
            // Add parameters
            controller.AddParameter("StateA", AnimatorControllerParameterType.Bool);
            controller.AddParameter("StateB", AnimatorControllerParameterType.Bool);

            // Create animation clips
            var clipA = new AnimationClip { name = "ClipA" };
            var clipB = new AnimationClip { name = "ClipB" };

            // Create layer
            var layer = new AnimatorControllerLayer
            {
                name = "Base Layer",
                stateMachine = new AnimatorStateMachine { name = "Base Layer" }
            };

            // Create states
            var stateA = layer.stateMachine.AddState("StateA");
            stateA.motion = clipA;
            
            var stateB = layer.stateMachine.AddState("StateB");
            stateB.motion = clipB;

            // Create entry transitions
            var entryToA = new AnimatorTransition
            {
                destinationState = stateA
            };
            entryToA.AddCondition(AnimatorConditionMode.If, 0, "StateA");
            layer.stateMachine.AddEntryTransition(entryToA);

            var entryToB = new AnimatorTransition
            {
                destinationState = stateB
            };
            entryToB.AddCondition(AnimatorConditionMode.If, 0, "StateB");
            layer.stateMachine.AddEntryTransition(entryToB);

            // Create exit transitions from each state
            var exitA = stateA.AddExitTransition();
            exitA.hasExitTime = false;
            exitA.duration = 0.25f;
            exitA.AddCondition(AnimatorConditionMode.If, 0, "StateA");

            var exitA2 = stateA.AddExitTransition();
            exitA2.hasExitTime = false;
            exitA2.duration = 0.25f;
            exitA2.AddCondition(AnimatorConditionMode.If, 0, "StateB");

            var exitB = stateB.AddExitTransition();
            exitB.hasExitTime = false;
            exitB.duration = 0.25f;
            exitB.AddCondition(AnimatorConditionMode.If, 0, "StateA");

            var exitB2 = stateB.AddExitTransition();
            exitB2.hasExitTime = false;
            exitB2.duration = 0.25f;
            exitB2.AddCondition(AnimatorConditionMode.If, 0, "StateB");

            // Self-transitions are preserved
            var selfA = stateA.AddTransition(stateA);
            selfA.hasExitTime = false;
            selfA.duration = 0.25f;
            selfA.AddCondition(AnimatorConditionMode.If, 0, "StateA");

            var selfB = stateB.AddTransition(stateB);
            selfB.hasExitTime = false;
            selfB.duration = 0.25f;
            selfB.AddCondition(AnimatorConditionMode.If, 0, "StateB");

            controller.AddLayer(layer);
            
            AssetDatabase.CreateAsset(controller, BasePath + "SimpleCompleteGraph.converted.controller");
            AssetDatabase.AddObjectToAsset(clipA, controller);
            AssetDatabase.AddObjectToAsset(clipB, controller);
        }

        private static void GenerateCompleteGraphWithSelfTransitions()
        {
            var controller = new AnimatorController();
            controller.name = "CompleteGraphWithSelfTransitions";
            
            controller.AddParameter("Param1", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Param2", AnimatorControllerParameterType.Bool);

            var clip1 = new AnimationClip { name = "Clip1" };
            var clip2 = new AnimationClip { name = "Clip2" };

            var layer = new AnimatorControllerLayer
            {
                name = "Base Layer",
                stateMachine = new AnimatorStateMachine { name = "Base Layer" }
            };

            var state1 = layer.stateMachine.AddState("State1");
            state1.motion = clip1;
            
            var state2 = layer.stateMachine.AddState("State2");
            state2.motion = clip2;

            // Complete graph with explicit self-transitions
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
            
            AssetDatabase.CreateAsset(controller, BasePath + "CompleteGraphWithSelfTransitions.controller");
            AssetDatabase.AddObjectToAsset(clip1, controller);
            AssetDatabase.AddObjectToAsset(clip2, controller);

            GenerateCompleteGraphWithSelfTransitionsConverted();
        }

        private static void GenerateCompleteGraphWithSelfTransitionsConverted()
        {
            var controller = new AnimatorController();
            controller.name = "CompleteGraphWithSelfTransitions.converted";
            
            controller.AddParameter("Param1", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Param2", AnimatorControllerParameterType.Bool);

            var clip1 = new AnimationClip { name = "Clip1" };
            var clip2 = new AnimationClip { name = "Clip2" };

            var layer = new AnimatorControllerLayer
            {
                name = "Base Layer",
                stateMachine = new AnimatorStateMachine { name = "Base Layer" }
            };

            var state1 = layer.stateMachine.AddState("State1");
            state1.motion = clip1;
            
            var state2 = layer.stateMachine.AddState("State2");
            state2.motion = clip2;

            // Entry transitions
            var entry1 = new AnimatorTransition { destinationState = state1 };
            entry1.AddCondition(AnimatorConditionMode.If, 0, "Param1");
            layer.stateMachine.AddEntryTransition(entry1);

            var entry2 = new AnimatorTransition { destinationState = state2 };
            entry2.AddCondition(AnimatorConditionMode.If, 0, "Param2");
            layer.stateMachine.AddEntryTransition(entry2);

            // Exit transitions
            var exit1a = state1.AddExitTransition();
            exit1a.hasExitTime = false;
            exit1a.duration = 0.1f;
            exit1a.AddCondition(AnimatorConditionMode.If, 0, "Param1");

            var exit1b = state1.AddExitTransition();
            exit1b.hasExitTime = false;
            exit1b.duration = 0.1f;
            exit1b.AddCondition(AnimatorConditionMode.If, 0, "Param2");

            // Self-transitions preserved
            var self1 = state1.AddTransition(state1);
            self1.hasExitTime = false;
            self1.duration = 0.1f;
            self1.AddCondition(AnimatorConditionMode.If, 0, "Param1");

            var exit2a = state2.AddExitTransition();
            exit2a.hasExitTime = false;
            exit2a.duration = 0.1f;
            exit2a.AddCondition(AnimatorConditionMode.If, 0, "Param1");

            var exit2b = state2.AddExitTransition();
            exit2b.hasExitTime = false;
            exit2b.duration = 0.1f;
            exit2b.AddCondition(AnimatorConditionMode.If, 0, "Param2");

            var self2 = state2.AddTransition(state2);
            self2.hasExitTime = false;
            self2.duration = 0.1f;
            self2.AddCondition(AnimatorConditionMode.If, 0, "Param2");

            controller.AddLayer(layer);
            
            AssetDatabase.CreateAsset(controller, BasePath + "CompleteGraphWithSelfTransitions.converted.controller");
            AssetDatabase.AddObjectToAsset(clip1, controller);
            AssetDatabase.AddObjectToAsset(clip2, controller);
        }

        private static void GenerateCompleteGraphWithDifferentConditions()
        {
            var controller = new AnimatorController();
            controller.name = "CompleteGraphWithDifferentConditions";
            
            controller.AddParameter("ToA", AnimatorControllerParameterType.Bool);
            controller.AddParameter("ToB", AnimatorControllerParameterType.Bool);
            controller.AddParameter("ToC", AnimatorControllerParameterType.Bool);

            var clipA = new AnimationClip { name = "ClipA" };
            var clipB = new AnimationClip { name = "ClipB" };
            var clipC = new AnimationClip { name = "ClipC" };

            var layer = new AnimatorControllerLayer
            {
                name = "Base Layer",
                stateMachine = new AnimatorStateMachine { name = "Base Layer" }
            };

            var stateA = layer.stateMachine.AddState("StateA");
            stateA.motion = clipA;
            
            var stateB = layer.stateMachine.AddState("StateB");
            stateB.motion = clipB;

            var stateC = layer.stateMachine.AddState("StateC");
            stateC.motion = clipC;

            // Create complete graph (all transitions from each state to all states including self)
            // From A
            var aToA = stateA.AddTransition(stateA);
            aToA.hasExitTime = false;
            aToA.duration = 0.2f;
            aToA.AddCondition(AnimatorConditionMode.If, 0, "ToA");

            var aToB = stateA.AddTransition(stateB);
            aToB.hasExitTime = false;
            aToB.duration = 0.2f;
            aToB.AddCondition(AnimatorConditionMode.If, 0, "ToB");

            var aToC = stateA.AddTransition(stateC);
            aToC.hasExitTime = false;
            aToC.duration = 0.2f;
            aToC.AddCondition(AnimatorConditionMode.If, 0, "ToC");

            // From B
            var bToA = stateB.AddTransition(stateA);
            bToA.hasExitTime = false;
            bToA.duration = 0.2f;
            bToA.AddCondition(AnimatorConditionMode.If, 0, "ToA");

            var bToB = stateB.AddTransition(stateB);
            bToB.hasExitTime = false;
            bToB.duration = 0.2f;
            bToB.AddCondition(AnimatorConditionMode.If, 0, "ToB");

            var bToC = stateB.AddTransition(stateC);
            bToC.hasExitTime = false;
            bToC.duration = 0.2f;
            bToC.AddCondition(AnimatorConditionMode.If, 0, "ToC");

            // From C
            var cToA = stateC.AddTransition(stateA);
            cToA.hasExitTime = false;
            cToA.duration = 0.2f;
            cToA.AddCondition(AnimatorConditionMode.If, 0, "ToA");

            var cToB = stateC.AddTransition(stateB);
            cToB.hasExitTime = false;
            cToB.duration = 0.2f;
            cToB.AddCondition(AnimatorConditionMode.If, 0, "ToB");

            var cToC = stateC.AddTransition(stateC);
            cToC.hasExitTime = false;
            cToC.duration = 0.2f;
            cToC.AddCondition(AnimatorConditionMode.If, 0, "ToC");

            controller.AddLayer(layer);
            
            AssetDatabase.CreateAsset(controller, BasePath + "CompleteGraphWithDifferentConditions.controller");
            AssetDatabase.AddObjectToAsset(clipA, controller);
            AssetDatabase.AddObjectToAsset(clipB, controller);
            AssetDatabase.AddObjectToAsset(clipC, controller);

            GenerateCompleteGraphWithDifferentConditionsConverted();
        }

        private static void GenerateCompleteGraphWithDifferentConditionsConverted()
        {
            var controller = new AnimatorController();
            controller.name = "CompleteGraphWithDifferentConditions.converted";
            
            controller.AddParameter("ToA", AnimatorControllerParameterType.Bool);
            controller.AddParameter("ToB", AnimatorControllerParameterType.Bool);
            controller.AddParameter("ToC", AnimatorControllerParameterType.Bool);

            var clipA = new AnimationClip { name = "ClipA" };
            var clipB = new AnimationClip { name = "ClipB" };
            var clipC = new AnimationClip { name = "ClipC" };

            var layer = new AnimatorControllerLayer
            {
                name = "Base Layer",
                stateMachine = new AnimatorStateMachine { name = "Base Layer" }
            };

            var stateA = layer.stateMachine.AddState("StateA");
            stateA.motion = clipA;
            
            var stateB = layer.stateMachine.AddState("StateB");
            stateB.motion = clipB;

            var stateC = layer.stateMachine.AddState("StateC");
            stateC.motion = clipC;

            // Entry transitions
            var entryA = new AnimatorTransition { destinationState = stateA };
            entryA.AddCondition(AnimatorConditionMode.If, 0, "ToA");
            layer.stateMachine.AddEntryTransition(entryA);

            var entryB = new AnimatorTransition { destinationState = stateB };
            entryB.AddCondition(AnimatorConditionMode.If, 0, "ToB");
            layer.stateMachine.AddEntryTransition(entryB);

            var entryC = new AnimatorTransition { destinationState = stateC };
            entryC.AddCondition(AnimatorConditionMode.If, 0, "ToC");
            layer.stateMachine.AddEntryTransition(entryC);

            // Exit transitions (one for each possible target state condition)
            // State A
            var exitA1 = stateA.AddExitTransition();
            exitA1.hasExitTime = false;
            exitA1.duration = 0.2f;
            exitA1.AddCondition(AnimatorConditionMode.If, 0, "ToA");

            var exitA2 = stateA.AddExitTransition();
            exitA2.hasExitTime = false;
            exitA2.duration = 0.2f;
            exitA2.AddCondition(AnimatorConditionMode.If, 0, "ToB");

            var exitA3 = stateA.AddExitTransition();
            exitA3.hasExitTime = false;
            exitA3.duration = 0.2f;
            exitA3.AddCondition(AnimatorConditionMode.If, 0, "ToC");

            // Self-transition preserved
            var selfA = stateA.AddTransition(stateA);
            selfA.hasExitTime = false;
            selfA.duration = 0.2f;
            selfA.AddCondition(AnimatorConditionMode.If, 0, "ToA");

            // State B
            var exitB1 = stateB.AddExitTransition();
            exitB1.hasExitTime = false;
            exitB1.duration = 0.2f;
            exitB1.AddCondition(AnimatorConditionMode.If, 0, "ToA");

            var exitB2 = stateB.AddExitTransition();
            exitB2.hasExitTime = false;
            exitB2.duration = 0.2f;
            exitB2.AddCondition(AnimatorConditionMode.If, 0, "ToB");

            var exitB3 = stateB.AddExitTransition();
            exitB3.hasExitTime = false;
            exitB3.duration = 0.2f;
            exitB3.AddCondition(AnimatorConditionMode.If, 0, "ToC");

            var selfB = stateB.AddTransition(stateB);
            selfB.hasExitTime = false;
            selfB.duration = 0.2f;
            selfB.AddCondition(AnimatorConditionMode.If, 0, "ToB");

            // State C
            var exitC1 = stateC.AddExitTransition();
            exitC1.hasExitTime = false;
            exitC1.duration = 0.2f;
            exitC1.AddCondition(AnimatorConditionMode.If, 0, "ToA");

            var exitC2 = stateC.AddExitTransition();
            exitC2.hasExitTime = false;
            exitC2.duration = 0.2f;
            exitC2.AddCondition(AnimatorConditionMode.If, 0, "ToB");

            var exitC3 = stateC.AddExitTransition();
            exitC3.hasExitTime = false;
            exitC3.duration = 0.2f;
            exitC3.AddCondition(AnimatorConditionMode.If, 0, "ToC");

            var selfC = stateC.AddTransition(stateC);
            selfC.hasExitTime = false;
            selfC.duration = 0.2f;
            selfC.AddCondition(AnimatorConditionMode.If, 0, "ToC");

            controller.AddLayer(layer);
            
            AssetDatabase.CreateAsset(controller, BasePath + "CompleteGraphWithDifferentConditions.converted.controller");
            AssetDatabase.AddObjectToAsset(clipA, controller);
            AssetDatabase.AddObjectToAsset(clipB, controller);
            AssetDatabase.AddObjectToAsset(clipC, controller);
        }

        private static void GenerateNonConvertibleIncompleteGraph()
        {
            var controller = new AnimatorController();
            controller.name = "NonConvertibleIncompleteGraph";
            
            controller.AddParameter("Param1", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Param2", AnimatorControllerParameterType.Bool);

            var clip1 = new AnimationClip { name = "Clip1" };
            var clip2 = new AnimationClip { name = "Clip2" };

            var layer = new AnimatorControllerLayer
            {
                name = "Base Layer",
                stateMachine = new AnimatorStateMachine { name = "Base Layer" }
            };

            var state1 = layer.stateMachine.AddState("State1");
            state1.motion = clip1;
            
            var state2 = layer.stateMachine.AddState("State2");
            state2.motion = clip2;

            // Incomplete graph - state1 doesn't have transition to state2
            var self1 = state1.AddTransition(state1);
            self1.hasExitTime = false;
            self1.AddCondition(AnimatorConditionMode.If, 0, "Param1");

            var trans21 = state2.AddTransition(state1);
            trans21.hasExitTime = false;
            trans21.AddCondition(AnimatorConditionMode.If, 0, "Param1");

            var self2 = state2.AddTransition(state2);
            self2.hasExitTime = false;
            self2.AddCondition(AnimatorConditionMode.If, 0, "Param2");

            controller.AddLayer(layer);
            
            AssetDatabase.CreateAsset(controller, BasePath + "NonConvertibleIncompleteGraph.controller");
            AssetDatabase.AddObjectToAsset(clip1, controller);
            AssetDatabase.AddObjectToAsset(clip2, controller);
        }

        private static void GenerateNonConvertibleDifferentTransitionSettings()
        {
            var controller = new AnimatorController();
            controller.name = "NonConvertibleDifferentTransitionSettings";
            
            controller.AddParameter("Param1", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Param2", AnimatorControllerParameterType.Bool);

            var clip1 = new AnimationClip { name = "Clip1" };
            var clip2 = new AnimationClip { name = "Clip2" };

            var layer = new AnimatorControllerLayer
            {
                name = "Base Layer",
                stateMachine = new AnimatorStateMachine { name = "Base Layer" }
            };

            var state1 = layer.stateMachine.AddState("State1");
            state1.motion = clip1;
            
            var state2 = layer.stateMachine.AddState("State2");
            state2.motion = clip2;

            // Complete graph but with different transition durations from state1
            var trans12 = state1.AddTransition(state2);
            trans12.hasExitTime = false;
            trans12.duration = 0.5f; // Different duration
            trans12.AddCondition(AnimatorConditionMode.If, 0, "Param2");

            var self1 = state1.AddTransition(state1);
            self1.hasExitTime = false;
            self1.duration = 0.1f; // Different duration
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
            
            AssetDatabase.CreateAsset(controller, BasePath + "NonConvertibleDifferentTransitionSettings.controller");
            AssetDatabase.AddObjectToAsset(clip1, controller);
            AssetDatabase.AddObjectToAsset(clip2, controller);
        }

        private static void GenerateNonConvertibleHasStateMachine()
        {
            var controller = new AnimatorController();
            controller.name = "NonConvertibleHasStateMachine";
            
            controller.AddParameter("Param1", AnimatorControllerParameterType.Bool);

            var clip1 = new AnimationClip { name = "Clip1" };

            var layer = new AnimatorControllerLayer
            {
                name = "Base Layer",
                stateMachine = new AnimatorStateMachine { name = "Base Layer" }
            };

            var state1 = layer.stateMachine.AddState("State1");
            state1.motion = clip1;
            
            // Add a child state machine
            layer.stateMachine.AddStateMachine("SubStateMachine");

            controller.AddLayer(layer);
            
            AssetDatabase.CreateAsset(controller, BasePath + "NonConvertibleHasStateMachine.controller");
            AssetDatabase.AddObjectToAsset(clip1, controller);
        }

        private static void GenerateNonConvertibleDifferentConditionsForSameTarget()
        {
            var controller = new AnimatorController();
            controller.name = "NonConvertibleDifferentConditionsForSameTarget";
            
            controller.AddParameter("Param1", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Param2", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Param3", AnimatorControllerParameterType.Bool);

            var clip1 = new AnimationClip { name = "Clip1" };
            var clip2 = new AnimationClip { name = "Clip2" };

            var layer = new AnimatorControllerLayer
            {
                name = "Base Layer",
                stateMachine = new AnimatorStateMachine { name = "Base Layer" }
            };

            var state1 = layer.stateMachine.AddState("State1");
            state1.motion = clip1;
            
            var state2 = layer.stateMachine.AddState("State2");
            state2.motion = clip2;

            // State1 -> State2 with Param2
            var trans12 = state1.AddTransition(state2);
            trans12.hasExitTime = false;
            trans12.duration = 0.25f;
            trans12.AddCondition(AnimatorConditionMode.If, 0, "Param2");

            var self1 = state1.AddTransition(state1);
            self1.hasExitTime = false;
            self1.duration = 0.25f;
            self1.AddCondition(AnimatorConditionMode.If, 0, "Param1");

            // State2 -> State1 with Param3 (different from State1's condition!)
            var trans21 = state2.AddTransition(state1);
            trans21.hasExitTime = false;
            trans21.duration = 0.25f;
            trans21.AddCondition(AnimatorConditionMode.If, 0, "Param3"); // Different condition!

            var self2 = state2.AddTransition(state2);
            self2.hasExitTime = false;
            self2.duration = 0.25f;
            self2.AddCondition(AnimatorConditionMode.If, 0, "Param2");

            controller.AddLayer(layer);
            
            AssetDatabase.CreateAsset(controller, BasePath + "NonConvertibleDifferentConditionsForSameTarget.controller");
            AssetDatabase.AddObjectToAsset(clip1, controller);
            AssetDatabase.AddObjectToAsset(clip2, controller);
        }
    }
}
