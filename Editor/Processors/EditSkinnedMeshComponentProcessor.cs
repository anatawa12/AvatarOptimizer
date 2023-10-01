using Anatawa12.AvatarOptimizer.ErrorReporting;
using Anatawa12.AvatarOptimizer.ndmf;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class EditSkinnedMeshComponentProcessor : Pass<EditSkinnedMeshComponentProcessor>
    {
        public override string DisplayName => "EditSkinnedMeshComponent";

        protected override void Execute(BuildContext context)
        {
            var graph = new SkinnedMeshEditorSorter();
            foreach (var component in context.GetComponents<EditSkinnedMeshComponent>())
                graph.AddComponent(component);

            var renderers = context.GetComponents<SkinnedMeshRenderer>();
            var processorLists = graph.GetSortedProcessors(renderers);
            foreach (var processors in processorLists)
            {
                var target = context.GetMeshInfoFor(processors.Target);

                foreach (var processor in processors.GetSorted())
                {
                    BuildReport.ReportingObject(processor.Component, () => processor.Process(context, target));
                    target.AssertInvariantContract(
                        $"after {processor.GetType().Name} " +
                        $"for {processor.Target.gameObject.name}");
                    Object.DestroyImmediate(processor.Component);
                }
            }
        }
    }
}
