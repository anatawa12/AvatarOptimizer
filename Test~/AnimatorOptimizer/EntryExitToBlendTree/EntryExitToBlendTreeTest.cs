using Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer;
using NUnit.Framework;
using UnityEditor;

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
        public void GestureConvertibleWithIntOrBoolCondition()
        {
            var controller = LoadCloneAnimatorController("GestureConvertibleWithIntOrBoolCondition");
            controller.name = "GestureConvertibleWithIntOrBoolCondition.converted";
            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));
            var except = LoadAnimatorController("GestureConvertibleWithIntOrBoolCondition.converted");
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

        [Test]
        public void LinearToggleConvertible()
        {
            var controller = LoadCloneAnimatorController("LinearToggleConvertible");
            controller.name = "LinearToggleConvertible.converted";
            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));
            var except = LoadAnimatorController("LinearToggleConvertible.converted");
            RecursiveCheckEquals(except, controller);
        }

        [Test]
        public void LinearToggleNonConvertibleBecauseCurve()
        {
            var controller = LoadCloneAnimatorController("LinearToggleNonConvertibleBecauseCurve");
            controller.name = "LinearToggleNonConvertibleBecauseCurve";
            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));
            var except = LoadAnimatorController("LinearToggleNonConvertibleBecauseCurve");
            RecursiveCheckEquals(except, controller);
        }

        // https://github.com/anatawa12/AvatarOptimizer/issues/1505
        // IndexOutOfRangeException at AnimatorOptimizer with empty layers
        [Test]
        public void EmptyLayerNoErrors()
        {
            var controller = LoadCloneAnimatorController("EmptyLayer");
            controller.name = "EmptyLayer";
            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));
            var except = LoadAnimatorController("EmptyLayer");
            RecursiveCheckEquals(except, controller);
        }
    }
}
