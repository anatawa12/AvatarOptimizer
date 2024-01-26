using System.Collections.Generic;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    class TraceAndOptimizeState
    {
        public bool Enabled;
        public bool FreezeBlendShape;
        public bool RemoveUnusedObjects;
        public bool RemoveZeroSizedPolygon;
        public bool OptimizePhysBone;
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
        public TraceAndOptimize.AnimatorOptimizer AnimatorOptimizer;

        public Dictionary<SkinnedMeshRenderer, HashSet<string>> PreserveBlendShapes =
            new Dictionary<SkinnedMeshRenderer, HashSet<string>>();

        public void Initialize(TraceAndOptimize config)
        {
            FreezeBlendShape = config.freezeBlendShape;
            RemoveUnusedObjects = config.removeUnusedObjects;
            RemoveZeroSizedPolygon = config.removeZeroSizedPolygons;
            OptimizePhysBone = config.optimizePhysBone;
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

            AnimatorOptimizer = config.animatorOptimizer;

            Enabled = true;
        }
    }

    internal class LoadTraceAndOptimizeConfiguration : Pass<LoadTraceAndOptimizeConfiguration>
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

    internal abstract class TraceAndOptimizePass<T> : Pass<T> where T : TraceAndOptimizePass<T>, new()
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
