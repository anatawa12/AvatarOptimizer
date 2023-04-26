using System;

namespace Anatawa12.AvatarOptimizer.Processors
{
    class UnusedBonesByReferencesToolProcessor
    {
        public void Process(OptimizerSession session)
        {
            var configuration = session.GetRootComponent<UnusedBonesByReferencesTool>();
            if (!configuration) return;

            throw new NotImplementedException();
        }
    }
}
