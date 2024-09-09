using System;
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
public class DupliacteAssets : Pass<DupliacteAssets>
{
    protected override void Execute(BuildContext context)
    {
        if (!context.GetState<AAOEnabled>().Enabled) return;

        var cloner = new Cloner();

        foreach (var component in context.GetComponents<Component>())
        {
            switch (component)
            {
                case SkinnedMeshRenderer renderer:
                {
                    var meshInfo2 = context.GetMeshInfoFor(renderer);
                    foreach (var subMesh in meshInfo2.SubMeshes)
                    foreach (ref var material in subMesh.SharedMaterials.AsSpan())
                            material = cloner.MapObject(material);
                }
                    break;
                default:
                {
                    using var serializedObject = new SerializedObject(component);

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
        protected override Object? CustomClone(Object o) => null;

        protected override ComponentSupport GetComponentSupport(Object o)
        {
            switch (o)
            {
                // Target Objects
                case Material:
                    return ComponentSupport.Clone;

                // intermediate objects
                case Motion:
                case AnimatorController:
                case AnimatorOverrideController:
                case AnimatorState:
                case AnimatorStateMachine:
                case AnimatorTransitionBase:
                case StateMachineBehaviour:

#if AAO_VRM0
                case VRM.BlendShapeAvatar:
                case VRM.BlendShapeClip:
#endif
#if AAO_VRM1
                case UniVRM10.VRM10Object:
                case UniVRM10.VRM10Expression:
#endif
                    return ComponentSupport.Clone;

                case Texture:
                case MonoScript:
                case Component:
                case GameObject:
                    return ComponentSupport.NoClone;

                case ScriptableObject:
                    return ComponentSupport.NoClone;

                default:
                    return ComponentSupport.NoClone;
            }
        }
    }
}
