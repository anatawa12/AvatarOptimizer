using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    public class ConfigureRemoveZeroSizedPolygon : Pass<ConfigureRemoveZeroSizedPolygon>
    {
        public override string DisplayName => "T&O: ConfigureRemoveZeroSizedPolygon";

        protected override void Execute(BuildContext context)
        {
            var state = context.GetState<TraceAndOptimizeState>();
            if (!state.RemoveZeroSizedPolygon) return;

            foreach (var renderer in context.GetComponents<SkinnedMeshRenderer>())
                renderer.gameObject.GetOrAddComponent<RemoveZeroSizedPolygon>();
        }
    }
}