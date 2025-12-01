using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    internal class ConfigureRemoveZeroSizedPolygon : TraceAndOptimizePass<ConfigureRemoveZeroSizedPolygon>
    {
        public override string DisplayName => "T&O: ConfigureRemoveZeroSizedPolygon";
        protected override bool Enabled(TraceAndOptimizeState state) => state.RemoveZeroSizedPolygon;

        protected override void Execute(BuildContext context, TraceAndOptimizeState state)
        {
            foreach (var renderer in context.GetComponents<SkinnedMeshRenderer>())
                if (!state.Exclusions.Contains(renderer.gameObject))
                    renderer.gameObject.GetOrAddComponent<RemoveZeroSizedPolygon>();
        }
    }
}
