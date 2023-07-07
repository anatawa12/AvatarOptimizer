using System.Linq;
using CustomLocalization4EditorExtension;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.ApplyOnPlay
{
    internal class ApplyOnPlayConfiguration : EditorWindow
    {
        [MenuItem("Tools/Apply on Play/Configuration", false, 100)]
        internal static void OpenWindow() => GetWindow<ApplyOnPlayConfiguration>("Apply on Play Config").Show();

        [AssemblyCL4EELocalization] private static Localization Localization { get; } = new Localization("588f55b2626b4d7fb0b79d34fc67de42", "en");

        private (string callbackId, string callbackName)[] _callbacks;
        private Vector2 _scroll;

        private void OnEnable()
        {
            _callbacks = ApplyOnPlayCallbackRegistry.Callbacks
                .OrderBy(x => x.callbackOrder)
                .Select(x => (x.CallbackId, x.CallbackName))
                .ToArray();
        }

        private void OnGUI()
        {
            CL4EE.DrawLanguagePicker();
            EditorGUILayout.HelpBox(CL4EE.Tr("window description"), MessageType.None);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            foreach (var (callbackId, callbackName) in _callbacks)
            {
                var key = ApplyOnPlayCallbackRegistry.ENABLE_EDITOR_PREFS_PREFIX + callbackId;
                var enabled = EditorPrefs.GetBool(key, true);
                EditorGUI.BeginChangeCheck();
                enabled = EditorGUILayout.ToggleLeft(callbackName, enabled);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetBool(key, enabled);
                }
            }

            EditorGUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Enable All"))
            {
                foreach (var (callbackId, callbackName) in _callbacks)
                    EditorPrefs.SetBool(ApplyOnPlayCallbackRegistry.ENABLE_EDITOR_PREFS_PREFIX + callbackId, true);
            }
            
            if (GUILayout.Button("Disable All"))
            {
                foreach (var (callbackId, callbackName) in _callbacks)
                    EditorPrefs.SetBool(ApplyOnPlayCallbackRegistry.ENABLE_EDITOR_PREFS_PREFIX + callbackId, false);
            }
            GUILayout.EndHorizontal();
        }
    }
}