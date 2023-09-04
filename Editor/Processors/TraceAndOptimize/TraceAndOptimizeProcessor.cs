using Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;
using JetBrains.Annotations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class TraceAndOptimizeProcessor
    {
        private ImmutableModificationsContainer _modifications;
        [CanBeNull] private TraceAndOptimize _config;

        public void Process(OptimizerSession session)
        {
            _config = session.GetRootComponent<TraceAndOptimize>();
            if (_config is null) return;
            Object.DestroyImmediate(_config);

            _modifications = new AnimatorParser(_config).GatherAnimationModifications(session);
            if (_config.freezeBlendShape)
                new AutoFreezeBlendShape(_modifications, session).Process();
        }

        public void ProcessLater(OptimizerSession session)
        {
            if (_config is null) return;

            if (_config.removeUnusedObjects)
                new FindUnusedObjectsProcessor(_modifications, session).Process();
        }
    }
}
