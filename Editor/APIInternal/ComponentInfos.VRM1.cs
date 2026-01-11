#if AAO_VRM1

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Anatawa12.AvatarOptimizer.API;
using UniGLTF.Extensions.VRMC_vrm;
using UnityEngine;
using UniVRM10;
using Humanoid = UniHumanoid.Humanoid;

namespace Anatawa12.AvatarOptimizer.APIInternal
{

    // NOTE: VRM1 bones are not animated their enabled states, therefore no need to configure ComponentDependencyInfo

    [ComponentInformation(typeof(Vrm10Instance))]
    internal class Vrm10InstanceInformation : ComponentInformation<Vrm10Instance>
    {
        protected override void CollectDependency(Vrm10Instance component, ComponentDependencyCollector collector)
        {
            var avatarRootTransform = component.transform;

            collector.MarkEntrypoint();
            if (component.TryGetComponent<Animator>(out var animator)) collector.AddDependency(animator);

            // SpringBones

            foreach (var spring in component.SpringBone.Springs)
            {
                foreach (var joint in spring.Joints) collector.AddDependency(joint);
                foreach (var collider in spring.ColliderGroups) collector.AddDependency(collider);
            }
            
            // Expressions
            
            // First Person and LookAt
            // NOTE: these dependencies are satisfied by either Animator or Humanoid 
            // collector.AddDependency(GetBoneTransformForVrm10(component, HumanBodyBones.Head));

            // if (component is { Vrm.LookAt.LookAtType: LookAtType.bone })
            // {
            //     if (GetBoneTransformForVrm10(component, HumanBodyBones.LeftEye) is Transform leftEye)
            //     {
            //         collector.AddDependency(leftEye);
            //     }
            //     if (GetBoneTransformForVrm10(component, HumanBodyBones.RightEye) is Transform rightEye)
            //     {
            //         collector.AddDependency(rightEye);
            //     }
            // }
        }
        
        protected override void CollectMutations(Vrm10Instance component, ComponentMutationsCollector collector)
        {
            // SpringBones
            foreach (var joint in component.SpringBone.Springs.SelectMany(spring => spring.Joints).Where(joint => joint))
            {
                collector.TransformPositionAndRotation(joint.transform);
            }

            // Expressions
            // BlendShape / Material mutations are collected through AnimatorParser, once we start tracking material changes

            // First Person and LookAt
            if (component is { Vrm.LookAt.LookAtType: LookAtType.bone })
            {
                if (GetBoneTransformForVrm10(component, HumanBodyBones.LeftEye) is Transform leftEye)
                {
                    collector.TransformRotation(leftEye);
                }
                if (GetBoneTransformForVrm10(component, HumanBodyBones.RightEye) is Transform rightEye)
                {
                    collector.TransformRotation(rightEye);
                }
            }
        }

        Transform? GetBoneTransformForVrm10(Vrm10Instance component, HumanBodyBones bones)
        {
            if (component.GetComponent<Humanoid>() is { } avatarHumanoid)
            {
                return avatarHumanoid.GetBoneTransform(bones);
            }
            
            if (component.GetComponent<Animator>() is { } avatarAnimator)
            {
                return avatarAnimator.GetBoneTransform(bones);
            }

            return null;
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
            collector.MarkHeavyBehaviour();
            collector.AddDependency(component.transform, component.Source);
        }

        protected override void CollectMutations(Vrm10AimConstraint component, ComponentMutationsCollector collector)
        {
            collector.TransformRotation(component.transform);
        }
    }
      
    [ComponentInformation(typeof(Vrm10RollConstraint))]
    internal class Vrm10RollConstraintInformation : ComponentInformation<Vrm10RollConstraint>
    {
        protected override void CollectDependency(Vrm10RollConstraint component,
            ComponentDependencyCollector collector)
        {
            collector.MarkHeavyBehaviour();
            collector.AddDependency(component.transform, component.Source);
        }

        protected override void CollectMutations(Vrm10RollConstraint component, ComponentMutationsCollector collector)
        {
            collector.TransformRotation(component.transform);
        }
    }

    [ComponentInformation(typeof(Vrm10RotationConstraint))]
    internal class Vrm10RotationConstraintInformation : ComponentInformation<Vrm10RotationConstraint>
    {
        protected override void CollectDependency(Vrm10RotationConstraint component,
            ComponentDependencyCollector collector)
        {
            collector.MarkHeavyBehaviour();
            collector.AddDependency(component.transform, component.Source);
        }

        protected override void CollectMutations(Vrm10RotationConstraint component, ComponentMutationsCollector collector)
        {
            collector.TransformRotation(component.transform);
        }
    }

    [ComponentInformation(typeof(Humanoid))]
    internal class HumanoidInformation : ComponentInformation<Humanoid>
    {
        protected override void CollectDependency(Humanoid component, ComponentDependencyCollector collector)
        {
            // VRM1 Humanoid has side effect because it overwrites Animator's Avatar on VRM1 export
            collector.MarkEntrypoint();

            // Use reflection to support UniVRM 0.99.4
            var boneMapProperty = typeof(Humanoid).GetProperty("BoneMap", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var boneMap = (IEnumerable<(Transform, HumanBodyBones)>)boneMapProperty.GetValue(component);
            foreach ((Transform transform, HumanBodyBones) bone in boneMap)
            {
                if (bone.transform) collector.AddDependency(bone.transform);
            }
        }
    }

}

#endif
