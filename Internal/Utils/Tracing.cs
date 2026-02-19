using System;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer;

/// <summary>
/// this class manages tracing level logs.
/// This can be enabled with 
/// </summary>
[InitializeOnLoad]
public static class Tracing
{
    private static TracingArea _enabled = TracingArea.None;
    private const string SessionStateKey = "com.anatawa12.avatar-optimizer.tracing-log";

    static Tracing()
    {
        Enabled = (TracingArea)SessionState.GetInt(SessionStateKey, (int)TracingArea.None);
    }

    public static TracingArea Enabled
    {
        get => _enabled;
        set {
            _enabled = value;
            SessionState.SetInt(SessionStateKey, (int)value);
        }
    }

    public static bool IsEnabled(TracingArea area) => (Enabled & area) != 0;

    public static void Trace(TracingArea area, string msg)
    {
        if (IsEnabled(area)) Debug.Log(msg);
    }

    public static void Trace(TracingArea area,
        [InterpolatedStringHandlerArgument("area")] TracingInterpolatedStringHandler builder)
    {
        if (IsEnabled(area)) Debug.Log(builder.GetFormattedText());
    }
}

[Flags]
public enum TracingArea : uint
{
    ApplyObjectMapping = 1,
    BuildObjectMapping = 2,
    None = 0,
    All = uint.MaxValue,
}

[InterpolatedStringHandler]
public readonly struct TracingInterpolatedStringHandler
{
    // Storage for the built-up string
    private readonly StringBuilder? _builder;

    public TracingInterpolatedStringHandler(int literalLength, int _, TracingArea area, out bool enabled)
    {
        enabled = Tracing.IsEnabled(area);
        if (enabled)
        {
            _builder = new StringBuilder(literalLength + "[AAO TRACING:]: ".Length);
            _builder.Append("[AAO TRACING:").Append(area).Append("]: ");
        }
        else
        {
            _builder = null;
        }
    }

    public void AppendLiteral(string s) => _builder?.Append(s);
    public void AppendFormatted<T>(T t) => _builder?.Append(t);
    public void AppendFormatted<T>(T t, string format) where T : IFormattable? => 
        _builder?.Append(t?.ToString(format, null));

    internal string? GetFormattedText() => _builder?.ToString();
}

internal sealed class TracingManagerWindow : EditorWindow
{
    [MenuItem("Tools/Avatar Optimizer/TracingLog Manager")]
    public static void OpenWindow() => GetWindow<TracingManagerWindow>("TracingLog Manager");

    private static TracingArea[] Values = (TracingArea[])Enum.GetValues(typeof(TracingArea));

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "This window manages AAO internal tracing log.\n" +
            "Those setting will be reset after restarting unity.",
            MessageType.None);

        foreach (var value in Values)
        {
            if (value == TracingArea.None || value == TracingArea.All) continue;
            var enabled = Tracing.IsEnabled(value);
            var newEnabled = EditorGUILayout.ToggleLeft($"{value}", enabled);
            if (enabled != newEnabled)
            {
                if (newEnabled) Tracing.Enabled |= value;
                else Tracing.Enabled &= ~value;
            }
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Enable All")) Tracing.Enabled = TracingArea.All;
        if (GUILayout.Button("Disable All")) Tracing.Enabled = TracingArea.None;
        EditorGUILayout.EndHorizontal();
    }
}
