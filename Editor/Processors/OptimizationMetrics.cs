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

internal readonly record struct MetricCategory(string Key, string DisplayName);

internal enum CustomMetricKeys
{
    BlendShapeCount,
    GameObjectCount,
}

internal static class MetricCategoryRegistry
{
    private static IReadOnlyList<MetricCategory>? _all;
    public static IReadOnlyList<MetricCategory> All => _all ??= Build();

    private static IReadOnlyList<MetricCategory> Build()
    {
        var list = new List<MetricCategory>();
#if AAO_VRCSDK3_AVATARS
        foreach (AvatarPerformanceCategory category in Enum.GetValues(typeof(AvatarPerformanceCategory)))
        {
            var displayName = VrcMetricsSource.TryGetCategoryDisplayName(category);
            if (displayName != null)
                list.Add(new MetricCategory(category.ToString(), displayName));
        }
#endif
        list.Add(new MetricCategory(CustomMetricKeys.BlendShapeCount.ToString(), "Blend Shapes"));
        list.Add(new MetricCategory(CustomMetricKeys.GameObjectCount.ToString(), "Game Objects"));
        return list;
    }
}

internal sealed class OptimizationMetricsSnapshot
{
    public Dictionary<string, string> Data { get; } = new();
}

internal interface IMetricsSource
{
    int Priority { get; }
    IReadOnlyDictionary<string, string> Capture(GameObject avatarRoot);
}

internal static class MetricsSourceRegistry
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
                snapshot.Data.TryAdd(key, value);
            }
        }
        return snapshot;
    }
}

internal sealed class CustomMetricsSource : IMetricsSource
{
    public int Priority => 0;

    public IReadOnlyDictionary<string, string> Capture(GameObject avatarRoot)
    {
        return new Dictionary<string, string>
        {
            [CustomMetricKeys.GameObjectCount.ToString()] = avatarRoot.GetComponentsInChildren<Transform>(true).Length.ToString(),
            [CustomMetricKeys.BlendShapeCount.ToString()] = avatarRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(smr => smr.sharedMesh != null)
                .Sum(smr => smr.sharedMesh.blendShapeCount).ToString(),
        };
    }
}

#if AAO_VRCSDK3_AVATARS
internal sealed class VrcMetricsSource : IMetricsSource
{
    public int Priority => 1000;

    private static readonly Dictionary<AvatarPerformanceCategory, Func<dynamic, string?>> Computers = BuildComputers();

    public static string? TryGetCategoryDisplayName(AvatarPerformanceCategory category)
    {
        try { return AvatarPerformanceStats.GetPerformanceCategoryDisplayName(category); }
        catch { return null; }
    }

    public IReadOnlyDictionary<string, string> Capture(GameObject avatarRoot)
    {
        try
        {
            var stats = new AvatarPerformanceStats(false);
            AvatarPerformance.CalculatePerformanceStats(avatarRoot.name, avatarRoot, stats, false);
            var result = new Dictionary<string, string>();
            foreach (AvatarPerformanceCategory category in Enum.GetValues(typeof(AvatarPerformanceCategory)))
            {
                if (TryGetCategoryDisplayName(category) == null) continue;
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

    private static Dictionary<AvatarPerformanceCategory, Func<dynamic, string?>> BuildComputers()
    {
        var dict = new Dictionary<AvatarPerformanceCategory, Func<dynamic, string?>>();
        string? ToStringBytes(int? bytes) => bytes is int i ? $"{i / 1024.0 / 1024.0:F2}MB" : null;
        string? ToStringMegabytes(float? mb) => mb is float f ? $"{f:F2}MB" : null;
        string? ToStringCount(int? v) => v?.ToString();
        string? ToStringCountZero(int? v) => v?.ToString() ?? "0";
        string? ToStringEnabled(bool? v) => v is bool b ? (b ? "Enabled" : "Disabled") : null;

        void Add(AvatarPerformanceCategory category, params Func<dynamic, string?>[] computers)
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

        void AddIfExists(string name, params Func<dynamic, string?>[] computers)
        {
            if (!Enum.TryParse(name, out AvatarPerformanceCategory category)) return;
            Add(category, computers);
        }

        Add(AvatarPerformanceCategory.DownloadSize, s => ToStringBytes(s.downloadSizeBytes), s => ToStringMegabytes(s.downloadSize));
        Add(AvatarPerformanceCategory.UncompressedSize, s => ToStringBytes(s.uncompressedSizeBytes), s => ToStringMegabytes(s.uncompressedSize));
        Add(AvatarPerformanceCategory.PolyCount, s => ToStringCount(s.polyCount));
        Add(AvatarPerformanceCategory.AABB, s => s.aabb?.ToString());
        Add(AvatarPerformanceCategory.SkinnedMeshCount, s => ToStringCount(s.skinnedMeshCount));
        Add(AvatarPerformanceCategory.MeshCount, s => ToStringCount(s.meshCount));
        Add(AvatarPerformanceCategory.MaterialCount, s => ToStringCount(s.materialCount));
        AddIfExists("DynamicBoneComponentCount", s => ToStringCountZero(s.dynamicBone?.componentCount));
        AddIfExists("DynamicBoneSimulatedBoneCount", s => ToStringCountZero(s.dynamicBone?.transformCount));
        AddIfExists("DynamicBoneColliderCount", s => ToStringCountZero(s.dynamicBone?.colliderCount));
        AddIfExists("DynamicBoneCollisionCheckCount", s => ToStringCountZero(s.dynamicBone?.collisionCheckCount));
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

internal class OptimizationMetricsState
{
    public OptimizationMetricsSnapshot? Before { get; set; }
}

internal class CaptureOptimizationMetricsBefore : Pass<CaptureOptimizationMetricsBefore>
{
    public override string DisplayName => "Optimization Metrics: Capture Before";

    protected override void Execute(BuildContext context)
    {
        if (!context.GetState<AAOEnabled>().Enabled) return;
        context.GetState<OptimizationMetricsState>().Before = MetricsSourceRegistry.Capture(context.AvatarRootObject);
    }
}

internal class LogOptimizationMetricsAfter : Pass<LogOptimizationMetricsAfter>
{
    public override string DisplayName => "Optimization Metrics: Log Result";

    protected override void Execute(BuildContext context)
    {
        if (!context.GetState<AAOEnabled>().Enabled) return;

        var before = context.GetState<OptimizationMetricsState>().Before;
        if (before == null) return;

        var after = MetricsSourceRegistry.Capture(context.AvatarRootObject);
        var lines = BuildDiffLines(before, after);
        if (lines.Count == 0) return;

        BuildLog.LogInfo("OptimizationMetrics:result", string.Join("\n", lines));
    }

    private static List<string> BuildDiffLines(OptimizationMetricsSnapshot before, OptimizationMetricsSnapshot after)
    {
        var lines = new List<string>();
        foreach (var cat in MetricCategoryRegistry.All)
        {
            if (!before.Data.TryGetValue(cat.Key, out var bStr)) continue;
            if (!after.Data.TryGetValue(cat.Key, out var aStr)) continue;
            if (string.Equals(bStr, aStr, StringComparison.Ordinal)) continue;

            lines.Add($"{cat.DisplayName}: {bStr} → {aStr}");
        }
        return lines;
    }
}
