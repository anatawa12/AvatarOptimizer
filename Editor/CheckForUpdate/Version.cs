using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Anatawa12.AvatarOptimizer.CheckForUpdate
{
    internal struct Version : IComparable<Version>, IEquatable<Version>
    {
        public readonly int Major;
        public readonly int Minor;
        public readonly int Patch;
        public readonly string Pre;

        public Version(int major, int minor, int patch) => (Major, Minor, Patch, Pre) = (major, minor, patch, "");

        internal Version(int major, int minor, int patch, string pre) =>
            (Major, Minor, Patch, Pre) = (major, minor, patch, pre);

        public bool IsPrerelease => Pre != "";

        public int CompareTo(Version other)
        {
            var majorComparison = Major.CompareTo(other.Major);
            if (majorComparison != 0) return majorComparison;
            var minorComparison = Minor.CompareTo(other.Minor);
            if (minorComparison != 0) return minorComparison;
            var patchComparison = Patch.CompareTo(other.Patch);
            if (patchComparison != 0) return patchComparison;
            // likely: in most case, prerelease is empty
            if (Pre == "" && other.Pre == "") return 0;

            // if one of them is empty, the other is greater
            if (Pre == "" && other.Pre != "") return 1;
            if (Pre != "" && other.Pre == "") return -1;

            // compare pre-release now
            var thisSplit = Pre.Split('.');
            var otherSplit = other.Pre.Split('.');

            foreach (var (a, b) in thisSplit.Zip(otherSplit, (a, b) => (a, b)))
            {
                if (a == b) continue;
                var aIsInt = int.TryParse(a, out var aInt);
                var bIsInt = int.TryParse(b, out var bInt);
                if (aIsInt && bIsInt) return aInt.CompareTo(bInt);
                if (!aIsInt && bIsInt) return 1;
                if (aIsInt && !bIsInt) return -1;
                return string.Compare(a, b, StringComparison.Ordinal);
            }

            if (thisSplit.Length < otherSplit.Length) return -1;
            if (thisSplit.Length > otherSplit.Length) return 1;

            return 0;
        }


        private static readonly Regex VersionRegex =
            new Regex(@"^(\d+)\.(\d+)\.(\d+)(?:-([a-zA-Z0-9.-]+))?(?:\+([a-zA-Z0-9.-]+))?$");

        public static bool TryParse(string version, out Version result)
        {
            result = default;

            var match = VersionRegex.Match(version);
            if (!match.Success) return false;


            if (!int.TryParse(match.Groups[1].Value, out var major)) return false;
            if (!int.TryParse(match.Groups[2].Value, out var minor)) return false;
            if (!int.TryParse(match.Groups[3].Value, out var patch)) return false;

            var pre = match.Groups[4].Value;
            _ = match.Groups[5].Value;

            if (!(pre.Length == 0 || pre.Split('.').All(x => x != ""))) return false; // invalid pre-release
            // we won't check for build-meta for now

            result = new Version(major, minor, patch, pre);
            return true;
        }

        public static Version Parse(string version)
        {
            if (!TryParse(version, out var result))
                throw new ArgumentException("invalid version format", nameof(version));
            return result;
        }

        public bool Equals(Version other) =>
            Major == other.Major && Minor == other.Minor && Patch == other.Patch && Pre == other.Pre;

        public override bool Equals(object obj) => obj is Version other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Major;
                hashCode = (hashCode * 397) ^ Minor;
                hashCode = (hashCode * 397) ^ Patch;
                hashCode = (hashCode * 397) ^ Pre.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(Version left, Version right) => left.Equals(right);
        public static bool operator !=(Version left, Version right) => !left.Equals(right);
        public static bool operator <(Version left, Version right) => left.CompareTo(right) < 0;
        public static bool operator >(Version left, Version right) => left.CompareTo(right) > 0;
        public static bool operator <=(Version left, Version right) => left.CompareTo(right) <= 0;
        public static bool operator >=(Version left, Version right) => left.CompareTo(right) >= 0;

        public override string ToString() =>
            Pre == "" ? $"{Major}.{Minor}.{Patch}" : $"{Major}.{Minor}.{Patch}-{Pre}";
    }
}
