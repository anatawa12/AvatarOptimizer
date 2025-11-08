using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;

internal class OptimizationWarnings : TraceAndOptimizePass<OptimizationWarnings>
{
    public override string DisplayName => "T&O: Analyze and show Optimization warnings";

    protected override void Execute(BuildContext context, TraceAndOptimizeState state)
    {
        if (state.SkipOptimizationWarnings) return;

        // Warning if some lints with almost-no-false-positives.
        // DO NOT add warnings with possible false positive.

        // Warn if multi-pass-rendering with exactly same materials.
        foreach (var smr in context.AvatarRootObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var meshInfo2 = context.GetMeshInfoFor(smr);
            foreach (var subMesh in meshInfo2.SubMeshes)
            {
                var multiPassRendering = subMesh.SharedMaterials.GroupBy(x => x).Where(x => x.Count() > 1).Select(x => x.Key);
                if (multiPassRendering.Any())
                {
                    BuildLog.LogWarning("OptimizationWarnings:multi-pass-rendering-with-same-material", 
                        multiPassRendering,
                        smr);
                }
            }
        }
    }
}
