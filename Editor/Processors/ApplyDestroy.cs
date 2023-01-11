using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class ApplyDestroy
    {
        public void Apply(OptimizerSession session)
        {
            // replace all objects
            foreach (var toDestroy in session.GetObjectsToDestroy())
                Object.DestroyImmediate(toDestroy);
        }
    }
}
