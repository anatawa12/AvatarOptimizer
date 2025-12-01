using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Serialization;

namespace Anatawa12.AvatarOptimizer
{
    /// <summary>
    /// The type of the component is the only public API of this component.
    ///
    /// You cannot configure this component from script.
    /// </summary>
    // previously known as Automatic Configuration
    [AddComponentMenu("Avatar Optimizer/AAO Trace And Optimize")]
    [DisallowMultipleComponent]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/trace-and-optimize/")]
    [PublicAPI]
    public sealed class TraceAndOptimize : AvatarGlobalComponent
    {
        internal TraceAndOptimize()
        {
        }

        [NotKeyable]
        [AAOLocalized("TraceAndOptimize:prop:optimizeBlendShape")]
        [ToggleLeft]
        [SerializeField]
        [FormerlySerializedAs("freezeBlendShape")] // renamed in 1.8.0
        internal bool optimizeBlendShape = true;
        [NotKeyable]
        [AAOLocalized("TraceAndOptimize:prop:removeUnusedObjects")]
        [ToggleLeft]
        [SerializeField]
        internal bool removeUnusedObjects = true;

        // Remove Unused Objects Options
        [NotKeyable]
        [AAOLocalized("TraceAndOptimize:prop:preserveEndBone",
            "TraceAndOptimize:tooltip:preserveEndBone")]
        [ToggleLeft]
        [SerializeField]
        internal bool preserveEndBone;

        [NotKeyable]
        [AAOLocalized("TraceAndOptimize:prop:removeZeroSizedPolygons")]
        [ToggleLeft]
        [SerializeField]
        // Note: this option is a part of Advanced Optimizations 
        internal bool removeZeroSizedPolygons = false;

        [NotKeyable]
        [AAOLocalized("TraceAndOptimize:prop:optimizePhysBone")]
        [ToggleLeft]
        [SerializeField]
#if !AAO_VRCSDK3_AVATARS
        // no meaning without VRCSDK
        [HideInInspector]
#endif
        internal bool optimizePhysBone = true;

        [NotKeyable]
        [AAOLocalized("TraceAndOptimize:prop:optimizeAnimator")]
        [ToggleLeft]
        [SerializeField]
        internal bool optimizeAnimator = true;

        [NotKeyable]
        [AAOLocalized("TraceAndOptimize:prop:mergeSkinnedMesh")]
        [ToggleLeft]
        [SerializeField]
        internal bool mergeSkinnedMesh = true;

        [NotKeyable]
        [AAOLocalized("TraceAndOptimize:prop:allowShuffleMaterialSlots",
            "TraceAndOptimize:tooltip:allowShuffleMaterialSlots")]
        [ToggleLeft]
        [SerializeField]
        internal bool allowShuffleMaterialSlots = true;

        [NotKeyable]
        [AAOLocalized("TraceAndOptimize:prop:optimizeTexture")]
        [ToggleLeft]
        [SerializeField]
        internal bool optimizeTexture = true;

        // common parsing configuration
        [NotKeyable]
        [AAOLocalized("TraceAndOptimize:prop:mmdWorldCompatibility",
            "TraceAndOptimize:tooltip:mmdWorldCompatibility")]
        [ToggleLeft]
        [SerializeField]
        internal bool mmdWorldCompatibility = true;

        [NotKeyable]
        [SerializeField]
        internal DebugOptions debugOptions;
        
        [Serializable]
        internal struct DebugOptions
        {
            [Tooltip("Exclude some GameObjects from Trace and Optimize")]
            public GameObject?[]? exclusions;
            [Tooltip("Add GC Debug Components instead of setting GC components if set to non-None")]
            public InternalGcDebugPosition gcDebug;
            [Tooltip("Do Not Sweep (Remove) Components")]
            [ToggleLeft]
            public bool noSweepComponents;
            [Tooltip("Do Not Configure MergeBone in New GC algorithm")]
            [ToggleLeft]
            public bool noConfigureLeafMergeBone;
            [ToggleLeft]
            public bool noConfigureMiddleMergeBone;
            [ToggleLeft]
            public bool noActivenessAnimation;
            [ToggleLeft]
            public bool skipFreezingNonAnimatedBlendShape;
            [ToggleLeft]
            public bool skipFreezingMeaninglessBlendShape;
            [ToggleLeft]
            public bool skipIsAnimatedOptimization;
            [ToggleLeft]
            public bool skipMergePhysBoneCollider;
            [ToggleLeft]
            public bool skipEntryExitToBlendTree;
            [ToggleLeft]
            public bool skipRemoveUnusedAnimatingProperties;
            [ToggleLeft]
            public bool skipMergeBlendTreeLayer;
            [ToggleLeft]
            public bool skipRemoveMeaninglessAnimatorLayer;
            [ToggleLeft]
            public bool skipMergeStaticSkinnedMesh;
            [ToggleLeft]
            public bool skipMergeAnimatingSkinnedMesh;
            [ToggleLeft]
            public bool skipMergeMaterialAnimatingSkinnedMesh;
            [ToggleLeft]
            public bool skipMergeMaterials;
            [ToggleLeft]
            public bool skipRemoveEmptySubMesh;
            [ToggleLeft]
            public bool skipAnyStateToEntryExit;
            [ToggleLeft]
            public bool skipRemoveMaterialUnusedProperties;
            [ToggleLeft]
            public bool skipRemoveMaterialUnusedTextures;
            [ToggleLeft]
            public bool skipAutoMergeBlendShape;
            [ToggleLeft]
            public bool skipRemoveUnusedSubMesh;
            [ToggleLeft]
            public bool skipMergePhysBones;
            [ToggleLeft]
            public bool skipCompleteGraphToEntryExit;
            [ToggleLeft]
            public bool skipReplaceEndBoneWithEndpointPosition;
            [ToggleLeft]
            public bool skipOptimizationWarnings;
        }
    }
    
    internal enum InternalGcDebugPosition
    {
        None,
        AtTheBeginning = 10,
        AfterPhysBone = 20,
        AfterMeshProcessing = 30,
        AfterAutoMergeSkinnedMesh = 40,
        AfterGcComponents = 50,
        AtTheEnd = 1000000,
    }
}
