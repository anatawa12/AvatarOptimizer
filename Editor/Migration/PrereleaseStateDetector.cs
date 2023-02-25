using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

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

        static PrereleaseStateDetector()
        {
            if (File.Exists(DataPath))
            {
                Data = JsonUtility.FromJson<JsonData>(File.ReadAllText(DataPath));
            }
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
