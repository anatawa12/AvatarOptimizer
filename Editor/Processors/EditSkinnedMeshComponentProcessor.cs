using System.Linq;
using Anatawa12.AvatarOptimizer.ndmf;
using nadena.dev.ndmf;
using UnityEngine;
using UnityEngine.Profiling;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class EditSkinnedMeshComponentProcessor : Pass<EditSkinnedMeshComponentProcessor>
    {
        public override string DisplayName => "EditSkinnedMeshComponent";

        protected override void Execute(BuildContext context)
        {
            Profiler.BeginSample("Initialize SkinnedMeshEditorSorter");
            var graph = new SkinnedMeshEditorSorter();
            foreach (var component in context.GetComponents<EditSkinnedMeshComponent>())
                graph.AddComponent(component);
            Profiler.EndSample();

            var renderers = context.GetComponents<SkinnedMeshRenderer>().
                Concat<Renderer>(context.GetComponents<MeshRenderer>());
            var processorLists = graph.GetSortedProcessors(renderers);
            foreach (var processors in processorLists)
            {
                Profiler.BeginSample($"EditSkinnedMeshComponents");
                var target = context.GetMeshInfoFor(processors.Target);

                foreach (var processor in processors.GetSorted())
                {
                    Profiler.BeginSample($"{processor.GetType().Name}");
                    using (ErrorReport.WithContextObject(processor.Component)) processor.Process(context, target);
                    target.AssertInvariantContract(
                        $"after {processor.GetType().Name} " +
                        $"for {processor.TargetGeneric.gameObject.name}");
                    DestroyTracker.DestroyImmediate(processor.Component);
                    Profiler.EndSample();
                }
                Profiler.EndSample();
            }
        }
    }
}
