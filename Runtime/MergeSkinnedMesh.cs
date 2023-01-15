using System;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Optimizer/Merge Skinned Mesh")]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    [DisallowMultipleComponent]
    public class MergeSkinnedMesh : EditSkinnedMeshComponent
    {
        public SkinnedMeshRenderer[] renderers = Array.Empty<SkinnedMeshRenderer>();
        public MeshRenderer[] staticRenderers = Array.Empty<MeshRenderer>();
        public MergeConfig[] merges = Array.Empty<MergeConfig>();
        public bool removeEmptyRendererObject;

        [Serializable]
        public class MergeConfig
        {
            public Material target;
            // long as pair of int,
            // 0xFFFFFFFF00000000 as index of renderer
            // 0x00000000FFFFFFFF as index of material in renderer
            public ulong[] merges;
        }
    }
}
