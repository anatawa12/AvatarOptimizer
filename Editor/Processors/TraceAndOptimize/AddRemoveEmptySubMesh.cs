using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    internal class AddRemoveEmptySubMesh : TraceAndOptimizePass<AddRemoveEmptySubMesh>
    {
        public override string DisplayName => "T&O: RemoveEmptySubMesh";

        protected override void Execute(BuildContext context, TraceAndOptimizeState state)
        {
            if (!state.RemoveEmptySubMesh) return;
            foreach (var renderer in context.GetComponents<SkinnedMeshRenderer>())
            {
                if (state.Exclusions.Contains(renderer.gameObject))
                    continue;
                renderer.gameObject.AddComponent<SkinnedMeshes.InternalRemoveEmptySubMesh>();
            }
        }
    }
}
