#if AAO_VRCSDK3_AVATARS
using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.API;
using nadena.dev.ndmf.runtime;
using UnityEngine;
using VRC.SDK3;
using VRC.Core;
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDKBase;

using VRC.Dynamics.ManagedTypes;
using VRC.SDK3.Dynamics.Constraint.Components;

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
            
            // for empty Armature trick which is only valid for VRCSDK, we need to keep parent objects of Hips bone
            if (component.TryGetComponent<Animator>(out var animator) && animator.isHuman)
            {
                var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                if (hips)
                {
                    var avatarRoot = component.gameObject;
                    foreach (var parent in hips.ParentEnumerable(avatarRoot.transform))
                    {
                        var path = RuntimeUtil.RelativePath(avatarRoot, parent.gameObject)!;
                        var parentByPath = Utils.ResolveAnimationPath(avatarRoot.transform, path);
                        collector.AddDependency(parentByPath).EvenIfDependantDisabled();
                    }
                }
            }
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
                    }
                    break;
                }
                case VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape:
                {
                    if (component.VisemeSkinnedMesh != null)
                    {
                        collector.ModifyProperties(component.VisemeSkinnedMesh,
                            component.VisemeBlendShapes.Select(blendShape => $"blendShape.{blendShape}"));
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

                    if (info.MappedComponent == null)
                    {
                        component.VisemeSkinnedMesh = null;
                        component.lipSync = VRC_AvatarDescriptor.LipSyncStyle.VisemeParameterOnly;
                    }
                    else
                    {
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
                    }

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
                        if (collider.transform) {
                            // The position of parent bone (iow local position of transform) will be used to 
                            // determine the position and rotation of the collider
                            collector.AddDependency(collider.transform).EvenIfDependantDisabled();
                            collector.AddDependency(collider.transform.parent).EvenIfDependantDisabled();
                        }
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
                        if (info.MappedComponent == null)
                        {
                            component.customEyeLookSettings.eyelidsSkinnedMesh = null;
                            component.customEyeLookSettings.eyelidType = VRCAvatarDescriptor.EyelidType.None;
                        }
                        else
                        {
                            component.customEyeLookSettings.eyelidsSkinnedMesh = info.MappedComponent;
                            var removed = false;
                            foreach (ref var eyelidsBlendshape in component.customEyeLookSettings.eyelidsBlendshapes
                                         .AsSpan())
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
        private static readonly Type? ParentChangeDetectorType = Type.GetType("VRC.Dynamics.ParentChangeDetector, VRC.Dynamics");

        protected override void CollectDependency(VRCPhysBoneBase component, ComponentDependencyCollector collector)
        {
            if (!IsOperatingPhysBone(component))
                return;
            
            var info = ((ComponentDependencyRetriever.Collector)collector)._info!;

            // first, Transform <=> PhysBone
            // Transform is used even if the bone is inactive so Transform => PB is always dependency
            // PhysBone works only if enabled so PB => Transform is active dependency
            var ignoreTransforms = new HashSet<Transform>(component.ignoreTransforms);
            CollectTransforms(component.GetTarget());

            void CollectTransforms(Transform bone)
            {
                collector.AddDependency(bone, component).EvenIfDependantDisabled().OnlyIfTargetCanBeEnable();
                info.AddDependency(bone, GCComponentInfo.DependencyType.PhysBone);
                foreach (var child in bone.DirectChildrenEnumerable())
                {
                    if (!ignoreTransforms.Contains(child))
                        CollectTransforms(child);
                }
            }

            if (ParentChangeDetectorType != null)
            {
                var target = component.GetTarget();
                if (target.TryGetComponent(ParentChangeDetectorType, out var parentChangeDetector))
                {
                    collector.AddDependency(parentChangeDetector);
                }
            }

            // then, PB => Collider
            // in PB, PB Colliders work only if Colliders are enabled
            foreach (var physBoneCollider in component.colliders)
                collector.AddDependency(physBoneCollider).OnlyIfTargetCanBeEnable();

            collector.MarkHeavyBehaviour();

            // If parameter is not empty, the PB can be required for Animator Parameter so it's Entrypoint Component
            // https://github.com/anatawa12/AvatarOptimizer/issues/450
            // https://github.com/anatawa12/AvatarOptimizer/issues/898
            if (!string.IsNullOrEmpty(component.parameter))
            {
                if (PhysBoneSuffix.Select(suffix => component.parameter + suffix)
                    .Any(collector.IsParameterUsed))
                {
                    collector.MarkEntrypoint();
                }
            }
        }

        public static void AddDependencyInformation(GCComponentInfo gcInfo, VRCPhysBone component, 
            GCComponentInfoContext gcContext)
        {
            if (!IsOperatingPhysBone(component))
                return;
            if (gcInfo.Activeness == false)
                return; // no-op if inactive

            // first, Transform <=> PhysBone
            // Transform is used even if the bone is inactive so Transform => PB is always dependency
            // PhysBone works only if enabled so PB => Transform is active dependency
            var ignoreTransforms = new HashSet<Transform>(component.ignoreTransforms);
            var targetRoot = component.GetTarget();
            if (gcContext.TryGetInfo(targetRoot) != null)
                CollectTransforms(targetRoot);

            void CollectTransforms(Transform bone)
            {
                gcContext.GetInfo(bone).AddDependency(component);
                gcInfo.AddDependency(bone, GCComponentInfo.DependencyType.PhysBone);
                foreach (var child in bone.DirectChildrenEnumerable())
                {
                    if (!ignoreTransforms.Contains(child))
                        CollectTransforms(child);
                }
            }

            // then, PB => Collider
            // in PB, PB Colliders work only if Colliders are enabled
            foreach (var physBoneCollider in component.colliders)
            {
                var colliderInfo = gcContext.TryGetInfo(physBoneCollider);
                if (colliderInfo?.Activeness != false)
                    gcInfo.AddDependency(physBoneCollider);
            }

            gcInfo.MarkHeavyBehaviour();

            // If parameter is not empty, the PB can be required for Animator Parameter so it's Entrypoint Component
            // https://github.com/anatawa12/AvatarOptimizer/issues/450
            // https://github.com/anatawa12/AvatarOptimizer/issues/898
            if (!string.IsNullOrEmpty(component.parameter))
            {
                if (PhysBoneSuffix.Select(suffix => component.parameter + suffix)
                    .Any(gcContext.IsParameterUsed.Invoke))
                {
                    gcInfo.MarkEntrypoint();
                }
            }
        }

        // https://creators.vrchat.com/avatars/avatar-dynamics/physbones#options
        private static string[] PhysBoneSuffix = {
            "_IsGrabbed",
            "_IsPosed",
            "_Angle",
            "_Stretch",
            "_Squish",
        };

        private static bool IsOperatingPhysBone(VRCPhysBoneBase component)
        {
            var ignoreTransforms = new HashSet<Transform>(component.ignoreTransforms);
            foreach (var bone in component.GetAffectedTransforms())
            {
                var childCount = bone.DirectChildrenEnumerable().Count(x => !ignoreTransforms.Contains(x));
                if (childCount == 0)
                {
                    // it's leaf bone: if endpoint position is not zero, it's swung
                    if (component.endpointPosition != Vector3.zero) return true;
                } 
                else if (childCount == 1)
                {
                    // single child: it's swung
                    return true;
                }
                else
                {
                    // two or more children: it's swung if multi child type is not Ignore
                    if (component.multiChildType != VRCPhysBoneBase.MultiChildType.Ignore)
                        return true;
                }
            }

            return false;
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

    [ComponentInformation(typeof(ContactReceiver))]
    [ComponentInformation(typeof(VRCContactReceiver))]
    internal class ContactReceiverInformation : ComponentInformation<ContactReceiver>
    {
        protected override void CollectDependency(ContactReceiver component, ComponentDependencyCollector collector)
        {
            // the contact receiver receives contact event from the sender
            if (collector.IsParameterUsed(component.parameter))
                collector.MarkEntrypoint();
            collector.AddDependency(component.rootTransform);
        }
    }

    [ComponentInformation(typeof(ContactSender))]
    [ComponentInformation(typeof(VRCContactSender))]
    internal class ContactSenderInformation : ComponentInformation<ContactSender>
    {
        protected override void CollectDependency(ContactSender component, ComponentDependencyCollector collector)
        {
            // contact sender is just exists to send contact event to the receiver
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
    
    // this component has no documentation so this implementation is based on assumption
    [ComponentInformation(typeof(VRCImpostorEnvironment))]
    internal class VRCImpostorEnvironmentInformation : ComponentInformation<VRCImpostorEnvironment>
    {
        protected override void CollectDependency(VRCImpostorEnvironment component, ComponentDependencyCollector collector)
        {
            // prevent from removing
            collector.MarkEntrypoint();
        }
    }

    [ComponentInformation(typeof(VRCHeadChop))]
    internal class VRCHeadChopInformation : ComponentInformation<VRCHeadChop>
    {
        protected override void CollectDependency(VRCHeadChop component, ComponentDependencyCollector collector)
        {
            // declare dependency relationship
            foreach (var headChopBone in component.targetBones)
            {
                collector.AddDependency(headChopBone.transform);
                collector.AddDependency(headChopBone.transform, component).EvenIfDependantDisabled();
            }

            collector.MarkBehaviour();
        }
    }

    [ComponentInformation(typeof(VRCConstraintBase))]
    [ComponentInformation(typeof(VRCParentConstraintBase))]
    [ComponentInformation(typeof(VRCParentConstraint))]
    [ComponentInformation(typeof(VRCPositionConstraintBase))]
    [ComponentInformation(typeof(VRCPositionConstraint))]
    [ComponentInformation(typeof(VRCRotationConstraintBase))]
    [ComponentInformation(typeof(VRCRotationConstraint))]
    [ComponentInformation(typeof(VRCScaleConstraintBase))]
    [ComponentInformation(typeof(VRCScaleConstraint))]
    internal class VRCConstraintInformation<T> : ComponentInformation<T> where T : VRCConstraintBase
    {
        protected override void CollectDependency(T component, ComponentDependencyCollector collector)
        {
            var target = component.TargetTransform != null ? component.TargetTransform : component.transform;
            collector.AddDependency(target, component)
                .OnlyIfTargetCanBeEnable()
                .EvenIfDependantDisabled();

            foreach (var source in component.Sources)
                collector.AddDependency(source.SourceTransform);

            // If the constraint is solved in local space, we need to preserve the parent transform of the source transforms and target transform.
            if (component.SolveInLocalSpace)
            {
                if (target.parent != null)
                    collector.AddDependency(target.parent);

                foreach (var source in component.Sources)
                    if (source.SourceTransform != null && source.SourceTransform.parent != null)
                        collector.AddDependency(source.SourceTransform.parent);
            }

            // we may mark heavy behavior with complex rules but it's extremely difficult to implement
            // so mark behavior for now
            collector.MarkBehaviour();
        }

        protected override void CollectMutations(T component, ComponentMutationsCollector collector)
        {
            collector.TransformRotation(component.TargetTransform ? component.TargetTransform : component.transform);
        }
    }

    [ComponentInformation(typeof(VRCWorldUpConstraintBase))]
    [ComponentInformation(typeof(VRCAimConstraintBase))]
    [ComponentInformation(typeof(VRCAimConstraint))]
    [ComponentInformation(typeof(VRCLookAtConstraintBase))]
    [ComponentInformation(typeof(VRCLookAtConstraint))]
    internal class VRCWorldUpConstraintInformation : VRCConstraintInformation<VRCWorldUpConstraintBase>
    {
        protected override void CollectDependency(VRCWorldUpConstraintBase component, ComponentDependencyCollector collector)
        {
            base.CollectDependency(component, collector);
            collector.AddDependency(component.WorldUpTransform);
        }
    }
    
    // VRCPerPlatformOverrides
    [ComponentInformationWithGUID("45da21a324e147228aaee066e399bff0", 11500000)]
    internal class VRCPerPlatformOverridesInformation : ComponentInformation<Component>
    {
        protected override void CollectDependency(Component component, ComponentDependencyCollector collector)
        {
            // this component is used only for storing platform overrides
        }
    }

    // ParentChangeDetector
    [ComponentInformationWithGUID("cdfe97a8253414b4bb5dd295880489bd", 1906240614)]
    internal class ParentChangeDetectorInformation : ComponentInformation<Component>
    {
        protected override void CollectDependency(Component component, ComponentDependencyCollector collector)
        {
        }
    }
}
#endif
