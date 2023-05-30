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
        private const int CurrentVersion = 4;
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
                Save();
            }
        }

        public static int GetCurrentVersion() => Data.currentSerializedVersion;

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
