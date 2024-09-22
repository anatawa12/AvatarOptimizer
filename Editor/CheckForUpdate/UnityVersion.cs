using System;
using System.Text.RegularExpressions;

namespace Anatawa12.AvatarOptimizer.CheckForUpdate
{
    internal struct UnityVersion : IEquatable<UnityVersion>, IComparable<UnityVersion>
    {
        public readonly int Major;
        public readonly int Minor;
        // minior, channel, increment are ignored

        public UnityVersion(int major, int minor) => (Major, Minor) = (major, minor);

        public bool Equals(UnityVersion other) => Major == other.Major && Minor == other.Minor;
        public override bool Equals(object obj) => obj is UnityVersion other && Equals(other);
        public override int GetHashCode() => unchecked((Major * 397) ^ Minor);
        public static bool operator ==(UnityVersion left, UnityVersion right) => left.Equals(right);
        public static bool operator !=(UnityVersion left, UnityVersion right) => !left.Equals(right);

        public int CompareTo(UnityVersion other)
        {
            var majorComparison = Major.CompareTo(other.Major);
            if (majorComparison != 0) return majorComparison;
            return Minor.CompareTo(other.Minor);
        }

        public static bool operator <(UnityVersion left, UnityVersion right) => left.CompareTo(right) < 0;
        public static bool operator >(UnityVersion left, UnityVersion right) => left.CompareTo(right) > 0;
        public static bool operator <=(UnityVersion left, UnityVersion right) => left.CompareTo(right) <= 0;
        public static bool operator >=(UnityVersion left, UnityVersion right) => left.CompareTo(right) >= 0;

        public override string ToString() => $"{Major}.{Minor}";

        private static readonly Regex VersionRegex = new Regex(@"^(\d+)\.(\d+)(?:\..*)?$");

        public static bool TryParse(string version, out UnityVersion result)
        {
            result = default;

            var match = VersionRegex.Match(version);
            if (!match.Success) return false;

            if (!int.TryParse(match.Groups[1].Value, out var major)) return false;
            if (!int.TryParse(match.Groups[2].Value, out var minor)) return false;

            result = new UnityVersion(major, minor);
            return true;
        }
    }
}
