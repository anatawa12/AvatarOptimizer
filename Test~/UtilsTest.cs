using System.Linq;
using NUnit.Framework;

namespace Anatawa12.AvatarOptimizer.Test
{
    public class UtilsTest
    {
        #region FindSubProps

        [Test]
        public void FindSubProps()
        {
            Assert.That(Utils.FindSubProps("").ToList(), Is.EquivalentTo(new [] { ("", "") }));
            Assert.That(Utils.FindSubProps("test").ToList(), 
                Is.EquivalentTo(new [] { ("test", "") }));

            Assert.That(Utils.FindSubProps("test.collection").ToList(), Is.EquivalentTo(new []
            {
                ("test.collection", ""),
                ("test", ".collection"),
            }));
            
            Assert.That(Utils.FindSubProps("test.collection.sub").ToList(), Is.EquivalentTo(new []
            {
                ("test.collection.sub", ""),
                ("test.collection", ".sub"),
                ("test", ".collection.sub"),
            }));
        }

        #endregion
    }
}
