using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorOptimizer.CompleteGraphToEntryExits
{
    /// <summary>
    /// Editor script to generate test animator controllers for CompleteGraphToEntryExit tests.
    /// Run this from Unity Editor menu: Avatar Optimizer > Tests > Generate CompleteGraphToEntryExit Test Controllers
    /// </summary>
    public static class CompleteGraphToEntryExitTestGenerator
    {
        private const string BasePath = "Assets/AAOTest/AnimatorOptimizer/CompleteGraphToEntryExit/";

        [MenuItem("Avatar Optimizer/Tests/Generate CompleteGraphToEntryExit Test Controllers")]
        public static void GenerateAllTestControllers()
        {
            // Ensure shared animation clips exist before generating controllers
            GenerateSharedClips();

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

        // Create shared clips once under BasePath/SharedClips and provide loader
        private static void GenerateSharedClips()
        {
            var baseFolder = BasePath.TrimEnd('/');
            var sharedFolder = baseFolder + "/SharedClips";
            if (!AssetDatabase.IsValidFolder(sharedFolder))
                AssetDatabase.CreateFolder(baseFolder, "SharedClips");

            CreateClipIfMissing("ClipA", sharedFolder);
            CreateClipIfMissing("ClipB", sharedFolder);
            CreateClipIfMissing("ClipC", sharedFolder);
            CreateClipIfMissing("Clip1", sharedFolder);
            CreateClipIfMissing("Clip2", sharedFolder);
            AssetDatabase.SaveAssets();
        }

        private static void CreateClipIfMissing(string name, string folder)
        {
            var path = folder + "/" + name + ".anim";
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null) return;
            var clip = new AnimationClip { name = name };
            AssetDatabase.CreateAsset(clip, path);
        }

        private static AnimationClip GetSharedClip(string name)
        {
            var baseFolder = BasePath.TrimEnd('/');
            var path = baseFolder + "/SharedClips/" + name + ".anim";
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        }

        private static void GenerateSimpleCompleteGraph()
        {
            var controller = new AnimatorController();
            controller.name = "SimpleCompleteGraph";
            AssetDatabase.CreateAsset(controller, BasePath + "SimpleCompleteGraph.controller");
            
            // Add parameters
            controller.AddParameter("StateA", AnimatorControllerParameterType.Bool);
            controller.AddParameter("StateB", AnimatorControllerParameterType.Bool);

            // Create animation clips
            var clipA = GetSharedClip("ClipA");
            var clipB = GetSharedClip("ClipB");

            // Create layer via AddLayer instead of new-ing it manually
            controller.AddLayer("Base Layer");
            var layer = controller.layers[^1];
            layer.name = "Base Layer";
            layer.stateMachine.name = "Base Layer";

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

            // clips are shared assets now (no AddObjectToAsset)
            
             // Generate expected output
             GenerateSimpleCompleteGraphConverted();
         }

        private static void GenerateSimpleCompleteGraphConverted()
        {
            var controller = new AnimatorController();
            controller.name = "SimpleCompleteGraph.converted";
            AssetDatabase.CreateAsset(controller, BasePath + "SimpleCompleteGraph.converted.controller");
            
            // Add parameters
            controller.AddParameter("StateA", AnimatorControllerParameterType.Bool);
            controller.AddParameter("StateB", AnimatorControllerParameterType.Bool);

            // Create animation clips
            var clipA = GetSharedClip("ClipA");
            var clipB = GetSharedClip("ClipB");

            // Create layer via AddLayer instead of new-ing it manually
            controller.AddLayer("Base Layer");
            var layer = controller.layers[^1];
            layer.name = "Base Layer";
            layer.stateMachine.name = "Base Layer";

            // Create states
            var stateA = layer.stateMachine.AddState("StateA");
            stateA.motion = clipA;
            
            var stateB = layer.stateMachine.AddState("StateB");
            stateB.motion = clipB;

            // Create entry transitions
            var entryToA = layer.stateMachine.AddEntryTransition(stateA);
            entryToA.AddCondition(AnimatorConditionMode.If, 0, "StateA");

            var entryToB = layer.stateMachine.AddEntryTransition(stateB);
            entryToB.AddCondition(AnimatorConditionMode.If, 0, "StateB");

            // Create exit transitions from each state
            var exitA2 = stateA.AddExitTransition();
            exitA2.hasExitTime = false;
            exitA2.duration = 0.25f;
            exitA2.AddCondition(AnimatorConditionMode.If, 0, "StateB");

            var exitB = stateB.AddExitTransition();
            exitB.hasExitTime = false;
            exitB.duration = 0.25f;
            exitB.AddCondition(AnimatorConditionMode.If, 0, "StateA");

            // Self-transitions are preserved
            var selfA = stateA.AddTransition(stateA);
            selfA.hasExitTime = false;
            selfA.duration = 0.25f;
            selfA.AddCondition(AnimatorConditionMode.If, 0, "StateA");

            var selfB = stateB.AddTransition(stateB);
            selfB.hasExitTime = false;
            selfB.duration = 0.25f;
            selfB.AddCondition(AnimatorConditionMode.If, 0, "StateB");

            // clips are shared assets now
        }

        private static void GenerateCompleteGraphWithSelfTransitions()
        {
            var controller = new AnimatorController();
            controller.name = "CompleteGraphWithSelfTransitions";
            AssetDatabase.CreateAsset(controller, BasePath + "CompleteGraphWithSelfTransitions.controller");
            
            controller.AddParameter("Param1", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Param2", AnimatorControllerParameterType.Bool);

            var clip1 = GetSharedClip("Clip1");
            var clip2 = GetSharedClip("Clip2");

            controller.AddLayer("Base Layer");
            var layer = controller.layers[^1];
            layer.name = "Base Layer";
            layer.stateMachine.name = "Base Layer";

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

            // clips are shared assets now
            
            GenerateCompleteGraphWithSelfTransitionsConverted();
        }

        private static void GenerateCompleteGraphWithSelfTransitionsConverted()
        {
            var controller = new AnimatorController();
            controller.name = "CompleteGraphWithSelfTransitions.converted";
            AssetDatabase.CreateAsset(controller, BasePath + "CompleteGraphWithSelfTransitions.converted.controller");
            
            controller.AddParameter("Param1", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Param2", AnimatorControllerParameterType.Bool);

            var clip1 = GetSharedClip("Clip1");
            var clip2 = GetSharedClip("Clip2");

            controller.AddLayer("Base Layer");
            var layer = controller.layers[^1];
            layer.name = "Base Layer";
            layer.stateMachine.name = "Base Layer";

            var state1 = layer.stateMachine.AddState("State1");
            state1.motion = clip1;
            
            var state2 = layer.stateMachine.AddState("State2");
            state2.motion = clip2;

            // Entry transitions
            var entry1 = layer.stateMachine.AddEntryTransition(state1);
            entry1.AddCondition(AnimatorConditionMode.If, 0, "Param1");

            var entry2 = layer.stateMachine.AddEntryTransition(state2);
            entry2.AddCondition(AnimatorConditionMode.If, 0, "Param2");

            // Exit transitions

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

            var self2 = state2.AddTransition(state2);
            self2.hasExitTime = false;
            self2.duration = 0.1f;
            self2.AddCondition(AnimatorConditionMode.If, 0, "Param2");

            // clips are shared assets now
        }

        private static void GenerateCompleteGraphWithDifferentConditions()
        {
            var controller = new AnimatorController();
            controller.name = "CompleteGraphWithDifferentConditions";
            AssetDatabase.CreateAsset(controller, BasePath + "CompleteGraphWithDifferentConditions.controller");
            
            controller.AddParameter("ToA", AnimatorControllerParameterType.Bool);
            controller.AddParameter("ToB", AnimatorControllerParameterType.Bool);
            controller.AddParameter("ToC", AnimatorControllerParameterType.Bool);

            var clipA = GetSharedClip("ClipA");
            var clipB = GetSharedClip("ClipB");
            var clipC = GetSharedClip("ClipC");

            controller.AddLayer("Base Layer");
            var layer = controller.layers[^1];
            layer.name = "Base Layer";
            layer.stateMachine.name = "Base Layer";

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

            // clips are shared assets now

            GenerateCompleteGraphWithDifferentConditionsConverted();
        }

        private static void GenerateCompleteGraphWithDifferentConditionsConverted()
        {
            var controller = new AnimatorController();
            controller.name = "CompleteGraphWithDifferentConditions.converted";
            AssetDatabase.CreateAsset(controller, BasePath + "CompleteGraphWithDifferentConditions.converted.controller");
            
            controller.AddParameter("ToA", AnimatorControllerParameterType.Bool);
            controller.AddParameter("ToB", AnimatorControllerParameterType.Bool);
            controller.AddParameter("ToC", AnimatorControllerParameterType.Bool);

            var clipA = GetSharedClip("ClipA");
            var clipB = GetSharedClip("ClipB");
            var clipC = GetSharedClip("ClipC");

            controller.AddLayer("Base Layer");
            var layer = controller.layers[^1];
            layer.name = "Base Layer";
            layer.stateMachine.name = "Base Layer";

            var stateA = layer.stateMachine.AddState("StateA");
            stateA.motion = clipA;
            
            var stateB = layer.stateMachine.AddState("StateB");
            stateB.motion = clipB;

            var stateC = layer.stateMachine.AddState("StateC");
            stateC.motion = clipC;

            // Entry transitions
            var entryA = layer.stateMachine.AddEntryTransition(stateA);
            entryA.AddCondition(AnimatorConditionMode.If, 0, "ToA");

            var entryB = layer.stateMachine.AddEntryTransition(stateB);
            entryB.AddCondition(AnimatorConditionMode.If, 0, "ToB");

            var entryC = layer.stateMachine.AddEntryTransition(stateC);
            entryC.AddCondition(AnimatorConditionMode.If, 0, "ToC");

            // Exit transitions (one for each possible target state condition)
            // State A
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

            var selfC = stateC.AddTransition(stateC);
            selfC.hasExitTime = false;
            selfC.duration = 0.2f;
            selfC.AddCondition(AnimatorConditionMode.If, 0, "ToC");

            // clips are shared assets now
        }

        private static void GenerateNonConvertibleIncompleteGraph()
        {
            var controller = new AnimatorController();
            controller.name = "NonConvertibleIncompleteGraph";
            AssetDatabase.CreateAsset(controller, BasePath + "NonConvertibleIncompleteGraph.controller");
            
            controller.AddParameter("Param1", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Param2", AnimatorControllerParameterType.Bool);

            var clip1 = GetSharedClip("Clip1");
            var clip2 = GetSharedClip("Clip2");

            controller.AddLayer("Base Layer");
            var layer = controller.layers[^1];
            layer.name = "Base Layer";
            layer.stateMachine.name = "Base Layer";

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

            // clips are shared assets now
        }

        private static void GenerateNonConvertibleDifferentTransitionSettings()
        {
            var controller = new AnimatorController();
            controller.name = "NonConvertibleDifferentTransitionSettings";
            
            controller.AddParameter("Param1", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Param2", AnimatorControllerParameterType.Bool);

            var clip1 = GetSharedClip("Clip1");
            var clip2 = GetSharedClip("Clip2");

            controller.AddLayer("Base Layer");
            var layer = controller.layers[^1];
            layer.name = "Base Layer";
            layer.stateMachine.name = "Base Layer";

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

            AssetDatabase.CreateAsset(controller, BasePath + "NonConvertibleDifferentTransitionSettings.controller");
            // clips are shared assets now
         }

        private static void GenerateNonConvertibleHasStateMachine()
        {
            var controller = new AnimatorController();
            controller.name = "NonConvertibleHasStateMachine";
            AssetDatabase.CreateAsset(controller, BasePath + "NonConvertibleHasStateMachine.controller");
            
            controller.AddParameter("Param1", AnimatorControllerParameterType.Bool);

            var clip1 = GetSharedClip("Clip1");

            controller.AddLayer("Base Layer");
            var layer = controller.layers[^1];
            layer.name = "Base Layer";
            layer.stateMachine.name = "Base Layer";

            var state1 = layer.stateMachine.AddState("State1");
            state1.motion = clip1;
            
            // Add a child state machine
            layer.stateMachine.AddStateMachine("SubStateMachine");

            // clip is a shared asset now
        }

        private static void GenerateNonConvertibleDifferentConditionsForSameTarget()
        {
            var controller = new AnimatorController();
            controller.name = "NonConvertibleDifferentConditionsForSameTarget";
            AssetDatabase.CreateAsset(controller, BasePath + "NonConvertibleDifferentConditionsForSameTarget.controller");
            
            controller.AddParameter("Param1", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Param2", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Param3", AnimatorControllerParameterType.Bool);

            var clip1 = GetSharedClip("Clip1");
            var clip2 = GetSharedClip("Clip2");
            var clip3 = GetSharedClip("Clip3");

            controller.AddLayer("Base Layer");
            var layer = controller.layers[^1];
            layer.name = "Base Layer";
            layer.stateMachine.name = "Base Layer";

            var state1 = layer.stateMachine.AddState("State1");
            state1.motion = clip1;
            
            var state2 = layer.stateMachine.AddState("State2");
            state2.motion = clip2;

            var state3 = layer.stateMachine.AddState("State3");
            state3.motion = clip3;

            // Create complete graph but with different conditions targeting State1
            // State1 -> State2 with Param2
            var trans12 = state1.AddTransition(state2);
            trans12.hasExitTime = false;
            trans12.duration = 0.25f;
            trans12.AddCondition(AnimatorConditionMode.If, 0, "Param2");

            // State1 -> State3 with Param3
            var trans13 = state1.AddTransition(state3);
            trans13.hasExitTime = false;
            trans13.duration = 0.25f;
            trans13.AddCondition(AnimatorConditionMode.If, 0, "Param3");

            // State1 -> State1 (self) with Param1
            var self1 = state1.AddTransition(state1);
            self1.hasExitTime = false;
            self1.duration = 0.25f;
            self1.AddCondition(AnimatorConditionMode.If, 0, "Param1");

            // State2 -> State1 with Param1 (same as State3's condition to State1)
            var trans21 = state2.AddTransition(state1);
            trans21.hasExitTime = false;
            trans21.duration = 0.25f;
            trans21.AddCondition(AnimatorConditionMode.If, 0, "Param1");

            // State2 -> State3 with Param3
            var trans23 = state2.AddTransition(state3);
            trans23.hasExitTime = false;
            trans23.duration = 0.25f;
            trans23.AddCondition(AnimatorConditionMode.If, 0, "Param3");

            // State2 -> State2 (self) with Param2
            var self2 = state2.AddTransition(state2);
            self2.hasExitTime = false;
            self2.duration = 0.25f;
            self2.AddCondition(AnimatorConditionMode.If, 0, "Param2");

            // State3 -> State1 with Param3 (DIFFERENT from State2's Param1 - this makes it non-convertible!)
            var trans31 = state3.AddTransition(state1);
            trans31.hasExitTime = false;
            trans31.duration = 0.25f;
            trans31.AddCondition(AnimatorConditionMode.If, 0, "Param3");

            // State3 -> State2 with Param2
            var trans32 = state3.AddTransition(state2);
            trans32.hasExitTime = false;
            trans32.duration = 0.25f;
            trans32.AddCondition(AnimatorConditionMode.If, 0, "Param2");

            // State3 -> State3 (self) with Param3
            var self3 = state3.AddTransition(state3);
            self3.hasExitTime = false;
            self3.duration = 0.25f;
            self3.AddCondition(AnimatorConditionMode.If, 0, "Param3");

        }
    }
}
