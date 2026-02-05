using NUnit.Framework;

namespace Anatawa12.AvatarOptimizer.Test.E2E
{
    [SetUpFixture]
    public class E2ETestSetup
    {
        [OneTimeSetUp]
        public void BeforeAll()
        {
            OptimizationMetricsSettings.EnableOptimizationMetrics = false;
        }
    }
}
