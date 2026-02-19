using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.PatchApplier
{
    /// <summary>
    /// Editor window for applying patches to Avatar Optimizer
    /// </summary>
    internal class PatchApplierWindow : EditorWindow
    {
        private string _patchFilePathOrUrl = "";
        private Vector2 _scrollPosition;

        [MenuItem("Tools/Avatar Optimizer/Apply Patch", priority = 902)]
        public static void ShowWindow()
        {
            var window = GetWindow<PatchApplierWindow>("AAO Patch Applier");
            window.minSize = new Vector2(500, 400);
        }

        private void OnGUI()
        {
            GUILayout.Label("Avatar Optimizer Patch Applier", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Show current version info
            var baseVersion = VersionInfo.GetBaseVersion();
            var currentVersion = VersionInfo.GetVersionString();
            var hasPatches = VersionInfo.HasPatches();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Current Version Information:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Base Version:", baseVersion);
            EditorGUILayout.LabelField("Current Version:", currentVersion);
            
            if (hasPatches)
            {
                var patchHash = VersionInfo.GetPatchCommitHash();
                EditorGUILayout.LabelField("Patch Commit:", patchHash ?? "unknown");
                EditorGUILayout.HelpBox("Patches are currently applied. You can only apply continuous patches based on the current patch.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("No patches are currently applied.", MessageType.Info);
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // Patch file input
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Patch File:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            _patchFilePathOrUrl = EditorGUILayout.TextField("Path or URL:", _patchFilePathOrUrl);
            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                var path = EditorUtility.OpenFilePanel("Select Patch File", "", "patch");
                if (!string.IsNullOrEmpty(path))
                    _patchFilePathOrUrl = path;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "Select a .patch file from the Avatar Optimizer repository.\n" +
                "Example: https://github.com/anatawa12/AvatarOptimizer/commit/<hash>.patch",
                MessageType.Info);
            
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // Apply button
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_patchFilePathOrUrl));
            if (GUILayout.Button("Apply Patch", GUILayout.Height(30)))
            {
                ApplyPatch();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);

            // Show applied patches
            var registry = PatchRegistry.Load();
            if (registry.Patches.Count > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("Applied Patches:", EditorStyles.boldLabel);
                
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                foreach (var patch in registry.Patches)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("Commit:", patch.CommitHash);
                    EditorGUILayout.LabelField("Base Version:", patch.BaseVersion);
                    if (!string.IsNullOrEmpty(patch.PullRequest))
                        EditorGUILayout.LabelField("Pull Request:", patch.PullRequest);
                    EditorGUILayout.LabelField("Applied:", patch.AppliedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
                    EditorGUILayout.EndVertical();
                    GUILayout.Space(5);
                }
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }
        }

        private void ApplyPatch()
        {
            try
            {
                string patchContent;

                // Check if it's a URL
                if (_patchFilePathOrUrl.StartsWith("http://") || _patchFilePathOrUrl.StartsWith("https://"))
                {
                    EditorUtility.DisplayProgressBar("Applying Patch", "Downloading patch file...", 0.3f);
                    try
                    {
                        using (var client = new System.Net.WebClient())
                        {
                            patchContent = client.DownloadString(_patchFilePathOrUrl);
                        }
                    }
                    catch (Exception ex)
                    {
                        EditorUtility.ClearProgressBar();
                        EditorUtility.DisplayDialog("Error", $"Failed to download patch file: {ex.Message}", "OK");
                        return;
                    }
                }
                else
                {
                    // It's a file path
                    if (!File.Exists(_patchFilePathOrUrl))
                    {
                        EditorUtility.DisplayDialog("Error", "Patch file does not exist", "OK");
                        return;
                    }

                    patchContent = File.ReadAllText(_patchFilePathOrUrl);
                }

                EditorUtility.DisplayProgressBar("Applying Patch", "Verifying and applying patch...", 0.6f);

                var result = PatchApplier.ApplyPatch(patchContent);

                EditorUtility.ClearProgressBar();

                if (result.Success)
                {
                    VersionInfo.ClearCache();
                    AssetDatabase.Refresh();
                    
                    var message = "Patch applied successfully!\n\n";
                    message += $"Commit: {result.AppliedPatch?.CommitHash}\n";
                    message += $"New Version: {VersionInfo.GetVersionString()}\n\n";
                    
                    if (result.Warnings.Count > 0)
                    {
                        message += "Warnings:\n";
                        foreach (var warning in result.Warnings)
                            message += $"- {warning}\n";
                    }

                    EditorUtility.DisplayDialog("Success", message, "OK");
                    Repaint();
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", $"Failed to apply patch:\n\n{result.ErrorMessage}", "OK");
                }
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Error", $"An error occurred while applying the patch:\n\n{ex.Message}", "OK");
            }
        }
    }
}
