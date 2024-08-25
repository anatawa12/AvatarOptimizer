using Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer;
using NUnit.Framework;
using UnityEditor;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorOptimizer
{
    public class AnyStateToEntryExitTest : AnimatorOptimizerTestBase
    {
        private AnimatorOptimizerState _state = new AnimatorOptimizerState();

        [Test]
        public void HaolanGestureFace()
        {
            var controller = LoadCloneAnimatorController("HAOLAN-GestureFace");
            controller.name = "HAOLAN-GestureFace.converted";
            AnyStateToEntryExit.Execute(_state, new AOAnimatorController(controller));
            var except = LoadAnimatorController("HAOLAN-GestureFace.converted");
            RecursiveCheckEquals(except, controller);
        }
    }
}
