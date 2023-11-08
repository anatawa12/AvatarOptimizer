using System;
using System.Linq;
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
            builder.RecordMoveProperty(child111, "m_IsActive", "m_IsActive");

            var built = builder.BuildObjectMapping();

            var rootMapper = built.CreateAnimationMapper(root);
            Assert.That(
                rootMapper.MapBinding(B("child1/child11", typeof(GameObject), "m_IsActive")),
                Is.EqualTo(B("child2/child11", typeof(GameObject), "m_IsActive")));

            Assert.That(
                rootMapper.MapBinding(B("child1/child11/child111", typeof(GameObject), "m_IsActive")),
                Is.EqualTo(B("child2/child11/child111", typeof(GameObject), "m_IsActive")));
            
            var child1Mapper = built.CreateAnimationMapper(child1);
            ExAsset.MapBindingRemoved(child1Mapper, B("child11", typeof(GameObject), "m_IsActive"));
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
            ExAsset.MapBindingRemoved(rootMapper, B("child1/child11", typeof(GameObject), "m_Enabled"));

            ExAsset.MapBindingUnchanged(rootMapper, B("child1", typeof(GameObject), "m_Enabled"));

            ExAsset.MapBindingRemoved(rootMapper, B("child1/child11/child111", typeof(GameObject), "m_Enabled"));

            var child1Mapper = built.CreateAnimationMapper(child1);
            ExAsset.MapBindingRemoved(child1Mapper, B("child11", typeof(GameObject), "m_Enabled"));
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
            ExAsset.MapBindingUnchanged(rootMapper, B("child1", typeof(GameObject), "m_Enabled"));
            
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
            ExAsset.MapBindingUnchanged(rootMapper, B("child1", typeof(GameObject), "m_Enabled"));

            // but should affect to component
            ExAsset.MapBindingRemoved(rootMapper, B("child1", typeof(SkinnedMeshRenderer), "blendShapes.test"));

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
            ExAsset.MapBindingUnchanged(rootMapper, B("child2", typeof(SkinnedMeshRenderer), "blendShapes.test"));
            
            // but should affect to component
            Assert.That(
                rootMapper.MapBinding(B("child1", typeof(SkinnedMeshRenderer), "blendShapes.test")),
                Is.EqualTo(B("child1", typeof(SkinnedMeshRenderer), "blendShapes.changed")));
        }

        [Test]
        public void RecordSwapPropertyTest()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child1Component = child1.AddComponent<SkinnedMeshRenderer>();

            var builder = new ObjectMappingBuilder(root);
            builder.RecordMoveProperties(child1Component, 
                ("blendShapes.first", "blendShapes.second"),
                ("blendShapes.second", "blendShapes.first"));

            var built = builder.BuildObjectMapping();

            var rootMapper = built.CreateAnimationMapper(root);

            // but should affect to component
            Assert.That(
                rootMapper.MapBinding(B("child1", typeof(SkinnedMeshRenderer), "blendShapes.first")),
                Is.EqualTo(B("child1", typeof(SkinnedMeshRenderer), "blendShapes.second")));

            Assert.That(
                rootMapper.MapBinding(B("child1", typeof(SkinnedMeshRenderer), "blendShapes.second")),
                Is.EqualTo(B("child1", typeof(SkinnedMeshRenderer), "blendShapes.first")));
        }

        [Test]
        public void RecordMovePropertyTwiceTest()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child1Component = child1.AddComponent<SkinnedMeshRenderer>();

            var builder = new ObjectMappingBuilder(root);
            builder.RecordMoveProperty(child1Component, "blendShapes.test", "blendShapes.changed0");
            builder.RecordMoveProperty(child1Component, "blendShapes.changed0", "blendShapes.changed");

            var built = builder.BuildObjectMapping();

            var rootMapper = built.CreateAnimationMapper(root);

            Assert.That(
                rootMapper.MapBinding(B("child1", typeof(SkinnedMeshRenderer), "blendShapes.test")),
                Is.EqualTo(B("child1", typeof(SkinnedMeshRenderer), "blendShapes.changed")));
            Assert.That(
                rootMapper.MapBinding(B("child1", typeof(SkinnedMeshRenderer), "blendShapes.changed0")),
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
            ExAsset.MapBindingUnchanged(rootMapper, B("child2", typeof(SkinnedMeshRenderer), "blendShapes.test"));
            
            // but should affect to component
            ExAsset.MapBindingRemoved(rootMapper, B("child1", typeof(SkinnedMeshRenderer), "blendShapes.test"));
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

            ExAsset.MapBindingUnchanged(rootMapper, B("child2", typeof(SkinnedMeshRenderer), "blendShapes.child1"));
            Assert.That(
                rootMapper.MapBinding(B("child2", typeof(SkinnedMeshRenderer), "blendShapes.child2")),
                Is.EqualTo(B("child2", typeof(SkinnedMeshRenderer), "blendShapes.child2Changed")));
            ExAsset.MapBindingUnchanged(rootMapper, B("child2", typeof(SkinnedMeshRenderer), "blendShapes.child2Other"));
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

        [Test]
        public void RecordRemovePropertyThenMergeComponent()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child1Component = child1.AddComponent<SkinnedMeshRenderer>();
            var child2 = Utils.NewGameObject("child2", root.transform);
            var child2Component = child2.AddComponent<SkinnedMeshRenderer>();

            var builder = new ObjectMappingBuilder(root);
            builder.RecordRemoveProperty(child1Component, "m_Enabled");
            builder.RecordMergeComponent(child1Component, child2Component);
            Object.DestroyImmediate(child1Component);
            var child1ComponentId = child1Component.GetInstanceID();

            var built = builder.BuildObjectMapping();

            var rootMapper = built.CreateAnimationMapper(root);

            ExAsset.MapBindingRemoved(rootMapper, B("child1", typeof(SkinnedMeshRenderer), "m_Enabled"));
            ExAsset.MapBindingUnchanged(rootMapper, B("child2", typeof(SkinnedMeshRenderer), "m_Enabled"));

            // check for component replication
            Assert.That(built.MapComponentInstance(child1ComponentId, out var component), Is.True);
            Assert.That(component, Is.SameAs(child2Component));
        }

        [Test]
        public void RecordMoveProperty()
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

        [Test]
        public void MovePropertyOfGameObject()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child11 = Utils.NewGameObject("child11", child1.transform);

            var builder = new ObjectMappingBuilder(root);
            builder.RecordMoveProperty(child11, "m_IsActive", child1, "m_IsActive");

            var built = builder.BuildObjectMapping();
            
            var rootMapper = built.CreateAnimationMapper(root);

            Assert.That(
                rootMapper.MapBinding(B("child1/child11", typeof(GameObject), "m_IsActive")),
                Is.EqualTo(B("child1", typeof(GameObject), "m_IsActive")));
        }


        [Test]
        public void CopyProperty()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child11 = Utils.NewGameObject("child11", child1.transform);
            var child12 = Utils.NewGameObject("child12", child1.transform);
            var child13 = Utils.NewGameObject("child13", child1.transform);
            var child14 = Utils.NewGameObject("child14", child1.transform);

            var builder = new ObjectMappingBuilder(root);
            builder.RecordCopyProperty(child11, "m_IsActive",
                child12, "m_IsActive");
            builder.RecordCopyProperty(child11, "m_IsActive",
                child13, "m_IsActive");
            builder.RecordCopyProperty(child12, "m_IsActive",
                child14, "m_IsActive");

            var built = builder.BuildObjectMapping();

            var rootMapper = built.CreateAnimationMapper(root);

            Assert.That(
                rootMapper.MapBinding("child1/child11", typeof(GameObject), "m_IsActive"),
                Is.Not.Null.And.EquivalentTo(new[]
                {
                    B("child1/child11", typeof(GameObject), "m_IsActive"),
                    B("child1/child12", typeof(GameObject), "m_IsActive"),
                    B("child1/child13", typeof(GameObject), "m_IsActive"),
                    B("child1/child14", typeof(GameObject), "m_IsActive"),
                }));

            Assert.That(
                rootMapper.MapBinding("child1/child12", typeof(GameObject), "m_IsActive"),
                Is.Not.Null.And.EquivalentTo(new[]
                {
                    B("child1/child12", typeof(GameObject), "m_IsActive"),
                    B("child1/child14", typeof(GameObject), "m_IsActive"),
                }));
        }

        private static (string, Type, string) B(string path, Type type, string prop) => (path, type, prop);

        private static (string, Type, string) ToTuple(EditorCurveBinding binding) =>
            (binding.path, binding.type, binding.propertyName);

        private static EditorCurveBinding Curve(string path, Type type, string prop)
            => EditorCurveBinding.PPtrCurve(path, type, prop);
    }

    static class ExAsset
    {
        public static void MapBindingRemoved(AnimationObjectMapper mapping, (string, Type, string) binding)
        {
            var result = mapping.MapBinding(binding.Item1, binding.Item2, binding.Item3);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(0));
        }

        public static void MapBindingUnchanged(AnimationObjectMapper mapping, (string, Type, string) binding)
        {
            var result = mapping.MapBinding(binding.Item1, binding.Item2, binding.Item3);

            Assert.That(result, Is.Null);
        }

        public static (string, Type, string) MapBinding(this AnimationObjectMapper mapping, (string, Type, string) binding)
        {
            var result = mapping.MapBinding(binding.Item1, binding.Item2, binding.Item3);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(1));

            return (result[0].path, result[0].type, result[0].propertyName);
        }
    }
}
