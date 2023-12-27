using CustomLocalization4EditorExtension;
using UnityEngine;
using UnityEngine.Animations;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/AAO Merge Skinned Mesh")]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    [DisallowMultipleComponent]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/merge-skinned-mesh/")]
    internal class MergeSkinnedMesh : EditSkinnedMeshComponent
    {
        [CL4EELocalized("MergeSkinnedMesh:prop:renderers")]
        public PrefabSafeSet.SkinnedMeshRendererSet renderersSet;
        [CL4EELocalized("MergeSkinnedMesh:prop:staticRenderers")]
        public PrefabSafeSet.MeshRendererSet staticRenderersSet;
        public PrefabSafeSet.MaterialSet doNotMergeMaterials;

        // common between v0 and v1
        [NotKeyable, CL4EELocalized("MergeSkinnedMesh:prop:removeEmptyRendererObject")]
        [ToggleLeft]
        public bool removeEmptyRendererObject = true;
        [CL4EELocalized("MergeSkinnedMesh:prop:skipEnablementMismatchedRenderers")]
        [NotKeyable]
        [ToggleLeft]
        public bool skipEnablementMismatchedRenderers;

        private void Reset()
        {
            skipEnablementMismatchedRenderers = true;
        }

        public MergeSkinnedMesh()
        {
            renderersSet = new PrefabSafeSet.SkinnedMeshRendererSet(this);
            staticRenderersSet = new PrefabSafeSet.MeshRendererSet(this);
            doNotMergeMaterials = new PrefabSafeSet.MaterialSet(this);
        }
    }
}
