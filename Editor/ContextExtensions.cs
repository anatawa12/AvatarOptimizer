using System;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
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

        public static ObjectMappingBuilder<PropertyInfo> GetMappingBuilder([NotNull] this BuildContext context)
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

        public static AnimationComponentInfo<PropertyInfo> GetAnimationComponent([NotNull] this BuildContext context,
            ComponentOrGameObject component) =>
            GetMappingBuilder(context).GetAnimationComponent(component);

        public static bool? GetConstantValue([NotNull] this BuildContext context, ComponentOrGameObject obj,
            string property, bool currentValue) =>
            !context.GetAnimationComponent(obj).TryGetFloat(property, out var node)
                ? currentValue
                : node.AsConstantValue(currentValue);

        public static bool? AsConstantValue([CanBeNull] this PropModNode<float> node, bool currentValue)
        {
            if (node == null) return currentValue;
            if (node.Value.PossibleValues is float[] values)
            {
                bool? constValue = null;
                foreach (var value in values)
                {
                    var current = value != 0;
                    if (!(constValue is bool b)) constValue = current;
                    else if (b != current) return null;
                }
                if (node.AppliedAlways || constValue == currentValue)
                    return constValue;
            }

            return null;
        }
    }
}