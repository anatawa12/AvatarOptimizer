using System;
using System.Collections.Generic;

namespace Anatawa12.AvatarOptimizer.CheckForUpdate
{
    internal class Latest2TextFile
    {
        private (Version latest, UnityVersion minUnityVersion)[] _lines;

        public Latest2TextFile((Version latest, UnityVersion minUnityVersion)[] lines) => _lines = lines;

        public static Latest2TextFile Parse(string text)
        {
            var result = new List<(Version latest, UnityVersion minUnityVersion)>();
            foreach (var lineRaw in text.Split('\n'))
            {
                var line = lineRaw.Trim();
                if (line == "") break;

                var parts = line.Split(':');
                if (parts.Length < 2) continue; // invalid

                if (!Version.TryParse(parts[0], out var version)) continue;
                if (!UnityVersion.TryParse(parts[1], out var unityVersion)) continue;

                result.Add((version, unityVersion));
            }

            var parsedLines = result.ToArray();

            // sort to the newest first
            Array.Sort(parsedLines, (a, b) => -a.latest.CompareTo(b.latest));

            return new Latest2TextFile(parsedLines);
        }

        public Version? LatestFor(UnityVersion unityVersion)
        {
            foreach (var (latest, minUnityVersion) in _lines)
                if (minUnityVersion <= unityVersion) return latest;

            return null;
        }
    }
}
