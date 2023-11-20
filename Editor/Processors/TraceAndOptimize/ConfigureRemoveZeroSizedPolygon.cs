using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    internal class ConfigureRemoveZeroSizedPolygon : TraceAndOptimizePass<ConfigureRemoveZeroSizedPolygon>
    {
        public override string DisplayName => "T&O: ConfigureRemoveZeroSizedPolygon";

        protected override void Execute(BuildContext context, TraceAndOptimizeState state)
        {
            if (!state.RemoveZeroSizedPolygon) return;

            foreach (var renderer in context.GetComponents<SkinnedMeshRenderer>())
                renderer.gameObject.GetOrAddComponent<RemoveZeroSizedPolygon>();
        }
    }
}