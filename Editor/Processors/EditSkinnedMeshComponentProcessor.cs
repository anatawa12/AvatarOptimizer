using Anatawa12.AvatarOptimizer.ErrorReporting;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class EditSkinnedMeshComponentProcessor
    {
        public void Process(OptimizerSession session)
        {
            var graph = new SkinnedMeshEditorSorter();
            foreach (var component in session.GetComponents<EditSkinnedMeshComponent>())
                graph.AddComponent(component);

            var holder = new MeshInfo2Holder();

            var renderers = session.GetComponents<SkinnedMeshRenderer>();
            var processorLists = graph.GetSortedProcessors(renderers);
            foreach (var processors in processorLists)
            {
                var target = holder.GetMeshInfoFor(processors.Target);

                foreach (var processor in processors.GetSorted())
                {
                    // TODO
                    BuildReport.ReportingObject(processor.Component, () => processor.Process(session, target, holder));
                    target.AssertInvariantContract(
                        $"after {processor.GetType().Name} " +
                        $"for {processor.Target.gameObject.name}");
                    Object.DestroyImmediate(processor.Component);
                }
            }

            holder.SaveToMesh(session);
        }
    }
}
