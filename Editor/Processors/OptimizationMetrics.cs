using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.ndmf;
using nadena.dev.ndmf;
using UnityEngine;

#if AAO_VRCSDK3_AVATARS
using VRC.SDKBase.Validation.Performance;
using VRC.SDKBase.Validation.Performance.Stats;
#endif

namespace Anatawa12.AvatarOptimizer.Processors.OptimizationMetrics;

internal class CaptureOptimizationMetricsBefore : Pass<CaptureOptimizationMetricsBefore>
{
    public override string DisplayName => "Optimization Metrics: Capture Before";

    protected override void Execute(BuildContext context)
    {
        if (!context.GetState<AAOEnabled>().Enabled || !OptimizationMetricsSettings.EnableOptimizationMetrics) return;
        context.GetState<OptimizationMetricsState>().Before = OptimizationMetricsImpl.Capture(context.AvatarRootObject);
    }
}

internal class LogOptimizationMetricsAfter : Pass<LogOptimizationMetricsAfter>
{
    public override string DisplayName => "Optimization Metrics: Log Result";

    protected override void Execute(BuildContext context)
    {
        if (!context.GetState<AAOEnabled>().Enabled || !OptimizationMetricsSettings.EnableOptimizationMetrics) return;

        var before = context.GetState<OptimizationMetricsState>().Before;
        if (before == null) return;

        var after = OptimizationMetricsImpl.Capture(context.AvatarRootObject);
        var lines = BuildDiffLines(before, after);
        if (lines.Count == 0) return;

        BuildLog.LogInfo("OptimizationMetrics:result", string.Join("\n", lines));
    }

    private static List<string> BuildDiffLines(OptimizationMetricsSnapshot before, OptimizationMetricsSnapshot after)
    {
        var lines = new List<string>();
        foreach (var (key, value) in before.ResultsByKey)
        {
            // This should not happen.
            if (!after.ResultsByKey.TryGetValue(key, out var aStr)) continue;
            // Skip if both values are identical, as we only want to show differences.
            if (string.Equals(value, aStr, StringComparison.Ordinal)) continue;

            var categoryName = AAOL10N.TryTr($"OptimizationMetrics:{key}") ?? key;
            lines.Add($"{categoryName}: {value} → {aStr}");
        }
        return lines;
    }
}

internal class OptimizationMetricsState
{
    public OptimizationMetricsSnapshot? Before { get; set; }
}

internal sealed class OptimizationMetricsSnapshot
{
    public Dictionary<string, string> ResultsByKey { get; } = new();
}

internal static class OptimizationMetricsImpl
{
    public static OptimizationMetricsSnapshot Capture(GameObject avatarRoot)
    {
        return MetricsSourceRegistry.Capture(avatarRoot);
    }

    interface IMetricsSource
    {
        int Priority { get; }
        IReadOnlyDictionary<string, string> Capture(GameObject avatarRoot);
    }

    static class MetricsSourceRegistry
    {
        private static IReadOnlyList<IMetricsSource>? _sources;
        public static IReadOnlyList<IMetricsSource> Sources => _sources ??= Build();

        private static IReadOnlyList<IMetricsSource> Build()
        {
            var list = new List<IMetricsSource>
            {
#if AAO_VRCSDK3_AVATARS
                new VrcMetricsSource(),
#endif
                new CustomMetricsSource()
            };
            return list.OrderBy(s => s.Priority).ToList();
        }

        public static OptimizationMetricsSnapshot Capture(GameObject avatarRoot)
        {
            var snapshot = new OptimizationMetricsSnapshot();
            foreach (var source in Sources)
            {
                var data = source.Capture(avatarRoot);
                foreach (var (key, value) in data)
                {
                    snapshot.ResultsByKey.TryAdd(key, value);
                }
            }
            return snapshot;
        }
    }

    class CustomMetricsSource : IMetricsSource
    {
        public int Priority => 0;

        static class CustomMetricKeys
        {
            public const string BlendShapeCount = "BlendShapeCount";
            public const string AnimatorLayerCount = "AnimatorLayerCount";
            public const string GameObjectCount = "GameObjectCount";
        }

        public IReadOnlyDictionary<string, string> Capture(GameObject avatarRoot)
        {
            return new Dictionary<string, string>
            {
                [CustomMetricKeys.GameObjectCount] = avatarRoot.GetComponentsInChildren<Transform>(true).Length.ToString(),
                [CustomMetricKeys.BlendShapeCount] = avatarRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Where(smr => smr.sharedMesh != null)
                    .Sum(smr => smr.sharedMesh.blendShapeCount).ToString(),
                [CustomMetricKeys.AnimatorLayerCount] = CountAnimatorLayers(avatarRoot).ToString(),
            };
        }

        private int CountAnimatorLayers(GameObject avatarRoot)
        {
            List<RuntimeAnimatorController> controllers = new();

            var animator = avatarRoot.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
                controllers.Add(animator.runtimeAnimatorController);

﻿#if AAO_VRCSDK3_AVATARS
            var descriptor = avatarRoot.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
            if (descriptor != null)
            {
                var layerControllers = VRCSDKUtils.GetAvatarLayerControllers(descriptor);
                foreach (var layer in AnimatorLayerMap.ValidLayerTypes)
                {
                    var c = layerControllers[layer];
                    if (c != null)
                        controllers.Add(c);
                }
            }
#endif

            return controllers.Sum(c => ACUtils.ComputeLayerCount(c));
        }
    }

#if AAO_VRCSDK3_AVATARS
    class VrcMetricsSource : IMetricsSource
    {
        public int Priority => 1000;

        private static Dictionary<AvatarPerformanceCategory, Func<AvatarPerformanceStats, string?>>? _computers;
        private static Dictionary<AvatarPerformanceCategory, Func<AvatarPerformanceStats, string?>> Computers => _computers ??= BuildComputers();

        public IReadOnlyDictionary<string, string> Capture(GameObject avatarRoot)
        {
            try
            {
                var stats = new AvatarPerformanceStats(false);
                AvatarPerformance.CalculatePerformanceStats(avatarRoot.name, avatarRoot, stats, false);
                var result = new Dictionary<string, string>();
                foreach (AvatarPerformanceCategory category in Enum.GetValues(typeof(AvatarPerformanceCategory)))
                {
                    if (!Computers.TryGetValue(category, out var computer)) continue;
                    try
                    {
                        var data = computer(stats);
                        if (data != null) result[category.ToString()] = data;
                    }
                    catch { }
                }
                return result;
            }
            catch { return new Dictionary<string, string>(); }
        }

        private static Dictionary<AvatarPerformanceCategory, Func<AvatarPerformanceStats, string?>> BuildComputers()
        {
            var dict = new Dictionary<AvatarPerformanceCategory, Func<AvatarPerformanceStats, string?>>();

            string? ToStringBytes(int? bytes) => bytes is { } i ? $"{i / 1024.0 / 1024.0:F2}MB" : null;
            string? ToStringMegabytes(float? mb) => mb is { } f ? $"{f:F2}MB" : null;
            string? ToStringCount(int? v) => v is { } ? v.ToString() : null;
            string? ToStringCountZero(int? v) => v is { } ? v.ToString() : "0";
            string? ToStringEnabled(bool? v) => v is { } b ? (b ? "Enabled" : "Disabled") : null;

            void Add(AvatarPerformanceCategory category, params Func<AvatarPerformanceStats, string?>[] computers)
            {
                dict[category] = s =>
                {
                    foreach (var c in computers)
                    {
                        try
                        {
                            var r = c(s);
                            if (r != null) return r;
                        }
                        catch
                        {
                            // ignored
                        }
                    }

                    return null;
                };
            }

            Add(AvatarPerformanceCategory.DownloadSize, s => ToStringBytes(s.downloadSizeBytes));
            Add(AvatarPerformanceCategory.UncompressedSize, s => ToStringBytes(s.uncompressedSizeBytes));
            Add(AvatarPerformanceCategory.PolyCount, s => ToStringCount(s.polyCount));
            Add(AvatarPerformanceCategory.AABB, s => s.aabb is { } b ? b.ToString() : null);
            Add(AvatarPerformanceCategory.SkinnedMeshCount, s => ToStringCount(s.skinnedMeshCount));
            Add(AvatarPerformanceCategory.MeshCount, s => ToStringCount(s.meshCount));
            Add(AvatarPerformanceCategory.MaterialCount, s => ToStringCount(s.materialCount));
            Add(AvatarPerformanceCategory.PhysBoneComponentCount, s => ToStringCountZero(s.physBone?.componentCount));
            Add(AvatarPerformanceCategory.PhysBoneTransformCount, s => ToStringCountZero(s.physBone?.transformCount));
            Add(AvatarPerformanceCategory.PhysBoneColliderCount, s => ToStringCountZero(s.physBone?.colliderCount));
            Add(AvatarPerformanceCategory.PhysBoneCollisionCheckCount, s => ToStringCountZero(s.physBone?.collisionCheckCount));
            Add(AvatarPerformanceCategory.ContactCount, s => ToStringCount(s.contactCount));
            Add(AvatarPerformanceCategory.AnimatorCount, s => ToStringCount(s.animatorCount));
            Add(AvatarPerformanceCategory.BoneCount, s => ToStringCount(s.boneCount));
            Add(AvatarPerformanceCategory.LightCount, s => ToStringCount(s.lightCount));
            Add(AvatarPerformanceCategory.ParticleSystemCount, s => ToStringCount(s.particleSystemCount));
            Add(AvatarPerformanceCategory.ParticleTotalCount, s => ToStringCount(s.particleTotalCount));
            Add(AvatarPerformanceCategory.ParticleMaxMeshPolyCount, s => ToStringCount(s.particleMaxMeshPolyCount));
            Add(AvatarPerformanceCategory.ParticleTrailsEnabled, s => ToStringEnabled(s.particleTrailsEnabled));
            Add(AvatarPerformanceCategory.ParticleCollisionEnabled, s => ToStringEnabled(s.particleCollisionEnabled));
            Add(AvatarPerformanceCategory.TrailRendererCount, s => ToStringCount(s.trailRendererCount));
            Add(AvatarPerformanceCategory.LineRendererCount, s => ToStringCount(s.lineRendererCount));
            Add(AvatarPerformanceCategory.ClothCount, s => ToStringCount(s.clothCount));
            Add(AvatarPerformanceCategory.ClothMaxVertices, s => ToStringCount(s.clothMaxVertices));
            Add(AvatarPerformanceCategory.PhysicsColliderCount, s => ToStringCount(s.physicsColliderCount));
            Add(AvatarPerformanceCategory.PhysicsRigidbodyCount, s => ToStringCount(s.physicsRigidbodyCount));
            Add(AvatarPerformanceCategory.AudioSourceCount, s => ToStringCount(s.audioSourceCount));
            Add(AvatarPerformanceCategory.TextureMegabytes, s => ToStringMegabytes(s.textureMegabytes));
            Add(AvatarPerformanceCategory.ConstraintsCount, s => ToStringCount(s.constraintsCount));
            Add(AvatarPerformanceCategory.ConstraintDepth, s => ToStringCount(s.constraintDepth));
            return dict;
        }
    }
#endif
}
