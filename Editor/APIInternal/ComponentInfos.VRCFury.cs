using Anatawa12.AvatarOptimizer.API;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.APIInternal.Externals
{
    // It's a No-op component, so empty implementation
    [ComponentInformationWithGUID("19d6be1140c9472cbc89e515ffd74126", 11500000)]
    internal class VRCFuryDebugInfoInformation : ComponentInformation<Component>, IExternalMarker
    {
        protected override void CollectDependency(Component component, ComponentDependencyCollector collector)
        {
        }

        protected override void CollectMutations(Component component, ComponentMutationsCollector collector)
        {
        }

        protected override void ApplySpecialMapping(Component component, MappingSource mappingSource)
        {
        }
    }

    // VF.Model.VRCFuryTest
    [ComponentInformationWithGUID("8ef9e6b301ba4ac59c2f7d2b836565e7", 11500000)]
    internal class VRCFuryTestInformation : ComponentInformation<Component>, IExternalMarker
    {
        protected override void CollectDependency(Component component, ComponentDependencyCollector collector)
        {
            // Keep the component if the GameObject lives
            collector.AddDependency(component, component.transform).EvenIfDependantDisabled();
        }

        protected override void CollectMutations(Component component, ComponentMutationsCollector collector)
        {
        }

        protected override void ApplySpecialMapping(Component component, MappingSource mappingSource)
        {
        }
    }
}
