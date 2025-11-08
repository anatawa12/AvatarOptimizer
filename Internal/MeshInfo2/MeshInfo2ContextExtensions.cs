using System;
using Anatawa12.AvatarOptimizer.Processors;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    public static class MeshInfo2ContextExtensions
    {
        private static MeshInfo2Holder GetHolder(this BuildContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            // it's activated so it's not null
            return context.Extension<MeshInfo2Context>().Holder!;
        }

        public static MeshInfo2 GetMeshInfoFor(this BuildContext context, Renderer renderer) =>
            context.GetHolder().GetMeshInfoFor(renderer);
    }
}
