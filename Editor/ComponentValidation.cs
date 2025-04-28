using Anatawa12.AvatarOptimizer.Processors;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    internal static class ComponentValidation
    {
        public static void ValidateAll(GameObject root)
        {
            foreach (var component in root.GetComponentsInChildren<AvatarTagComponent>(true))
            {
                switch (component)
                {
#if AAO_VRCSDK3_AVATARS
                    case MergePhysBone mergePhysBone:
                    {
                        using (ErrorReport.WithContextObject(mergePhysBone))
                            MergePhysBoneValidator.Validate(mergePhysBone);
                        break;
                    }
#endif
                    case MergeSkinnedMesh mergeSkinnedMesh:
                    {
                        var smr = mergeSkinnedMesh.GetComponent<SkinnedMeshRenderer>();
                        if (smr.sharedMesh)
                            BuildLog.LogWarning("MergeSkinnedMesh:warning:MeshIsNotNone", mergeSkinnedMesh);

                        if (mergeSkinnedMesh.renderersSet.GetAsSet().Contains(smr))
                            BuildLog.LogError("MergeSkinnedMesh:validation:self-recursive", mergeSkinnedMesh);
                        break;
                    }
                    case MergeBone mergeBone:
                    {
                        MergeBoneProcessor.Validate(mergeBone, root);
                        break;
                    }
                    case AvatarGlobalComponent _:
                    {
                        if (component.transform != root.transform)
                            BuildLog.LogError("AvatarGlobalComponent:NotOnAvatarRoot", component);
                        break;
                    }
                }

                if (component is INoSourceEditSkinnedMeshComponent)
                {
                    if (component.TryGetComponent<ISourceSkinnedMeshComponent>(out _))
                        BuildLog.LogError("NoSourceEditSkinnedMeshComponent:HasSourceSkinnedMeshComponent", component);
                }
            }
        }
    }
}
