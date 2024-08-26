using Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer;
using NUnit.Framework;
using UnityEditor;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorOptimizer
{
    public class AnyStateToEntryExitTest : AnimatorOptimizerTestBase
    {
        private AnimatorOptimizerState _state = new AnimatorOptimizerState();

        // This basically animates BlendShapes, which we can determine if they are no-op or not
        [Test]
        public void HaolanGestureFace()
        {
            var controller = LoadCloneAnimatorController("HAOLAN-GestureFace");
            controller.name = "HAOLAN-GestureFace.converted";
            AnyStateToEntryExit.Execute(_state, new AOAnimatorController(controller));
            var except = LoadAnimatorController("HAOLAN-GestureFace.converted");
            RecursiveCheckEquals(except, controller);
        }
        
        // This animates Humanoid bones (Animator parameters), which we cannot determine if they are no-op or not 
        [Test]
        public void HaolanGestureHand()
        {
            var controller = LoadCloneAnimatorController("HAOLAN-GestureHand");
            controller.name = "HAOLAN-GestureHand.converted";
            AnyStateToEntryExit.Execute(_state, new AOAnimatorController(controller));
            var except = LoadAnimatorController("HAOLAN-GestureHand.converted");
            RecursiveCheckEquals(except, controller);
        }

        // RIS Initialized state will have no motion so no-op check fails.
        // therefore, Initialize layer will not be converted  
        [Test]
        public void RISNoRoot()
        {
            var controller = LoadCloneAnimatorController("RIS-Test");
            controller.name = "RIS-Test.converted.noroot";
            AnyStateToEntryExit.Execute(_state, new AOAnimatorController(controller));
            var except = LoadAnimatorController("RIS-Test.converted.noroot");
            RecursiveCheckEquals(except, controller);
        }

        // RIS Initialized state will have no motion so no-op check fails.
        // therefore, Initialize layer will not be converted  
        [Test]
        public void RISWithRoot()
        {
            var controller = LoadCloneAnimatorController("RIS-Test");
            var root = LoadPrefab("RIS-Test");
            controller.name = "RIS-Test.converted.withroot";
            AnyStateToEntryExit.Execute(_state, new AOAnimatorController(controller, root));
            var except = LoadAnimatorController("RIS-Test.converted.withroot");
            RecursiveCheckEquals(except, controller);
        }
    }
}
