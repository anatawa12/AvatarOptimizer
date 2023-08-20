namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class TraceAndOptimizeProcessor
    {
        public void Process(OptimizerSession session)
        {
            var config = session.GetRootComponent<TraceAndOptimize>();
            if (!config) return;

            var animationParser = new TraceAndOptimizes.AnimationParser(session, config);
            animationParser.GatherAnimationModifications();
            if (config.freezeBlendShape)
                new TraceAndOptimizes.AutoFreezeBlendShape(animationParser, session).Process();
            if (config.removeUnusedObjects)
                new TraceAndOptimizes.FindUnusedObjectsProcessor(animationParser, session).Process();
        }
    }
}
