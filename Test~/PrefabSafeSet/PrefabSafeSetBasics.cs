using NUnit.Framework;

namespace Anatawa12.AvatarOptimizer.Test.PrefabSafeSet
{
    public class PrefabSafeSetBasics
    {
        private readonly string[] _prefabValues = {
            "mainSet", "addedTwiceInVariant", "removedInVariant", "addedTwiceInInstance", "removedInInstance",
        };

        private readonly string[] _variantValues = {
            "mainSet", "addedTwiceInVariant", "addedTwiceInInstance", "removedInInstance",
            "addedInVariant", "addedInVariantRemovedInInstance",
        };

        private readonly string[] _instanceValues = {
            "mainSet", "addedTwiceInVariant", "addedTwiceInInstance",
            "addedInVariant",
            "addedInInstance"
        };

        [Test]
        public void GetAsSet()
        {
            using (var scope = new PSSTestUtil.Scope())
            {
                Assert.That(scope.Prefab.stringSet.GetAsSet(), Is.EquivalentTo(_prefabValues));
                Assert.That(scope.Variant.stringSet.GetAsSet(), Is.EquivalentTo(_variantValues));
                Assert.That(scope.Instance.stringSet.GetAsSet(), Is.EquivalentTo(_instanceValues));
            }
        }
        
        [Test]
        public void GetAsList()
        {
            using (var scope = new PSSTestUtil.Scope())
            {
                Assert.That(scope.Prefab.stringSet.GetAsList(), Is.EqualTo(_prefabValues).AsCollection);
                Assert.That(scope.Variant.stringSet.GetAsList(), Is.EqualTo(_variantValues).AsCollection);
                Assert.That(scope.Instance.stringSet.GetAsList(), Is.EqualTo(_instanceValues).AsCollection);
            }
        }
    }
}
