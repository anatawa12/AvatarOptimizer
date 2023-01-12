using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class EditSkinnedMeshComponentProcessor
    {
        public void Process(OptimizerSession session)
        {
            foreach (var component in session.GetComponents<EditSkinnedMeshComponent>())
                EditSkinnedMeshComponentUtil.OnAwake(component);
            var renderers = session.GetComponents<SkinnedMeshRenderer>();
            var processorLists = EditSkinnedMeshComponentUtil.GetSortedProcessors(renderers);
            foreach (var processors in processorLists)
            foreach (var processor in processors)
                processor.Process(session);
        }
    }
}
