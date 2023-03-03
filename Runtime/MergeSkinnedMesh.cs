using System;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Optimizer/Merge Skinned Mesh")]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    [DisallowMultipleComponent]
    internal class MergeSkinnedMesh : EditSkinnedMeshComponent
    {
        #region v1
        [Obsolete("traditional save format")]
        public SkinnedMeshRenderer[] renderers = Array.Empty<SkinnedMeshRenderer>();
        [Obsolete("traditional save format")]
        public MeshRenderer[] staticRenderers = Array.Empty<MeshRenderer>();
        [Obsolete("traditional save format")]
        public MergeConfig[] merges = Array.Empty<MergeConfig>();
        [Serializable]
        public class MergeConfig
        {
            public Material target;
            // long as pair of int,
            // 0xFFFFFFFF00000000 as index of renderer
            // 0x00000000FFFFFFFF as index of material in renderer
            public ulong[] merges;
        }
        #endregion
        
        #region v2
        public PrefabSafeSet.SkinnedMeshRendererSet renderersSet;
        public PrefabSafeSet.MeshRendererSet staticRenderersSet;
        public PrefabSafeSet.MaterialSet doNotMergeMaterials;
        #endregion

        // common between v0 and v1
        public bool removeEmptyRendererObject;

        public MergeSkinnedMesh()
        {
            renderersSet = new PrefabSafeSet.SkinnedMeshRendererSet(this);
            staticRenderersSet = new PrefabSafeSet.MeshRendererSet(this);
            doNotMergeMaterials = new PrefabSafeSet.MaterialSet(this);
        }
    }
}
