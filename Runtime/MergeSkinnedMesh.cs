using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Animations;

namespace Anatawa12.AvatarOptimizer
{
    /// <summary>
    /// The Merge Skinned Mesh Component.
    /// </summary>
    [AddComponentMenu("Avatar Optimizer/AAO Merge Skinned Mesh")]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    [DisallowMultipleComponent]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/merge-skinned-mesh/")]
    [PublicAPI]
    public sealed class MergeSkinnedMesh : EditSkinnedMeshComponent, ISourceSkinnedMeshComponent
    {
        [AAOLocalized("MergeSkinnedMesh:prop:renderers")]
        [SerializeField]
        internal PrefabSafeSet.PrefabSafeSet<SkinnedMeshRenderer> renderersSet;
        [AAOLocalized("MergeSkinnedMesh:prop:staticRenderers")]
        [SerializeField]
        internal PrefabSafeSet.PrefabSafeSet<MeshRenderer> staticRenderersSet;
        [SerializeField]
        internal PrefabSafeSet.PrefabSafeSet<Material> doNotMergeMaterials;

        // common between v0 and v1
        [NotKeyable, AAOLocalized("MergeSkinnedMesh:prop:removeEmptyRendererObject")]
        [ToggleLeft]
        [SerializeField]
        internal bool removeEmptyRendererObject = true;
        [AAOLocalized("MergeSkinnedMesh:prop:skipEnablementMismatchedRenderers")]
        [NotKeyable]
        [ToggleLeft]
        [SerializeField]
        internal bool skipEnablementMismatchedRenderers;

        [AAOLocalized("MergeSkinnedMesh:prop:copyEnablementAnimation")]
        [NotKeyable]
        [ToggleLeft]
        [SerializeField]
        internal bool copyEnablementAnimation;

        APIChecker _checker;

        internal MergeSkinnedMesh()
        {
            renderersSet = new PrefabSafeSet.PrefabSafeSet<SkinnedMeshRenderer>(this);
            staticRenderersSet = new PrefabSafeSet.PrefabSafeSet<MeshRenderer>(this);
            doNotMergeMaterials = new PrefabSafeSet.PrefabSafeSet<Material>(this);
        }

        /// <summary>
        /// Initializes the MergeSkinnedMesh with the specified default behavior version.
        ///
        /// As Described in the documentation, you have to call this method after `AddComponent` to make sure
        /// the default configuration is what you want.
        /// Without calling this method, the default configuration might be changed in the future.
        /// </summary>
        /// <param name="version">
        /// The default configuration version.
        /// Since 1.7.0, version 1 is supported.
        ///
        /// Since 1.8.0, version 2 is supported.
        /// Changes:
        /// - Default value for skipEnablementMismatchedRenderers is changed. Before 1.8.0: true, 1.8.0 and later: false
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">Unsupported configuration version</exception>
        [PublicAPI]
        public void Initialize(int version)
        {
            switch (version)
            {
                case 1:
                    skipEnablementMismatchedRenderers = true;
                    goto case 2;
                case 2:
                    // nothing to do
                    break; 
                default:
                    throw new ArgumentOutOfRangeException(nameof(version), $"unsupported version: {version}");
            }
            _checker.OnInitialize(version, this);
        }

        /// <summary>
        /// If this is true, if the source renderer GameObject is not used by the merged mesh,
        /// the source renderer GameObject doesn't have any other components, and has no children.
        ///
        /// Since the GameObjects of static source renderers will always be used as bone, they will not be removed.
        /// </summary>
        [PublicAPI]
        public bool RemoveEmptyRendererObject
        {
            get => _checker.OnAPIUsage(this, removeEmptyRendererObject);
            set => _checker.OnAPIUsage(this, removeEmptyRendererObject = value);
        }

        /// <summary>
        /// Skips merging renderers that have different enablement / activeness.
        /// I personally recommend disabling this option with scripting usage.
        ///
        /// If this option is enabled, and the target SkinnedMeshRenderer is enabled and active in hierarchy,
        /// source renderers that are disabled or inactive in hierarchy will not be merged.
        ///
        /// If this option is enabled, and the target SkinnedMeshRenderer is disabled or inactive in hierarchy,
        /// source renderers that are enabled and active in hierarchy will not be merged.
        ///
        /// If this option is disabled, all source renderers will be merged.
        /// </summary>
        [PublicAPI]
        public bool SkipEnablementMismatchedRenderers
        {
            get => _checker.OnAPIUsage(this, skipEnablementMismatchedRenderers);
            set => _checker.OnAPIUsage(this, skipEnablementMismatchedRenderers = value);
        }

        /// <summary>
        /// Gets the set of source SkinnedMeshRenderers.
        /// </summary>
        [PublicAPI]
        public API.PrefabSafeSetAccessor<SkinnedMeshRenderer> SourceSkinnedMeshRenderers =>
            _checker.OnAPIUsage(this, new API.PrefabSafeSetAccessor<SkinnedMeshRenderer>(renderersSet));

        /// <summary>
        /// Gets the set of source MeshRenderers.
        /// </summary>
        [PublicAPI]
        public API.PrefabSafeSetAccessor<MeshRenderer> SourceStaticMeshRenderers =>
            _checker.OnAPIUsage(this, new API.PrefabSafeSetAccessor<MeshRenderer>(staticRenderersSet));
    }
}
