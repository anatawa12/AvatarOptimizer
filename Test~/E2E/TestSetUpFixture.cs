using NUnit.Framework;

namespace Anatawa12.AvatarOptimizer.Test.E2E
{
    [SetUpFixture]
    public class TestSetUpFixture
    {
        [OneTimeSetUp]
        public void SetupAddFormatter() => TestUtils.AddValueFormatters();

        private bool _checkForUpdateEnabled;

        [OneTimeSetUp]
        public void DisableCheckForUpdate()
        {
            _checkForUpdateEnabled = CheckForUpdate.MenuItems.CheckForUpdateEnabled;
            CheckForUpdate.MenuItems.CheckForUpdateEnabled = false;
        }

        [OneTimeTearDown]
        public void RestoreCheckForUpdate()
        {
            CheckForUpdate.MenuItems.CheckForUpdateEnabled = _checkForUpdateEnabled;
        }
    }
}