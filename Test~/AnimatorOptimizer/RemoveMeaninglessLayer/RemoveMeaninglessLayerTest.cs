using Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer;
using NUnit.Framework;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorOptimizer
{
    public class RemoveMeaninglessLayerTest : AnimatorOptimizerTestBase
    {
        [Test]
        public void TestAfterBlendTreeOptimization()
        {
            var controller = LoadCloneAnimatorController("AfterDirectBlendTree");
            var except = LoadAnimatorController("AfterDirectBlendTree.converted");
            controller.name = except.name;
            RemoveMeaninglessLayer.Execute(new AOAnimatorController(controller));
            RecursiveCheckEquals(except, controller);
        }
    }
}
