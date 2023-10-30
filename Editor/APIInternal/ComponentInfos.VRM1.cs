#if AAO_VRM1

using System.Linq;
using Anatawa12.AvatarOptimizer.API;
using UniHumanoid;
using UnityEngine;
using UniVRM10;

namespace Anatawa12.AvatarOptimizer.APIInternal
{

    // NOTE: VRM1 bones are not animated their enabled states, therefore no need to configure ComponentDependencyInfo

    [ComponentInformation(typeof(Vrm10Instance))]
    internal class Vrm10InstanceInformation : ComponentInformation<Vrm10Instance>
    {
        protected override void CollectDependency(Vrm10Instance component, ComponentDependencyCollector collector)
        {
            // XXX we need BuildContext.AvatarRootTransform, assume this is VRM1 avatar...
            var avatarRootTransform = component.GetComponentInParent<Vrm10Instance>().transform;

            collector.MarkEntrypoint();
            
            // SpringBones

            foreach (var spring in component.SpringBone.Springs)
            {
                foreach (var joint in spring.Joints) collector.AddDependency(joint);
                foreach (var collider in spring.ColliderGroups) collector.AddDependency(collider);
            }
            
            // Expressions
            
            foreach (var clip in component.Vrm.Expression.Clips.Select(c => c.Clip))
            {
                foreach (var binding in clip.MorphTargetBindings)
                {
                    var target = avatarRootTransform.Find(binding.RelativePath);
                    collector.AddDependency(target, component);
                    collector.AddDependency(target);
                }
                foreach (var materialUVBinding in clip.MaterialUVBindings)
                {
                    // XXX I don't know what to do with BlendShape materials, so I pretend material names does not change (ex. MergeToonLitMaterial)
                }
                foreach (var materialColorBinding in clip.MaterialColorBindings)
                {
                    // XXX I don't know what to do with BlendShape materials, so I pretend material names does not change (ex. MergeToonLitMaterial)
                }
            }

            // First Person and LookAt
        }
    }

    [ComponentInformation(typeof(VRM10SpringBoneColliderGroup))]
    internal class Vrm10SpringBoneColliderGroupInformation : ComponentInformation<VRM10SpringBoneColliderGroup>
    {
        protected override void CollectDependency(VRM10SpringBoneColliderGroup component,
            ComponentDependencyCollector collector)
        {
            foreach (var collider in component.Colliders) collector.AddDependency(collider);
        }
    }

    [ComponentInformation(typeof(VRM10SpringBoneJoint))]
    [ComponentInformation(typeof(VRM10SpringBoneCollider))]
    internal class Vrm10ReferenceHolderInformation : ComponentInformation<Component>
    {
        protected override void CollectDependency(Component component,
            ComponentDependencyCollector collector)
        {
        }
    }

    [ComponentInformation(typeof(Vrm10AimConstraint))]
    internal class Vrm10AimConstraintInformation : ComponentInformation<Vrm10AimConstraint>
    {
        protected override void CollectDependency(Vrm10AimConstraint component,
            ComponentDependencyCollector collector)
        {
            collector.MarkBehaviour();
            collector.AddDependency(component.transform, component.Source);
        }
    }
      
    [ComponentInformation(typeof(Vrm10RollConstraint))]
    internal class Vrm10RollConstraintInformation : ComponentInformation<Vrm10RollConstraint>
    {
        protected override void CollectDependency(Vrm10RollConstraint component,
            ComponentDependencyCollector collector)
        {
            collector.MarkBehaviour();
            collector.AddDependency(component.transform, component.Source);
        }
    }

    [ComponentInformation(typeof(Vrm10RotationConstraint))]
    internal class Vrm10RotationConstraintInformation : ComponentInformation<Vrm10RotationConstraint>
    {
        protected override void CollectDependency(Vrm10RotationConstraint component,
            ComponentDependencyCollector collector)
        {
            collector.MarkBehaviour();
            collector.AddDependency(component.transform, component.Source);
        }
    }

    [ComponentInformation(typeof(Humanoid))]
    internal class HumanoidInformation : ComponentInformation<Humanoid>
    {
        protected override void CollectDependency(Humanoid component, ComponentDependencyCollector collector)
        {
            collector.MarkBehaviour();
            foreach ((Transform transform, HumanBodyBones) bone in component.BoneMap)
            {
                collector.AddDependency(bone.transform);
            }
        }
    }

}

#endif