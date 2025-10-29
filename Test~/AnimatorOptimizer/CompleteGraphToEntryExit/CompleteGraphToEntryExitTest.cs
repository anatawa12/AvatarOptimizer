using Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer;
using NUnit.Framework;
using UnityEditor;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorOptimizer
{
    public class CompleteGraphToEntryExitTest : AnimatorOptimizerTestBase
    {
        private AnimatorOptimizerState _state = new AnimatorOptimizerState();

        [Test]
        public void SimpleCompleteGraph()
        {
            var controller = LoadCloneAnimatorController("SimpleCompleteGraph");
            controller.name = "SimpleCompleteGraph.converted";
            CompleteGraphToEntryExit.Execute(_state, new AOAnimatorController(controller));
            var except = LoadAnimatorController("SimpleCompleteGraph.converted");
            RecursiveCheckEquals(except, controller);
        }

        [Test]
        public void CompleteGraphWithSelfTransitions()
        {
            var controller = LoadCloneAnimatorController("CompleteGraphWithSelfTransitions");
            controller.name = "CompleteGraphWithSelfTransitions.converted";
            CompleteGraphToEntryExit.Execute(_state, new AOAnimatorController(controller));
            var except = LoadAnimatorController("CompleteGraphWithSelfTransitions.converted");
            RecursiveCheckEquals(except, controller);
        }

        [Test]
        public void CompleteGraphWithDifferentConditions()
        {
            var controller = LoadCloneAnimatorController("CompleteGraphWithDifferentConditions");
            controller.name = "CompleteGraphWithDifferentConditions.converted";
            CompleteGraphToEntryExit.Execute(_state, new AOAnimatorController(controller));
            var except = LoadAnimatorController("CompleteGraphWithDifferentConditions.converted");
            RecursiveCheckEquals(except, controller);
        }

        [Test]
        public void NonConvertibleIncompleteGraph()
        {
            var controller = LoadCloneAnimatorController("NonConvertibleIncompleteGraph");
            controller.name = "NonConvertibleIncompleteGraph";
            CompleteGraphToEntryExit.Execute(_state, new AOAnimatorController(controller));
            var except = LoadAnimatorController("NonConvertibleIncompleteGraph");
            RecursiveCheckEquals(except, controller);
        }

        [Test]
        public void NonConvertibleDifferentTransitionSettings()
        {
            var controller = LoadCloneAnimatorController("NonConvertibleDifferentTransitionSettings");
            controller.name = "NonConvertibleDifferentTransitionSettings";
            CompleteGraphToEntryExit.Execute(_state, new AOAnimatorController(controller));
            var except = LoadAnimatorController("NonConvertibleDifferentTransitionSettings");
            RecursiveCheckEquals(except, controller);
        }

        [Test]
        public void NonConvertibleHasStateMachine()
        {
            var controller = LoadCloneAnimatorController("NonConvertibleHasStateMachine");
            controller.name = "NonConvertibleHasStateMachine";
            CompleteGraphToEntryExit.Execute(_state, new AOAnimatorController(controller));
            var except = LoadAnimatorController("NonConvertibleHasStateMachine");
            RecursiveCheckEquals(except, controller);
        }

        [Test]
        public void NonConvertibleDifferentConditionsForSameTarget()
        {
            var controller = LoadCloneAnimatorController("NonConvertibleDifferentConditionsForSameTarget");
            controller.name = "NonConvertibleDifferentConditionsForSameTarget";
            CompleteGraphToEntryExit.Execute(_state, new AOAnimatorController(controller));
            var except = LoadAnimatorController("NonConvertibleDifferentConditionsForSameTarget");
            RecursiveCheckEquals(except, controller);
        }
    }
}
