#if AAO_VRM0

using Anatawa12.AvatarOptimizer.API;
using UnityEngine;
using VRM;

namespace Anatawa12.AvatarOptimizer.APIInternal
{

    // NOTE: VRM0 bones are not animated, therefore no need to configure ComponentDependencyInfo

    [ComponentInformation(typeof(VRMMeta))]
    internal class VRMMetaInformation : ComponentInformation<VRMMeta>
    {
        protected override void CollectDependency(VRMMeta component, ComponentDependencyCollector collector)
        {
            collector.MarkEntrypoint();
        }
    }

    [ComponentInformation(typeof(VRMSpringBone))]
    internal class VRMSpringBoneInformation : ComponentInformation<VRMSpringBone>
    {
        protected override void CollectDependency(VRMSpringBone component, ComponentDependencyCollector collector)
        {
            collector.MarkBehaviour();
            foreach (var transform in component.GetComponentsInChildren<Transform>()) collector.AddDependency(transform);
            foreach (var collider in component.ColliderGroups) collector.AddDependency(collider);
        }

    }

    [ComponentInformation(typeof(VRMSpringBoneColliderGroup))]
    internal class VRMSpringBoneColliderGroupInformation : ComponentInformation<VRMSpringBoneColliderGroup>
    {
        protected override void CollectDependency(VRMSpringBoneColliderGroup component,
            ComponentDependencyCollector collector)
        {
        }
    }

    [ComponentInformation(typeof(VRMBlendShapeProxy))]
    internal class VRMBlendShapeProxyInformation : ComponentInformation<VRMBlendShapeProxy>
    {
        protected override void CollectDependency(VRMBlendShapeProxy component, ComponentDependencyCollector collector)
        {
            // XXX we need BuildContext.AvatarRootTransform, assume this is VRM0 avatar...
            var avatarRootTransform = component.GetComponentInParent<VRMMeta>().transform;

            collector.MarkBehaviour();
            foreach (var clip in component.BlendShapeAvatar.Clips)
            {
                foreach (var binding in clip.Values)
                {
                    var target = avatarRootTransform.Find(binding.RelativePath);
                    collector.AddDependency(target, component);
                    collector.AddDependency(target);
                }
                foreach (var materialBinding in clip.MaterialValues)
                {
                    // XXX I don't know what to do with BlendShape materials, so I pretend material names does not change (ex. MergeToonLitMaterial)
                }
            }
        }
    }

    [ComponentInformation(typeof(VRMLookAtHead))]
    internal class VRMLookAtHeadInformation : ComponentInformation<VRMLookAtHead>
    {
        protected override void CollectDependency(VRMLookAtHead component, ComponentDependencyCollector collector)
        {
            collector.MarkBehaviour();
            collector.AddDependency(component.Head, component);
            collector.AddDependency(component.Head);
        }
    }

    [ComponentInformation(typeof(VRMLookAtBlendShapeApplyer))]
    internal class VRMLookAtBlendShapeApplyerInformation : ComponentInformation<VRMLookAtBlendShapeApplyer>
    {
        protected override void CollectDependency(VRMLookAtBlendShapeApplyer component, ComponentDependencyCollector collector)
        {
            collector.MarkBehaviour();
        }
    }


    [ComponentInformation(typeof(VRMFirstPerson))]
    internal class VRMFirstPersonInformation : ComponentInformation<VRMFirstPerson>
    {
        protected override void CollectDependency(VRMFirstPerson component, ComponentDependencyCollector collector)
        {
            collector.MarkBehaviour();
            collector.AddDependency(component.FirstPersonBone, component);
            collector.AddDependency(component.FirstPersonBone);
        }
    }

}

#endif