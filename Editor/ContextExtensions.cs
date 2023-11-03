using System;
using Anatawa12.AvatarOptimizer.AnimatorParsers;
using Anatawa12.AvatarOptimizer.Processors;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using JetBrains.Annotations;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    internal static class ContextExtensions
    {
        public static T[] GetComponents<T>([NotNull] this BuildContext context) where T : Component
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return context.AvatarRootObject.GetComponentsInChildren<T>(true);
        }

        private static MeshInfo2Holder GetHolder([NotNull] this BuildContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return context.Extension<MeshInfo2Context>().Holder;
        }

        public static MeshInfo2 GetMeshInfoFor([NotNull] this BuildContext context, SkinnedMeshRenderer renderer) =>
            context.GetHolder().GetMeshInfoFor(renderer);

        public static MeshInfo2 GetMeshInfoFor([NotNull] this BuildContext context, MeshRenderer renderer) =>
            context.GetHolder().GetMeshInfoFor(renderer);

        private static ObjectMappingBuilder GetMappingBuilder([NotNull] this BuildContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return context.Extension<ObjectMappingContext>().MappingBuilder;
        }

        public static void RecordMergeComponent<T>([NotNull] this BuildContext context, T from, T mergeTo)
            where T : Component =>
            GetMappingBuilder(context).RecordMergeComponent(from, mergeTo);

        public static void RecordMoveProperties([NotNull] this BuildContext context, Component from,
            params (string old, string @new)[] props) =>
            GetMappingBuilder(context).RecordMoveProperties(from, props);

        public static void RecordMoveProperty([NotNull] this BuildContext context, Component from, string oldProp,
            string newProp) =>
            GetMappingBuilder(context).RecordMoveProperty(from, oldProp, newProp);

        public static void RecordRemoveProperty([NotNull] this BuildContext context, Component from, string oldProp) =>
            GetMappingBuilder(context).RecordRemoveProperty(from, oldProp);

        public static AnimationComponentInfo GetAnimationComponent([NotNull] this BuildContext context,
            ComponentOrGameObject component) =>
            GetMappingBuilder(context).GetAnimationComponent(component);
        
        public static bool? GetConstantValue([NotNull] this BuildContext context, ComponentOrGameObject obj, string property, bool currentValue)
        {
            if (!context.GetAnimationComponent(obj).TryGetFloat(property, out var prop))
                return currentValue;

            switch (prop.State)
            {
                case AnimationFloatProperty.PropertyState.ConstantAlways:
                    return FloatToBool(prop.ConstValue);
                case AnimationFloatProperty.PropertyState.ConstantPartially:
                    var constValue = FloatToBool(prop.ConstValue);
                    if (constValue == currentValue) return currentValue;
                    return null;
                case AnimationFloatProperty.PropertyState.Variable:
                    return null;
                case AnimationFloatProperty.PropertyState.Invalid:
                default:
                    throw new ArgumentOutOfRangeException();
            }

            bool? FloatToBool(float f)
            {
                switch (f)
                {
                    case 0:
                        return false;
                    case 1:
                        return true;
                    default:
                        return null;
                }
            }
        }
    }
}