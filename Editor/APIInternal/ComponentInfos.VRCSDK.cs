#if AAO_VRCSDK3_AVATARS
using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.API;
using Anatawa12.AvatarOptimizer.ErrorReporting;
using JetBrains.Annotations;
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

        protected override void CollectMutations(T component, ComponentMutationsCollector collector)
        {
            base.CollectMutations(component, collector);
            switch (component.lipSync)
            {
                case VRC_AvatarDescriptor.LipSyncStyle.Default:
                    // TODO
                    break;
                case VRC_AvatarDescriptor.LipSyncStyle.JawFlapBone:
                    collector.TransformRotation(component.lipSyncJawBone);
                    break;
                case VRC_AvatarDescriptor.LipSyncStyle.JawFlapBlendShape when component.VisemeSkinnedMesh != null:
                {
                    collector.ModifyProperties(component.VisemeSkinnedMesh,
                        new[] { $"blendShape.{component.MouthOpenBlendShapeName}" });
                    break;
                }
                case VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape when component.VisemeSkinnedMesh != null:
                {
                    collector.ModifyProperties(component.VisemeSkinnedMesh,
                        component.VisemeBlendShapes.Select(blendShape => $"blendShape.{blendShape}"));
                    break;
                }
                case VRC_AvatarDescriptor.LipSyncStyle.VisemeParameterOnly:
                    // NOP
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal override void ApplySpecialMapping(T component, MappingSource mappingSource)
        {
            base.ApplySpecialMapping(component, mappingSource);
            
            switch (component.lipSync)
            {
                case VRC_AvatarDescriptor.LipSyncStyle.Default:
                    // TODO
                    break;
                case VRC_AvatarDescriptor.LipSyncStyle.JawFlapBone:
                    break;
                case VRC_AvatarDescriptor.LipSyncStyle.JawFlapBlendShape when component.VisemeSkinnedMesh != null:
                {
                    var info = mappingSource.GetMappedComponent(component.VisemeSkinnedMesh);
                    if (info.TryMapProperty($"blendShape.{component.MouthOpenBlendShapeName}", out var mapped))
                    {
                        component.VisemeSkinnedMesh = mapped.Item1 as SkinnedMeshRenderer;
                        component.MouthOpenBlendShapeName = ParseBlendShapeProperty(mapped.Item2);
                    }
                    else
                    {
                        component.VisemeSkinnedMesh = null;
                    }
                    break;
                }
                case VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape when component.VisemeSkinnedMesh != null:
                {
                    var info = mappingSource.GetMappedComponent(component.VisemeSkinnedMesh);
                    component.VisemeSkinnedMesh = info.MappedComponent;
                    var removed = false;
                    foreach (ref var shapeName in component.VisemeBlendShapes.AsSpan())
                    {
                        if (info.TryMapProperty($"blendShape.{shapeName}", out var mapped)
                            && mapped.component == info.MappedComponent)
                            shapeName = ParseBlendShapeProperty(mapped.property);
                        else
                            removed = true;
                    }
                    if (removed)
                        BuildReport.LogFatal("ApplyObjectMapping:VRCAvatarDescriptor:viseme BlendShape Removed");
                    break;
                }
                case VRC_AvatarDescriptor.LipSyncStyle.VisemeParameterOnly:
                    // NOP
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        [NotNull]
        private static string ParseBlendShapeProperty(string prop) =>
            prop.StartsWith("blendShape.", StringComparison.Ordinal)
                ? prop.Substring("blendShape.".Length)
                : throw new Exception("invalid blendShape property");
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

        protected override void CollectMutations(VRCAvatarDescriptor component, ComponentMutationsCollector collector)
        {
            base.CollectMutations(component, collector);

            if (component.enableEyeLook)
            {
                var leftEye = component.customEyeLookSettings.leftEye;
                var rightEye = component.customEyeLookSettings.rightEye;

                if (leftEye) collector.TransformRotation(leftEye);
                if (rightEye) collector.TransformRotation(rightEye);

                switch (component.customEyeLookSettings.eyelidType)
                {
                    case VRCAvatarDescriptor.EyelidType.None:
                        break;
                    case VRCAvatarDescriptor.EyelidType.Bones:
                    {
                        foreach (var eyelids in new[]
                                 {
                                     component.customEyeLookSettings.lowerLeftEyelid,
                                     component.customEyeLookSettings.upperLeftEyelid,
                                     component.customEyeLookSettings.lowerRightEyelid,
                                     component.customEyeLookSettings.upperRightEyelid,
                                 })
                            collector.TransformRotation(eyelids);
                    }
                        break;
                    case VRCAvatarDescriptor.EyelidType.Blendshapes
                        when component.customEyeLookSettings.eyelidsSkinnedMesh != null:
                    {
                        var skinnedMeshRenderer = component.customEyeLookSettings.eyelidsSkinnedMesh;
                        var mesh = skinnedMeshRenderer.sharedMesh;

                        collector.ModifyProperties(skinnedMeshRenderer,
                            from index in component.customEyeLookSettings.eyelidsBlendshapes
                            where 0 <= index && index < mesh.blendShapeCount
                            select $"blendShape.{mesh.GetBlendShapeName(index)}");
                    }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        internal override void ApplySpecialMapping(VRCAvatarDescriptor component, MappingSource mappingSource)
        {
            base.ApplySpecialMapping(component, mappingSource);
            
            if (component.enableEyeLook)
            {
                switch (component.customEyeLookSettings.eyelidType)
                {
                    case VRCAvatarDescriptor.EyelidType.None:
                        break;
                    case VRCAvatarDescriptor.EyelidType.Bones:
                        break;
                    case VRCAvatarDescriptor.EyelidType.Blendshapes
                        when component.customEyeLookSettings.eyelidsSkinnedMesh != null:
                    {
                        var info = mappingSource.GetMappedComponent(component.customEyeLookSettings.eyelidsSkinnedMesh);
                        component.customEyeLookSettings.eyelidsSkinnedMesh = info.MappedComponent;
                        var removed = false;
                        foreach (ref var eyelidsBlendshape in component.customEyeLookSettings.eyelidsBlendshapes.AsSpan())
                        {
                            if (info.TryMapProperty(VProp.BlendShapeIndex(eyelidsBlendshape), out var mapped)
                                && mapped.component == info.MappedComponent)
                                eyelidsBlendshape = VProp.ParseBlendShapeIndex(mapped.property);
                            else
                                removed = true;
                        }
                        
                        if (removed)
                            BuildReport.LogFatal("ApplyObjectMapping:VRCAvatarDescriptor:eyelids BlendShape Removed");
                    }
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
