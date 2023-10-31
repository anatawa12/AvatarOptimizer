using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.API;
using Anatawa12.AvatarOptimizer.APIInternal;
using Anatawa12.AvatarOptimizer.ErrorReporting;
using JetBrains.Annotations;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer
{
    internal class ObjectMappingContext : IExtensionContext
    {
        public ObjectMappingBuilder MappingBuilder { get; private set; }

        public void OnActivate(BuildContext context)
        {
            MappingBuilder = new ObjectMappingBuilder(context.AvatarRootObject);
        }

        public void OnDeactivate(BuildContext context)
        {
            var mapping = MappingBuilder.BuildObjectMapping();
            var mappingSource = new MappingSourceImpl(mapping);

            // replace all objects
            BuildReport.ReportingObjects(context.GetComponents<Component>(), component =>
            {
                if (component is Transform) return;
                
                // apply special mapping
                if (ComponentInfoRegistry.TryGetInformation(component.GetType(), out var info))
                    info.ApplySpecialMappingInternal(component, mappingSource);

                var serialized = new SerializedObject(component);
                AnimatorControllerMapper mapper = null;

                foreach (var p in serialized.ObjectReferenceProperties())
                {
                    if (mapping.MapComponentInstance(p.objectReferenceInstanceIDValue, out var mappedComponent))
                        p.objectReferenceValue = mappedComponent;

                    if (p.objectReferenceValue is RuntimeAnimatorController controller)
                    {
                        if (mapper == null)
                            mapper = new AnimatorControllerMapper(mapping.CreateAnimationMapper(component.gameObject));

                        // ReSharper disable once AccessToModifiedClosure
                        var mapped = BuildReport.ReportingObject(controller,
                            () => mapper.MapAnimatorController(controller));
                        if (mapped != controller)
                            p.objectReferenceValue = mapped;
                    }
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
            });
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
        }

        private class ComponentInfo<T> : MappedComponentInfo<T> where T : Object
        {
            [NotNull] private readonly ComponentInfo _info;

            public ComponentInfo([NotNull] ComponentInfo info) => _info = info;

            public override T MappedComponent => EditorUtility.InstanceIDToObject(_info.MergedInto) as T;
            public override bool TryMapProperty(string property, out API.MappedPropertyInfo found)
            {
                found = default;

                if (!_info.PropertyMapping.TryGetValue(property, out var mappedProp)) return false;
                if (mappedProp.MappedProperty.Name == null) return false;

                found = new API.MappedPropertyInfo(
                    EditorUtility.InstanceIDToObject(mappedProp.MappedProperty.InstanceId),
                    mappedProp.MappedProperty.Name);
                return true;

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

        public T MapAnimatorController<T>(T controller) where T : RuntimeAnimatorController =>
            DeepClone(controller, CustomClone);

        // https://github.com/bdunderscore/modular-avatar/blob/db49e2e210bc070671af963ff89df853ae4514a5/Packages/nadena.dev.modular-avatar/Editor/AnimatorMerger.cs#L199-L241
        // Originally under MIT License
        // Copyright (c) 2022 bd_
        private Object CustomClone(Object o)
        {
            if (o is AnimationClip clip)
            {
                var newClip = new AnimationClip();
                newClip.name = "rebased " + clip.name;

                // copy m_UseHighQualityCurve with SerializedObject since m_UseHighQualityCurve doesn't have public API
                using (var serializedClip = new SerializedObject(clip))
                using (var serializedNewClip = new SerializedObject(newClip))
                {
                    serializedNewClip.FindProperty("m_UseHighQualityCurve")
                        .boolValue = serializedClip.FindProperty("m_UseHighQualityCurve").boolValue;
                    serializedNewClip.ApplyModifiedPropertiesWithoutUndo();
                }

                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    var newBindings = _mapping.MapBinding(binding);
                    if (newBindings == null)
                    {
                        newClip.SetCurve(binding.path, binding.type, binding.propertyName,
                            AnimationUtility.GetEditorCurve(clip, binding));
                    }
                    else
                    {
                        _mapped = true;
                        foreach (var newBinding in newBindings)
                        {
                            newClip.SetCurve(newBinding.path, newBinding.type, newBinding.propertyName,
                                AnimationUtility.GetEditorCurve(clip, binding));
                        }
                    }
                }

                foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                {
                    var newBindings = _mapping.MapBinding(binding);
                    if (newBindings == null)
                    {
                        AnimationUtility.SetObjectReferenceCurve(newClip, binding,
                            AnimationUtility.GetObjectReferenceCurve(clip, binding));
                    }
                    else
                    {
                        _mapped = true;
                        foreach (var newBinding in newBindings)
                        {
                            AnimationUtility.SetObjectReferenceCurve(newClip, newBinding,
                                AnimationUtility.GetObjectReferenceCurve(clip, binding));
                        }
                    }
                }

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (newClip.length != clip.length)
                {
                    // if newClip has less properties than original clip (especially for no properties), 
                    // length of newClip can be changed which is bad.
                    newClip.SetCurve(
                        "$AvatarOptimizerClipLengthDummy$", typeof(GameObject), "m_IsActive",
                        AnimationCurve.Constant(clip.length, clip.length, 1f));
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
                for (var part = AvatarMaskBodyPart.Root; part < AvatarMaskBodyPart.LastBodyPart; ++part)
                    newMask.SetHumanoidBodyPartActive(part, mask.GetHumanoidBodyPartActive(part));
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
            else if (o is RuntimeAnimatorController controller)
            {
                using (new MappedScope(this))
                {
                    var newController = DefaultDeepClone(controller, CustomClone);
                    newController.name = controller.name + " (rebased)";
                    if (!_mapped) newController = controller;
                    _cache[controller] = newController;
                    return newController;
                }
            }
            else
            {
                return null;
            }
        }

        private readonly struct MappedScope : IDisposable
        {
            private readonly AnimatorControllerMapper _mapper;
            private readonly bool _previous;

            public MappedScope(AnimatorControllerMapper mapper)
            {
                _mapper = mapper;
                _previous = mapper._mapped;
                mapper._mapped = false;
            }

            public void Dispose()
            {
                _mapper._mapped |= _previous;
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
                case AnimatorOverrideController _:
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
                _cache[obj] = obj;
                return (T)obj;
            }

            return DefaultDeepClone(original, visitor);
        }

        private T DefaultDeepClone<T>(T original, Func<Object, Object> visitor) where T : Object
        {
            Object obj;
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
            _cache[obj] = obj;

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