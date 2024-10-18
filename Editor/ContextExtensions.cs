using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    internal static class ContextExtensions
    {
        public static T[] GetComponents<T>(this BuildContext context) where T : Component
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return context.AvatarRootObject.GetComponentsInChildren<T>(true);
        }

        public static ObjectMappingBuilder<PropertyInfo> GetMappingBuilder(this BuildContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return context.Extension<ObjectMappingContext>().MappingBuilder!; // activated so not null
        }

        public static void RecordMergeComponent<T>(this BuildContext context, T from, T mergeTo)
            where T : Component =>
            GetMappingBuilder(context).RecordMergeComponent(from, mergeTo);

        public static void RecordMoveProperties(this BuildContext context, Component from,
            params (string old, string @new)[] props) =>
            GetMappingBuilder(context).RecordMoveProperties(from, props);

        public static void RecordMoveProperty(this BuildContext context, Component from, string oldProp,
            string newProp) =>
            GetMappingBuilder(context).RecordMoveProperty(from, oldProp, newProp);

        public static void RecordRemoveProperty(this BuildContext context, Component from, string oldProp) =>
            GetMappingBuilder(context).RecordRemoveProperty(from, oldProp);

        public static AnimationComponentInfo<PropertyInfo> GetAnimationComponent(this BuildContext context,
            ComponentOrGameObject component) =>
            GetMappingBuilder(context).GetAnimationComponent(component);

        public static bool? GetConstantValue(this BuildContext context, ComponentOrGameObject obj,
            string property, bool currentValue) =>
            context.GetAnimationComponent(obj).GetFloatNode(property).AsConstantValue(currentValue);

        public static bool? AsConstantValue(this PropModNode<FloatValueInfo>? node, bool currentValue)
        {
            if (node == null) return currentValue;
            if (node.ApplyState == ApplyState.Never) return currentValue;
            if (node.Value.PossibleValues is { } values)
            {
                bool? constValue = null;
                foreach (var value in values)
                {
                    var current = value != 0;
                    if (constValue is not { } b) constValue = current;
                    else if (b != current) return null;
                }
                if (node.ApplyState == ApplyState.Always || constValue == currentValue)
                    return constValue;
            }

            return null;
        }

        public static IEnumerable<Material> GetAllPossibleMaterialFor(this BuildContext context, Renderer renderer)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (renderer == null) throw new ArgumentNullException(nameof(renderer));
            var materialsRaw =
                renderer is SkinnedMeshRenderer skinned
                    ? context.GetMeshInfoFor(skinned).SubMeshes.SelectMany(x => x.SharedMaterials)
                    : renderer.sharedMaterials;
            var materials = materialsRaw.OfType<Material>();
            var animated = context.GetAnimationComponent(renderer).GetAllObjectProperties()
                .SelectMany(x => x.node.Value.PossibleValues).OfType<Material>();
            return materials.Concat(animated).Distinct();
        }
    }
}
