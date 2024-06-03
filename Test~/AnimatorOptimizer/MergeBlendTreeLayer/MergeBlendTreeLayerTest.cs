using Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer;
using NUnit.Framework;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorOptimizer
{
    public class MergeBlendTreeLayerTest : AnimatorOptimizerTestBase
    {
        [Test]
        public void Merge()
        {
            var controller = LoadCloneAnimatorController("ConvertibleSimple");
            controller.name = "ConvertibleSimple.converted";
            MergeBlendTreeLayer.Execute(new AOAnimatorController(controller));
            var except = LoadAnimatorController("ConvertibleSimple.converted");
            RecursiveCheckEquals(except, controller);
        }
        
        [Test]
        public void BlockedCompletely()
        {
            var controller = LoadCloneAnimatorController("BlockedCompletely");
            controller.name = "BlockedCompletely.converted";
            MergeBlendTreeLayer.Execute(new AOAnimatorController(controller));
            var except = LoadAnimatorController("BlockedCompletely.converted");
            RecursiveCheckEquals(except, controller);
        }
        
        [Test]
        public void BlockedPartially()
        {
            var controller = LoadCloneAnimatorController("BlockedPartially");
            controller.name = "BlockedPartially.converted";
            MergeBlendTreeLayer.Execute(new AOAnimatorController(controller));
            var except = LoadAnimatorController("BlockedPartially.converted");
            RecursiveCheckEquals(except, controller);
        }
    }
}
