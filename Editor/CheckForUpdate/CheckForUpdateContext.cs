using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.CheckForUpdate
{
    /// <summary>
    /// This is a context for check for update.
    /// </summary>
    internal struct CheckForUpdateContext
    {
        public readonly bool Beta;
        public readonly Version CurrentVersion;
        public readonly UnityVersion UnityVersion;

        public CheckForUpdateContext(bool beta, Version currentVersion, UnityVersion unityVersion) : this()
        {
            Beta = beta;
            CurrentVersion = currentVersion;
            UnityVersion = unityVersion;
        }

        const string KeyPrefix = "com.anatawa12.avatar-optimizer.check-for-update.v2";
        private string Channel => Beta ? "beta" : "stable";
        private string ValueKey => $"{KeyPrefix}.{UnityVersion}.{Channel}.value";
        private string UpdatedAtKey => $"{KeyPrefix}.{UnityVersion}.{Channel}.updated";

        private string LatestVersionUrl => Beta
            ? "https://vpm.anatawa12.com/avatar-optimizer/beta/latest2.txt"
            : "https://vpm.anatawa12.com/avatar-optimizer/latest2.txt";

        public async Task<Version?> GetLatestVersion()
        {
            // first, try cache
            var (cachedLatest, cacheOutdated) = TryGetVersionFromCache();

            if (cachedLatest is Version cacheLoaded && !cacheOutdated) return cacheLoaded;

            // then try fetch
            var latestInfo = await FetchLatest2();
            if (latestInfo != null)
            {
                var latestFromRemote = latestInfo.LatestFor(UnityVersion);
                if (latestFromRemote != null)
                {
                    // if success, save to cache and return
                    EditorPrefs.SetString(ValueKey, latestFromRemote.ToString());
                    EditorPrefs.SetString(UpdatedAtKey, DateTime.Now.ToString(CultureInfo.InvariantCulture));
                    return latestFromRemote;
                }
            }

            // if cache fails, use cache
            return cachedLatest;
        }

        private (Version? loaded, bool outdated) TryGetVersionFromCache()
        {
            var cachedLatest = EditorPrefs.GetString(ValueKey, "");
            var updatedAt = EditorPrefs.GetString(UpdatedAtKey, "");

            if (!Version.TryParse(cachedLatest, out var version)) return (null, true);
            if (!DateTime.TryParse(updatedAt, out var updated)) return (null, true);

            var outDate = DateTime.Now - updated > TimeSpan.FromHours(1);
            return (version, outDate);

        }

        private async Task<Latest2TextFile?> FetchLatest2()
        {
            // latest version:unity version
            string responseText;
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                        $"AvatarOptimizer-UpdateCheck/{CurrentVersion}");
                    var response = await client.GetAsync(LatestVersionUrl);
                    responseText = await response.Content.ReadAsStringAsync();
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new Exception(
                            $"Non OK response from remote: {response.StatusCode}\n\n{responseText}");
                    return Latest2TextFile.Parse(responseText);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }
    }
}
