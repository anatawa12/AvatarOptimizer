using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Test
{
    public class ObjectMappingTest
    {
        [Test]
        public void MoveObjectTest()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child11 = Utils.NewGameObject("child11", child1.transform);
            var child2 = Utils.NewGameObject("child2", root.transform);

            var builder = new ObjectMappingBuilder(root);
            builder.RecordMoveObject(child11, child2);
            child11.transform.parent = child2.transform;

            var built = builder.BuildObjectMapping();

            Assert.That(
                built.MapPath("", B("child1/child11", typeof(GameObject), "m_Enabled")),
                Is.EqualTo(B("child2/child11", typeof(GameObject), "m_Enabled")));
            
            Assert.That(
                built.MapPath("", B("child1/child11/child111", typeof(GameObject), "m_Enabled")),
                Is.EqualTo(B("child2/child11/child111", typeof(GameObject), "m_Enabled")));
            
            Assert.That(
                built.MapPath("child1", B("child11", typeof(GameObject), "m_Enabled")),
                Is.EqualTo(Default));
        }

        [Test]
        public void RecordRemoveGameObject()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child11 = Utils.NewGameObject("child11", child1.transform);

            var builder = new ObjectMappingBuilder(root);
            builder.RecordRemoveGameObject(child11);
            Object.DestroyImmediate(child11);

            var built = builder.BuildObjectMapping();

            Assert.That(
                built.MapPath("", B("child1/child11", typeof(GameObject), "m_Enabled")),
                Is.EqualTo(Default));

            Assert.That(
                built.MapPath("", B("child1", typeof(GameObject), "m_Enabled")),
                Is.EqualTo(B("child1", typeof(GameObject), "m_Enabled")));

            Assert.That(
                built.MapPath("", B("child1/child11/child111", typeof(GameObject), "m_Enabled")),
                Is.EqualTo(Default));

            Assert.That(
                built.MapPath("child1", B("child11", typeof(GameObject), "m_Enabled")),
                Is.EqualTo(Default));
        }

        [Test]
        public void RecordMoveComponentTest()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child1Component = child1.AddComponent<SkinnedMeshRenderer>();
            var child2 = Utils.NewGameObject("child2", root.transform);
            var child2Component = child2.AddComponent<SkinnedMeshRenderer>();

            var builder = new ObjectMappingBuilder(root);
            builder.RecordMoveComponent(child1Component, child2);
            Object.DestroyImmediate(child1Component);
            var child1ComponentId = child1Component.GetInstanceID();

            var built = builder.BuildObjectMapping();

            // should not affect to GameObject
            Assert.That(
                built.MapPath("", B("child1", typeof(GameObject), "m_Enabled")),
                Is.EqualTo(B("child1", typeof(GameObject), "m_Enabled")));
            
            // but should affect to component
            Assert.That(
                built.MapPath("", B("child1", typeof(SkinnedMeshRenderer), "blendShapes.test")),
                Is.EqualTo(B("child2", typeof(SkinnedMeshRenderer), "blendShapes.test")));

            // check for component replication
            Assert.That(built.InstanceIdToComponent[child1ComponentId], Is.SameAs(child2Component));
        }

        [Test]
        public void RecordRemoveComponentTest()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child1Component = child1.AddComponent<SkinnedMeshRenderer>();

            var builder = new ObjectMappingBuilder(root);
            builder.RecordRemoveComponent(child1Component);
            Object.DestroyImmediate(child1Component);
            var child1ComponentId = child1Component.GetInstanceID(); 

            var built = builder.BuildObjectMapping();

            // should not affect to GameObject itself
            Assert.That(
                built.MapPath("", B("child1", typeof(GameObject), "m_Enabled")),
                Is.EqualTo(B("child1", typeof(GameObject), "m_Enabled")));

            // but should affect to component
            Assert.That(
                built.MapPath("", B("child1", typeof(SkinnedMeshRenderer), "blendShapes.test")),
                Is.EqualTo(Default));

            // check for component replication
            Assert.That(built.InstanceIdToComponent[child1ComponentId], Is.SameAs(null));
        }

        [Test]
        public void RecordMovePropertyTest()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child1Component = child1.AddComponent<SkinnedMeshRenderer>();

            var builder = new ObjectMappingBuilder(root);
            builder.RecordMoveProperty(child1Component, "blendShapes.test", "blendShapes.changed");
            Object.DestroyImmediate(child1Component);

            var built = builder.BuildObjectMapping();

            // should not affect to other component
            Assert.That(
                built.MapPath("", B("child2", typeof(SkinnedMeshRenderer), "blendShapes.test")),
                Is.EqualTo(B("child2", typeof(SkinnedMeshRenderer), "blendShapes.test")));
            
            // but should affect to component
            Assert.That(
                built.MapPath("", B("child1", typeof(SkinnedMeshRenderer), "blendShapes.test")),
                Is.EqualTo(B("child1", typeof(SkinnedMeshRenderer), "blendShapes.changed")));
        }

        [Test]
        public void RecordRemovePropertyTest()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child1Component = child1.AddComponent<SkinnedMeshRenderer>();

            var builder = new ObjectMappingBuilder(root);
            builder.RecordRemoveProperty(child1Component, "blendShapes.test");
            Object.DestroyImmediate(child1Component);

            var built = builder.BuildObjectMapping();

            // should not affect to other component
            Assert.That(
                built.MapPath("", B("child2", typeof(SkinnedMeshRenderer), "blendShapes.test")),
                Is.EqualTo(B("child2", typeof(SkinnedMeshRenderer), "blendShapes.test")));
            
            // but should affect to component
            Assert.That(
                built.MapPath("", B("child1", typeof(SkinnedMeshRenderer), "blendShapes.test")),
                Is.EqualTo(Default));
        }

        [Test]
        public void RecordMovePropertyThenComponentThenPropertyTest()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child2 = Utils.NewGameObject("child2", root.transform);
            var child1Component = child1.AddComponent<SkinnedMeshRenderer>();
            var child2Component = child2.AddComponent<SkinnedMeshRenderer>();

            var builder = new ObjectMappingBuilder(root);
            builder.RecordMoveProperty(child2Component, "blendShapes.child2", "blendShapes.child2Changed");
            builder.RecordMoveProperty(child1Component, "blendShapes.child1", "blendShapes.child1Changed");
            builder.RecordMoveComponent(child1Component, child2);
            builder.RecordMoveProperty(child2Component, "blendShapes.moved", "blendShapes.movedChanged");

            var built = builder.BuildObjectMapping();

            Assert.That(
                built.MapPath("", B("child1", typeof(SkinnedMeshRenderer), "blendShapes.child1")),
                Is.EqualTo(B("child2", typeof(SkinnedMeshRenderer), "blendShapes.child1Changed")));
            Assert.That(
                built.MapPath("", B("child1", typeof(SkinnedMeshRenderer), "blendShapes.child2")),
                Is.EqualTo(B("child2", typeof(SkinnedMeshRenderer), "blendShapes.child2")));
            Assert.That(
                built.MapPath("", B("child1", typeof(SkinnedMeshRenderer), "blendShapes.child1Other")),
                Is.EqualTo(B("child2", typeof(SkinnedMeshRenderer), "blendShapes.child1Other")));
            Assert.That(
                built.MapPath("", B("child1", typeof(SkinnedMeshRenderer), "blendShapes.moved")),
                Is.EqualTo(B("child2", typeof(SkinnedMeshRenderer), "blendShapes.movedChanged")));

            Assert.That(
                built.MapPath("", B("child2", typeof(SkinnedMeshRenderer), "blendShapes.child1")),
                Is.EqualTo(B("child2", typeof(SkinnedMeshRenderer), "blendShapes.child1")));
            Assert.That(
                built.MapPath("", B("child2", typeof(SkinnedMeshRenderer), "blendShapes.child2")),
                Is.EqualTo(B("child2", typeof(SkinnedMeshRenderer), "blendShapes.child2Changed")));
            Assert.That(
                built.MapPath("", B("child2", typeof(SkinnedMeshRenderer), "blendShapes.child2Other")),
                Is.EqualTo(B("child2", typeof(SkinnedMeshRenderer), "blendShapes.child2Other")));
            Assert.That(
                built.MapPath("", B("child2", typeof(SkinnedMeshRenderer), "blendShapes.moved")),
                Is.EqualTo(B("child2", typeof(SkinnedMeshRenderer), "blendShapes.movedChanged")));
        }

        [Test]
        public void RecordMovePropertyThenGameObjectThenPropertyTest()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child11 = Utils.NewGameObject("child11", child1.transform);
            var child11Component = child11.AddComponent<SkinnedMeshRenderer>();
            var child2 = Utils.NewGameObject("child2", root.transform);

            var builder = new ObjectMappingBuilder(root);
            builder.RecordMoveProperty(child11Component, "blendShapes.child11", "blendShapes.child11Changed");
            builder.RecordMoveObject(child11, child2);
            child11.transform.parent = child2.transform;
            builder.RecordMoveProperty(child11Component, "blendShapes.moved", "blendShapes.movedChanged");

            var built = builder.BuildObjectMapping();

            Assert.That(
                built.MapPath("", B("child1/child11", typeof(SkinnedMeshRenderer), "blendShapes.child11")),
                Is.EqualTo(B("child2/child11", typeof(SkinnedMeshRenderer), "blendShapes.child11Changed")));
            Assert.That(
                built.MapPath("", B("child1/child11", typeof(SkinnedMeshRenderer), "blendShapes.moved")),
                Is.EqualTo(B("child2/child11", typeof(SkinnedMeshRenderer), "blendShapes.movedChanged")));

            Assert.That(built.InstanceIdToComponent[child11Component.GetInstanceID()], Is.SameAs(child11Component));
        }


        private static (string, Type, string) B(string path, Type type, string prop) => (path, type, prop);
        private static (string, Type, string) Default = default;
    }

    static class ObjectMappingTestUtils
    {
        public static (string, Type, string) MapPath(this ObjectMapping mapping, string rootPath, (string, Type, string) binding)
        {
            var result = mapping.MapPath(rootPath,
                EditorCurveBinding.PPtrCurve(binding.Item1, binding.Item2, binding.Item3));

            return (result.path, result.type, result.propertyName);
        }
    }
}
