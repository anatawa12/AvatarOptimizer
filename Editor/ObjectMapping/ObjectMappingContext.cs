using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.API;
using Anatawa12.AvatarOptimizer.APIInternal;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

#if AAO_VRM1
using UniGLTF.Extensions.VRMC_vrm;
#endif

namespace Anatawa12.AvatarOptimizer
{
    [DependsOnContext(typeof(DestroyTracker.ExtensionContext))]
    internal class ObjectMappingContext : IExtensionContext
    {
        public ObjectMappingBuilder<PropertyInfo>? MappingBuilder { get; private set; }

        public void OnActivate(BuildContext context)
        {
            var avatarTagComponent = context.AvatarRootObject.GetComponentInChildren<AvatarTagComponent>(true);
            if (avatarTagComponent == null)
                MappingBuilder = null;
            else
                MappingBuilder = new ObjectMappingBuilder<PropertyInfo>(context.AvatarRootObject);
        }

        public void OnDeactivate(BuildContext context)
        {
            if (MappingBuilder == null) return;

            Tracing.Trace(TracingArea.ApplyObjectMapping, "Deactivating ObjectMappingContext");

            var mapping = MappingBuilder.BuildObjectMapping();
            var mappingSource = new MappingSourceImpl(mapping);

            // replace all objects
            foreach (var component in context.GetComponents<Component>())
            {
                using (ErrorReport.WithContextObject(component))
                {
                    if (component is Transform) continue;

                    // apply special mapping
                    if (ComponentInfoRegistry.TryGetInformation(component.GetType(), out var info))
                    {
                        Tracing.Trace(TracingArea.ApplyObjectMapping, $"Applying Special Mapping for {component}");
                        info.ApplySpecialMappingInternal(component, mappingSource);
                    }

                    var mapAnimatorController = false;

                    switch (component)
                    {
                        case Animator _:
#if AAO_VRCSDK3_AVATARS
                        case VRC.SDK3.Avatars.Components.VRCAvatarDescriptor _:
#endif
#if AAO_VRM0
                        case VRM.VRMBlendShapeProxy _:
#endif
#if AAO_VRM1
                        case UniVRM10.Vrm10Instance _:
#endif
                            mapAnimatorController = true;
                            break;
                    }

                    var serialized = new SerializedObject(component);
                    AnimatorControllerMapper? mapper = null;

                    foreach (var p in serialized.ObjectReferenceProperties())
                    {
                        if (mapping.MapComponentInstance(p.objectReferenceInstanceIDValue, out var mappedComponent))
                            p.objectReferenceValue = mappedComponent;

                        if (mapAnimatorController)
                        {
                            var objectReferenceValue = p.objectReferenceValue;
                            switch (objectReferenceValue)
                            {
                                case RuntimeAnimatorController runtimeController:
                                    Tracing.Trace(TracingArea.ApplyObjectMapping, $"Applying AnimatorController Mapping for {component}");
                                    mapper ??= new AnimatorControllerMapper(
                                        mapping.CreateAnimationMapper(component.gameObject), context);

                                    // all RuntimeAnimatorControllers in those components should be flattened to
                                    // AnimatorController
                                    mapper.FixAnimatorController(runtimeController);
                                    break;
#if AAO_VRM0
                                case VRM.BlendShapeAvatar avatar:
                                    Tracing.Trace(TracingArea.ApplyObjectMapping, $"Applying BlendShapeAvatar Mapping for {component}");
                                    mapper ??= new AnimatorControllerMapper(
                                        mapping.CreateAnimationMapper(component.gameObject), context);
                                    mapper.FixBlendShapeAvatar(avatar);
                                    break;
#endif
#if AAO_VRM1
                                case UniVRM10.VRM10Object vrm10Object:
                                    Tracing.Trace(TracingArea.ApplyObjectMapping, $"Applying VRM10Object Mapping for {component}");
                                    mapper ??= new AnimatorControllerMapper(
                                        mapping.CreateAnimationMapper(component.gameObject), context);
                                    mapper.FixVRM10Object(vrm10Object);
                                    break;
#endif
                            }
                        }
                    }

                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }
    }

    class MappingSourceImpl : MappingSource
    {
        private readonly ObjectMapping _mapping;

        public MappingSourceImpl(ObjectMapping mapping)
        {
            _mapping = mapping;
        }

        private MappedComponentInfo<T> GetMappedInternal<T>(T component) where T : Object
        {
            var componentInfo = _mapping.GetComponentMapping(component.GetInstanceID());
            if (componentInfo == null) return new OriginalComponentInfo<T>(component);
            return new ComponentInfo<T>(componentInfo);
        }

        public override MappedComponentInfo<T> GetMappedComponent<T>(T component) =>
            GetMappedInternal(component);

        public override MappedComponentInfo<GameObject> GetMappedGameObject(GameObject component) =>
            GetMappedInternal(component);

        private class OriginalComponentInfo<T> : MappedComponentInfo<T> where T : Object
        {
            private readonly T _component;

            public OriginalComponentInfo(T component) => _component = component;

            public override T MappedComponent => _component;
            public override bool TryMapProperty(string property, out API.MappedPropertyInfo found)
            {
                found = new API.MappedPropertyInfo(_component, property);
                return true;
            }

            internal override bool TryGetMappedVrmFirstPersonFlag(out VrmFirstPersonFlag vrmFirstPersonFlag)
            {
                vrmFirstPersonFlag = default;
                return false;
            }
        }

        private class ComponentInfo<T> : MappedComponentInfo<T> where T : Object
        {
            private readonly ComponentInfo _info;

            public ComponentInfo(ComponentInfo info) => _info = info;

            public override T MappedComponent => (T)EditorUtility.InstanceIDToObject(_info.MergedInto);
            public override bool TryMapProperty(string property, out API.MappedPropertyInfo found)
            {
                found = default;

                if (!_info.PropertyMapping.TryGetValue(property, out var mappedProp))
                {
                    found = new API.MappedPropertyInfo(MappedComponent, property);
                    return true;
                }
                if (mappedProp.MappedProperty == default) return false;

                found = new API.MappedPropertyInfo(
                    EditorUtility.InstanceIDToObject(mappedProp.MappedProperty.InstanceId),
                    mappedProp.MappedProperty.Name);
                return true;

            }
            
            internal override bool TryGetMappedVrmFirstPersonFlag(out VrmFirstPersonFlag vrmFirstPersonFlag)
            {
                switch (_info.VrmFirstPersonFlag)
                {
                    case { } firstPersonFlag: 
                        vrmFirstPersonFlag = firstPersonFlag;
                        return true;
                    case null:
                        vrmFirstPersonFlag = default;
                        return false;
                }
            }
        }
    }

    internal class AnimatorControllerMapper
    {
        private readonly AnimationObjectMapper _mapping;
        private readonly BuildContext _context;
        private readonly HashSet<Object> _fixedObjects = new();

        public AnimatorControllerMapper(AnimationObjectMapper mapping, BuildContext context)
        {
            _mapping = mapping;
            _context = context;
        }

        private void ValidateTemporaryAsset(Object asset)
        {
            if (asset == null) return;
            if (!_context.IsTemporaryAsset(asset))
                throw new ArgumentException("asset is not temporary asset", nameof(asset));
        }

        public void FixAnimatorController(RuntimeAnimatorController? controller)
        {
            if (controller == null) return;
            ValidateTemporaryAsset(controller);
            if (controller is not AnimatorController animatorController)
                throw new ArgumentException($"controller {controller.name} is not AnimatorController", nameof(controller));
            FixAnimatorController(animatorController);
        }

        public void FixAnimatorController(AnimatorController? controller)
        {
            if (controller == null) return;
            ValidateTemporaryAsset(controller);
            if (!_fixedObjects.Add(controller)) return;

            Profiler.BeginSample("FixAnimatorController");

            var layers = controller.layers;

            // Setting empty array (removing relationship between StateMachines and AnimatorController)
            // would speed up most setter of AnimatorController related classes like
            // AnimatorState.motion and BlendTree.children.
            controller.layers = Array.Empty<AnimatorControllerLayer>();

            foreach (ref var layer in layers.AsSpan())
            {
                FixAvatarMask(layer.avatarMask);
                if (layer.syncedLayerIndex != -1)
                {
                    foreach (var animatorState in ACUtils.AllStates(layers[layer.syncedLayerIndex].stateMachine))
                        if (layer.GetOverrideMotion(animatorState) is {} motion)
                            layer.SetOverrideMotion(animatorState, MapMotion(motion));
                }
                else
                {
                    foreach (var animatorState in ACUtils.AllStates(layer.stateMachine))
                        animatorState.motion = MapMotion(animatorState.motion);
                }
            }
            controller.layers = layers;
            foreach (var stateMachineBehaviour in ACUtils.StateMachineBehaviours(controller))
            {
                FixStateMachineBehaviour(stateMachineBehaviour);
            }

            Profiler.EndSample();
        }

        public Motion? MapMotion(Motion? motion)
        {
            if (motion == null) return null;
            switch (motion)
            {
                case AnimationClip clip:
                    return MapClip(clip);
                case BlendTree blendTree:
                    ValidateTemporaryAsset(motion);
                    if (!_fixedObjects.Add(motion)) return blendTree;
                    var children = blendTree.children;
                    foreach (ref var childMotion in children.AsSpan())
                        childMotion.motion = MapMotion(childMotion.motion);
                    blendTree.children = children;
                    return blendTree;
                default:
                    throw new NotSupportedException($"Unsupported motion type: {motion.GetType()}");
            }
        }

        Dictionary<AnimationClip, AnimationClip> _clipMapping = new();

        public AnimationClip? MapClip(AnimationClip? clip)
        {
            if (clip == null) return null;
#if AAO_VRCSDK3_AVATARS
            // TODO: when BuildContext have property to check if it is for VRCSDK3, additionally use it.
            if (clip.IsProxy()) return clip;
#endif
            if (_clipMapping.TryGetValue(clip, out var mapped)) return mapped;

            Profiler.BeginSample("MapClip");
            Tracing.Trace(TracingArea.ApplyObjectMapping, $"Applying Clip map {clip}");

            var floatBindings = AnimationUtility.GetCurveBindings(clip);
            var objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);

            var shouldMap = floatBindings.Concat(objectBindings)
                .Any(binding => _mapping.MapBinding(binding.path, binding.type, binding.propertyName) != null);

            if (!shouldMap)
            {
                Profiler.EndSample();
                _clipMapping[clip] = clip;
                return clip;
            }

            var newClip = new AnimationClip();
            ObjectRegistry.RegisterReplacedObject(clip, newClip);
            newClip.name = "rebased " + clip.name;

            // copy m_UseHighQualityCurve with SerializedObject since m_UseHighQualityCurve doesn't have public API
            using (var serializedClip = new SerializedObject(clip))
            using (var serializedNewClip = new SerializedObject(newClip))
            {
                serializedNewClip.FindProperty("m_UseHighQualityCurve")
                    .boolValue = serializedClip.FindProperty("m_UseHighQualityCurve").boolValue;
                serializedNewClip.ApplyModifiedPropertiesWithoutUndo();
            }

            foreach (var binding in floatBindings)
            {
                var newBindings = _mapping.MapBinding(binding.path, binding.type, binding.propertyName);
                Tracing.Trace(TracingArea.ApplyObjectMapping, $"Mapping Float ({binding.path}, {binding.type}, {binding.propertyName}): {(newBindings == null ? "same mapping" : newBindings.Length == 0 ? "empty" : string.Join(", ", newBindings))}");
                if (newBindings == null)
                {
                    AnimationUtility.SetEditorCurve(newClip, binding,
                        AnimationUtility.GetEditorCurve(clip, binding));
                }
                else
                {
                    foreach (var tuple in newBindings)
                    {
                        // We cannot generate animations targeting non-first component of the type on the GameObject
                        if (tuple.index != 0)
                        {
                            Debug.LogWarning($"Mapping AnimationClip {clip.name}: Animation targeting non-first component of the type {tuple.type} on GameObject {tuple.path} is not supported. Skipping this binding.");
                            continue;
                        }
                        var newBinding = binding;
                        newBinding.path = tuple.path;
                        newBinding.type = tuple.type;
                        newBinding.propertyName = tuple.propertyName;
                        AnimationUtility.SetEditorCurve(newClip, newBinding,
                            AnimationUtility.GetEditorCurve(clip, binding));
                    }
                }
            }

            foreach (var binding in objectBindings)
            {
                var newBindings = _mapping.MapBinding(binding.path, binding.type, binding.propertyName);
                Tracing.Trace(TracingArea.ApplyObjectMapping, $"Mapping Object ({binding.path}, {binding.type}, {binding.propertyName}): {(newBindings == null ? "same mapping" : newBindings.Length == 0 ? "empty" : string.Join(", ", newBindings))}");
                if (newBindings == null)
                {
                    AnimationUtility.SetObjectReferenceCurve(newClip, binding,
                        AnimationUtility.GetObjectReferenceCurve(clip, binding));
                }
                else
                {
                    foreach (var tuple in newBindings)
                    {
                        // We cannot generate animations targeting non-first component of the type on the GameObject
                        if (tuple.index != 0)
                        {
                            Debug.LogWarning($"Mapping AnimationClip {clip.name}: Animation targeting non-first component of the type {tuple.type} on GameObject {tuple.path} is not supported. Skipping this binding.");
                            continue;
                        }
                        var newBinding = binding;
                        newBinding.path = tuple.path;
                        newBinding.type = tuple.type;
                        newBinding.propertyName = tuple.propertyName;
                        AnimationUtility.SetObjectReferenceCurve(newClip, newBinding,
                            AnimationUtility.GetObjectReferenceCurve(clip, binding));
                    }
                }
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            var hasLengthMismatch = newClip.length != clip.length;
            var hasKeysRemoved = (floatBindings.Length > 0 || objectBindings.Length > 0) && 
                                 AnimationUtility.GetCurveBindings(newClip).Length == 0 && 
                                 AnimationUtility.GetObjectReferenceCurveBindings(newClip).Length == 0;
            
            // In Play Mode, add descriptive messages for both length mismatch and keys removed.
            // In Edit Mode (upload builds), only add dummy curve for length mismatch to preserve clip length.
            var shouldAddDummyCurves = false;
            
            if (hasLengthMismatch)
            {
                Tracing.Trace(TracingArea.ApplyObjectMapping, $"Animation Clip Length Mismatch; {clip.length} -> {newClip.length}");
                shouldAddDummyCurves = true;
            }
            
            if (hasKeysRemoved)
            {
                Tracing.Trace(TracingArea.ApplyObjectMapping, $"All animation keys removed from clip {clip.name}");
                // Only add dummy curves for keys removed in Play Mode, not in Edit Mode
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    shouldAddDummyCurves = true;
                }
            }
            
            if (shouldAddDummyCurves)
            {
                // In Play Mode, use descriptive localized messages to help users understand what happened.
                // We check isPlayingOrWillChangePlaymode (not just isPlaying) because NDMF builds happen
                // during the transition to Play Mode (when willChangePlaymode is true), before isPlaying becomes true.
                // This ensures upload builds (which happen in Edit Mode) use the terse internal identifier,
                // while Play Mode testing uses the descriptive messages.
                // Split messages into multiple curves to avoid multi-line object names.
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    // Add multiple short messages as separate curves
                    AnimationUtility.SetEditorCurve(newClip,
                        EditorCurveBinding.FloatCurve(AAOL10N.Tr("ObjectMapping:DummyAnimationObject:1"), typeof(GameObject), Props.IsActive), 
                        AnimationCurve.Constant(clip.length, clip.length, 1f));
                    AnimationUtility.SetEditorCurve(newClip,
                        EditorCurveBinding.FloatCurve(AAOL10N.Tr("ObjectMapping:DummyAnimationObject:2"), typeof(GameObject), Props.IsActive), 
                        AnimationCurve.Constant(clip.length, clip.length, 1f));
                    AnimationUtility.SetEditorCurve(newClip,
                        EditorCurveBinding.FloatCurve(AAOL10N.Tr("ObjectMapping:DummyAnimationObject:3"), typeof(GameObject), Props.IsActive), 
                        AnimationCurve.Constant(clip.length, clip.length, 1f));
                }
                else
                {
                    // For upload builds, use terse internal identifier to minimize size
                    AnimationUtility.SetEditorCurve(newClip,
                        EditorCurveBinding.FloatCurve("$AvatarOptimizerClipLengthDummy$", typeof(GameObject), Props.IsActive), 
                        AnimationCurve.Constant(clip.length, clip.length, 1f));
                }
            }

            newClip.wrapMode = clip.wrapMode;
            newClip.legacy = clip.legacy;
            newClip.frameRate = clip.frameRate;
            newClip.localBounds = clip.localBounds;
            // We have to add the clip to _clipMapping before processing additiveReferencePoseClip to avoid infinite recursion
            _clipMapping[clip] = newClip;
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.additiveReferencePoseClip = MapClip(settings.additiveReferencePoseClip);
            AnimationUtility.SetAnimationClipSettings(newClip, settings);

            Profiler.EndSample();
            return newClip;
        }

        public void FixStateMachineBehaviour(StateMachineBehaviour? stateMachineBehaviour)
        {
            if (stateMachineBehaviour == null) return;
            ValidateTemporaryAsset(stateMachineBehaviour);
            if (!_fixedObjects.Add(stateMachineBehaviour)) return;
            Profiler.BeginSample("FixStateMachineBehaviour");
#if AAO_VRCSDK3_AVATARS
            if (stateMachineBehaviour is VRC.SDKBase.VRC_AnimatorPlayAudio playAudio)
            {
                if (playAudio.SourcePath != null)
                    playAudio.SourcePath = _mapping.MapPath(playAudio.SourcePath, typeof(AudioSource));
            }
#endif
            Profiler.EndSample();
        }

        public void FixAvatarMask(AvatarMask? mask)
        {
            if (mask == null) return;
            ValidateTemporaryAsset(mask);
            if (!_fixedObjects.Add(mask)) return;
            var dstI = 0;
            for (var srcI = 0; srcI < mask.transformCount; srcI++)
            {
                var path = mask.GetTransformPath(srcI);
                var newPath = _mapping.MapPath(path, typeof(Transform));
                if (newPath != null)
                {
                    mask.SetTransformPath(dstI, newPath);
                    mask.SetTransformActive(dstI, mask.GetTransformActive(srcI));
                    dstI++;
                }
            }
            mask.transformCount = dstI;
        }

#if AAO_VRM0
        public void FixBlendShapeAvatar(VRM.BlendShapeAvatar? blendShapeAvatar)
        {
            if (blendShapeAvatar == null) return;
            ValidateTemporaryAsset(blendShapeAvatar);
            if (!_fixedObjects.Add(blendShapeAvatar)) return;
            foreach (var clip in blendShapeAvatar.Clips)
                FixVRMBlendShapeClip(clip);
        }

        public void FixVRMBlendShapeClip(VRM.BlendShapeClip? blendShapeClip)
        {
            if (blendShapeClip == null) return;
            ValidateTemporaryAsset(blendShapeClip);
            if (!_fixedObjects.Add(blendShapeClip)) return;
            blendShapeClip.Values = blendShapeClip.Values.SelectMany(binding =>
            {
                var mappedBindings = _mapping.MapBinding(binding.RelativePath, typeof(SkinnedMeshRenderer), VProp.BlendShapeIndex(binding.Index));
                if (mappedBindings == null)
                {
                    return new[] { binding };
                }
                return mappedBindings
                    .Select(mapped => new VRM.BlendShapeBinding
                    {
                        RelativePath = mapped.path,
                        Index = VProp.ParseBlendShapeIndex(mapped.propertyName),
                        Weight = binding.Weight
                    });
            }).ToArray(); 
            // Currently, MaterialValueBindings are guaranteed to not change (MaterialName, in particular)
            // unless MergeToonLitMaterial is used, which breaks material animations anyway.
            // Map MaterialValues here once we start tracking material changes...
        }
#endif

#if AAO_VRM1
        public void FixVRM10Object(UniVRM10.VRM10Object? vrm10Object)
        {
            if (vrm10Object == null) return;
            ValidateTemporaryAsset(vrm10Object);
            if (!_fixedObjects.Add(vrm10Object)) return;
            foreach (var clip in vrm10Object.Expression.Clips)
                FixVRM10Expression(clip.Clip);

            vrm10Object.FirstPerson.Renderers = vrm10Object.FirstPerson.Renderers
                .Select(renderer => renderer.Renderer)
                .Where(rendererPath => rendererPath != null)
                .Select(rendererPath => _mapping.MapPath(rendererPath, typeof(Renderer)))
                .Where(mappedRendererPath => mappedRendererPath != null)
                .Distinct()
                .Select(mappedRendererPath =>
                {
                    if (mappedRendererPath == null || !_mapping.TryGetMappedVrmFirstPersonFlag(mappedRendererPath, out var vrmFirstPersonFlag))
                    {
                        vrmFirstPersonFlag = VrmFirstPersonFlag.Auto;
                    }
                    return new UniVRM10.RendererFirstPersonFlags
                    {
                        Renderer = mappedRendererPath,
                        FirstPersonFlag = vrmFirstPersonFlag switch
                        {

                            VrmFirstPersonFlag.Auto => FirstPersonType.auto,
                            VrmFirstPersonFlag.Both => FirstPersonType.both,
                            VrmFirstPersonFlag.ThirdPersonOnly => FirstPersonType.thirdPersonOnly,
                            VrmFirstPersonFlag.FirstPersonOnly => FirstPersonType.firstPersonOnly,
                            _ => throw new ArgumentOutOfRangeException()
                        }
                    };
                }).ToList();
        }

        public void FixVRM10Expression(UniVRM10.VRM10Expression? vrm10Expression)
        {
            if (vrm10Expression == null) return;
            ValidateTemporaryAsset(vrm10Expression);
            if (!_fixedObjects.Add(vrm10Expression)) return;
            vrm10Expression.Prefab = null; // This likely to point prefab before mapping, which is invalid by now
            vrm10Expression.MorphTargetBindings = vrm10Expression.MorphTargetBindings.SelectMany(binding =>
            {
                var mappedBindings = _mapping.MapBinding(binding.RelativePath, typeof(SkinnedMeshRenderer), VProp.BlendShapeIndex(binding.Index));
                if (mappedBindings == null)
                {
                    return new[] { binding };
                }
                return mappedBindings
                    .Select(mapped => new UniVRM10.MorphTargetBinding
                    {
                        RelativePath = mapped.path,
                        Index = VProp.ParseBlendShapeIndex(mapped.propertyName),
                        Weight = binding.Weight
                    });
            }).ToArray(); 
            // Currently, MaterialColorBindings and MaterialUVBindings are guaranteed to not change (MaterialName, in particular)
            // unless MergeToonLitMaterial is used, which breaks material animations anyway.
            // Map MaterialColorBindings / MaterialUVBindings here once we start tracking material changes...
        }
#endif
    }
}
