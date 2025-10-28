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
            // Add necessary components
            Profiler.BeginSample("Pre-Initialize SkinnedMeshEditorSorter");
            foreach (var byBlendShape in context.GetComponents<RemoveMeshByBlendShape>())
                if (!byBlendShape.gameObject.TryGetComponent<FreezeBlendShape>(out _))
                    byBlendShape.gameObject.AddComponent<FreezeBlendShape>();
            Profiler.EndSample();

            Profiler.BeginSample("Initialize SkinnedMeshEditorSorter");
            var graph = new SkinnedMeshEditorSorter();
            foreach (var component in context.GetComponents<EditSkinnedMeshComponent>())
                graph.AddComponent(component);
            Profiler.EndSample();

            var renderers = context.GetComponents<SkinnedMeshRenderer>();
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
                        $"for {processor.Target.gameObject.name}");
                    DestroyTracker.DestroyImmediate(processor.Component);
                    Profiler.EndSample();
                }
                Profiler.EndSample();
            }
        }
    }
}
