using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Anatawa12.AvatarOptimizer.Migration
{
    /// <summary>
    /// This class will detect version migration.
    /// </summary>
    [InitializeOnLoad]
    internal static class PrereleaseStateDetector
    {
        private const string DataPath = "ProjectSettings/com.anatawa12.avatar-optimizer.v0.json";
        private const int CurrentVersion = 1;
        private static readonly JsonData Data = new JsonData();
        internal static bool AutoDetectionInProgress = false;

        static PrereleaseStateDetector()
        {
            if (File.Exists(DataPath))
            {
                Data = JsonUtility.FromJson<JsonData>(File.ReadAllText(DataPath));
            }
            else
            {
                AutoDetectionInProgress = true;
                EditorApplication.CallbackFunction callback = null;
                callback = AutoDetection;
                EditorApplication.update += callback;
                void AutoDetection()
                {
                    AutoDetectionInProgress = false;
                    // ReSharper disable once AccessToModifiedClosure
                    EditorApplication.update -= callback;
                    try
                    {
                        if (FindAOComponent())
                        {
                            Data.currentSerializedVersion = 1;
                        }
                    }
                    catch
                    {
                        // ignore
                    }

                    Save();
                }
                
            }
        }

        private static bool FindAOComponent()
        {
            bool CheckPrefabType(PrefabAssetType type) =>
                type != PrefabAssetType.MissingAsset && type != PrefabAssetType.Model &&
                type != PrefabAssetType.NotAPrefab;

            if (AssetDatabase
                .FindAssets("t:prefab")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                .Where(x => x)
                .Where(x => CheckPrefabType(PrefabUtility.GetPrefabAssetType(x)))
                .Any(x => x.GetComponentsInChildren<AvatarTagComponent>(true).Length != 0))
                return true;
            
            // guids of scripts in v0.1.3
            var guids = string.Join("|", new[]
            {
                "20329659ce304935acccbb043aeb3c9c",
                "42fdd72c6d8349358bf15109de81a373",
                "63d518a37a53491c80d63a7f46a178af",
                "50d2f7c45b304f669b10d2f85c065464",
                "7b78da52962c457aa3fc301eaba981aa",
                "0556b75ec8ef4868ab20ce8404c1edae",
                "1d42113ec3c34311b1548e7f0cbf46f2",
                "2650884bd6834672915418cf56ffbfde",
                "d95379eb5690423ebd102a3902be341b",
                "885785ecaa724d6d8bb45dd0d62241f7",
                "a9fd0617dd174314b0a375fb2188510c",
                "3a3212f462e5430a81a74acb08e41adb",
                "3c0e29fd88964bcdbaa605adf7d8ead9",
                "f69eeb3e25674f4a9bd20e6d7e69e0e6",
            });
            var regex = new Regex(guids);

            var assetsInfo = new DirectoryInfo("Assets");

            var found = false;

            async Task ProcessFile(FileInfo file)
            {
                Debug.Log($"processing {file.Name}");
                try
                {
                    var sb = new StringBuilder();
                    using (var sr = new StreamReader(file.OpenRead(), Encoding.UTF8,
                               detectEncodingFromByteOrderMarks: true))
                    {
                        var buffer = new char[4096];
                        while (true)
                        {
                            if (Volatile.Read(ref found)) return;
                            var read = await sr.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                            if (read == 0)
                                break;
                            sb.Append(buffer, 0, read);
                        }
                    }

                    if (regex.IsMatch(sb.ToString()))
                        Volatile.Write(ref found, true);
                }
                catch
                {
                    // ignored
                }
            }

            Task.WaitAll(assetsInfo.GetFiles("*.unity", SearchOption.AllDirectories)
                .Select(ProcessFile).ToArray());

            return found;
        }

        public static bool MigrationRequired() =>
            Data.currentSerializedVersion != 0 && Data.currentSerializedVersion < CurrentVersion;

        public static void MigrationFinished()
        {
            Data.currentSerializedVersion = CurrentVersion;
            Save();
        }

        private static void Save()
        {
            File.WriteAllText(DataPath, JsonUtility.ToJson(Data, true));
        }

        [Serializable]
        private class JsonData
        {
            // serialize version is 0.x. if 0.0, it means nothing is reloaded
            public int currentSerializedVersion;
        }
    }
}
