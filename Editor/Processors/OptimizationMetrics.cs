using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

            // Todo: Localize CategoryName
            lines.Add($"{key}: {value} → {aStr}");
        }
        return lines;
    }
}

internal class OptimizationMetricsState
{
    public OptimizationMetricsSnapshot? Before { get; set; }
}

internal readonly record struct MetricCategory(string Key, string DisplayName);

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

    enum CustomMetricKeys
    {
        BlendShapeCount,
        GameObjectCount,
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
    class VrcMetricsSource : IMetricsSource
    {
        public int Priority => 1000;

        private static Dictionary<AvatarPerformanceCategory, Func<AvatarPerformanceStats, string?>>? _computers;
        private static Dictionary<AvatarPerformanceCategory, Func<AvatarPerformanceStats, string?>> Computers => _computers ??= BuildComputers();

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

        private static Dictionary<AvatarPerformanceCategory, Func<AvatarPerformanceStats, string?>> BuildComputers()
        {
            var dict = new Dictionary<AvatarPerformanceCategory, Func<AvatarPerformanceStats, string?>>();

            string? ToStringBytes(int? bytes) => bytes is { } i ? $"{i / 1024.0 / 1024.0:F2}MB" : null;
            string? ToStringMegabytes(float? mb) => mb is { } f ? $"{f:F2}MB" : null;
            string? ToStringCount(int? v) => v is { } ? v.ToString() : null;
            string? ToStringEnabled(bool? v) => v is { } b ? (b ? "Enabled" : "Disabled") : null;

            T? GetVal<T>(object obj, string name)
            {
                var type = obj.GetType();
                
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public);
                if (field != null)
                {
                    var val = field.GetValue(obj);
                    if (val is T tVal) return tVal;
                }

                var prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
                if (prop != null && prop.CanRead)
                {
                    var val = prop.GetValue(obj);
                    if (val is T tVal) return tVal;
                }

                return default(T);
            }

            void Add(string categoryName, params Func<AvatarPerformanceStats, string?>[] computers)
            {
                if (!Enum.TryParse(categoryName, out AvatarPerformanceCategory category)) return;
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

            Add("DownloadSize", s => ToStringBytes(GetVal<int>(s, "downloadSizeBytes")  ));
            Add("UncompressedSize", s => ToStringBytes(GetVal<int>(s, "uncompressedSizeBytes")));
            Add("PolyCount", s => ToStringCount(GetVal<int>(s, "polyCount")));
            Add("AABB", s => GetVal<Bounds>(s, "aabb") is { } b ? b.ToString() : null);
            Add("SkinnedMeshCount", s => ToStringCount(GetVal<int>(s, "skinnedMeshCount")));
            Add("MeshCount", s => ToStringCount(GetVal<int>(s, "meshCount")));
            Add("MaterialCount", s => ToStringCount(GetVal<int>(s, "materialCount")));

            string? GetDynamicBoneStat(AvatarPerformanceStats s, string fieldName)
            {
                var db = GetVal<object>(s, "dynamicBone");
                if (db == null) return "0";
                return ToStringCount(GetVal<int>(db, fieldName));
            }
            
            Add("DynamicBoneComponentCount", s => GetDynamicBoneStat(s, "componentCount"));
            Add("DynamicBoneSimulatedBoneCount", s => GetDynamicBoneStat(s, "transformCount"));
            Add("DynamicBoneColliderCount", s => GetDynamicBoneStat(s, "colliderCount"));
            Add("DynamicBoneCollisionCheckCount", s => GetDynamicBoneStat(s, "collisionCheckCount"));

            string? GetPhysBoneStat(AvatarPerformanceStats s, string fieldName)
            {
                var pb = GetVal<object>(s, "physBone"); // PhysBoneStats
                if (pb == null) return "0";
                return ToStringCount(GetVal<int>(pb, fieldName));
            }

            Add("PhysBoneComponentCount", s => GetPhysBoneStat(s, "componentCount"));
            Add("PhysBoneTransformCount", s => GetPhysBoneStat(s, "transformCount"));
            Add("PhysBoneColliderCount", s => GetPhysBoneStat(s, "colliderCount"));
            Add("PhysBoneCollisionCheckCount", s => GetPhysBoneStat(s, "collisionCheckCount"));

            Add("ContactCount", s => ToStringCount(GetVal<int>(s, "contactCount")));
            Add("AnimatorCount", s => ToStringCount(GetVal<int>(s, "animatorCount")));
            Add("BoneCount", s => ToStringCount(GetVal<int>(s, "boneCount")));
            Add("LightCount", s => ToStringCount(GetVal<int>(s, "lightCount")));
            Add("ParticleSystemCount", s => ToStringCount(GetVal<int>(s, "particleSystemCount")));
            Add("ParticleTotalCount", s => ToStringCount(GetVal<int>(s, "particleTotalCount")));
            Add("ParticleMaxMeshPolyCount", s => ToStringCount(GetVal<int>(s, "particleMaxMeshPolyCount")));
            Add("ParticleTrailsEnabled", s => ToStringEnabled(GetVal<bool>(s, "particleTrailsEnabled")));
            Add("ParticleCollisionEnabled", s => ToStringEnabled(GetVal<bool>(s, "particleCollisionEnabled")));
            Add("TrailRendererCount", s => ToStringCount(GetVal<int>(s, "trailRendererCount")));
            Add("LineRendererCount", s => ToStringCount(GetVal<int>(s, "lineRendererCount")));
            Add("ClothCount", s => ToStringCount(GetVal<int>(s, "clothCount")));
            Add("ClothMaxVertices", s => ToStringCount(GetVal<int>(s, "clothMaxVertices")));
            Add("PhysicsColliderCount", s => ToStringCount(GetVal<int>(s, "physicsColliderCount")));
            Add("PhysicsRigidbodyCount", s => ToStringCount(GetVal<int>(s, "physicsRigidbodyCount")));
            Add("AudioSourceCount", s => ToStringCount(GetVal<int>(s, "audioSourceCount")));
            Add("TextureMegabytes", s => ToStringMegabytes(GetVal<float>(s, "textureMegabytes")));
            Add("ConstraintsCount", s => ToStringCount(GetVal<int>(s, "constraintsCount")));
            Add("ConstraintDepth", s => ToStringCount(GetVal<int>(s, "constraintDepth")));
            return dict;
        }
    }
#endif
}
