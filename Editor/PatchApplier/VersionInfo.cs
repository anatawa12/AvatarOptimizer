using System;
using System.IO;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.PatchApplier
{
    /// <summary>
    /// Provides version information including patch data
    /// </summary>
    internal static class VersionInfo
    {
        private static PatchRegistry? _cachedRegistry;
        private static string? _cachedBaseVersion;

        /// <summary>
        /// Gets the base version from package.json
        /// </summary>
        public static string GetBaseVersion()
        {
            if (_cachedBaseVersion != null)
                return _cachedBaseVersion;

            try
            {
                var packageJsonPath = Path.Combine("Packages", "com.anatawa12.avatar-optimizer", "package.json");
                var packageJson = JsonUtility.FromJson<PackageJson>(File.ReadAllText(packageJsonPath));
                _cachedBaseVersion = packageJson.version ?? "unknown";
            }
            catch
            {
                _cachedBaseVersion = "unknown";
            }

            return _cachedBaseVersion;
        }

        /// <summary>
        /// Gets the patch registry
        /// </summary>
        public static PatchRegistry GetPatchRegistry()
        {
            if (_cachedRegistry == null)
                _cachedRegistry = PatchRegistry.Load();
            return _cachedRegistry;
        }

        /// <summary>
        /// Clears the cached data (call after applying a patch)
        /// </summary>
        public static void ClearCache()
        {
            _cachedRegistry = null;
        }

        /// <summary>
        /// Gets the full version string including patch information
        /// </summary>
        public static string GetVersionString()
        {
            var baseVersion = GetBaseVersion();
            var registry = GetPatchRegistry();
            return registry.GetVersionString(baseVersion);
        }

        /// <summary>
        /// Gets the full commit hash if a patch is applied
        /// </summary>
        public static string? GetPatchCommitHash()
        {
            var registry = GetPatchRegistry();
            return registry.GetFullCommitHash();
        }

        /// <summary>
        /// Checks if any patches are applied
        /// </summary>
        public static bool HasPatches()
        {
            var registry = GetPatchRegistry();
            return registry.Patches.Count > 0;
        }

        [Serializable]
        private class PackageJson
        {
            public string? version;
        }
    }
}
