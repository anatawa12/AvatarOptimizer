using System;
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
    }
}