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
                        collector.PreserveProperties(component.VisemeSkinnedMesh, new []{
                            $"blendShape.{component.MouthOpenBlendShapeName}"
                        });
                    }
                    break;
                }
                case VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape:
                {
                    if (component.VisemeSkinnedMesh != null)
                    {
                        collector.PreserveProperties(component.VisemeSkinnedMesh,
                            component.VisemeBlendShapes.Select(blendShape => $"blendShape.{blendShape}"));
                    }
                    break;
                }
                case VRC_AvatarDescriptor.LipSyncStyle.VisemeParameterOnly:
                    // NOP
                    break;
#if AAO_VRCSDK3_AVATARS_NATIVE_FACE_TRACKING
                case VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShapeFaceTracking when component is VRCAvatarDescriptor:
                    // Done in VRCAvatarDescriptor with enableFaceTracking
                    break;
#endif
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
#if AAO_VRCSDK3_AVATARS_NATIVE_FACE_TRACKING
                case VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShapeFaceTracking when component is VRCAvatarDescriptor:
                    // Done in VRCAvatarDescriptor with enableFaceTracking
                    break;
#endif
                default:
                    // Warning Reported in CollectMutations
                    break;
            }
        }
        
        protected static string ParseBlendShapeProperty(string prop) =>
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

            if (component.enableEyeLook)
            {
#if AAO_VRCSDK3_AVATARS_NATIVE_FACE_TRACKING
                switch (component.customEyeLookSettings.eyeGazeMode)
#endif
                {
#if AAO_VRCSDK3_AVATARS_NATIVE_FACE_TRACKING
                    case VRCAvatarDescriptor.CustomEyeLookSettings.EyeGazeMode.BoneRotation:
                    case VRCAvatarDescriptor.CustomEyeLookSettings.EyeGazeMode.Both:
#else
                    {
#endif
                        var leftEye = component.customEyeLookSettings.leftEye;
                        var rightEye = component.customEyeLookSettings.rightEye;
                        if (leftEye != null && rightEye != null)
                        {
                            // We create cross dependency relationship to prevent deleting one
                            // Deleting both is acceptable
                            collector.AddDependency(leftEye, rightEye);
                            collector.AddDependency(rightEye, leftEye);
                        }
#if AAO_VRCSDK3_AVATARS_NATIVE_FACE_TRACKING
                        break;
                    case VRCAvatarDescriptor.CustomEyeLookSettings.EyeGazeMode.FaceTracking:
                        break;
                    default:
                        // Warning Reported in CollectMutations
                        break;
#else
                    }
#endif
                }
            }

#if AAO_VRCSDK3_AVATARS_NATIVE_FACE_TRACKING
            if (component.enableFaceTracking)
            {
                // We generally do not want to prevent merging meshes.
                // However, we need to prevent merging meshes among faceTrackingMeshes
                // unless we can enforce merging BlendShapes for face tracking,
                // because VRC Native Face Tracking uses single BlendShape name among faceTrackingMeshes.
                // AAO cannot enforce merging blendShapes when AAO merges Skinned Mesh Renderer for several reasons.
                //
                // Therefore, we mark the faceTrackingMeshes 'not for merge' if there are multiple faceTrackingMeshes.
                // We don't mark such if there is only one mesh in faceTrackingMeshes
                // since merging single faceTrackingMeshes to non-faceTrackingMeshes SMR is harmless.
                // In theory, we can skip marking 'some' faceTrackingMeshes from dependency registration for same reaosn.
                // However, it would make AAO behavior unstable and hard to predict, so we do not do so.
                //
                // Marking the Skinned Mesh Renderer as a dependency of some component would mark
                // the Skinned Mesh Renderer 'not for merge'.
                //
                // TODO: When all BlendShapes names are available in single SMR, We can skip this `AddDependency`-ing.
                // Note that SMR might have additional BlendShapes with MergeSMR or renaming. You must care that.
                var meshes = component.faceTrackingMeshes.Where(x => x).ToList();
                if (meshes.Count >= 2)
                {
                    foreach (var renderer in meshes)
                        collector.AddDependency(renderer);
                }
            }
#endif

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
#if AAO_VRCSDK3_AVATARS_NATIVE_FACE_TRACKING
                switch (component.customEyeLookSettings.eyeGazeMode)
#endif
                {
#if AAO_VRCSDK3_AVATARS_NATIVE_FACE_TRACKING
                    case VRCAvatarDescriptor.CustomEyeLookSettings.EyeGazeMode.BoneRotation:
                    case VRCAvatarDescriptor.CustomEyeLookSettings.EyeGazeMode.Both:
#else
                    {
#endif
                        var leftEye = component.customEyeLookSettings.leftEye;
                        var rightEye = component.customEyeLookSettings.rightEye;

                        if (leftEye) collector.TransformRotation(leftEye);
                        if (rightEye) collector.TransformRotation(rightEye);
#if AAO_VRCSDK3_AVATARS_NATIVE_FACE_TRACKING
                        break;
                    case VRCAvatarDescriptor.CustomEyeLookSettings.EyeGazeMode.FaceTracking:
                        break;
                    default:
                        BuildLog.LogWarning("ComponentInfos:VRCAvatarDescriptor:warning:UnknownEyeGazeMode",
                            component.customEyeLookSettings.eyeGazeMode.ToString(),
                            component);
                        break;
#else
                    }
#endif
                }

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
                                collector.PreserveProperties(skinnedMeshRenderer,
                                    from index in component.customEyeLookSettings.eyelidsBlendshapes
                                    where 0 <= index && index < mesh.blendShapeCount
                                    select $"blendShape.{mesh.GetBlendShapeName(index)}");
                            }
                        }
                    }
                        break;
#if AAO_VRCSDK3_AVATARS_NATIVE_FACE_TRACKING
                    case VRCAvatarDescriptor.EyelidType.FaceTracking:
                    {
                        break;
                    }
#endif
                    default:
                        BuildLog.LogWarning("ComponentInfos:VRCAvatarDescriptor:warning:UnknownEyelidType", 
                                component.customEyeLookSettings.eyelidType.ToString(),
                                component);
                        break;
                }
            }

#if AAO_VRCSDK3_AVATARS_NATIVE_FACE_TRACKING
            if (component.enableFaceTracking)
            {
                var blendShapesSetIndices = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int index2 = 0; index2 < (component.faceTrackingBlendshapesSetList?.Count ?? 0); ++index2)
                {
                    var setName = component.faceTrackingBlendshapesSetList![index2]?.setName;
                    if (setName != null && !string.IsNullOrEmpty(setName) && setName.Length <= 100)
                        blendShapesSetIndices[setName] = index2;
                }

                var faceTrackingMeshes = component.faceTrackingMeshes.Where(x => x != null).ToList();

                foreach (var shapeSet in component.faceTrackingBlendshapesSetList ?? new List<VRCAvatarDescriptor.FaceTrackingBlendshapesSet>())
                {
                    if (shapeSet == null) continue;
                    // Does it have effect with weight = 0 like cancelling animators values? I think so
                    //if (collector.GetAnimatedFlag(component, $"__AAO_.faceTrackingBlendshapesSet.{shapeSet.setName}.setWeight", shapeSet.setWeight != 0) != false)
                    {
                        // Basic Face Tracking
                        {
                            for (VRCAvatarDescriptor.FaceTrackingKey key = 0;
                                 key < VRCAvatarDescriptor.FaceTrackingKey.Max
                                 && (int)key < shapeSet.faceTrackingBlendshapeNames.Length;
                                 key++)
                            {
                                var shapeName = shapeSet.faceTrackingBlendshapeNames[(int)key];
                                if (string.IsNullOrEmpty(shapeName)) continue;
                                if (!IsFaceTrackingKeyEnabled(component, key)) continue;

                                FaceTrackingBlendShape(shapeName);
                            }
                        }

                        if (component.lipSync == VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShapeFaceTracking
                            && shapeSet.visemeBlendshapeNames.Length == (int)VRC_AvatarDescriptor.Viseme.Count)
                        {
                            for (VRC_AvatarDescriptor.Viseme key = 0; key < VRC_AvatarDescriptor.Viseme.Count; key++)
                            {
                                var shapeName = shapeSet.visemeBlendshapeNames[(int)key];
                                if (string.IsNullOrEmpty(shapeName)) continue;
                                FaceTrackingBlendShape(shapeName);
                            }
                        }
                    }
                }

                foreach (var parameterBehavior in component.faceTrackingParameterBehaviors.Take(128))
                {
                    if (parameterBehavior == null) continue;

                    switch (parameterBehavior.targetMode)
                    {
                        case VRCAvatarDescriptor.ParameterBehaviorTargetMode.ForwardToAnimator:
                            if (string.IsNullOrEmpty(parameterBehavior.outputParameterName)) continue;
                            //collector.ModifyProperties(component.GetComponent<Animator>(), parameterBehavior.outputParameterName);
                            break;
                        case VRCAvatarDescriptor.ParameterBehaviorTargetMode.ApplyBlendSetWeight:
                            if (string.IsNullOrEmpty(parameterBehavior.targetBlendSetName)) continue;
                            if (!blendShapesSetIndices.ContainsKey(parameterBehavior.targetBlendSetName)) continue;
                            //collector.ModifyProperties(component, $"__AAO_.faceTrackingBlendshapesSet.{parameterBehavior.targetBlendSetName}.setWeight");
                            break;
                        case VRCAvatarDescriptor.ParameterBehaviorTargetMode.ApplyToBlendshape:
                        case VRCAvatarDescriptor.ParameterBehaviorTargetMode.LimitBlendshapeMax:
                        case VRCAvatarDescriptor.ParameterBehaviorTargetMode.LimitBlendshapeMin:
                            FaceTrackingBlendShape(parameterBehavior.targetBlendshapeName);
                            break;
                        default:
                            BuildLog.LogWarning("ComponentInfos:VRCAvatarDescriptor:warning:UnknownParameterBehaviorTargetMode",
                                parameterBehavior.targetMode);
                            break;
                    }

                    // parameterBehavior.combineMode and parameterBehavior.inputTrackingKeys does not need any AAO work
                }

                //component.visemeDampenAmount and component.visemeDampenMode does not need any AAO work

                void FaceTrackingBlendShape(string blendShape)
                {
                    foreach (var renderer in faceTrackingMeshes)
                        if (renderer)
                            collector.ModifyProperties(renderer, $"blendShape.{blendShape}");

                    // VRC Native Face Tracking uses single BlendShape names for multiple Skinned Mesh Renderers.
                    // AAO generally manages BlendShape name independently so we exceptionally handle them.
                    if (faceTrackingMeshes.Count > 1)
                        collector.AddBlendShapeGroup(blendShape, faceTrackingMeshes);
                }
            }
#endif
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

#if AAO_VRCSDK3_AVATARS_NATIVE_FACE_TRACKING
            if (component.enableFaceTracking)
            {
                var faceTrackingMeshes = new List<MappedComponentInfo<SkinnedMeshRenderer>>();
                int writeI = 0;
                for (int readI = 0; readI < component.faceTrackingMeshes.Count; readI++)
                {
                    var renderer = component.faceTrackingMeshes[readI];
                    var info = mappingSource.GetMappedComponent(renderer);
                    if (info.MappedComponent == null)
                    {
                        // skip this element
                    }
                    else
                    {
                        faceTrackingMeshes.Add(info);
                        component.faceTrackingMeshes[writeI] = info.MappedComponent;
                        writeI++;
                    }
                }

                component.faceTrackingMeshes.RemoveRange(writeI, component.faceTrackingMeshes.Count - writeI);

                var blendShapesSetIndices = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int index2 = 0; index2 < (component.faceTrackingBlendshapesSetList?.Count ?? 0); ++index2)
                {
                    var setName = component.faceTrackingBlendshapesSetList![index2]?.setName;
                    if (setName != null && !string.IsNullOrEmpty(setName) && setName.Length <= 100)
                        blendShapesSetIndices[setName] = index2;
                }

                foreach (var shapeSet in component.faceTrackingBlendshapesSetList ??
                                         new List<VRCAvatarDescriptor.FaceTrackingBlendshapesSet>())
                {
                    if (shapeSet == null) continue;
                    // Does it have effect with weight = 0 like cancelling animators values? I think so
                    //if (collector.GetAnimatedFlag(component, $"__AAO_.faceTrackingBlendshapesSet.{shapeSet.setName}.setWeight", shapeSet.setWeight != 0) != false)
                    {
                        // Basic Face Tracking
                        {
                            for (VRCAvatarDescriptor.FaceTrackingKey key = 0;
                                 key < VRCAvatarDescriptor.FaceTrackingKey.Max
                                 && (int)key < shapeSet.faceTrackingBlendshapeNames.Length;
                                 key++)
                            {
                                ref var shapeName = ref shapeSet.faceTrackingBlendshapeNames[(int)key];
                                if (string.IsNullOrEmpty(shapeName)) continue;
                                if (!IsFaceTrackingKeyEnabled(component, key)) continue;

                                FaceTrackingBlendShape(ref shapeName);
                            }
                        }

                        if (component.lipSync == VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShapeFaceTracking
                            && shapeSet.visemeBlendshapeNames.Length == (int)VRC_AvatarDescriptor.Viseme.Count)
                        {
                            for (VRC_AvatarDescriptor.Viseme key = 0; key < VRC_AvatarDescriptor.Viseme.Count; key++)
                            {
                                ref var shapeName = ref shapeSet.visemeBlendshapeNames[(int)key];
                                if (string.IsNullOrEmpty(shapeName)) continue;
                                FaceTrackingBlendShape(ref shapeName);
                            }
                        }
                    }
                }

                foreach (var parameterBehavior in component.faceTrackingParameterBehaviors.Take(128))
                {
                    if (parameterBehavior == null) continue;

                    switch (parameterBehavior.targetMode)
                    {
                        case VRCAvatarDescriptor.ParameterBehaviorTargetMode.ForwardToAnimator:
                            if (string.IsNullOrEmpty(parameterBehavior.outputParameterName)) continue;
                            //collector.ModifyProperties(component.GetComponent<Animator>(), parameterBehavior.outputParameterName);
                            break;
                        case VRCAvatarDescriptor.ParameterBehaviorTargetMode.ApplyBlendSetWeight:
                            if (string.IsNullOrEmpty(parameterBehavior.targetBlendSetName)) continue;
                            if (!blendShapesSetIndices.ContainsKey(parameterBehavior.targetBlendSetName)) continue;
                            //collector.ModifyProperties(component, $"__AAO_.faceTrackingBlendshapesSet.{parameterBehavior.targetBlendSetName}.setWeight");
                            break;
                        case VRCAvatarDescriptor.ParameterBehaviorTargetMode.ApplyToBlendshape:
                        case VRCAvatarDescriptor.ParameterBehaviorTargetMode.LimitBlendshapeMax:
                        case VRCAvatarDescriptor.ParameterBehaviorTargetMode.LimitBlendshapeMin:
                            FaceTrackingBlendShape(ref parameterBehavior.targetBlendshapeName);
                            break;
                        default:
                            // Warning Reported in CollectMutations
                            break;
                    }

                    // parameterBehavior.combineMode and parameterBehavior.inputTrackingKeys does not need any AAO work
                }

                //component.visemeDampenAmount and component.visemeDampenMode does not need any AAO work

                void FaceTrackingBlendShape(ref string shapeName)
                {
                    if (faceTrackingMeshes.Count == 0) return; // This avatar does not have face tracking in practice

                    HashSet<string> newShapeName = new HashSet<string>();

                    foreach (var renderer in faceTrackingMeshes)
                    {
                        if (renderer.TryMapProperty($"blendShape.{shapeName}", out var mapped))
                        {
                            if (mapped.Component == renderer.MappedComponent)
                            {
                                var newName = ParseBlendShapeProperty(mapped.Property);
                                newShapeName.Add(newName);
                            }
                        }
                        else
                        {
                            // removed. no-op
                        }
                    }

                    if (newShapeName.Count == 0)
                    {
                        // removed
                    }
                    else if (newShapeName.Count == 1)
                    {
                        shapeName = newShapeName.First();
                    }
                    else
                    {
                        BuildLog.LogError("ComponentInfos:VRCAvatarDescriptor:FaceTracking:BlendShapeNameDiverge",
                            shapeName, string.Join(", ", newShapeName));
                    }
                }
            }
#endif
        }

#if AAO_VRCSDK3_AVATARS_NATIVE_FACE_TRACKING
        private bool IsFaceTrackingKeyEnabled(VRCAvatarDescriptor component, VRCAvatarDescriptor.FaceTrackingKey key)
        {
            // Canny: IsBasicFaceTrackingKey should take VRCAvatarDescriptor.FaceTrackingKey
            // https://github.com/anatawa12/AvatarOptimizer/pull/1803
            if (component.useAllTrackingKeys || VRCAvatarDescriptor.IsBasicFaceTrackingKey((int)key))
            {
                switch (key)
                {
                    case >= VRCAvatarDescriptor.FaceTrackingKey.EyeLookOutRight and <= VRCAvatarDescriptor.FaceTrackingKey.EyeLookDownLeft:
                    {
                        if (component.enableEyeLook)
                        {
                            if (component.customEyeLookSettings.eyeGazeMode is not (VRCAvatarDescriptor.CustomEyeLookSettings.EyeGazeMode.FaceTracking
                                or VRCAvatarDescriptor.CustomEyeLookSettings.EyeGazeMode.Both))
                                return false;
                        }

                        return true;
                    }
                    case VRCAvatarDescriptor.FaceTrackingKey.EyeClosedRight or VRCAvatarDescriptor.FaceTrackingKey.EyeClosedLeft:
                    {
                        if (component.enableEyeLook)
                        {
                            if (component.customEyeLookSettings.eyelidType is not (VRCAvatarDescriptor.EyelidType.None
                                or VRCAvatarDescriptor.EyelidType.FaceTracking)) return false;
                        }

                        return true;
                    }
                    default:
                        return true;
                }
            }
            return false;
        }
#endif
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
#if AAO_VRCSDK3_AVATARS_PHYSBONE_GLOBAL_COLLIDER
            if (component.globalCollisionFlags != 0)
            {
                collector.MarkEntrypoint();
            }
#endif
        }
    }

    [ComponentInformation(typeof(ContactReceiver))]
    [ComponentInformation(typeof(VRCContactReceiver))]
    internal class ContactReceiverInformation : ComponentInformation<ContactReceiver>
    {
        protected override void CollectDependency(ContactReceiver component, ComponentDependencyCollector collector)
        {
            // the contact receiver receives contact event from the sender
            if (
                collector.IsParameterUsed(component.parameter)
#if AAO_VRCSDK3_AVATARS_MATERIAL_CONTACTS_AND_ADVANCED_PROXIMITY
                || (component is { receiverType: ContactReceiver.ReceiverType.Proximity, advancedProximityParameters: true }
                    && (
                        collector.IsParameterUsed(component.parameter + ContactReceiver.PARAM_SENDER_TYPE)
                        || collector.IsParameterUsed(component.parameter + ContactReceiver.PARAM_SENDER_LOCAL_POSITION_X)
                        || collector.IsParameterUsed(component.parameter + ContactReceiver.PARAM_SENDER_LOCAL_POSITION_Y)
                        || collector.IsParameterUsed(component.parameter + ContactReceiver.PARAM_SENDER_LOCAL_POSITION_Z)
                        || collector.IsParameterUsed(component.parameter + ContactReceiver.PARAM_SENDER_VELOCITY_X)
                        || collector.IsParameterUsed(component.parameter + ContactReceiver.PARAM_SENDER_VELOCITY_Y)
                        || collector.IsParameterUsed(component.parameter + ContactReceiver.PARAM_SENDER_VELOCITY_Z)
                        || collector.IsParameterUsed(component.parameter + ContactReceiver.PARAM_SENDER_VELOCITY_MAGNITUDE)
                        || collector.IsParameterUsed(component.parameter + ContactReceiver.PARAM_PROXIMITY_SPEED)
                        || component.collisionTags.Any(tag =>
                            collector.IsParameterUsed(component.parameter + ContactReceiver.PARAM_COLLISION_TAG_PREFIX + tag))
                    )
                )
#endif
            )
                collector.MarkEntrypoint();

#if AAO_VRCSDK3_AVATARS_MATERIAL_CONTACTS_AND_ADVANCED_PROXIMITY
            if (component.receiverType == ContactReceiver.ReceiverType.Proximity)
            {
                if (component.writeToMaterials)
                {
                    foreach (var renderer in component.shaderTargetRenderers.Take(4))
                    {
                        collector.AddDependency(renderer, component).OnlyIfTargetCanBeEnable();
                    }
                }
                if (component.advancedProximityParameters)
                {
                    foreach (var renderer in component.shaderTargetRenderers.Take(4))
                    {
                        collector.AddDependency(renderer, component).OnlyIfTargetCanBeEnable();
                    }
                }
            }
#endif
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
            collector.AddDependency(component.transform, component).EvenIfDependantDisabled();
            foreach (var transform in component.transformsToIgnore)
                collector.AddDependency(transform, component).EvenIfDependantDisabled();
            foreach (var transform in component.reparentHere)
                collector.AddDependency(transform, component).EvenIfDependantDisabled();
            foreach (var transform in component.extraChildTransforms)
                collector.AddDependency(transform, component).EvenIfDependantDisabled();
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

#if AAO_VRCSDK3_AVATARS_VRC_RAYCAST
    [ComponentInformation(typeof(VRCRaycast))]
    internal class VRCRaycastInformation : ComponentInformation<VRCRaycast>
    {
        protected override void CollectDependency(VRCRaycast component, ComponentDependencyCollector collector)
        {
#if AAO_VRCSDK3_AVATARS_VRC_RAYCAST_V1_1
            switch (component.Version)
            {
                case VRCRaycast.RaycastComponentVersion.Version_1_0:
                case VRCRaycast.RaycastComponentVersion.Version_1_1:
                    break;
                default:
                    BuildLog.LogWarning("ComponentInfos:VRCRaycast:warning:UnknownVersion", component.Version);
                    break;
            }
#endif
            var transform = component.ResultTransform;
            // Raycast do nothing if target is null in Version_1_0
            if (transform != null
#if AAO_VRCSDK3_AVATARS_VRC_RAYCAST_V1_1
                || component.Version != VRCRaycast.RaycastComponentVersion.Version_1_0
#endif
            )
            {
                collector.AddDependency(transform);
                collector.AddDependency(transform, component);
                collector.MarkBehaviour();
            }

            // if the component drives parameter that is used, 
            // we need to keep the raycast
            if (new[]
                {
                    component.Parameter + VRCRaycast.PARAM_DISTANCE,
                    component.Parameter + VRCRaycast.PARAM_HIT,
                    component.Parameter + VRCRaycast.PARAM_RATIO,
#if AAO_VRCSDK3_AVATARS_VRC_RAYCAST_V1_1
#endif
                }.Any(collector.IsParameterUsed))
            {
                collector.MarkEntrypoint();
            }
        }

        protected override void CollectMutations(VRCRaycast component, ComponentMutationsCollector collector)
        {
            var transform = component.ResultTransform;
            if (transform != null) // Raycast do nothing if target is null
            {
                collector.TransformPosition(transform);
                if (component.ApplyRotation)
                    collector.TransformRotation(transform);
            }
        }
    }
#endif
}
#endif
