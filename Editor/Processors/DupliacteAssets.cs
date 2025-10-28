using System;
using System.Collections.Generic;
using System.Reflection;
using Anatawa12.AvatarOptimizer.ndmf;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Processors;

/// <summary>
/// The class to clone something for future in-place modification
/// 
/// Currently this class is intended to clone
/// </summary>
internal class DupliacteAssets : Pass<DupliacteAssets>
{
    protected override void Execute(BuildContext context)
    {
        if (!context.GetState<AAOEnabled>().Enabled) return;

        var sharedCache = new Dictionary<Object, Object>();

        foreach (var component in context.GetComponents<Component>())
        {
            switch (component)
            {
                // skip some known unrelated components
                case Transform:
                case ParticleSystem:
                    break;
                case SkinnedMeshRenderer renderer:
                {
                    var cloner = new Cloner(sharedCache);
                    var meshInfo2 = context.GetMeshInfoFor(renderer);
                    foreach (var subMesh in meshInfo2.SubMeshes)
                    foreach (ref var material in subMesh.SharedMaterials.AsSpan())
                            material = cloner.MapObject(material);
                }
                    break;
                default:
                {
                    using var serializedObject = new SerializedObject(component);
                    var cloner = new Cloner(sharedCache);

                    cloner.FlattenOverrideController = component is Animator
#if AAO_VRCSDK3_AVATARS
                            or VRC.SDK3.Avatars.Components.VRCAvatarDescriptor
#endif
                        ;

                    foreach (var objectReferenceProperty in serializedObject.ObjectReferenceProperties())
                    {
                        objectReferenceProperty.objectReferenceValue = cloner.MapObject(objectReferenceProperty.objectReferenceValue);
                    }

                    serializedObject.ApplyModifiedPropertiesWithoutUndo();

                    break;
                }
            }
        }
    }

    class Cloner : DeepCloneHelper
    {
        public Dictionary<Object, Object> SharedObjects;
        public bool FlattenOverrideController;

        public Cloner(Dictionary<Object, Object> sharedObjects)
        {
            SharedObjects = sharedObjects;
        }

        protected override Dictionary<Object, Object> GetCache(Type type)
        {
            if (type == typeof(Material)) return SharedObjects;
            return base.GetCache(type);
        }

        private IReadOnlyDictionary<AnimationClip,AnimationClip>? _mapping;

        protected override Object? CustomClone(Object o)
        {
            if (o is Material mat)
            {
                Material newMat;
                using (MaterialEditorReflection.BeginNoApplyMaterialPropertyDrawers())
                    newMat = new Material(mat);
                newMat.parent = null; // force flatten material variants
                ObjectRegistry.RegisterReplacedObject(mat, newMat);
                return newMat;
            }
            else if (o is AnimationClip clip)
            {
                if (_mapping != null && _mapping.TryGetValue(clip, out var mapped))
                    return DefaultDeepClone(mapped);
                return DefaultDeepClone(clip);
            }
            else if (o is AnimatorOverrideController overrideController)
            {
                if (!FlattenOverrideController) return DefaultDeepClone(overrideController);
                if (_mapping != null)
                    throw new NotImplementedException("AnimatorOverrideController recursive clone");
                var (controller, mapping) = ACUtils.GetControllerAndOverrides(overrideController);
                _mapping = mapping;
                try
                {
                    return DeepClone(controller);
                } finally {
                    _mapping = null;
                }
            }

            return null;
        }


        protected override ComponentSupport GetComponentSupport(Object o)
        {
            switch (o)
            {
                // Target Objects
                case Material:
                case Motion:
                case AnimatorController:
                case AnimatorOverrideController:
                case AnimatorState:
                case AnimatorStateMachine:
                case AnimatorTransitionBase:
                case StateMachineBehaviour:
                case AvatarMask:

#if AAO_VRM0
                case VRM.BlendShapeAvatar:
                case VRM.BlendShapeClip:
#endif
#if AAO_VRM1
                case UniVRM10.VRM10Object:
                case UniVRM10.VRM10Expression:
#endif
                    return ComponentSupport.Clone;

                default:
                    return ComponentSupport.NoClone;
            }
        }
    }

    static class MaterialEditorReflection
    {
        static MaterialEditorReflection()
        {
            DisableApplyMaterialPropertyDrawersPropertyInfo = typeof(EditorMaterialUtility).GetProperty(
                "disableApplyMaterialPropertyDrawers", BindingFlags.Static | BindingFlags.NonPublic)!;
        }

        public static readonly System.Reflection.PropertyInfo DisableApplyMaterialPropertyDrawersPropertyInfo;

        public static DisableApplyMaterialPropertyDisposable BeginNoApplyMaterialPropertyDrawers()
        {
            return new DisableApplyMaterialPropertyDisposable(true);
        }

        public static bool DisableApplyMaterialPropertyDrawers
        {
            get => (bool)DisableApplyMaterialPropertyDrawersPropertyInfo.GetValue(null);
            set => DisableApplyMaterialPropertyDrawersPropertyInfo.SetValue(null, value);
        }

        public struct DisableApplyMaterialPropertyDisposable : IDisposable
        {
            private readonly bool _originalValue;

            public DisableApplyMaterialPropertyDisposable(bool value)
            {
                _originalValue = DisableApplyMaterialPropertyDrawers;
                DisableApplyMaterialPropertyDrawers = value;
            }

            public void Dispose()
            {
                DisableApplyMaterialPropertyDrawers = _originalValue;
            }
        }
    }
}
