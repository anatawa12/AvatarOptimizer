using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.ErrorReporting;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class ApplyObjectMapping : Pass<ApplyObjectMapping>
    {
        protected override void Execute(BuildContext context)
        {
            var mapping = ((OptimizerSession)context).MappingBuilder.BuildObjectMapping();

            // replace all objects
            BuildReport.ReportingObjects(context.GetComponents<Component>(), component =>
            {
                if (component is Transform) return;
                var serialized = new SerializedObject(component);
                AnimatorControllerMapper mapper = null;
                SpecialMappingApplier.Apply(component.GetType(), serialized, mapping, ref mapper);

                foreach (var p in serialized.ObjectReferenceProperties())
                {
                    if (mapping.MapComponentInstance(p.objectReferenceInstanceIDValue, out var mappedComponent))
                        p.objectReferenceValue = mappedComponent;

                    if (p.objectReferenceValue is AnimatorController controller)
                    {
                        if (mapper == null)
                            mapper = new AnimatorControllerMapper(mapping.CreateAnimationMapper(component.gameObject));

                        // ReSharper disable once AccessToModifiedClosure
                        var mapped = BuildReport.ReportingObject(controller,
                            () => mapper.MapAnimatorController(controller));
                        if (mapped != null)
                            p.objectReferenceValue = mapped;
                    }
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
            });
        }
    }

    internal static class SpecialMappingApplier
    {
        public static void Apply(Type type, SerializedObject serialized, 
            ObjectMapping mapping, ref AnimatorControllerMapper mapper)
        {
            if (type.IsAssignableFrom(typeof(VRCAvatarDescriptor)))
                VRCAvatarDescriptor(serialized, mapping, ref mapper);
        }
        
        // customEyeLookSettings.eyelidsBlendshapes is index
        private static void VRCAvatarDescriptor(SerializedObject serialized,
            ObjectMapping mapping, ref AnimatorControllerMapper mapper)
        {
            var eyelidsEnabled = serialized.FindProperty("enableEyeLook");
            var eyelidType = serialized.FindProperty("customEyeLookSettings.eyelidType");
            var eyelidsSkinnedMesh = serialized.FindProperty("customEyeLookSettings.eyelidsSkinnedMesh");
            var eyelidsBlendshapes = serialized.FindProperty("customEyeLookSettings.eyelidsBlendshapes");

            if (eyelidsEnabled.boolValue && 
                eyelidType.enumValueIndex == (int)VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.EyelidType.Blendshapes)
            {
                var info = mapping.GetComponentMapping(eyelidsSkinnedMesh.objectReferenceInstanceIDValue);
                if (info == null) return;

                eyelidsSkinnedMesh.objectReferenceValue = EditorUtility.InstanceIDToObject(info.MergedInto);

                for (var i = 0; i < eyelidsBlendshapes.arraySize; i++)
                {
                    var indexProp = eyelidsBlendshapes.GetArrayElementAtIndex(i);
                    if (info.PropertyMapping.TryGetValue(
                            VProp.BlendShapeIndex(indexProp.intValue),
                            out var mappedPropName))
                    {
                        if (mappedPropName == null)
                        {
                            BuildReport.LogFatal("ApplyObjectMapping:VRCAvatarDescriptor:eyelids BlendShape Removed");
                            return;
                        }
                        indexProp.intValue = VProp.ParseBlendShapeIndex(mappedPropName);
                    }
                }
            }
        }
    }

    internal class AnimatorControllerMapper
    {
        private readonly AnimationObjectMapper _mapping;
        private readonly Dictionary<Object, Object> _cache = new Dictionary<Object, Object>();
        private bool _mapped = false;

        public AnimatorControllerMapper(AnimationObjectMapper mapping)
        {
            _mapping = mapping;
        }

        public AnimatorController MapAnimatorController(AnimatorController controller)
        {
            if (_cache.TryGetValue(controller, out var cached)) return (AnimatorController)cached;
            _mapped = false;
            var newController = new AnimatorController
            {
                name = controller.name + " (rebased)",
                parameters = controller.parameters,
                layers = controller.layers.Select(MapAnimatorControllerLayer).ToArray()
            };
            if (!_mapped) newController = null;
            _cache[controller] = newController;
            return newController;
        }

        private AnimatorControllerLayer MapAnimatorControllerLayer(AnimatorControllerLayer layer) =>
            new AnimatorControllerLayer
            {
                name = layer.name,
                avatarMask = DeepClone(layer.avatarMask, CustomClone),
                blendingMode = layer.blendingMode,
                defaultWeight = layer.defaultWeight,
                syncedLayerIndex = layer.syncedLayerIndex,
                syncedLayerAffectsTiming = layer.syncedLayerAffectsTiming,
                iKPass = layer.iKPass,
                stateMachine = MapStateMachine(layer.stateMachine),
            };

        private AnimatorStateMachine MapStateMachine(AnimatorStateMachine stateMachine) =>
            DeepClone(stateMachine, CustomClone);

        // https://github.com/bdunderscore/modular-avatar/blob/db49e2e210bc070671af963ff89df853ae4514a5/Packages/nadena.dev.modular-avatar/Editor/AnimatorMerger.cs#L199-L241
        // Originally under MIT License
        // Copyright (c) 2022 bd_
        private Object CustomClone(Object o)
        {
            if (o is AnimationClip clip)
            {
                var newClip = new AnimationClip();
                newClip.name = "rebased " + clip.name;

                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    var newBinding = _mapping.MapBinding(binding);
                    _mapped |= newBinding != binding;
                    if (newBinding.type == null) continue;
                    newClip.SetCurve(newBinding.path, newBinding.type, newBinding.propertyName,
                        AnimationUtility.GetEditorCurve(clip, binding));
                }

                foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                {
                    var newBinding = _mapping.MapBinding(binding);
                    _mapped |= newBinding != binding;
                    if (newBinding.type == null) continue;
                    AnimationUtility.SetObjectReferenceCurve(newClip, newBinding,
                        AnimationUtility.GetObjectReferenceCurve(clip, binding));
                }

                newClip.wrapMode = clip.wrapMode;
                newClip.legacy = clip.legacy;
                newClip.frameRate = clip.frameRate;
                newClip.localBounds = clip.localBounds;
                AnimationUtility.SetAnimationClipSettings(newClip, AnimationUtility.GetAnimationClipSettings(clip));

                return newClip;
            }
            else if (o is AvatarMask mask)
            {
                var newMask = new AvatarMask();
                newMask.name = "rebased " + mask.name;
                newMask.transformCount = mask.transformCount;
                var dstI = 0;
                for (var srcI = 0; srcI < mask.transformCount; srcI++)
                {
                    var path = mask.GetTransformPath(srcI);
                    var newPath = _mapping.MapPath(path, typeof(Transform));
                    if (newPath != null)
                    {
                        newMask.SetTransformPath(dstI, newPath);
                        newMask.SetTransformActive(dstI, mask.GetTransformActive(srcI));
                        dstI++;
                    }
                }
                newMask.transformCount = dstI;

                return newMask;
            }
            else
            {
                return null;
            }
        }

        // https://github.com/bdunderscore/modular-avatar/blob/db49e2e210bc070671af963ff89df853ae4514a5/Packages/nadena.dev.modular-avatar/Editor/AnimatorMerger.cs#LL242-L340C10
        // Originally under MIT License
        // Copyright (c) 2022 bd_
        private T DeepClone<T>(T original, Func<Object, Object> visitor) where T : Object
        {
            if (original == null) return null;

            // We want to avoid trying to copy assets not part of the animation system (eg - textures, meshes,
            // MonoScripts...), so check for the types we care about here
            switch (original)
            {
                // Any object referenced by an animator that we intend to mutate needs to be listed here.
                case Motion _:
                case AnimatorController _:
                case AnimatorState _:
                case AnimatorStateMachine _:
                case AnimatorTransitionBase _:
                case StateMachineBehaviour _:
                case AvatarMask _:
                    break; // We want to clone these types

                // Leave textures, materials, and script definitions alone
                case Texture _:
                case MonoScript _:
                case Material _:
                    return original;

                // Also avoid copying unknown scriptable objects.
                // This ensures compatibility with e.g. avatar remote, which stores state information in a state
                // behaviour referencing a custom ScriptableObject
                case ScriptableObject _:
                    return original;

                default:
                    throw new Exception($"Unknown type referenced from animator: {original.GetType()}");
            }
            if (_cache.TryGetValue(original, out var cached)) return (T)cached;

            var obj = visitor(original);
            if (obj != null)
            {
                _cache[original] = obj;
                return (T)obj;
            }

            var ctor = original.GetType().GetConstructor(Type.EmptyTypes);
            if (ctor == null || original is ScriptableObject)
            {
                obj = Object.Instantiate(original);
            }
            else
            {
                obj = (T)ctor.Invoke(Array.Empty<object>());
                EditorUtility.CopySerialized(original, obj);
            }

            _cache[original] = obj;

            using (var so = new SerializedObject(obj))
            {
                foreach (var prop in so.ObjectReferenceProperties())
                    prop.objectReferenceValue = DeepClone(prop.objectReferenceValue, visitor);

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            return (T)obj;
        }
    }
}
