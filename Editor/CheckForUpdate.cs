using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [InitializeOnLoad]
    internal static class CheckForUpdate
    {
        static CheckForUpdate()
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
            var currentVersion = GetCurrentVersion();
            if (currentVersion == null)
            {
                Debug.LogError("AAO CheckForUpdate: Failed to get current version");
                return;
            }

            CurrentVersionName = currentVersion;

            var isBeta = currentVersion.Contains("-");
            var latestVersion = await GetLatestVersion(isBeta, currentVersion);
            LatestVersionName = latestVersion;

            var outOf = OutOfDate = latestVersion != currentVersion;
            if (outOf)
            {
                Debug.Log("AAO CheckForUpdate: Out of date detected! " +
                          $"current version: {currentVersion}, latest version: {latestVersion}");
            }
        }

        [CanBeNull]
        static string GetCurrentVersion()
        {
            try
            {
                var packageJson =
                    JsonUtility.FromJson<PackageJson>(
                        File.ReadAllText("Packages/com.anatawa12.avatar-optimizer/package.json"));
                if (packageJson.version == null) return null;
                if (!VersionRegex.IsMatch(packageJson.version)) return null;
                return packageJson.version;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        [ItemCanBeNull]
        static async Task<string> GetLatestVersion(bool beta, string version)
        {
            var latestVersionUrl =
                beta
                    ? "https://vpm.anatawa12.com/avatar-optimizer/beta/latest.txt"
                    : "https://vpm.anatawa12.com/avatar-optimizer/latest.txt";

            var keyPrefix =
                beta
                    ? "com.anatawa12.avatar-optimizer.beta.latest"
                    : "com.anatawa12.avatar-optimizer.latest";
            var updatedAtKey = $"{keyPrefix}.updated";
            var checkedWithKey = $"{keyPrefix}.checked-with";
            var latestVersionKey = $"{keyPrefix}.value";

            // fetch cached version
            var cachedVersion = EditorPrefs.GetString(latestVersionKey);
            if (!VersionRegex.IsMatch(cachedVersion))
                cachedVersion = null;
            
            if (cachedVersion != null 
                && EditorPrefs.GetString(checkedWithKey, "") == CurrentVersionName
                && DateTime.TryParse(EditorPrefs.GetString(updatedAtKey, ""), out var updatedAt)
                && updatedAt >= DateTime.UtcNow - TimeSpan.FromHours(1))
            {
                // it looks cached version is not out of date
                return cachedVersion;
            }

            // out of date or invalid cached version

            string fetchedLatestVersion = null;
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                        $"AvatarOptimizer-UpdateCheck/{version}");
                    var response = await client.GetAsync(latestVersionUrl);
                    var responseText = await response.Content.ReadAsStringAsync();
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new Exception($"Non OK response from remote: {response.StatusCode}\n\n{responseText}");
                    responseText = responseText.Trim();
                    if (VersionRegex.IsMatch(responseText))
                        fetchedLatestVersion = responseText;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (fetchedLatestVersion != null)
            {
                // we successfully fetched latest version!
                EditorPrefs.SetString(latestVersionKey, fetchedLatestVersion);
                EditorPrefs.SetString(checkedWithKey, CurrentVersionName);
                EditorPrefs.SetString(updatedAtKey, DateTime.UtcNow.ToString("O"));
                return fetchedLatestVersion;
            }

            if (cachedVersion != null)
            {
                Debug.Log("AAO CheckForUpdate: Failed to fetch latest version. fall back to outdated version");
                // we failed to update cached version so fallback to cached one
                return cachedVersion;
            }

            return null;
        }

        private static readonly Regex VersionRegex = new Regex(
            @"\d+\.\d+\.\d+(-[a-zA-Z0-9.-]+)?(\+[a-zA-Z0-9.-]+)?");

        [Serializable]
        class PackageJson
        {
            public string version;
        }
    }
}