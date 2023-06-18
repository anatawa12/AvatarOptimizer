using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.ErrorReporting;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class ApplyObjectMapping
    {
        public void Apply(OptimizerSession session)
        {
            var mapping = session.MappingBuilder.BuildObjectMapping();

            // replace all objects
            foreach (var component in session.GetComponents<Component>())
            {
                var serialized = new SerializedObject(component);
                if (component is Transform) continue;
                AnimatorControllerMapper mapper = null;
                SpecialMappingApplier.Apply(component.GetType(), serialized, mapping, ref mapper);
                var p = serialized.GetIterator();
                var enterChildren = true;
                while (p.Next(enterChildren))
                {
                    if (p.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        if (mapping.MapComponentInstance(p.objectReferenceInstanceIDValue, out var mappedComponent))
                            p.objectReferenceValue = mappedComponent;

                        if (p.objectReferenceValue is AnimatorController controller)
                        {
                            if (mapper == null)
                                mapper = new AnimatorControllerMapper(mapping.CreateAnimationMapper(component.gameObject),
                                    session.RelativePath(component.transform), session);

                            // ReSharper disable once AccessToModifiedClosure
                            var mapped = BuildReport.ReportingObject(controller,
                                () => mapper.MapAnimatorController(controller));
                            if (mapped != null)
                                p.objectReferenceValue = mapped;
                        }
                    }

                    switch (p.propertyType)
                    {
                        case SerializedPropertyType.String:
                        case SerializedPropertyType.Integer:
                        case SerializedPropertyType.Boolean:
                        case SerializedPropertyType.Float:
                        case SerializedPropertyType.Color:
                        case SerializedPropertyType.ObjectReference:
                        case SerializedPropertyType.LayerMask:
                        case SerializedPropertyType.Enum:
                        case SerializedPropertyType.Vector2:
                        case SerializedPropertyType.Vector3:
                        case SerializedPropertyType.Vector4:
                        case SerializedPropertyType.Rect:
                        case SerializedPropertyType.ArraySize:
                        case SerializedPropertyType.Character:
                        case SerializedPropertyType.AnimationCurve:
                        case SerializedPropertyType.Bounds:
                        case SerializedPropertyType.Gradient:
                        case SerializedPropertyType.Quaternion:
                        case SerializedPropertyType.FixedBufferSize:
                        case SerializedPropertyType.Vector2Int:
                        case SerializedPropertyType.Vector3Int:
                        case SerializedPropertyType.RectInt:
                        case SerializedPropertyType.BoundsInt:
                            enterChildren = false;
                            break;
                        case SerializedPropertyType.Generic:
                        case SerializedPropertyType.ExposedReference:
                        case SerializedPropertyType.ManagedReference:
                        default:
                            enterChildren = true;
                            break;
                    }
                }

                serialized.ApplyModifiedProperties();
            }
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
        private readonly OptimizerSession _session;
        private readonly string _rootPath;
        private bool _mapped = false;

        public AnimatorControllerMapper(AnimationObjectMapper mapping, string rootPath, OptimizerSession session)
        {
            _session = session;
            _mapping = mapping;
            _rootPath = rootPath;
        }

        public AnimatorController MapAnimatorController(AnimatorController controller)
        {
            if (_cache.TryGetValue(controller, out var cached)) return (AnimatorController)cached;
            _mapped = false;
            var newController = new AnimatorController
            {
                parameters = controller.parameters,
                layers = controller.layers.Select(MapAnimatorControllerLayer).ToArray()
            };
            if (!_mapped) newController = null;
            _cache[controller] = newController;
            return _session.AddToAsset(newController);
        }

        private AnimatorControllerLayer MapAnimatorControllerLayer(AnimatorControllerLayer layer) =>
            new AnimatorControllerLayer
            {
                name = layer.name,
                avatarMask = layer.avatarMask,
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
                var newClip = _session.AddToAsset(new AnimationClip());
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

            _cache[original] = _session.AddToAsset(obj);

            SerializedObject so = new SerializedObject(obj);
            SerializedProperty prop = so.GetIterator();

            bool enterChildren = true;
            while (prop.Next(enterChildren))
            {
                enterChildren = true;
                switch (prop.propertyType)
                {
                    case SerializedPropertyType.ObjectReference:
                        prop.objectReferenceValue = DeepClone(prop.objectReferenceValue, visitor);
                        break;
                    // Iterating strings can get super slow...
                    case SerializedPropertyType.String:
                        enterChildren = false;
                        break;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            return (T)obj;
        }
    }
}
