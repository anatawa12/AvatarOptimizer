using Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer;
using NUnit.Framework;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorOptimizer
{
    public class EntryExitToBlendTreeTest : AnimatorOptimizerTestBase
    {
        private AnimatorOptimizerState _state = new AnimatorOptimizerState();

        [Test]
        public void GestureConvertibleSimple()
        {
            var controller = LoadCloneAnimatorController("GestureConvertibleSimple");
            controller.name = "GestureConvertibleSimple.converted";
            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));
            var except = LoadAnimatorController("GestureConvertibleSimple.converted");
            RecursiveCheckEquals(except, controller);
        }

        [Test]
        public void GestureNonConvertibleBecauseCurve()
        {
            var controller = LoadCloneAnimatorController("GestureNonConvertibleBecauseCurve");
            controller.name = "GestureNonConvertibleBecauseCurve";
            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));
            var except = LoadAnimatorController("GestureNonConvertibleBecauseCurve");
            RecursiveCheckEquals(except, controller);
        }
    }
}
