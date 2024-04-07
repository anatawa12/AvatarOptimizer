using Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer;
using NUnit.Framework;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorOptimizer
{
    // https://github.com/anatawa12/AvatarOptimizer/issues/989
    public class Issue989Test : AnimatorOptimizerTestBase
    {
        private AnimatorOptimizerState _state = new();
        public override string TestName => "EntryExitToBlendTree/Issue989";

        // FX_Without_2.controller
        [Test]
        public void FXWithout2()
        {
            var controller = LoadCloneAnimatorController("FX_Without_2");
            controller.name = "FX_Without_2.converted";
            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));
            var except = LoadAnimatorController("FX_Without_2.converted");
            RecursiveCheckEquals(except, controller);
        }

        // FX_Without_3.controller
        [Test]
        public void FXWithout3()
        {
            var controller = LoadCloneAnimatorController("FX_Without_3");
            controller.name = "FX_Without_3.converted";
            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));
            var except = LoadAnimatorController("FX_Without_3.converted");
            RecursiveCheckEquals(except, controller);
        }
    }
}
