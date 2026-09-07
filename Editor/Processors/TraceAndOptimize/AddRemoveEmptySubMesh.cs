using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    internal class AddRemoveEmptySubMesh : TraceAndOptimizePass<AddRemoveEmptySubMesh>
    {
        public override string DisplayName => "T&O: RemoveEmptySubMesh";
        protected override bool Enabled(TraceAndOptimizeState state) => state.RemoveEmptySubMesh;

        protected override void Execute(BuildContext context, TraceAndOptimizeState state)
        {
            var componentInfos = context.Extension<GCComponentInfoContext>();
            var map = DependantMap.CreateDependantsMap(context);
            foreach (var renderer in context.GetComponents<SkinnedMeshRenderer>())
            {
                if (state.Exclusions.Contains(renderer.gameObject))
                    continue;

                var info = componentInfos.GetInfo(renderer);
                if (map[info].Keys.Any(x => x is ParticleSystem))
                    continue;
                renderer.gameObject.AddComponent<SkinnedMeshes.InternalRemoveEmptySubMesh>();
            }
        }
    }
}
