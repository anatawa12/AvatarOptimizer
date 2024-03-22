using System;
using Anatawa12.AvatarOptimizer.Processors;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using JetBrains.Annotations;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    public static class MeshInfo2ContextExtensions
    {
        [NotNull]
        private static MeshInfo2Holder GetHolder([NotNull] this BuildContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return context.Extension<MeshInfo2Context>().Holder;
        }

        public static MeshInfo2 GetMeshInfoFor([NotNull] this BuildContext context, SkinnedMeshRenderer renderer) =>
            context.GetHolder().GetMeshInfoFor(renderer);
    }
}