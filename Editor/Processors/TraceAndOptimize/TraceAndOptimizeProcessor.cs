namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class TraceAndOptimizeProcessor
    {
        public void Process(OptimizerSession session)
        {
            var config = session.GetRootComponent<TraceAndOptimize>();
            if (!config) return;

            var modifications = new TraceAndOptimizes.AnimatorParser(session, config).GatherAnimationModifications();
            if (config.freezeBlendShape)
                new TraceAndOptimizes.AutoFreezeBlendShape(modifications, session).Process();
            if (config.removeUnusedObjects)
                new TraceAndOptimizes.FindUnusedObjectsProcessor(modifications, session).Process();
        }
    }
}
