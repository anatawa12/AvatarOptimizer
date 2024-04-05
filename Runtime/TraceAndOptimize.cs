using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Serialization;

namespace Anatawa12.AvatarOptimizer
{
    // previously known as Automatic Configuration
    [AddComponentMenu("Avatar Optimizer/AAO Trace And Optimize")]
    [DisallowMultipleComponent]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/trace-and-optimize/")]
    internal class TraceAndOptimize : AvatarGlobalComponent
    {
        [NotKeyable]
        [AAOLocalized("TraceAndOptimize:prop:freezeBlendShape")]
        [ToggleLeft]
        public bool freezeBlendShape = true;
        [NotKeyable]
        [AAOLocalized("TraceAndOptimize:prop:removeUnusedObjects")]
        [ToggleLeft]
        public bool removeUnusedObjects = true;

        // Remove Unused Objects Options
        [NotKeyable]
        [AAOLocalized("TraceAndOptimize:prop:preserveEndBone",
            "TraceAndOptimize:tooltip:preserveEndBone")]
        [ToggleLeft]
        public bool preserveEndBone;

        [NotKeyable]
        [AAOLocalized("TraceAndOptimize:prop:removeZeroSizedPolygons")]
        [ToggleLeft]
        public bool removeZeroSizedPolygons = false;

        [NotKeyable]
        [AAOLocalized("TraceAndOptimize:prop:optimizePhysBone")]
        [ToggleLeft]
#if !AAO_VRCSDK3_AVATARS
        // no meaning without VRCSDK
        [HideInInspector]
#endif
        public bool optimizePhysBone = true;

        [NotKeyable]
        [AAOLocalized("TraceAndOptimize:prop:optimizeAnimator")]
        [ToggleLeft]
        public bool optimizeAnimator = true;

        [NotKeyable]
        [AAOLocalized("TraceAndOptimize:prop:mergeSkinnedMesh")]
        [ToggleLeft]
        public bool mergeSkinnedMesh = true;

        // TODO: make true by default if it's safe
        // see https://github.com/anatawa12/AvatarOptimizer/issues/954
        [NotKeyable]
        [AAOLocalized("TraceAndOptimize:prop:allowShuffleMaterialSlots",
            "TraceAndOptimize:tooltip:allowShuffleMaterialSlots")]
        [ToggleLeft]
        public bool allowShuffleMaterialSlots;

        // common parsing configuration
        [NotKeyable]
        [AAOLocalized("TraceAndOptimize:prop:mmdWorldCompatibility",
            "TraceAndOptimize:tooltip:mmdWorldCompatibility")]
        [ToggleLeft]
        public bool mmdWorldCompatibility = true;

        [NotKeyable]
        public AdvancedSettings advancedSettings;
        
        [Serializable]
        public struct AdvancedSettings
        {
            [Tooltip("Exclude some GameObjects from Trace and Optimize")]
            public GameObject[] exclusions;
            [Tooltip("Add GC Debug Components instead of setting GC components")]
            [ToggleLeft]
            public bool gcDebug;
            [Tooltip("Do Not Configure MergeBone in New GC algorithm")]
            [ToggleLeft]
            public bool noConfigureMergeBone;
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
        }
    }
}
