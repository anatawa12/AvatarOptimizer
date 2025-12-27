using System.Collections.Generic;
using nadena.dev.ndmf;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    class InternalRemoveEmptySubMesh : EditSkinnedMeshComponent
    {
    }

    internal class RemoveEmptySubMeshProcessor : EditSkinnedMeshProcessor<InternalRemoveEmptySubMesh>
    {
        public RemoveEmptySubMeshProcessor(InternalRemoveEmptySubMesh component) : base(component)
        {
        }

        public override EditSkinnedMeshProcessorOrder ProcessOrder => EditSkinnedMeshProcessorOrder.AfterRemoveMesh;

        public override void Process(BuildContext context, MeshInfo2 target)
        {
            var srcIndex = 0;
            var dstIndex = 0;

            var mappings = new List<(string old, string @new)>();

            while (srcIndex < target.SubMeshes.Count)
            {
                if (target.SubMeshes[srcIndex].Vertices.Count == 0)
                {
                    srcIndex++;
                }
                else
                {
                    if (srcIndex != dstIndex)
                    {
                        target.SubMeshes[dstIndex] = target.SubMeshes[srcIndex];
                        mappings.Add(($"m_Materials.Array.data[{srcIndex}]",
                            $"m_Materials.Array.data[{dstIndex}]"));
                    }
                    srcIndex++;
                    dstIndex++;
                }
            }

            target.SubMeshes.RemoveRange(dstIndex, target.SubMeshes.Count - dstIndex);

            context.RecordMoveProperties(TargetGeneric, mappings.ToArray());
        }

        public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => upstream;
    }
}
