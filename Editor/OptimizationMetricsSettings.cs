
using UnityEditor;

namespace Anatawa12.AvatarOptimizer;

internal static class OptimizationMetricsSettings
{
    private const string PrefsKey = "com.anatawa12.avatar-optimizer.optimization-metrics";
    private const string OptimizationMetricsMenuName = "Tools/Avatar Optimizer/Optimization Metrics";

    public static bool EnableOptimizationMetrics
    {
        get => EditorPrefs.GetBool(PrefsKey, true);
        set => EditorPrefs.SetBool(PrefsKey, value);
    }

    [MenuItem(OptimizationMetricsMenuName, true)]
    private static bool ValidateOptimizationMetrics()
    {
        Menu.SetChecked(OptimizationMetricsMenuName, EnableOptimizationMetrics);
        return true;
    }

    [MenuItem(OptimizationMetricsMenuName, false)]
    private static void ToggleOptimizationMetrics()
    {
        EnableOptimizationMetrics = !EnableOptimizationMetrics;
    }
}
