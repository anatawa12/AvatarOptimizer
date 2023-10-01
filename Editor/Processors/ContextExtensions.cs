using System;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using JetBrains.Annotations;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors
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
    }
}