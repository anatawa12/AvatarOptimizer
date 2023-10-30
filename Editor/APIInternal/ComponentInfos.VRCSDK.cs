#if AAO_VRCSDK3_AVATARS
using System;
using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.API;
using UnityEngine;
using VRC.SDK3;
using VRC.Core;
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDKBase;

namespace Anatawa12.AvatarOptimizer.APIInternal.VRCSDK
{
    [ComponentInformation(typeof(VRCTestMarker))]
#pragma warning disable CS0618
    [ComponentInformation(typeof(PipelineSaver))]
#pragma warning restore CS0618
    [ComponentInformation(typeof(PipelineManager))]
    [ComponentInformation(typeof(VRCSpatialAudioSource))]
    [ComponentInformation(typeof(VRC_SpatialAudioSource))]
    // nadena.dev.ndmf.VRChat.ContextHolder with reflection
    internal class EntrypointComponentInformation : ComponentInformation<Component>
    {
        protected override void CollectDependency(Component component, ComponentDependencyCollector collector)
        {
            collector.MarkEntrypoint();
        }
    }

    [ComponentInformation(typeof(VRC_AvatarDescriptor))]
    internal class VRCAvatarDescriptorInformation<T> : ComponentInformation<T> where T : VRC_AvatarDescriptor
    {
        protected override void CollectDependency(T component,
            ComponentDependencyCollector collector)
        {
            collector.MarkEntrypoint();
            collector.AddDependency(component.GetComponent<PipelineManager>()).EvenIfDependantDisabled();
        }
    }

    [ComponentInformation(typeof(VRCAvatarDescriptor))]
    internal class VRCAvatarDescriptorInformation : VRCAvatarDescriptorInformation<VRCAvatarDescriptor>
    {
        protected override void CollectDependency(VRCAvatarDescriptor component,
            ComponentDependencyCollector collector)
        {
            base.CollectDependency(component, collector);

            AddCollider(component.collider_head);
            AddCollider(component.collider_torso);
            AddCollider(component.collider_footR);
            AddCollider(component.collider_footL);
            AddCollider(component.collider_handR);
            AddCollider(component.collider_handL);
            AddCollider(component.collider_fingerIndexL);
            AddCollider(component.collider_fingerMiddleL);
            AddCollider(component.collider_fingerRingL);
            AddCollider(component.collider_fingerLittleL);
            AddCollider(component.collider_fingerIndexR);
            AddCollider(component.collider_fingerMiddleR);
            AddCollider(component.collider_fingerRingR);
            AddCollider(component.collider_fingerLittleR);
            void AddCollider(VRCAvatarDescriptor.ColliderConfig collider)
            {
                switch (collider.state)
                {
                    case VRCAvatarDescriptor.ColliderConfig.State.Automatic:
                    case VRCAvatarDescriptor.ColliderConfig.State.Custom:
                        collector.AddDependency(collider.transform).EvenIfDependantDisabled();
                        break;
                    case VRCAvatarDescriptor.ColliderConfig.State.Disabled:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }

    [ComponentInformation(typeof(VRC.SDKBase.VRCStation))]
    [ComponentInformation(typeof(VRC.SDK3.Avatars.Components.VRCStation))]
    internal class VRCStationInformation : ComponentInformation<VRC.SDKBase.VRCStation>
    {
        protected override void CollectDependency(VRC.SDKBase.VRCStation component, ComponentDependencyCollector collector)
        {
            // first, Transform <=> PhysBone
            // Transform is used even if the bone is inactive so Transform => PB is always dependency
            // PhysBone works only if enabled so PB => Transform is active dependency
            collector.MarkEntrypoint();
            collector.AddDependency(component.stationEnterPlayerLocation);
            collector.AddDependency(component.stationExitPlayerLocation);
            collector.AddDependency(component.GetComponentInChildren<Collider>());
        }

        protected override void CollectMutations(VRC.SDKBase.VRCStation component, ComponentMutationsCollector collector)
        {
        }
    }

    [ComponentInformation(typeof(VRCPhysBoneBase))]
    [ComponentInformation(typeof(VRCPhysBone))]
    internal class VRCPhysBoneInformation : ComponentInformation<VRCPhysBoneBase>
    {
        protected override void CollectDependency(VRCPhysBoneBase component, ComponentDependencyCollector collector)
        {
            // first, Transform <=> PhysBone
            // Transform is used even if the bone is inactive so Transform => PB is always dependency
            // PhysBone works only if enabled so PB => Transform is active dependency
            var ignoreTransforms = new HashSet<Transform>(component.ignoreTransforms);
            CollectTransforms(component.GetTarget());

            void CollectTransforms(Transform bone)
            {
                collector.AddDependency(bone, component).EvenIfDependantDisabled().OnlyIfTargetCanBeEnable();
                collector.AddDependency(bone);
                foreach (var child in bone.DirectChildrenEnumerable())
                {
                    if (!ignoreTransforms.Contains(child))
                        CollectTransforms(child);
                }
            }

            // then, PB => Collider
            // in PB, PB Colliders work only if Colliders are enabled
            foreach (var physBoneCollider in component.colliders)
                collector.AddDependency(physBoneCollider).OnlyIfTargetCanBeEnable();

            collector.MarkHeavyBehaviour();

            // If parameter is not empty, the PB can be required for Animator Parameter so it's Entrypoint Component
            // https://github.com/anatawa12/AvatarOptimizer/issues/450
            if (!string.IsNullOrEmpty(component.parameter))
                collector.MarkEntrypoint();
        }

        protected override void CollectMutations(VRCPhysBoneBase component, ComponentMutationsCollector collector)
        {
            foreach (var transform in component.GetAffectedTransforms())
                collector.TransformPositionAndRotation(transform);
        }
    }

    [ComponentInformation(typeof(VRCPhysBoneColliderBase))]
    [ComponentInformation(typeof(VRCPhysBoneCollider))]
    internal class VRCPhysBoneColliderInformation : ComponentInformation<VRCPhysBoneColliderBase>
    {
        protected override void CollectDependency(VRCPhysBoneColliderBase component,
            ComponentDependencyCollector collector)
        {
            collector.AddDependency(component.rootTransform);
        }
    }

    [ComponentInformation(typeof(ContactBase))]
    [ComponentInformation(typeof(ContactReceiver))]
    [ComponentInformation(typeof(VRCContactReceiver))]
    [ComponentInformation(typeof(ContactSender))]
    [ComponentInformation(typeof(VRCContactSender))]
    internal class ContactBaseInformation : ComponentInformation<ContactBase>
    {
        protected override void CollectDependency(ContactBase component, ComponentDependencyCollector collector)
        {
            collector.MarkEntrypoint();
            collector.AddDependency(component.rootTransform);
        }
    }
}
#endif
