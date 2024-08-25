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
    }
}
