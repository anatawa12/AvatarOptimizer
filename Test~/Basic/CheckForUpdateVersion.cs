using Anatawa12.AvatarOptimizer.CheckForUpdate;
using NUnit.Framework;

namespace Anatawa12.AvatarOptimizer.Test
{
    public class CheckForUpdateVersion
    {
        [Test]
        public static void Parse()
        {
            Assert.That(Version.Parse("1.0.0"), Is.EqualTo(new Version(1, 0, 0)));
            Assert.That(Version.Parse("1.0.0-alpha"), Is.EqualTo(new Version(1, 0, 0, "alpha")));
            Assert.That(Version.Parse("1.0.0+build"), Is.EqualTo(new Version(1, 0, 0, "")));
            Assert.That(Version.Parse("1.0.0-alpha+build"), Is.EqualTo(new Version(1, 0, 0, "alpha")));
            Assert.That(Version.Parse("1.0.0-alpha.1"), Is.EqualTo(new Version(1, 0, 0, "alpha.1")));
            Assert.That(Version.Parse("1.0.0-alpha.1+build"), Is.EqualTo(new Version(1, 0, 0, "alpha.1")));

            // invalid versions should return false for TryParse
            Assert.That(Version.TryParse("1", out _), Is.False);
            Assert.That(Version.TryParse("1.0", out _), Is.False);
            Assert.That(Version.TryParse("1.0.0-beta..1", out _), Is.False);
            Assert.That(Version.TryParse("1.0.0-alpha.1+build+build", out _), Is.False);
        }

        [Test]
        public new static void ToString()
        {
            Assert.That(new Version(1, 0, 0).ToString(), Is.EqualTo("1.0.0"));
            Assert.That(new Version(1, 0, 0, "alpha").ToString(), Is.EqualTo("1.0.0-alpha"));
            Assert.That(new Version(1, 0, 0, "alpha.1").ToString(), Is.EqualTo("1.0.0-alpha.1"));
        }

        [Test]
        public static void Compare()
        {
            Assert.That(new Version(1, 0, 0).CompareTo(new Version(1, 0, 0)), Is.Zero);
            Assert.That(new Version(1, 0, 0).CompareTo(new Version(1, 0, 1)), Is.LessThan(0));
            Assert.That(new Version(1, 0, 0).CompareTo(new Version(1, 1, 0)), Is.LessThan(0));
            Assert.That(new Version(1, 0, 0).CompareTo(new Version(2, 0, 0)), Is.LessThan(0));

            // prereleases are before the normal version, but after previous normal version
            Assert.That(new Version(1, 0, 0, "alpha").CompareTo(new Version(1, 0, 0)), Is.LessThan(0));
            Assert.That(new Version(1, 0, 0, "alpha").CompareTo(new Version(0, 99, 99)), Is.GreaterThan(0));

            // compare Prereleases
            Assert.That(new Version(1, 0, 0, "alpha").CompareTo(new Version(1, 0, 0, "alpha")), Is.Zero);
            Assert.That(new Version(1, 0, 0, "alpha").CompareTo(new Version(1, 0, 0, "alpha.1")), Is.LessThan(0));
            Assert.That(new Version(1, 0, 0, "alpha.1").CompareTo(new Version(1, 0, 0, "alpha")), Is.GreaterThan(0));
            Assert.That(new Version(1, 0, 0, "alpha.1").CompareTo(new Version(1, 0, 0, "alpha.1")), Is.Zero);
            Assert.That(new Version(1, 0, 0, "alpha.1").CompareTo(new Version(1, 0, 0, "alpha.2")), Is.LessThan(0));
            Assert.That(new Version(1, 0, 0, "alpha.2").CompareTo(new Version(1, 0, 0, "alpha.1")), Is.GreaterThan(0));
            Assert.That(new Version(1, 0, 0, "alpha.2").CompareTo(new Version(1, 0, 0, "alpha.2")), Is.Zero);
            Assert.That(new Version(1, 0, 0, "alpha.2").CompareTo(new Version(1, 0, 0, "alpha.10")), Is.LessThan(0));
            Assert.That(new Version(1, 0, 0, "alpha.10").CompareTo(new Version(1, 0, 0, "alpha.2")), Is.GreaterThan(0));
            Assert.That(new Version(1, 0, 0, "alpha.10").CompareTo(new Version(1, 0, 0, "alpha.10")), Is.Zero);
            Assert.That(new Version(1, 0, 0, "alpha").CompareTo(new Version(1, 0, 0, "beta")), Is.LessThan(0));
            Assert.That(new Version(1, 0, 0, "beta").CompareTo(new Version(1, 0, 0, "alpha")), Is.GreaterThan(0));
            Assert.That(new Version(1, 0, 0, "beta").CompareTo(new Version(1, 0, 0, "beta")), Is.Zero);
            Assert.That(new Version(1, 0, 0, "beta").CompareTo(new Version(1, 0, 0, "beta.1")), Is.LessThan(0));
            Assert.That(new Version(1, 0, 0, "beta.1").CompareTo(new Version(1, 0, 0, "beta")), Is.GreaterThan(0));
            Assert.That(new Version(1, 0, 0, "beta.1").CompareTo(new Version(1, 0, 0, "beta.1")), Is.Zero);

            // copied example from semver spec
            // 1.0.0-alpha < 1.0.0-alpha.1 < 1.0.0-alpha.beta < 1.0.0-beta < 1.0.0-beta.2 < 1.0.0-beta.11 < 1.0.0-rc.1 < 1.0.0
            Assert.That(new Version(1, 0, 0, "alpha").CompareTo(new Version(1, 0, 0, "alpha.1")), Is.LessThan(0));
            Assert.That(new Version(1, 0, 0, "alpha.1").CompareTo(new Version(1, 0, 0, "alpha.beta")), Is.LessThan(0));
            Assert.That(new Version(1, 0, 0, "alpha.beta").CompareTo(new Version(1, 0, 0, "beta")), Is.LessThan(0));
            Assert.That(new Version(1, 0, 0, "beta").CompareTo(new Version(1, 0, 0, "beta.2")), Is.LessThan(0));
            Assert.That(new Version(1, 0, 0, "beta.2").CompareTo(new Version(1, 0, 0, "beta.11")), Is.LessThan(0));
            Assert.That(new Version(1, 0, 0, "beta.11").CompareTo(new Version(1, 0, 0, "rc.1")), Is.LessThan(0));
            Assert.That(new Version(1, 0, 0, "rc.1").CompareTo(new Version(1, 0, 0)), Is.LessThan(0));
        }
    }
}
