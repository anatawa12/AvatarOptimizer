using System.Collections.Generic;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    public class TraceAndOptimizeState
    {
        public bool Enabled;
        public bool FreezeBlendShape;
        public bool RemoveUnusedObjects;
        public bool RemoveZeroSizedPolygon;
        public bool OptimizePhysBone;
        public bool OptimizeAnimator;
        public bool MergeSkinnedMesh;
        public bool AllowShuffleMaterialSlots;
        public bool MmdWorldCompatibility = true;

        public bool PreserveEndBone;
        public HashSet<GameObject> Exclusions = new HashSet<GameObject>();
        public bool GCDebug;
        public bool NoConfigureMergeBone;
        public bool NoActivenessAnimation;
        public bool SkipFreezingNonAnimatedBlendShape;
        public bool SkipFreezingMeaninglessBlendShape;
        public bool SkipIsAnimatedOptimization;
        public bool SkipMergePhysBoneCollider;
        public bool SkipEntryExitToBlendTree;
        public bool SkipRemoveUnusedAnimatingProperties;
        public bool SkipMergeBlendTreeLayer;
        public bool SkipRemoveMeaninglessAnimatorLayer;
        public bool SkipMergeStaticSkinnedMesh;
        public bool SkipMergeAnimatingSkinnedMesh;
        public bool SkipMergeMaterialAnimatingSkinnedMesh;
        public bool SkipMergeMaterials;
        public bool SkipRemoveEmptySubMesh;

        public Dictionary<SkinnedMeshRenderer, HashSet<string>> PreserveBlendShapes =
            new Dictionary<SkinnedMeshRenderer, HashSet<string>>();

        internal void Initialize(TraceAndOptimize config)
        {
            FreezeBlendShape = config.freezeBlendShape;
            RemoveUnusedObjects = config.removeUnusedObjects;
            RemoveZeroSizedPolygon = config.removeZeroSizedPolygons;
            OptimizePhysBone = config.optimizePhysBone;
            OptimizeAnimator = config.optimizeAnimator;
            MergeSkinnedMesh = config.mergeSkinnedMesh;
            AllowShuffleMaterialSlots = config.allowShuffleMaterialSlots;
            MmdWorldCompatibility = config.mmdWorldCompatibility;

            PreserveEndBone = config.preserveEndBone;

            Exclusions = new HashSet<GameObject>(config.advancedSettings.exclusions);
            GCDebug = config.advancedSettings.gcDebug;
            NoConfigureMergeBone = config.advancedSettings.noConfigureMergeBone;
            NoActivenessAnimation = config.advancedSettings.noActivenessAnimation;
            SkipFreezingNonAnimatedBlendShape = config.advancedSettings.skipFreezingNonAnimatedBlendShape;
            SkipFreezingMeaninglessBlendShape = config.advancedSettings.skipFreezingMeaninglessBlendShape;
            SkipIsAnimatedOptimization = config.advancedSettings.skipIsAnimatedOptimization;
            SkipMergePhysBoneCollider = config.advancedSettings.skipMergePhysBoneCollider;
            SkipEntryExitToBlendTree = config.advancedSettings.skipEntryExitToBlendTree;
            SkipRemoveUnusedAnimatingProperties = config.advancedSettings.skipRemoveUnusedAnimatingProperties;
            SkipMergeBlendTreeLayer = config.advancedSettings.skipMergeBlendTreeLayer;
            SkipRemoveMeaninglessAnimatorLayer = config.advancedSettings.skipRemoveMeaninglessAnimatorLayer;
            SkipMergeStaticSkinnedMesh = config.advancedSettings.skipMergeStaticSkinnedMesh;
            SkipMergeAnimatingSkinnedMesh = config.advancedSettings.skipMergeAnimatingSkinnedMesh;
            SkipMergeMaterialAnimatingSkinnedMesh = config.advancedSettings.skipMergeMaterialAnimatingSkinnedMesh;
            SkipMergeMaterials = config.advancedSettings.skipMergeMaterials;
            SkipRemoveEmptySubMesh = config.advancedSettings.skipRemoveEmptySubMesh;

            Enabled = true;
        }
    }

    public class LoadTraceAndOptimizeConfiguration : Pass<LoadTraceAndOptimizeConfiguration>
    {
        public override string DisplayName => "T&O: Load Configuration";

        protected override void Execute(BuildContext context)
        {
            var config = context.AvatarRootObject.GetComponent<TraceAndOptimize>();
            if (config)
                context.GetState<TraceAndOptimizeState>().Initialize(config);
            DestroyTracker.DestroyImmediate(config);
        }
    }

    public abstract class TraceAndOptimizePass<T> : Pass<T> where T : TraceAndOptimizePass<T>, new()
    {
        protected sealed override void Execute(BuildContext context)
        {
            var state = context.GetState<TraceAndOptimizeState>();
            if (!state.Enabled) return;
            Execute(context, state);
        }

        protected abstract void Execute(BuildContext context, TraceAndOptimizeState state);
    }
}
