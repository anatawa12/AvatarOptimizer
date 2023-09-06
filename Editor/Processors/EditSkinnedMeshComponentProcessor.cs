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

            session.MeshInfo2Holder = new MeshInfo2Holder();

            var renderers = session.GetComponents<SkinnedMeshRenderer>();
            var processorLists = graph.GetSortedProcessors(renderers);
            foreach (var processors in processorLists)
            {
                var target = session.MeshInfo2Holder.GetMeshInfoFor(processors.Target);

                foreach (var processor in processors.GetSorted())
                {
                    // TODO
                    BuildReport.ReportingObject(processor.Component, () => processor.Process(session, target));
                    target.AssertInvariantContract(
                        $"after {processor.GetType().Name} " +
                        $"for {processor.Target.gameObject.name}");
                    Object.DestroyImmediate(processor.Component);
                }
            }
        }
    }
}
