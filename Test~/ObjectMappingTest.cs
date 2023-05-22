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
            var child111 = Utils.NewGameObject("child111", child11.transform);
            var child2 = Utils.NewGameObject("child2", root.transform);

            var builder = new ObjectMappingBuilder(root);
            child11.transform.parent = child2.transform;

            var built = builder.BuildObjectMapping();

            var rootMapper = built.CreateAnimationMapper(root);
            Assert.That(
                rootMapper.MapBinding(B("child1/child11", typeof(GameObject), "m_Enabled")),
                Is.EqualTo(B("child2/child11", typeof(GameObject), "m_Enabled")));
            
            Assert.That(
                rootMapper.MapBinding(B("child1/child11/child111", typeof(GameObject), "m_Enabled")),
                Is.EqualTo(B("child2/child11/child111", typeof(GameObject), "m_Enabled")));
            
            var child1Mapper = built.CreateAnimationMapper(child1);
            Assert.That(
                child1Mapper.MapBinding(B("child11", typeof(GameObject), "m_Enabled")),
                Is.EqualTo(Default));
        }

        [Test]
        public void RecordRemoveGameObject()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child11 = Utils.NewGameObject("child11", child1.transform);
            var child111 = Utils.NewGameObject("child111", child11.transform);

            var builder = new ObjectMappingBuilder(root);
            Object.DestroyImmediate(child11);

            var built = builder.BuildObjectMapping();

            var rootMapper = built.CreateAnimationMapper(root);
            Assert.That(
                rootMapper.MapBinding(B("child1/child11", typeof(GameObject), "m_Enabled")),
                Is.EqualTo(Default));

            Assert.That(
                rootMapper.MapBinding(B("child1", typeof(GameObject), "m_Enabled")),
                Is.EqualTo(B("child1", typeof(GameObject), "m_Enabled")));

            Assert.That(
                rootMapper.MapBinding(B("child1/child11/child111", typeof(GameObject), "m_Enabled")),
                Is.EqualTo(Default));

            var child1Mapper = built.CreateAnimationMapper(child1);
            Assert.That(
                child1Mapper.MapBinding(B("child11", typeof(GameObject), "m_Enabled")),
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
            builder.RecordMergeComponent(child1Component, child2Component);
            Object.DestroyImmediate(child1Component);
            var child1ComponentId = child1Component.GetInstanceID();

            var built = builder.BuildObjectMapping();

            var rootMapper = built.CreateAnimationMapper(root);
            // should not affect to GameObject
            Assert.That(
                rootMapper.MapBinding(B("child1", typeof(GameObject), "m_Enabled")),
                Is.EqualTo(B("child1", typeof(GameObject), "m_Enabled")));
            
            // but should affect to component
            Assert.That(
                rootMapper.MapBinding(B("child1", typeof(SkinnedMeshRenderer), "blendShapes.test")),
                Is.EqualTo(B("child2", typeof(SkinnedMeshRenderer), "blendShapes.test")));

            // check for component replication
            Assert.That(built.MapComponentInstance(child1ComponentId, out var component), Is.True);
            Assert.That(component, Is.SameAs(child2Component));
        }

        [Test]
        public void RecordRemoveComponentTest()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child1Component = child1.AddComponent<SkinnedMeshRenderer>();

            var builder = new ObjectMappingBuilder(root);
            Object.DestroyImmediate(child1Component);
            var child1ComponentId = child1Component.GetInstanceID(); 

            var built = builder.BuildObjectMapping();

            var rootMapper = built.CreateAnimationMapper(root);

            // should not affect to GameObject itself
            Assert.That(
                rootMapper.MapBinding(B("child1", typeof(GameObject), "m_Enabled")),
                Is.EqualTo(B("child1", typeof(GameObject), "m_Enabled")));

            // but should affect to component
            Assert.That(
                rootMapper.MapBinding(B("child1", typeof(SkinnedMeshRenderer), "blendShapes.test")),
                Is.EqualTo(Default));

            // check for component replication
            Assert.That(built.MapComponentInstance(child1ComponentId, out var component), Is.True);
            Assert.That(component, Is.SameAs(null));
        }

        [Test]
        public void RecordMovePropertyTest()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child1Component = child1.AddComponent<SkinnedMeshRenderer>();

            var builder = new ObjectMappingBuilder(root);
            builder.RecordMoveProperty(child1Component, "blendShapes.test", "blendShapes.changed");

            var built = builder.BuildObjectMapping();

            var rootMapper = built.CreateAnimationMapper(root);

            // should not affect to other component
            Assert.That(
                rootMapper.MapBinding(B("child2", typeof(SkinnedMeshRenderer), "blendShapes.test")),
                Is.EqualTo(B("child2", typeof(SkinnedMeshRenderer), "blendShapes.test")));
            
            // but should affect to component
            Assert.That(
                rootMapper.MapBinding(B("child1", typeof(SkinnedMeshRenderer), "blendShapes.test")),
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

            var built = builder.BuildObjectMapping();

            var rootMapper = built.CreateAnimationMapper(root);

            // should not affect to other component
            Assert.That(
                rootMapper.MapBinding(B("child2", typeof(SkinnedMeshRenderer), "blendShapes.test")),
                Is.EqualTo(B("child2", typeof(SkinnedMeshRenderer), "blendShapes.test")));
            
            // but should affect to component
            Assert.That(
                rootMapper.MapBinding(B("child1", typeof(SkinnedMeshRenderer), "blendShapes.test")),
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
            builder.RecordMergeComponent(child1Component, child2Component);
            builder.RecordMoveProperty(child2Component, "blendShapes.moved", "blendShapes.movedChanged");

            var built = builder.BuildObjectMapping();
            
            var rootMapper = built.CreateAnimationMapper(root);

            Assert.That(
                rootMapper.MapBinding(B("child1", typeof(SkinnedMeshRenderer), "blendShapes.child1")),
                Is.EqualTo(B("child2", typeof(SkinnedMeshRenderer), "blendShapes.child1Changed")));
            Assert.That(
                rootMapper.MapBinding(B("child1", typeof(SkinnedMeshRenderer), "blendShapes.child2")),
                Is.EqualTo(B("child2", typeof(SkinnedMeshRenderer), "blendShapes.child2")));
            Assert.That(
                rootMapper.MapBinding(B("child1", typeof(SkinnedMeshRenderer), "blendShapes.child1Other")),
                Is.EqualTo(B("child2", typeof(SkinnedMeshRenderer), "blendShapes.child1Other")));
            Assert.That(
                rootMapper.MapBinding(B("child1", typeof(SkinnedMeshRenderer), "blendShapes.moved")),
                Is.EqualTo(B("child2", typeof(SkinnedMeshRenderer), "blendShapes.movedChanged")));

            Assert.That(
                rootMapper.MapBinding(B("child2", typeof(SkinnedMeshRenderer), "blendShapes.child1")),
                Is.EqualTo(B("child2", typeof(SkinnedMeshRenderer), "blendShapes.child1")));
            Assert.That(
                rootMapper.MapBinding(B("child2", typeof(SkinnedMeshRenderer), "blendShapes.child2")),
                Is.EqualTo(B("child2", typeof(SkinnedMeshRenderer), "blendShapes.child2Changed")));
            Assert.That(
                rootMapper.MapBinding(B("child2", typeof(SkinnedMeshRenderer), "blendShapes.child2Other")),
                Is.EqualTo(B("child2", typeof(SkinnedMeshRenderer), "blendShapes.child2Other")));
            Assert.That(
                rootMapper.MapBinding(B("child2", typeof(SkinnedMeshRenderer), "blendShapes.moved")),
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
            child11.transform.parent = child2.transform;
            builder.RecordMoveProperty(child11Component, "blendShapes.moved", "blendShapes.movedChanged");

            var built = builder.BuildObjectMapping();
            
            var rootMapper = built.CreateAnimationMapper(root);

            Assert.That(
                rootMapper.MapBinding(B("child1/child11", typeof(SkinnedMeshRenderer), "blendShapes.child11")),
                Is.EqualTo(B("child2/child11", typeof(SkinnedMeshRenderer), "blendShapes.child11Changed")));
            Assert.That(
                rootMapper.MapBinding(B("child1/child11", typeof(SkinnedMeshRenderer), "blendShapes.moved")),
                Is.EqualTo(B("child2/child11", typeof(SkinnedMeshRenderer), "blendShapes.movedChanged")));

            Assert.That(built.MapComponentInstance(child11Component.GetInstanceID(), out var component), Is.False);
        }


        private static (string, Type, string) B(string path, Type type, string prop) => (path, type, prop);
        private static (string, Type, string) Default = default;
    }

    static class ObjectMappingTestUtils
    {
        public static (string, Type, string) MapBinding(this AnimationObjectMapper mapping, (string, Type, string) binding)
        {
            var result = mapping.MapBinding(EditorCurveBinding.PPtrCurve(binding.Item1, binding.Item2, binding.Item3));

            return (result.path, result.type, result.propertyName);
        }
    }
}
