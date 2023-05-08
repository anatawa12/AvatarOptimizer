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
            Assert.That(built.InstanceIdToComponent[child1ComponentId].Item3,
                Is.SameAs(child2Component));
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
