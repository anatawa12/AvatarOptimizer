namespace Anatawa12.AvatarOptimizer.Processors
{
    internal partial class AutomaticConfigurationProcessor
    {
        private AutomaticConfiguration _config;
        private OptimizerSession _session;
        

        public void Process(OptimizerSession session)
        {
            _session = session;
            _config = session.GetRootComponent<AutomaticConfiguration>();
            if (!_config) return;

            // TODO: implement
        }
    }
}
