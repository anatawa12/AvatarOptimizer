
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer;

[FilePath("ProjectSettings/AvatarOptimizer/OptimizationMetricsSettings.asset", FilePathAttribute.Location.ProjectFolder)]
internal class OptimizationMetricsSettings : ScriptableSingleton<OptimizationMetricsSettings>
{
    [SerializeField]
    private bool enableOptimizationMetrics = true;
    public static bool EnableOptimizationMetrics
    {
        get => instance.enableOptimizationMetrics;
        set
        {
            if (instance.enableOptimizationMetrics == value) return;
            instance.enableOptimizationMetrics = value;
            Save();
        }
    }

    private static void Save() => instance.Save(false);
    
    private const string OptimizationMetricsMenuName = "Tools/Avatar Optimizer/Optimization Metrics";
    
    [MenuItem(OptimizationMetricsMenuName, true)]
    private static bool validateOptimizationMetrics()
    {
        Menu.SetChecked(OptimizationMetricsMenuName, EnableOptimizationMetrics);
        return true;
    }

    [MenuItem(OptimizationMetricsMenuName, false)]
    private static void toggleOptimizationMetrics()
    {
        EnableOptimizationMetrics = !EnableOptimizationMetrics;
    }
}
