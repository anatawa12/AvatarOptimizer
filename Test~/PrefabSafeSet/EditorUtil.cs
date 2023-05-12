using Anatawa12.AvatarOptimizer.PrefabSafeSet;
using NUnit.Framework;

namespace Anatawa12.AvatarOptimizer.Test.PrefabSafeSet
{
    public class EditorUtil
    {
        #region SetExistence

        [TestCase("mainSet", true, ElementStatus.Natural)]
        [TestCase("mainSet", false, ElementStatus.NewSlot)]
        [TestCase("notExists", true, ElementStatus.Natural)]
        [TestCase("notExists", false, ElementStatus.NewSlot)]
        public void SetExistencePrefab(string name, bool value, ElementStatus postStatus)
        {
            using (var scope = new PSSTestUtil.Scope())
            {
                var element = scope.PrefabEditorUtil.GetElementOf(name);
                element.SetExistence(value);
                Assert.That(element.Status, Is.EqualTo(postStatus));
            }
        }

        [TestCase("mainSet", true, ElementStatus.Natural)]
        [TestCase("mainSet", false, ElementStatus.Removed)]
        [TestCase("removedInInstance", true, ElementStatus.AddedTwice)]
        [TestCase("removedInInstance", false, ElementStatus.Removed)]
        [TestCase("addedInInstance", true, ElementStatus.NewElement)]
        [TestCase("addedInInstance", false, ElementStatus.FakeRemoved)]
        [TestCase("addedTwiceInInstance", true, ElementStatus.AddedTwice)]
        [TestCase("addedTwiceInInstance", false, ElementStatus.Removed)]
        [TestCase("fakeRemovedInInstance", true, ElementStatus.NewElement)]
        [TestCase("fakeRemovedInInstance", false, ElementStatus.FakeRemoved)]
        [TestCase("notExists", true, ElementStatus.NewElement)]
        [TestCase("notExists", false, ElementStatus.NewSlot)]
        public void SetExistenceInstance(string name, bool value, ElementStatus postStatus)
        {
            using (var scope = new PSSTestUtil.Scope())
            {
                var element = scope.InstanceEditorUtil.GetElementOf(name);
                element.SetExistence(value);
                Assert.That(element.Status, Is.EqualTo(postStatus));
            }
        }

        #endregion

        #region EnsureAdded

        [TestCase("mainSet", ElementStatus.Natural)]
        [TestCase("notExists", ElementStatus.Natural)]
        public void PrefabEnsureAdded(string name, ElementStatus postStatus)
        {
            using (var scope = new PSSTestUtil.Scope())
            {
                var element = scope.PrefabEditorUtil.GetElementOf(name);
                element.EnsureAdded();
                Assert.That(element.Status, Is.EqualTo(postStatus));
            }
        }

        [TestCase("mainSet", ElementStatus.Natural)]
        [TestCase("removedInInstance", ElementStatus.Natural)]
        [TestCase("addedInInstance", ElementStatus.NewElement)]
        [TestCase("addedTwiceInInstance", ElementStatus.AddedTwice)]
        [TestCase("fakeRemovedInInstance", ElementStatus.NewElement)]
        [TestCase("notExists", ElementStatus.NewElement)]
        public void InstanceEnsureAdded(string name, ElementStatus postStatus)
        {
            using (var scope = new PSSTestUtil.Scope())
            {
                var element = scope.InstanceEditorUtil.GetElementOf(name);
                element.EnsureAdded();
                Assert.That(element.Status, Is.EqualTo(postStatus));
            }
        }

        #endregion

        #region EnsureAdded

        [TestCase("mainSet", ElementStatus.Natural)]
        [TestCase("notExists", ElementStatus.Natural)]
        public void PrefabAdd(string name, ElementStatus postStatus)
        {
            using (var scope = new PSSTestUtil.Scope())
            {
                var element = scope.PrefabEditorUtil.GetElementOf(name);
                element.Add();
                Assert.That(element.Status, Is.EqualTo(postStatus));
            }
        }

        [TestCase("mainSet", ElementStatus.AddedTwice)]
        [TestCase("removedInInstance", ElementStatus.Natural)]
        [TestCase("addedInInstance", ElementStatus.NewElement)]
        [TestCase("addedTwiceInInstance", ElementStatus.AddedTwice)]
        [TestCase("fakeRemovedInInstance", ElementStatus.NewElement)]
        [TestCase("notExists", ElementStatus.NewElement)]
        public void InstanceAdd(string name, ElementStatus postStatus)
        {
            using (var scope = new PSSTestUtil.Scope())
            {
                var element = scope.InstanceEditorUtil.GetElementOf(name);
                element.Add();
                Assert.That(element.Status, Is.EqualTo(postStatus));
            }
        }

        #endregion

        #region EnsureRemoved

        [TestCase("mainSet", ElementStatus.NewSlot)]
        [TestCase("notExists", ElementStatus.NewSlot)]
        public void PrefabEnsureRemoved(string name, ElementStatus postStatus)
        {
            using (var scope = new PSSTestUtil.Scope())
            {
                var element = scope.PrefabEditorUtil.GetElementOf(name);
                element.EnsureRemoved();
                Assert.That(element.Status, Is.EqualTo(postStatus));
            }
        }

        [TestCase("mainSet", ElementStatus.Removed)]
        [TestCase("removedInInstance", ElementStatus.Removed)]
        [TestCase("addedInInstance", ElementStatus.NewSlot)]
        [TestCase("addedTwiceInInstance", ElementStatus.Removed)]
        [TestCase("fakeRemovedInInstance", ElementStatus.FakeRemoved)]
        [TestCase("notExists", ElementStatus.NewSlot)]
        public void InstanceEnsureRemoved(string name, ElementStatus postStatus)
        {
            using (var scope = new PSSTestUtil.Scope())
            {
                var element = scope.InstanceEditorUtil.GetElementOf(name);
                element.EnsureRemoved();
                Assert.That(element.Status, Is.EqualTo(postStatus));
            }
        }

        #endregion

        #region Remove

        [TestCase("mainSet", ElementStatus.NewSlot)]
        [TestCase("notExists", ElementStatus.NewSlot)]
        public void PrefabRemove(string name, ElementStatus postStatus)
        {
            using (var scope = new PSSTestUtil.Scope())
            {
                var element = scope.PrefabEditorUtil.GetElementOf(name);
                element.Remove();
                Assert.That(element.Status, Is.EqualTo(postStatus));
            }
        }

        [TestCase("mainSet", ElementStatus.Removed)]
        [TestCase("removedInInstance", ElementStatus.Removed)]
        [TestCase("addedInInstance", ElementStatus.NewSlot)]
        [TestCase("addedTwiceInInstance", ElementStatus.Removed)]
        [TestCase("fakeRemovedInInstance", ElementStatus.FakeRemoved)]
        [TestCase("notExists", ElementStatus.FakeRemoved)]
        public void InstanceRemove(string name, ElementStatus postStatus)
        {
            using (var scope = new PSSTestUtil.Scope())
            {
                var element = scope.InstanceEditorUtil.GetElementOf(name);
                element.Remove();
                Assert.That(element.Status, Is.EqualTo(postStatus));
            }
        }

        #endregion

        #region Elements

        [Test]
        public void PrefabElements()
        {
            using (var scope = new PSSTestUtil.Scope())
            {
                var elements = scope.PrefabEditorUtil.Elements;
                Assert.That(elements.Count, Is.EqualTo(5));

                Assert.That(elements[0].Value, Is.EqualTo("mainSet"));
                Assert.That(elements[0].Status, Is.EqualTo(ElementStatus.Natural));
                Assert.That(elements[0].Contains, Is.True);

                Assert.That(elements[1].Value, Is.EqualTo("addedTwiceInVariant"));
                Assert.That(elements[1].Status, Is.EqualTo(ElementStatus.Natural));
                Assert.That(elements[1].Contains, Is.True);

                Assert.That(elements[2].Value, Is.EqualTo("removedInVariant"));
                Assert.That(elements[2].Status, Is.EqualTo(ElementStatus.Natural));
                Assert.That(elements[2].Contains, Is.True);

                Assert.That(elements[3].Value, Is.EqualTo("addedTwiceInInstance"));
                Assert.That(elements[3].Status, Is.EqualTo(ElementStatus.Natural));
                Assert.That(elements[3].Contains, Is.True);

                Assert.That(elements[4].Value, Is.EqualTo("removedInInstance"));
                Assert.That(elements[4].Status, Is.EqualTo(ElementStatus.Natural));
                Assert.That(elements[4].Contains, Is.True);
            }
        }
        
        [Test]
        public void PrefabVariantElements()
        {
            using (var scope = new PSSTestUtil.Scope())
            {
                var elements = scope.VariantEditorUtil.Elements;
                Assert.That(elements.Count, Is.EqualTo(8));

                Assert.That(elements[0].Value, Is.EqualTo("mainSet"));
                Assert.That(elements[0].Status, Is.EqualTo(ElementStatus.Natural));
                Assert.That(elements[0].Contains, Is.True);

                Assert.That(elements[1].Value, Is.EqualTo("addedTwiceInVariant"));
                Assert.That(elements[1].Status, Is.EqualTo(ElementStatus.AddedTwice));
                Assert.That(elements[1].Contains, Is.True);

                Assert.That(elements[2].Value, Is.EqualTo("removedInVariant"));
                Assert.That(elements[2].Status, Is.EqualTo(ElementStatus.Removed));
                Assert.That(elements[2].Contains, Is.False);

                Assert.That(elements[3].Value, Is.EqualTo("addedTwiceInInstance"));
                Assert.That(elements[3].Status, Is.EqualTo(ElementStatus.Natural));
                Assert.That(elements[3].Contains, Is.True);

                Assert.That(elements[4].Value, Is.EqualTo("removedInInstance"));
                Assert.That(elements[4].Status, Is.EqualTo(ElementStatus.Natural));
                Assert.That(elements[4].Contains, Is.True);

                Assert.That(elements[5].Value, Is.EqualTo("addedInVariant"));
                Assert.That(elements[5].Status, Is.EqualTo(ElementStatus.NewElement));
                Assert.That(elements[5].Contains, Is.True);

                Assert.That(elements[6].Value, Is.EqualTo("addedInVariantRemovedInInstance"));
                Assert.That(elements[6].Status, Is.EqualTo(ElementStatus.NewElement));
                Assert.That(elements[6].Contains, Is.True);

                Assert.That(elements[7].Value, Is.EqualTo("fakeRemovedInVariant"));
                Assert.That(elements[7].Status, Is.EqualTo(ElementStatus.FakeRemoved));
                Assert.That(elements[7].Contains, Is.False);
            }
        }
        
        
        [Test]
        public void InstanceElements()
        {
            using (var scope = new PSSTestUtil.Scope())
            {
                var elements = scope.InstanceEditorUtil.Elements;
                Assert.That(elements.Count, Is.EqualTo(8));

                Assert.That(elements[0].Value, Is.EqualTo("mainSet"));
                Assert.That(elements[0].Status, Is.EqualTo(ElementStatus.Natural));
                Assert.That(elements[0].Contains, Is.True);

                Assert.That(elements[1].Value, Is.EqualTo("addedTwiceInVariant"));
                Assert.That(elements[1].Status, Is.EqualTo(ElementStatus.Natural));
                Assert.That(elements[1].Contains, Is.True);

                Assert.That(elements[2].Value, Is.EqualTo("addedTwiceInInstance"));
                Assert.That(elements[2].Status, Is.EqualTo(ElementStatus.AddedTwice));
                Assert.That(elements[2].Contains, Is.True);

                Assert.That(elements[3].Value, Is.EqualTo("removedInInstance"));
                Assert.That(elements[3].Status, Is.EqualTo(ElementStatus.Removed));
                Assert.That(elements[3].Contains, Is.False);

                Assert.That(elements[4].Value, Is.EqualTo("addedInVariant"));
                Assert.That(elements[4].Status, Is.EqualTo(ElementStatus.Natural));
                Assert.That(elements[4].Contains, Is.True);

                Assert.That(elements[5].Value, Is.EqualTo("addedInVariantRemovedInInstance"));
                Assert.That(elements[5].Status, Is.EqualTo(ElementStatus.Removed));
                Assert.That(elements[5].Contains, Is.False);

                Assert.That(elements[6].Value, Is.EqualTo("addedInInstance"));
                Assert.That(elements[6].Status, Is.EqualTo(ElementStatus.NewElement));
                Assert.That(elements[6].Contains, Is.True);

                Assert.That(elements[7].Value, Is.EqualTo("fakeRemovedInInstance"));
                Assert.That(elements[7].Status, Is.EqualTo(ElementStatus.FakeRemoved));
                Assert.That(elements[7].Contains, Is.False);
            }
        }

        #endregion
        
        #region Elements

        [Test]
        public void PrefabClear()
        {
            using (var scope = new PSSTestUtil.Scope())
            {
                scope.PrefabEditorUtil.Clear();
                Assert.That(scope.PrefabEditorUtil.Count, Is.EqualTo(0));
            }
        }
        
        [Test]
        public void VariantClear()
        {
            using (var scope = new PSSTestUtil.Scope())
            {
                scope.VariantEditorUtil.Clear();
                Assert.That(scope.VariantEditorUtil.Count, Is.EqualTo(0));
            }
        }
        
        
        [Test]
        public void InstanceClear()
        {
            using (var scope = new PSSTestUtil.Scope())
            {
                scope.InstanceEditorUtil.Clear();
                Assert.That(scope.InstanceEditorUtil.Count, Is.EqualTo(0));
            }
        }

        #endregion
    }
}
