using System;
using Anatawa12.AvatarOptimizer.ErrorReporting;
using CustomLocalization4EditorExtension;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Optimizer/Merge Skinned Mesh")]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    [DisallowMultipleComponent]
    internal class MergeSkinnedMesh : EditSkinnedMeshComponent, IStaticValidated
    {
        #region v1
        [Obsolete("legacy v1", true)]
        public SkinnedMeshRenderer[] renderers = Array.Empty<SkinnedMeshRenderer>();
        [Obsolete("legacy v1", true)]
        public MeshRenderer[] staticRenderers = Array.Empty<MeshRenderer>();
        [Obsolete("legacy v1", true)]
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
        [CL4EELocalized("MergeSkinnedMesh:prop:renderers")]
        public PrefabSafeSet.SkinnedMeshRendererSet renderersSet;
        [CL4EELocalized("MergeSkinnedMesh:prop:staticRenderers")]
        public PrefabSafeSet.MeshRendererSet staticRenderersSet;
        public PrefabSafeSet.MaterialSet doNotMergeMaterials;
        #endregion

        // common between v0 and v1
        [CL4EELocalized("MergeSkinnedMesh:prop:removeEmptyRendererObject")]
        [ToggleLeft]
        public bool removeEmptyRendererObject = true;

        public MergeSkinnedMesh()
        {
            renderersSet = new PrefabSafeSet.SkinnedMeshRendererSet(this);
            staticRenderersSet = new PrefabSafeSet.MeshRendererSet(this);
            doNotMergeMaterials = new PrefabSafeSet.MaterialSet(this);
        }
    }
}
