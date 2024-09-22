using Anatawa12.AvatarOptimizer.CheckForUpdate;
using NUnit.Framework;

namespace Anatawa12.AvatarOptimizer.Test
{
    public class CheckForUpdateLatest2TextFile
    {
        [Test]
        public static void Parse()
        {
            Assert.That(Latest2TextFile.Parse("1.7.0:2019.4\n1.8.0:2022.3\n").LatestFor(new UnityVersion(2019, 4)), Is.EqualTo(new Version(1, 7, 0)));
            Assert.That(Latest2TextFile.Parse("1.7.0:2019.4\n1.8.0:2022.3\n").LatestFor(new UnityVersion(2022, 2)), Is.EqualTo(new Version(1, 7, 0)));
            Assert.That(Latest2TextFile.Parse("1.7.0:2019.4\n1.8.0:2022.3\n").LatestFor(new UnityVersion(2022, 3)), Is.EqualTo(new Version(1, 8, 0)));
        }
    }
}
