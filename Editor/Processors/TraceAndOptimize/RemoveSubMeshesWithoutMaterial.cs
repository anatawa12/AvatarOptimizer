using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;

class RemoveSubMeshesWithoutMaterial : TraceAndOptimizePass<RemoveSubMeshesWithoutMaterial>
{
    protected override void Execute(BuildContext context, TraceAndOptimizeState state)
    {
        if (state.SkipRemoveUnusedSubMesh) return;

        var componentInfos = context.Extension<GCComponentInfoContext>();
        var entrypointMap = DependantMap.CreateEntrypointsMap(context);

        foreach (var renderer in context.AvatarRootObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (!renderer) continue;
            var meshInfo2 = context.GetMeshInfoFor(renderer);
            if (componentInfos.TryGetInfo(renderer) is not {} componentInfo) continue;
            if (entrypointMap[componentInfo].Count == 1 &&
                entrypointMap[componentInfo].ContainsKey(renderer))
            {
                // The SkinnedMeshRenderer is only used by itself, therefore it is safe to remove subMeshes without materials.
                // Removing subMeshes without materials was performed in MeshInfo2 until 1.8.7 (inclusive).
                // However, it broke particle system shape module with SkinnedMeshRenderer so we have to check
                // for dependencies here.
                meshInfo2.SubMeshes.RemoveAll(x => x.SharedMaterials.Length == 0);
            }
        }
    }
}
