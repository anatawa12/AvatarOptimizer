using Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer;
using NUnit.Framework;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorOptimizer
{
    public class MergeBlendTreeLayersTest : AnimatorOptimizerTestBase
    {
        [Test]
        public void Merge()
        {
            var controller = LoadCloneAnimatorController("ConvertibleSimple");
            controller.name = "ConvertibleSimple.converted";
            MergeBlendTreeLayers.Execute(new AOAnimatorController(controller));
            var except = LoadAnimatorController("ConvertibleSimple.converted");
            RecursiveCheckEquals(except, controller);
        }
    }
}
