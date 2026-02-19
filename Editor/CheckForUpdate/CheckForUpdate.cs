using System;
using System.IO;
using Anatawa12.AvatarOptimizer.PatchApplier;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.CheckForUpdate
{
    [InitializeOnLoad]
    internal static class Checker
    {
        static Checker()
        {
            EditorApplication.delayCall += DoCheckForUpdate;
        }

        public static bool OutOfDate
        {
            get => SessionState.GetBool("com.anatawa12.avatar-optimizer.out-of-date", false);
            private set => SessionState.SetBool("com.anatawa12.avatar-optimizer.out-of-date", value);
        }

        public static string LatestVersionName
        {
            get => SessionState.GetString("com.anatawa12.avatar-optimizer.latest", "");
            private set => SessionState.SetString("com.anatawa12.avatar-optimizer.latest", value);
        }

        public static string CurrentVersionName
        {
            get => SessionState.GetString("com.anatawa12.avatar-optimizer.current", "");
            private set => SessionState.SetString("com.anatawa12.avatar-optimizer.current", value);
        }

        public static bool IsBeta => CurrentVersionName.Contains("-");

        static async void DoCheckForUpdate()
        {
            if (!MenuItems.CheckForUpdateEnabled)
            {
                // if disabled, do nothing
                OutOfDate = false;
                return;
            }

            if (!(GetCurrentVersion() is Version currentVersion))
            {
                Debug.LogError("Avatar Optimizer CheckForUpdate: Failed to get current version");
                return;
            }

            // Update current version name to include patch information
            CurrentVersionName = VersionInfo.GetVersionString();

            var isBeta = currentVersion.IsPrerelease || MenuItems.ForceBetaChannel;
            if (!UnityVersion.TryParse(Application.unityVersion, out var unityVersion))
            {
                Debug.LogError("Avatar Optimizer CheckForUpdate: Failed to get unity version");
                return;
            }

            var ctx = new CheckForUpdateContext(isBeta, currentVersion, unityVersion);

            if (await ctx.GetLatestVersion() is Version latestVersion)
            {
                // there is known latest version
                if (currentVersion < latestVersion)
                {
                    OutOfDate = true;
                    LatestVersionName = latestVersion.ToString();
                    
                    Debug.Log("AAO CheckForUpdate: Out of date detected! " +
                              $"current version: {currentVersion}, latest version: {latestVersion}");
                }
                else
                {
                    OutOfDate = false;
                }
            }
            else
            {
                OutOfDate = false;
            }
        }

        static Version? GetCurrentVersion()
        {
            try
            {
                var packageJson =
                    JsonUtility.FromJson<PackageJson>(
                        File.ReadAllText("Packages/com.anatawa12.avatar-optimizer/package.json"));
                if (packageJson.version == null) return null;
                if (!Version.TryParse(packageJson.version, out var version)) return null;
                return version;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        [Serializable]
        class PackageJson
        {
            public string? version;
        }
    }
}
