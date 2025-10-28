using System.Threading.Tasks;
using Anatawa12.AvatarOptimizer.APIInternal;
using nadena.dev.ndmf;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;

internal class EvacuateProcessor : EditSkinnedMeshProcessor<InternalEvacuateUVChannel>
{
    public EvacuateProcessor(InternalEvacuateUVChannel component) : base(component)
    {
    }

    public override EditSkinnedMeshProcessorOrder ProcessOrder => EditSkinnedMeshProcessorOrder.Evacuate;

    public override void Process(BuildContext context, MeshInfo2 target)
    {
        var swaps = Component.evacuations;

        Parallel.ForEach(target.Vertices, vertex =>
        {
            foreach (var swap in swaps)
            {
                var original = vertex.GetTexCoord(swap.originalChannel);
                var saved = vertex.GetTexCoord(swap.savedChannel);

                vertex.SetTexCoord(swap.originalChannel, saved);
                vertex.SetTexCoord(swap.savedChannel, original);
            }
        });
    }

    public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => upstream;
}

internal class RevertEvacuateProcessor : EditSkinnedMeshProcessor<InternalRevertEvacuateUVChannel>
{
    public RevertEvacuateProcessor(InternalRevertEvacuateUVChannel component) : base(component)
    {
    }

    public override EditSkinnedMeshProcessorOrder ProcessOrder => EditSkinnedMeshProcessorOrder.ReverseEvacuate;

    public override void Process(BuildContext context, MeshInfo2 target)
    {
        var swaps = Component.evacuate.evacuations;

        Parallel.ForEach(target.Vertices, vertex =>
        {
            for (var index = swaps.Count - 1; index >= 0; index--)
            {
                var swap = swaps[index];
                var saved = vertex.GetTexCoord(swap.savedChannel);

                vertex.SetTexCoord(swap.originalChannel, saved);
            }
        });
        
        // remove saved UV
        for (var index = swaps.Count - 1; index >= 0; index--)
        {
            var swap = swaps[index];
            target.SetTexCoordStatus(swap.savedChannel, TexCoordStatus.NotDefined);
        }
    }

    public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => upstream;
}
