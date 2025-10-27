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
    public sealed class MergeSkinnedMesh : EditSkinnedMeshComponent, ISourceSkinnedMeshComponent, ISerializationCallbackReceiver
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

        [AAOLocalized("MergeSkinnedMesh:prop:blendShapeMode")]
        [NotKeyable]
        [SerializeField]
        internal BlendShapeMode blendShapeMode = BlendShapeMode.TraditionalCompability;

        internal enum BlendShapeMode
        {
            /// <summary>
            /// Merge the same name blend shapes.
            ///
            /// This mode will be considered by AutoFreezeBlendShape by Trace and Optimize so
            /// if blendShape will be animated after merging, it will not be frozen. 
            /// </summary>
            MergeSameName,
            /// <summary>
            /// Rename BlendShapes to avoid conflict. This is the default behavior for new components.
            /// </summary>
            RenameToAvoidConflict,
            /// <summary>
            /// The v1.7.0 or earlier behavior. Default behavior for old components.
            /// This mode cannot be specified manually unless you use debug inspector.
            ///
            /// This mode is similar to RenameToAvoidConflict, but AutoFreezeBlendShape by Trace and Optimize will not
            /// consider this merge so if it's not animated directly, it will be frozen.
            /// </summary>
            TraditionalCompability,
        }

        APIChecker _checker;

        private void Reset()
        {
            blendShapeMode = BlendShapeMode.RenameToAvoidConflict;
        }

        internal MergeSkinnedMesh()
        {
            renderersSet = new PrefabSafeSet.PrefabSafeSet<SkinnedMeshRenderer>(this);
            staticRenderersSet = new PrefabSafeSet.PrefabSafeSet<MeshRenderer>(this);
            doNotMergeMaterials = new PrefabSafeSet.PrefabSafeSet<Material>(this);
        }

        private void ValidatePSUC()
        {
            PrefabSafeSet.PrefabSafeSet.OnValidate(this, x => x.renderersSet);
            PrefabSafeSet.PrefabSafeSet.OnValidate(this, x => x.staticRenderersSet);
            PrefabSafeSet.PrefabSafeSet.OnValidate(this, x => x.doNotMergeMaterials);
        }

        private void OnValidate() => ValidatePSUC();
        void ISerializationCallbackReceiver.OnBeforeSerialize() => ValidatePSUC();

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
        }

        /// <summary>
        /// Initializes the MergeSkinnedMesh with the specified default behavior version.
        ///
        /// <p>
        /// As Described in the documentation, you have to call this method after `AddComponent` to make sure
        /// the default configuration is what you want.
        /// Without calling this method, the default configuration might be changed in the future.
        /// </p>
        /// </summary>
        /// <param name="version">
        /// <para>
        /// The default configuration version.
        /// </para>
        /// <para>
        /// Since 1.7.0, version 1 is supported.
        /// </para>
        ///
        /// <para>
        /// Since 1.8.0, version 2 which changed the following value is supported.
        /// <list type="bullet">
        /// <item>Default value for skipEnablementMismatchedRenderers is changed. Before 1.8.0: true, 1.8.0 and later: false</item>
        /// <item>
        ///     BlendShape Mode is added. Before 1.8.0, the behavior is similar to merge sane name,
        ///     but different behavior with Trace And Optimize. (Merging behavior is considered as bug in this version).
        ///     With 1.8.0 and later, the default behavior is rename to avoid conflict, and you can configure to
        ///     Merge same name blendShape with <see cref="MergeBlendShapes"/> property.
        /// </item>
        /// </list>
        /// </para>
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">Unsupported configuration version</exception>
        [PublicAPI]
        public void Initialize(int version)
        {
            switch (version)
            {
                case 1:
                    skipEnablementMismatchedRenderers = true;
                    blendShapeMode = BlendShapeMode.RenameToAvoidConflict;
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
        /// <remarks>Due to historical reasons, this named "static mesh renderers", but called "basic mesh renderers"</remarks>
        [PublicAPI]
        public API.PrefabSafeSetAccessor<MeshRenderer> SourceStaticMeshRenderers =>
            _checker.OnAPIUsage(this, new API.PrefabSafeSetAccessor<MeshRenderer>(staticRenderersSet));

        /// <summary>
        /// Gets or Sets behavior of blendShape merging.
        ///
        /// If this value is true, BlendShapes with same name will be merged.
        /// If this value is false, BlendShape names will be mangled to avoid conflict.
        /// </summary>
        /// <remarks>
        /// This API is added in v1.8.0 and available with Initialize version 2.
        /// If you <see cref="Initialize"/>d with version 1 or earlier, this API will throw an exception.
        /// </remarks>
        /// <exception cref="InvalidOperationException">If the API is used before initialization or with unsupported version.</exception>
        [PublicAPI]
        public bool MergeBlendShapes
        {
            get => _checker.OnAPIUsageVersioned(this, 2, () => blendShapeMode == BlendShapeMode.MergeSameName);
            set => _checker.OnAPIUsageVersioned(this, 2,
                () => blendShapeMode = value ? BlendShapeMode.MergeSameName : BlendShapeMode.RenameToAvoidConflict);
        }
    }
}
