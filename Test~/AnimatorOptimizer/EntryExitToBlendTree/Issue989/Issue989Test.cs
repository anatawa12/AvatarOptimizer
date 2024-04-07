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
        // Parameter values 1, 3, 4 have corresponding states and motions
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
        // Parameter values 1, 2, 4 have corresponding states and motions
        [Test]
        public void FXWithout3()
        {
            var controller = LoadCloneAnimatorController("FX_Without_3");
            controller.name = "FX_Without_3.converted";
            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));
            var except = LoadAnimatorController("FX_Without_3.converted");
            RecursiveCheckEquals(except, controller);
        }

        // FX_Without_2_With_Same_Motion.controller
        // Parameter values 1, 3, 4 have corresponding states and the states 1, 3 have same motion
        [Test]
        public void FXWithout2WithSameMotion()
        {
            var controller = LoadCloneAnimatorController("FX_Without_2_With_Same_Motion");
            controller.name = "FX_Without_2_With_Same_Motion.converted";
            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));
            var except = LoadAnimatorController("FX_Without_2_With_Same_Motion.converted");
            RecursiveCheckEquals(except, controller);
        }

        // FX_Without_3_With_Same_Motion.controller
        // Parameter values 1, 2, 4 have corresponding states and the states 2, 4 have same motion
        [Test]
        public void FXWithout3WithSameMotion()
        {
            var controller = LoadCloneAnimatorController("FX_Without_3_With_Same_Motion");
            controller.name = "FX_Without_3_With_Same_Motion.converted";
            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));
            var except = LoadAnimatorController("FX_Without_3_With_Same_Motion.converted");
            RecursiveCheckEquals(except, controller);
        }
    }
}
