using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace Anatawa12.Merger.Processors
{
    internal class ApplyObjectMapping
    {
        public void Apply(MergerSession session)
        {
            var mapping = session.GetMapping();

            // replace all objects
            foreach (var component in session.GetComponents<Component>())
            {
                var serialized = new SerializedObject(component);
                var p = serialized.GetIterator();
                AnimatorControllerMapper mapper = null;
                while (p.NextVisible(true))
                {
                    if (p.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        if (p.objectReferenceValue != null)
                            if (mapping.TryGetValue(p.objectReferenceValue, out var mapped))
                                p.objectReferenceValue = mapped;
                        if (p.objectReferenceValue is AnimatorController controller)
                        {
                            if (mapper == null)
                                mapper = new AnimatorControllerMapper(mapping, component.transform, session);

                            var mapped = mapper.MapAnimatorController(controller);
                            if (mapped != null)
                                p.objectReferenceValue = mapped;
                        }
                    }
                }

                serialized.ApplyModifiedProperties();
            }
        }
    }

    internal class AnimatorControllerMapper
    {
        private readonly Dictionary<(string, Type), string> _mapping = new Dictionary<(string, Type), string>();
        private readonly Dictionary<Object, Object> _cache = new Dictionary<Object, Object>();
        private readonly MergerSession _session;
        private bool _mapped = false;

        public AnimatorControllerMapper(Dictionary<Object, Object> mapping, Transform root, MergerSession session)
        {
            _session = session;
            foreach (var kvp in mapping)
            {
                if (!(kvp.Key is Component key)) continue;
                if (kvp.Value == null) continue;
                Assert.AreEqual(key.GetType(), kvp.Value.GetType());
                var value = (Component) kvp.Value;
                var relativeKey = Utils.RelativePath(root, key.transform);
                if (relativeKey == null) continue;
                var relativeValue = Utils.RelativePath(root, value.transform);
                if (relativeValue == null) continue;
                _mapping[(relativeKey, key.GetType())] = relativeValue;
            }
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
                    var newBinding = binding;
                    newBinding.path = MapPath(binding.path, binding.type);
                    newClip.SetCurve(newBinding.path, newBinding.type, newBinding.propertyName,
                        AnimationUtility.GetEditorCurve(clip, binding));
                }

                foreach (var objBinding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                {
                    var newBinding = objBinding;
                    newBinding.path = MapPath(objBinding.path, objBinding.type);
                    AnimationUtility.SetObjectReferenceCurve(newClip, newBinding,
                        AnimationUtility.GetObjectReferenceCurve(clip, objBinding));
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

        private string MapPath(string bindingPath, Type bindingType)
        {
            if (!_mapping.TryGetValue((bindingPath, bindingType), out var newPath))
                return bindingPath;
            _mapped = true;
            return newPath;
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
