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
    [ComponentInformation(typeof(ONSPAudioSource))]
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
            collector.AddDependency(component.GetComponent<Animator>()).EvenIfDependantDisabled();
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
                case VRC_AvatarDescriptor.LipSyncStyle.JawFlapBlendShape:
                {
                    if (component.VisemeSkinnedMesh != null)
                    {
                        collector.ModifyProperties(component.VisemeSkinnedMesh,
                            $"blendShape.{component.MouthOpenBlendShapeName}");
                    } else {
                        BuildLog.LogWarning("ComponentInfos:VRCAvatarDescriptor:warning:NoVisemeSkinnedMesh", component);
                    }
                    break;
                }
                case VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape:
                {
                    if (component.VisemeSkinnedMesh != null)
                    {
                        collector.ModifyProperties(component.VisemeSkinnedMesh,
                            component.VisemeBlendShapes.Select(blendShape => $"blendShape.{blendShape}"));
                    } else {
                        BuildLog.LogWarning("ComponentInfos:VRCAvatarDescriptor:warning:NoVisemeSkinnedMesh", component);
                    }
                    break;
                }
                case VRC_AvatarDescriptor.LipSyncStyle.VisemeParameterOnly:
                    // NOP
                    break;
                default:
                    BuildLog.LogWarning("ComponentInfos:VRCAvatarDescriptor:warning:UnknownLipSyncStyle", 
                            component.lipSync.ToString(),
                            component);
                    break;
            }
        }

        protected override void ApplySpecialMapping(T component, MappingSource mappingSource)
        {
            base.ApplySpecialMapping(component, mappingSource);
            
            // NOTE: we should not check VisemeSkinnedMesh for null because it can be missing object
            switch (component.lipSync)
            {
                case VRC_AvatarDescriptor.LipSyncStyle.Default:
                    // TODO
                    break;
                case VRC_AvatarDescriptor.LipSyncStyle.JawFlapBone:
                    break;
                case VRC_AvatarDescriptor.LipSyncStyle.JawFlapBlendShape:
                {
                    var info = mappingSource.GetMappedComponent(component.VisemeSkinnedMesh);
                    if (info.TryMapProperty($"blendShape.{component.MouthOpenBlendShapeName}", out var mapped))
                    {
                        component.VisemeSkinnedMesh = mapped.Component as SkinnedMeshRenderer;
                        component.MouthOpenBlendShapeName = ParseBlendShapeProperty(mapped.Property);
                    }
                    else
                    {
                        component.VisemeSkinnedMesh = null;
                    }
                    break;
                }
                case VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape:
                {
                    var info = mappingSource.GetMappedComponent(component.VisemeSkinnedMesh);
                    component.VisemeSkinnedMesh = info.MappedComponent;
                    var removed = false;
                    foreach (ref var shapeName in component.VisemeBlendShapes.AsSpan())
                    {
                        if (info.TryMapProperty($"blendShape.{shapeName}", out var mapped)
                            && mapped.Component == info.MappedComponent)
                            shapeName = ParseBlendShapeProperty(mapped.Property);
                        else
                            removed = true;
                    }
                    if (removed)
                        BuildLog.LogError("ApplyObjectMapping:VRCAvatarDescriptor:viseme BlendShape Removed");
                    break;
                }
                case VRC_AvatarDescriptor.LipSyncStyle.VisemeParameterOnly:
                    // NOP
                    break;
                default:
                    // Warning Reported in CollectMutations
                    break;
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

            AddCollider(component.collider_head, "Head");
            AddCollider(component.collider_torso, "Torso");
            AddCollider(component.collider_footR, "FootR");
            AddCollider(component.collider_footL, "FootL");
            AddCollider(component.collider_handR, "HandR");
            AddCollider(component.collider_handL, "HandL");
            AddCollider(component.collider_fingerIndexL, "FingerIndexL");
            AddCollider(component.collider_fingerMiddleL, "FingerMiddleL");
            AddCollider(component.collider_fingerRingL, "FingerRingL");
            AddCollider(component.collider_fingerLittleL, "FingerLittleL");
            AddCollider(component.collider_fingerIndexR, "FingerIndexR");
            AddCollider(component.collider_fingerMiddleR, "FingerMiddleR");
            AddCollider(component.collider_fingerRingR, "FingerRingR");
            AddCollider(component.collider_fingerLittleR, "FingerLittleR");
            void AddCollider(VRCAvatarDescriptor.ColliderConfig collider, string where)
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
                        BuildLog.LogWarning("ComponentInfos:VRCAvatarDescriptor:warning:UnknownColliderState",
                                collider.ToString(), where,
                                component);
                        break;
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
                    case VRCAvatarDescriptor.EyelidType.Blendshapes:
                    {
                        if (component.customEyeLookSettings.eyelidsSkinnedMesh != null)
                        {
                            var skinnedMeshRenderer = component.customEyeLookSettings.eyelidsSkinnedMesh;
                            var mesh = skinnedMeshRenderer.sharedMesh;

                            if (mesh != null)
                            {
                                collector.ModifyProperties(skinnedMeshRenderer,
                                    from index in component.customEyeLookSettings.eyelidsBlendshapes
                                    where 0 <= index && index < mesh.blendShapeCount
                                    select $"blendShape.{mesh.GetBlendShapeName(index)}");
                            }
                            else
                            {
                                BuildLog.LogWarning("ComponentInfos:VRCAvatarDescriptor:warning:NoMeshInEyelidsSkinnedMesh",
                                        component);
                            }
                        }
                        else
                        {
                            BuildLog.LogWarning("ComponentInfos:VRCAvatarDescriptor:warning:NoEyelidsSkinnedMesh",
                                    component);
                        }
                    }
                        break;
                    default:
                        BuildLog.LogWarning("ComponentInfos:VRCAvatarDescriptor:warning:UnknownEyelidType", 
                                component.customEyeLookSettings.eyelidType.ToString(),
                                component);
                        break;
                }
            }
        }

        protected override void ApplySpecialMapping(VRCAvatarDescriptor component, MappingSource mappingSource)
        {
            base.ApplySpecialMapping(component, mappingSource);
            
            if (component.enableEyeLook)
            {
                // NOTE: we should not check eyelidsSkinnedMesh for null because it can be missing object
                switch (component.customEyeLookSettings.eyelidType)
                {
                    case VRCAvatarDescriptor.EyelidType.None:
                        break;
                    case VRCAvatarDescriptor.EyelidType.Bones:
                        break;
                    case VRCAvatarDescriptor.EyelidType.Blendshapes:
                    {
                        var info = mappingSource.GetMappedComponent(component.customEyeLookSettings.eyelidsSkinnedMesh);
                        component.customEyeLookSettings.eyelidsSkinnedMesh = info.MappedComponent;
                        var removed = false;
                        foreach (ref var eyelidsBlendshape in component.customEyeLookSettings.eyelidsBlendshapes.AsSpan())
                        {
                            if (info.TryMapProperty(VProp.BlendShapeIndex(eyelidsBlendshape), out var mapped)
                                && mapped.Component == info.MappedComponent)
                                eyelidsBlendshape = VProp.ParseBlendShapeIndex(mapped.Property);
                            else
                                removed = true;
                        }
                        
                        if (removed)
                            BuildLog.LogError("ApplyObjectMapping:VRCAvatarDescriptor:eyelids BlendShape Removed");
                    }
                        break;
                    default:
                        // Warning Reported in CollectMutations
                        break;
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

    [ComponentInformation(typeof(VRCImpostorSettings))]
    internal class VRCImpostorSettingsInformation : ComponentInformation<VRCImpostorSettings>
    {
        protected override void CollectDependency(VRCImpostorSettings component, ComponentDependencyCollector collector)
        {
            foreach (var transform in component.transformsToIgnore)
                collector.AddDependency(transform);
            foreach (var transform in component.reparentHere)
                collector.AddDependency(transform);
            foreach (var transform in component.extraChildTransforms)
                collector.AddDependency(transform);
        }
    }
}
#endif
