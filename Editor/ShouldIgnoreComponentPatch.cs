#if false // disable
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDKBase.Validation.Performance;

namespace Anatawa12.AvatarOptimizer
{
    [InitializeOnLoad]
    internal static class ShouldIgnoreComponentPatch
    {
        static ShouldIgnoreComponentPatch()
        {
            // The API is marked as 'PublicAPI' so I use it.
            var upstream = AvatarPerformance.ShouldIgnoreComponent;
            AvatarPerformance.ShouldIgnoreComponent = component =>
                (upstream?.Invoke(component) ?? false) || ShouldIgnoreComponent(component);
        }

        private static bool ShouldIgnoreComponent(Component component)
        {
            if (component is VRCPhysBoneBase)
            {
                // TODO: cache for performance
                if (Object.FindObjectsOfType<MergePhysBone>()
                    .SelectMany(x => x.componentsSet.GetAsSet())
                    .Any(x => x == component))
                {
                    return true;
                }
            }
            
            if (component is SkinnedMeshRenderer renderer)
                return EditSkinnedMeshComponentUtil.IsModifiedByEditComponent(renderer);

            return false;
        }
    }
}
#endif
