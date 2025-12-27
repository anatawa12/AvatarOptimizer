using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using static Anatawa12.AvatarOptimizer.TraceAndOptimizePlatformSettings;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    public class TraceAndOptimizeState
    {
        // optimization options
        public bool AllowShuffleMaterialSlots;
        public bool MmdWorldCompatibility = true;
        public bool PreserveEndBone;

        // Debug Options
        public HashSet<GameObject?> Exclusions = new();
        public int GCDebug;

        // feature group flags
        public bool OptimizeAnimator;
        public bool MergeSkinnedMesh;

        // all feature flags
        public bool RemoveZeroSizedPolygon;
        public bool OptimizeTexture;
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
        public bool MergeMaterials; // This is feature flag but option of merge skinned mesh
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

        internal void Initialize(TraceAndOptimize config, TraceAndOptimizePlatformSettings settings)
        {
            // optimization settings
            AllowShuffleMaterialSlots = config.allowShuffleMaterialSlots;
            MmdWorldCompatibility = config.mmdWorldCompatibility;
            PreserveEndBone = config.preserveEndBone;

            Exclusions = new HashSet<GameObject?>(config.debugOptions.exclusions ?? Array.Empty<GameObject?>());
            GCDebug = (int)config.debugOptions.gcDebug;

            if (config.optimizeBlendShape && settings.optimizeBlendShape)
            {
                FreezingNonAnimatedBlendShape = !config.debugOptions.skipFreezingNonAnimatedBlendShape && settings.freezingNonAnimatedBlendShape;
                FreezingMeaninglessBlendShape = !config.debugOptions.skipFreezingMeaninglessBlendShape && settings.freezingMeaninglessBlendShape;
                AutoMergeBlendShape = !config.debugOptions.skipAutoMergeBlendShape && settings.autoMergeBlendShape;
            }

            if (config.removeUnusedObjects && settings.removeUnusedObjects)
            {
                SweepComponents = !config.debugOptions.noSweepComponents && settings.sweepComponents;
                ConfigureLeafMergeBone = !config.debugOptions.noConfigureLeafMergeBone && settings.configureLeafMergeBone;
                ConfigureMiddleMergeBone = !config.debugOptions.noConfigureMiddleMergeBone && settings.configureMiddleMergeBone;
                ActivenessAnimation = !config.debugOptions.noActivenessAnimation && settings.activenessAnimation;
                RemoveEmptySubMesh = !config.debugOptions.skipRemoveEmptySubMesh && settings.removeEmptySubMesh;
                RemoveMaterialUnusedProperties = !config.debugOptions.skipRemoveMaterialUnusedProperties && settings.removeMaterialUnusedProperties;
                RemoveMaterialUnusedTextures = !config.debugOptions.skipRemoveMaterialUnusedTextures && settings.removeMaterialUnusedTextures;
                MergeMaterials = !config.debugOptions.skipMergeMaterials && settings.mergeMaterials;
            }

            if (config.optimizePhysBone && settings.optimizePhysBone)
            {
                IsAnimatedOptimization = !config.debugOptions.skipIsAnimatedOptimization && settings.isAnimatedOptimization;
                MergePhysBoneCollider = !config.debugOptions.skipMergePhysBoneCollider && settings.mergePhysBoneCollider;
                ReplaceEndBoneWithEndpointPosition = !config.debugOptions.skipReplaceEndBoneWithEndpointPosition && settings.replaceEndBoneWithEndpointPosition;
                MergePhysBones = !config.debugOptions.skipMergePhysBones && settings.mergePhysBones;
            }

            if (config.removeZeroSizedPolygons && settings.removeZeroSizedPolygons)
            {
                RemoveZeroSizedPolygon = true;
            }

            if (config.optimizeAnimator && settings.optimizeAnimator)
            {
                OptimizeAnimator = true;
                EntryExitToBlendTree = !config.debugOptions.skipEntryExitToBlendTree && settings.entryExitToBlendTree;
                RemoveUnusedAnimatingProperties = !config.debugOptions.skipRemoveUnusedAnimatingProperties && settings.removeUnusedAnimatingProperties;
                MergeBlendTreeLayer = !config.debugOptions.skipMergeBlendTreeLayer && settings.mergeBlendTreeLayer;
                RemoveMeaninglessAnimatorLayer = !config.debugOptions.skipRemoveMeaninglessAnimatorLayer && settings.removeMeaninglessAnimatorLayer;
                AnyStateToEntryExit = !config.debugOptions.skipAnyStateToEntryExit && settings.anyStateToEntryExit;
                CompleteGraphToEntryExit = !config.debugOptions.skipCompleteGraphToEntryExit && settings.completeGraphToEntryExit;
            }

            if (config.mergeSkinnedMesh && settings.mergeSkinnedMesh)
            {
                MergeSkinnedMesh = true;
                MergeStaticSkinnedMesh = !config.debugOptions.skipMergeStaticSkinnedMesh;
                MergeAnimatingSkinnedMesh = !config.debugOptions.skipMergeAnimatingSkinnedMesh;
                MergeMaterialAnimatingSkinnedMesh = !config.debugOptions.skipMergeMaterialAnimatingSkinnedMesh;
            }

            if (config.optimizeTexture && settings.optimizeTexture)
            {
                OptimizeTexture = true;
            }

            // always applied
            RemoveUnusedSubMesh = !config.debugOptions.skipRemoveUnusedSubMesh;
            OptimizationWarnings = !config.debugOptions.skipOptimizationWarnings;
        }
    }

    public class LoadTraceAndOptimizeConfiguration : Pass<LoadTraceAndOptimizeConfiguration>
    {
        public override string DisplayName => "T&O: Load Configuration";

        protected override void Execute(BuildContext context)
        {
            var config = context.AvatarRootObject.GetComponent<TraceAndOptimize>();
            TraceAndOptimizePlatformSettings settings = LoadPlatformSettings(context.PlatformProvider.QualifiedName,
                context.PlatformProvider.DisplayName);

            if (config)
            {
                context.GetState<TraceAndOptimizeState>().Initialize(config, settings);
            }

            DestroyTracker.DestroyImmediate(config);
        }

        private static TraceAndOptimizePlatformSettings? _full;

        private static TraceAndOptimizePlatformSettings LoadPlatformSettings(
            string platformQualifiedName, string displayName)
        {
            if (_full == null) _full = ScriptableObject.CreateInstance<TraceAndOptimizePlatformSettings>();
            if (platformQualifiedName == WellKnownPlatforms.VRChatAvatar30) return _full;
            if (platformQualifiedName == WellKnownPlatforms.Generic)
            {
                BuildLog.LogInfo("NonVRChatPlatformSupport:genericPlatformMessage");
                return _full;
            }

            var settings = AssetDatabase.FindAssets("t:TraceAndOptimizePlatformSettings")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<TraceAndOptimizePlatformSettings>)
                .Where(x => x != null)
                .Where(x => x.platformQualifiedName == platformQualifiedName)
                .ToList();

            settings.Sort((a, b) =>
            {
                var aDiff = Math.Abs(a!.experimentalPlatformSettingsVersion - CurrentExperimentalPlatformSettingsVersion);
                var bDiff = Math.Abs(b!.experimentalPlatformSettingsVersion - CurrentExperimentalPlatformSettingsVersion);
                return aDiff.CompareTo(bDiff);
            });

            BuildLog.LogWarning("NonVRChatPlatformSupport:experimentalMessage");

            using var enumerator = settings.GetEnumerator();

            if (!enumerator.MoveNext())
            {
                BuildLog.LogWarning("NonVRChatPlatformSupport:noPlatformSettings", displayName, platformQualifiedName);
                return _full;
            }

            var result = enumerator.Current!;

            if (enumerator.MoveNext())
            {
                BuildLog.LogWarning("NonVRChatPlatformSupport:manyPlatformSettings", displayName, platformQualifiedName,
                    AssetDatabase.GetAssetPath(result));
            }

            if (result.experimentalPlatformSettingsVersion != CurrentExperimentalPlatformSettingsVersion)
            {
                BuildLog.LogWarning("NonVRChatPlatformSupport:versionMismatchMessage:build",
                    AssetDatabase.GetAssetPath(result),
                    result!.experimentalPlatformSettingsVersion, 
                    CurrentExperimentalPlatformSettingsVersion);
            }

            return result!;
        }
    }

    public abstract class TraceAndOptimizePass<T> : Pass<T> where T : TraceAndOptimizePass<T>, new()
    {
        public abstract override string DisplayName { get; }
        protected abstract bool Enabled(TraceAndOptimizeState state);

        protected sealed override void Execute(BuildContext context)
        {
            var state = context.GetState<TraceAndOptimizeState>();
            if (!Enabled(state)) return;
            Execute(context, state);
        }

        protected abstract void Execute(BuildContext context, TraceAndOptimizeState state);
    }
}
