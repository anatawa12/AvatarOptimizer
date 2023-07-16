namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class AutomaticConfigurationProcessor
    {
        private AutomaticConfiguration _config;

        public void Process(OptimizerSession session)
        {
            _config = session.GetRootComponent<AutomaticConfiguration>();
            if (!_config) return;

            // TODO: implement
        }
    }
}