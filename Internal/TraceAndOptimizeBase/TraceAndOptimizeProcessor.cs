using System;
using System.Collections.Generic;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    public class TraceAndOptimizeState
    {
        public bool Enabled;
        public bool RemoveZeroSizedPolygon;
        public bool OptimizeAnimator;
        public bool MergeSkinnedMesh;
        public bool OptimizeTexture;

        // optimization options
        public bool AllowShuffleMaterialSlots;
        public bool MmdWorldCompatibility = true;
        public bool PreserveEndBone;

        // Debug Options
        public HashSet<GameObject?> Exclusions = new();
        public int GCDebug;

        // all feature flags
        public bool SweepComponents;
        public bool ConfigureLeafMergeBone;
        public bool ConfigureMiddleMergeBone;
        public bool ActivenessAnimation;
        public bool FreezingNonAnimatedBlendShape;
        public bool FreezingMeaninglessBlendShape;
        public bool IsAnimatedOptimization;
        public bool MergePhysBoneCollider;
        public bool EntryExitToBlendTree;
        public bool RemoveUnusedAnimatingProperties;
        public bool MergeBlendTreeLayer;
        public bool RemoveMeaninglessAnimatorLayer;
        public bool MergeStaticSkinnedMesh;
        public bool MergeAnimatingSkinnedMesh;
        public bool MergeMaterialAnimatingSkinnedMesh;
        public bool MergeMaterials;
        public bool RemoveEmptySubMesh;
        public bool AnyStateToEntryExit;
        public bool RemoveMaterialUnusedProperties;
        public bool RemoveMaterialUnusedTextures;
        public bool AutoMergeBlendShape;
        public bool RemoveUnusedSubMesh;
        public bool MergePhysBones;
        public bool CompleteGraphToEntryExit;
        public bool ReplaceEndBoneWithEndpointPosition;
        public bool OptimizationWarnings;

        public Dictionary<SkinnedMeshRenderer, HashSet<string>> PreserveBlendShapes =
            new Dictionary<SkinnedMeshRenderer, HashSet<string>>();

        internal void Initialize(TraceAndOptimize config)
        {
            // optimization settings
            AllowShuffleMaterialSlots = config.allowShuffleMaterialSlots;
            MmdWorldCompatibility = config.mmdWorldCompatibility;
            PreserveEndBone = config.preserveEndBone;

            Exclusions = new HashSet<GameObject?>(config.debugOptions.exclusions ?? Array.Empty<GameObject?>());
            GCDebug = (int)config.debugOptions.gcDebug;

            if (config.optimizeBlendShape)
            {
                FreezingNonAnimatedBlendShape = !config.debugOptions.skipFreezingNonAnimatedBlendShape;
                FreezingMeaninglessBlendShape = !config.debugOptions.skipFreezingMeaninglessBlendShape;
                AutoMergeBlendShape = !config.debugOptions.skipAutoMergeBlendShape;
            }

            if (config.removeUnusedObjects)
            {
                SweepComponents = !config.debugOptions.noSweepComponents;
                ConfigureLeafMergeBone = !config.debugOptions.noConfigureLeafMergeBone;
                ConfigureMiddleMergeBone = !config.debugOptions.noConfigureMiddleMergeBone;
                ActivenessAnimation = !config.debugOptions.noActivenessAnimation;
                RemoveEmptySubMesh = !config.debugOptions.skipRemoveEmptySubMesh;
                RemoveMaterialUnusedProperties = !config.debugOptions.skipRemoveMaterialUnusedProperties;
                RemoveMaterialUnusedTextures = !config.debugOptions.skipRemoveMaterialUnusedTextures;
                MergeMaterials = !config.debugOptions.skipMergeMaterials;
            }

            if (config.optimizePhysBone)
            {
                IsAnimatedOptimization = !config.debugOptions.skipIsAnimatedOptimization;
                MergePhysBoneCollider = !config.debugOptions.skipMergePhysBoneCollider;
                ReplaceEndBoneWithEndpointPosition = !config.debugOptions.skipReplaceEndBoneWithEndpointPosition;
                MergePhysBones = !config.debugOptions.skipMergePhysBones;
            }

            if (config.removeZeroSizedPolygons)
            {
                RemoveZeroSizedPolygon = true;
            }

            if (config.optimizeAnimator)
            {
                OptimizeAnimator = true;
                EntryExitToBlendTree = !config.debugOptions.skipEntryExitToBlendTree;
                RemoveUnusedAnimatingProperties = !config.debugOptions.skipRemoveUnusedAnimatingProperties;
                MergeBlendTreeLayer = !config.debugOptions.skipMergeBlendTreeLayer;
                RemoveMeaninglessAnimatorLayer = !config.debugOptions.skipRemoveMeaninglessAnimatorLayer;
                AnyStateToEntryExit = !config.debugOptions.skipAnyStateToEntryExit;
                CompleteGraphToEntryExit = !config.debugOptions.skipCompleteGraphToEntryExit;
            }

            if (config.mergeSkinnedMesh)
            {
                MergeSkinnedMesh = true;
                MergeStaticSkinnedMesh = !config.debugOptions.skipMergeStaticSkinnedMesh;
                MergeAnimatingSkinnedMesh = !config.debugOptions.skipMergeAnimatingSkinnedMesh;
                MergeMaterialAnimatingSkinnedMesh = !config.debugOptions.skipMergeMaterialAnimatingSkinnedMesh;
            }

            if (config.optimizeTexture)
            {
                OptimizeTexture = true;
            }

            // always applied
            RemoveUnusedSubMesh = !config.debugOptions.skipRemoveUnusedSubMesh;
            OptimizationWarnings = !config.debugOptions.skipOptimizationWarnings;

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
