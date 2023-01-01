using UnityEngine;

namespace Anatawa12.Merger.Processors
{
    internal class ApplyDestroy
    {
        public void Apply(MergerSession session)
        {
            // replace all objects
            foreach (var toDestroy in session.GetObjectsToDestroy())
                Object.DestroyImmediate(toDestroy);
        }
    }
}
