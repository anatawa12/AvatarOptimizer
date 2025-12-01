using System.Linq;
using nadena.dev.ndmf;
using nadena.dev.ndmf.platform;
using UnityEditor;
using UnityEngine;
using static Anatawa12.AvatarOptimizer.TraceAndOptimizePlatformSettings;

namespace Anatawa12.AvatarOptimizer;

[CustomEditor(typeof(TraceAndOptimizePlatformSettings))]
class TraceAndOptimizePlatformSettingsEditor : Editor
{
    private static SerializedProperty _platformQualifiedName = null!;
    private static SerializedProperty _experimentalPlatformSettingsVersion = null!;
    private static SerializedProperty _firstSettingProperty = null!;

    private void OnEnable()
    {
        _platformQualifiedName = serializedObject.FindProperty(nameof(TraceAndOptimizePlatformSettings.platformQualifiedName));
        _experimentalPlatformSettingsVersion = serializedObject.FindProperty(nameof(TraceAndOptimizePlatformSettings.experimentalPlatformSettingsVersion));
        _firstSettingProperty = serializedObject.FindProperty(nameof(TraceAndOptimizePlatformSettings.optimizeBlendShape));
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox(
            AAOL10N.Tr("NonVRChatPlatformSupport:experimentalMessage"),
            MessageType.Warning
        );
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(_platformQualifiedName);

        var selectedPlatform = Utils.PopupSuggestion(GetPlatforms, x => x, GUILayout.Width(20));
        if (selectedPlatform != null) _platformQualifiedName.stringValue = selectedPlatform;
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginDisabledGroup(disabled: true);
        EditorGUILayout.PropertyField(_experimentalPlatformSettingsVersion);
        EditorGUI.EndDisabledGroup();

        if (_experimentalPlatformSettingsVersion.intValue != CurrentExperimentalPlatformSettingsVersion)
        {
            EditorGUILayout.HelpBox(
                AAOL10N.Tr("NonVRChatPlatformSupport:versionMismatchMessage:inspector")
                    .Replace("{0}", _experimentalPlatformSettingsVersion.intValue.ToString())
                    .Replace("{1}", CurrentExperimentalPlatformSettingsVersion.ToString()),
                MessageType.Info
            );
            if (GUILayout.Button($"Change Version to {CurrentExperimentalPlatformSettingsVersion}"))
            {
                _experimentalPlatformSettingsVersion.intValue = CurrentExperimentalPlatformSettingsVersion;
            }
        }

        EditorGUILayout.LabelField("Platform Settings", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(disabled: _experimentalPlatformSettingsVersion.intValue != CurrentExperimentalPlatformSettingsVersion);
        EditorGUI.BeginDisabledGroup(disabled: false);
        EditorGUI.indentLevel++;
        var p = _firstSettingProperty.Copy();
        do
        {
            EditorGUILayout.PropertyField(p);
        } while (p.NextVisible(true));

        EditorGUI.indentLevel--;
        EditorGUI.EndDisabledGroup();
        EditorGUI.EndDisabledGroup();
        serializedObject.ApplyModifiedProperties();
    }

    private static string[] GetPlatforms()
    {
        return PlatformRegistry.PlatformProviders.Keys
            .Except(new[] { WellKnownPlatforms.VRChatAvatar30, WellKnownPlatforms.Generic })
            .ToArray();
    }
}

[CustomPropertyDrawer(typeof(Section))]
class SectionDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.EndDisabledGroup();
        EditorGUI.indentLevel--;
        EditorGUI.PropertyField(position, property, label);
        EditorGUI.indentLevel++;
        EditorGUI.BeginDisabledGroup(disabled: !property.boolValue);
    }
}
