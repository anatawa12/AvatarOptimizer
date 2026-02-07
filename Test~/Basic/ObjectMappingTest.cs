using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Test
{
    struct DummyPropInfo : IPropertyInfo<DummyPropInfo>
    {
        public void MergeTo(ref DummyPropInfo property)
        {
        }

        public void CopyTo(ref DummyPropInfo property)
        {
        }
    }

    public class ObjectMappingTest
    {
        [TestCase("")]
        [TestCase("child1")]
        [TestCase("child1/child11")]
        //[TestCase("child2/child21")] AnimationUtility.GetAnimatedObject is broken
        [TestCase("child2/child21/child211")]
        //[TestCase("child2/child21/inWithPathOnly")] AnimationUtility.GetAnimatedObject is broken
        [TestCase("child2/child21/child211-2")]
        //[TestCase("child2/child21-2/inWithPathOnly-2-21-2")] AnimationUtility.GetAnimatedObject is broken
        [Test]
        public void PathResolution(string testPath)
        {
            var root = Utils.NewGameObject("root", new[]
            {
                Utils.NewGameObject("child1", new[]
                {
                    Utils.NewGameObject("child11"),
                }),
                Utils.NewGameObject("child2/child21", new []
                {
                    Utils.NewGameObject("inWithPathOnly"),
                }),
                Utils.NewGameObject("child2/child21-2", new []
                {
                    Utils.NewGameObject("inWithPathOnly-2-21-2"),
                }),
                Utils.NewGameObject("child2", new[]
                {
                    Utils.NewGameObject("child21", new[]
                    {
                        Utils.NewGameObject("child211"),
                    }),
                }),
                Utils.NewGameObject("child2", new[]
                {
                    Utils.NewGameObject("child21", new[]
                    {
                        Utils.NewGameObject("child211-2"),
                    }),
                }),
            });

            var builder = new ObjectMappingBuilder<DummyPropInfo>(root).BuildObjectMapping().GetBeforeGameObjectTree(root);

            var transform = (Transform) AnimationUtility.GetAnimatedObject(root,
                EditorCurveBinding.FloatCurve(testPath, typeof(Transform), "m_LocalPosition.x"));
            var resolvedGameObjectId = builder.ResolvePath(testPath)?.InstanceId;
            var expectedGameObjectId = transform ? transform.gameObject.GetInstanceID() : (int?)null;

            var resolvedName = resolvedGameObjectId is int id ? EditorUtility.InstanceIDToObject(id).name: "null";
            Assert.That(resolvedGameObjectId, Is.EqualTo(expectedGameObjectId),
                $"Expected {(transform ? transform.name : "null")} but was {resolvedName}");
        }

        [Test]
        public void ValidPathResolutionWithSlash()
        {
            var root = Utils.NewGameObject("root");
            var childWithSlash = Utils.NewGameObject("child/with/slash", root.transform);
            var son = Utils.NewGameObject("son", childWithSlash.transform);

            var builder = new ObjectMappingBuilder<DummyPropInfo>(root).BuildObjectMapping()
                .GetBeforeGameObjectTree(root);

            Assert.That(builder.ResolvePath("child/with/slash")?.InstanceId,
                Is.EqualTo(childWithSlash.GetInstanceID()));
            Assert.That(builder.ResolvePath("child/with/slash/son")?.InstanceId, Is.EqualTo(son.GetInstanceID()));

            Assert.That(Utils.ResolveAnimationPath(root.transform, "child/with/slash")?.GetInstanceID(),
                Is.EqualTo(childWithSlash.transform.GetInstanceID()));
            Assert.That(Utils.ResolveAnimationPath(root.transform, "child/with/slash/son")?.GetInstanceID(),
                Is.EqualTo(son.transform.GetInstanceID()));

            // tests for Unity's problem
            Assert.That(root.transform.Find("child/with/slash"), Is.Null);
            Assert.That(root.transform.Find("child/with/slash/son"), Is.Null);

            Assert.That(AnimationUtility.GetAnimatedObject(root, MakeBinding("child/with/slash")), Is.Null);
            Assert.That(AnimationUtility.GetAnimatedObject(root, MakeBinding("child/with/slash/son")), Is.Null);

            EditorCurveBinding MakeBinding(string path) =>
                EditorCurveBinding.FloatCurve(path, typeof(GameObject), "m_IsActive");
        }

        [UnityTest]
        public IEnumerator ValidPathResolutionWithSlashRuntimeBehavior()
        {
            // Tests unity Animator behavior in play mode
            yield return new EnterPlayMode();

            var root = Utils.NewGameObject("root");
            var childWithSlash = Utils.NewGameObject("child/with/slash", root.transform);
            var son = Utils.NewGameObject("son", childWithSlash.transform);

            var animator = root.AddComponent<Animator>();

            // check for childWithSlash
            Assert.That(childWithSlash.activeSelf, Is.True);
            var controller = NewController(MakeBinding("child/with/slash"), 0f);
            animator.runtimeAnimatorController = controller;
            yield return null;
            Assert.That(childWithSlash.activeSelf, Is.False);

            // check for son
            Assert.That(son.activeSelf, Is.True);
            controller = NewController(MakeBinding("child/with/slash/son"), 0f);
            animator.runtimeAnimatorController = controller;
            yield return null;
            Assert.That(son.activeSelf, Is.False);

            yield return new ExitPlayMode();

            yield break;

            EditorCurveBinding MakeBinding(string path) =>
                EditorCurveBinding.FloatCurve(path, typeof(GameObject), "m_IsActive");

            AnimatorController NewController(EditorCurveBinding binding, float value)
            {
                var controller = new AnimatorController();
                controller.AddLayer("Base Layer");
                var stateMachine = controller.layers[0].stateMachine;
                var state = stateMachine.AddState("State");
                var clip = new AnimationClip();
                clip.SetCurve(binding.path, binding.type, binding.propertyName,
                    AnimationCurve.Constant(0, 1, value));
                state.motion = clip;
                return controller;
            }
        }

        [Test]
        public void ConflictingPathWithSlash()
        {
            var root = Utils.NewGameObject("root");
            var firstSon = Utils.NewGameObject("child/with/slash/son", root.transform);
            var childWithSlash = Utils.NewGameObject("child/with/slash", root.transform);
            var son = Utils.NewGameObject("son", childWithSlash.transform);

            var builder = new ObjectMappingBuilder<DummyPropInfo>(root).BuildObjectMapping().GetBeforeGameObjectTree(root);

            Assert.That(builder.ResolvePath("child/with/slash")?.InstanceId, Is.EqualTo(childWithSlash.GetInstanceID()));
            Assert.That(builder.ResolvePath("child/with/slash/son")?.InstanceId, Is.EqualTo(firstSon.GetInstanceID()));

            Assert.That(Utils.ResolveAnimationPath(root.transform, "child/with/slash")?.GetInstanceID(), Is.EqualTo(childWithSlash.transform.GetInstanceID()));
            Assert.That(Utils.ResolveAnimationPath(root.transform, "child/with/slash/son")?.GetInstanceID(), Is.EqualTo(firstSon.transform.GetInstanceID()));

            // tests for Unity's problem
            Assert.That(root.transform.Find("child/with/slash"), Is.Null);
            Assert.That(root.transform.Find("child/with/slash/son"), Is.Null);

            Assert.That(AnimationUtility.GetAnimatedObject(root, MakeBinding("child/with/slash")), Is.Null);
            Assert.That(AnimationUtility.GetAnimatedObject(root, MakeBinding("child/with/slash/son")), Is.Null);

            EditorCurveBinding MakeBinding(string path) =>
                EditorCurveBinding.FloatCurve(path, typeof(GameObject), "m_IsActive");
        }

        [UnityTest]
        public IEnumerator ConflictingPathWithSlashRuntimeBehavior()
        {
            // Tests unity Animator behavior in play mode
            yield return new EnterPlayMode();

            var root = Utils.NewGameObject("root");
            var firstSon = Utils.NewGameObject("child/with/slash/son", root.transform);
            var childWithSlash = Utils.NewGameObject("child/with/slash", root.transform);
            var son = Utils.NewGameObject("son", childWithSlash.transform);

            var animator = root.AddComponent<Animator>();

            // check for childWithSlash
            Assert.That(childWithSlash.activeSelf, Is.True);
            var controller = NewController(MakeBinding("child/with/slash"), 0f);
            animator.runtimeAnimatorController = controller;
            yield return null;
            Assert.That(childWithSlash.activeSelf, Is.False);

            // check for son
            Assert.That(firstSon.activeSelf, Is.True);
            controller = NewController(MakeBinding("child/with/slash/son"), 0f);
            animator.runtimeAnimatorController = controller;
            yield return null;
            Assert.That(firstSon.activeSelf, Is.False);

            yield return new ExitPlayMode();

            yield break;

            EditorCurveBinding MakeBinding(string path) =>
                EditorCurveBinding.FloatCurve(path, typeof(GameObject), "m_IsActive");

            AnimatorController NewController(EditorCurveBinding binding, float value)
            {
                var controller = new AnimatorController();
                controller.AddLayer("Base Layer");
                var stateMachine = controller.layers[0].stateMachine;
                var state = stateMachine.AddState("State");
                var clip = new AnimationClip();
                clip.SetCurve(binding.path, binding.type, binding.propertyName,
                    AnimationCurve.Constant(0, 1, value));
                state.motion = clip;
                return controller;
            }
        }

        [Test]
        public void MoveObjectTest()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child11 = Utils.NewGameObject("child11", child1.transform);
            var child111 = Utils.NewGameObject("child111", child11.transform);
            var child2 = Utils.NewGameObject("child2", root.transform);

            var builder = new ObjectMappingBuilder<DummyPropInfo>(root);
            child11.transform.parent = child2.transform;
            builder.RecordMoveProperty(child111, Props.IsActive, Props.IsActive);

            var built = builder.BuildObjectMapping();

            var rootMapper = built.CreateAnimationMapper(root);
            Assert.That(
                rootMapper.MapBinding("child1/child11", typeof(GameObject), Props.IsActive),
                Is.EquivalentTo(new[]{B("child2/child11", typeof(GameObject), Props.IsActive, 0)}));

            Assert.That(
                rootMapper.MapBinding("child1/child11/child111", typeof(GameObject), Props.IsActive),
                Is.EquivalentTo(new[]{B("child2/child11/child111", typeof(GameObject), Props.IsActive, 0)}));
            
            var child1Mapper = built.CreateAnimationMapper(child1);
            Assert.That(
                child1Mapper.MapBinding("child11", typeof(GameObject), Props.IsActive),
                Is.Empty);
        }

        [Test]
        public void RecordRemoveGameObject()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child11 = Utils.NewGameObject("child11", child1.transform);
            var child111 = Utils.NewGameObject("child111", child11.transform);

            var builder = new ObjectMappingBuilder<DummyPropInfo>(root);
            Object.DestroyImmediate(child11);

            var built = builder.BuildObjectMapping();

            var rootMapper = built.CreateAnimationMapper(root);
            Assert.That(rootMapper.MapBinding("child1/child11", typeof(GameObject), Props.IsActive),
                Is.Empty);

            Assert.That(rootMapper.MapBinding("child1", typeof(GameObject), Props.IsActive),
                Is.Null);

            Assert.That(rootMapper.MapBinding("child1/child11/child111", typeof(GameObject), Props.IsActive),
                Is.Empty);

            var child1Mapper = built.CreateAnimationMapper(child1);
            Assert.That(child1Mapper.MapBinding("child11", typeof(GameObject), Props.IsActive),
                Is.Empty);
        }

        [Test]
        public void RecordMoveComponentTest()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child1Component = child1.AddComponent<SkinnedMeshRenderer>();
            var child2 = Utils.NewGameObject("child2", root.transform);
            var child2Component = child2.AddComponent<SkinnedMeshRenderer>();

            var builder = new ObjectMappingBuilder<DummyPropInfo>(root);
            builder.RecordMergeComponent(child1Component, child2Component);
            Object.DestroyImmediate(child1Component);
            var child1ComponentId = child1Component.GetInstanceID();

            var built = builder.BuildObjectMapping();

            var rootMapper = built.CreateAnimationMapper(root);
            // should not affect to GameObject

            Assert.That(rootMapper.MapBinding("child1", typeof(GameObject), Props.IsActive),
                Is.Null);
            
            // but should affect to component
            Assert.That(
                rootMapper.MapBinding("child1", typeof(SkinnedMeshRenderer), "blendShapes.test"),
                Is.EquivalentTo(new[]{B("child2", typeof(SkinnedMeshRenderer), "blendShapes.test", 0)}));

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

            var builder = new ObjectMappingBuilder<DummyPropInfo>(root);
            Object.DestroyImmediate(child1Component);
            var child1ComponentId = child1Component.GetInstanceID(); 

            var built = builder.BuildObjectMapping();

            var rootMapper = built.CreateAnimationMapper(root);

            // should not affect to GameObject itself
            Assert.That(rootMapper.MapBinding("child1", typeof(GameObject), Props.IsActive),
                Is.Null);

            // but should affect to component
            Assert.That(rootMapper.MapBinding("child1", typeof(SkinnedMeshRenderer), "blendShapes.test"),
                Is.Empty);

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
            var child2 = Utils.NewGameObject("child2", root.transform);
            var child2Component = child2.AddComponent<SkinnedMeshRenderer>();

            var builder = new ObjectMappingBuilder<DummyPropInfo>(root);
            builder.RecordMoveProperty(child1Component, "blendShapes.test", "blendShapes.changed");

            var built = builder.BuildObjectMapping();

            var rootMapper = built.CreateAnimationMapper(root);

            // should not affect to other component
            Assert.That(rootMapper.MapBinding("child2", typeof(SkinnedMeshRenderer), "blendShapes.test"),
                Is.Null);
            
            // but should affect to component
            Assert.That(
                rootMapper.MapBinding("child1", typeof(SkinnedMeshRenderer), "blendShapes.test"),
                Is.EquivalentTo(new[]{B("child1", typeof(SkinnedMeshRenderer), "blendShapes.changed", 0)}));
        }

        [Test]
        public void RecordSwapPropertyTest()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child1Component = child1.AddComponent<SkinnedMeshRenderer>();

            var builder = new ObjectMappingBuilder<DummyPropInfo>(root);
            builder.RecordMoveProperties(child1Component, 
                ("blendShapes.first", "blendShapes.second"),
                ("blendShapes.second", "blendShapes.first"));

            var built = builder.BuildObjectMapping();

            var rootMapper = built.CreateAnimationMapper(root);

            // but should affect to component
            Assert.That(
                rootMapper.MapBinding("child1", typeof(SkinnedMeshRenderer), "blendShapes.first"),
                Is.EquivalentTo(new[]{B("child1", typeof(SkinnedMeshRenderer), "blendShapes.second", 0)}));

            Assert.That(
                rootMapper.MapBinding("child1", typeof(SkinnedMeshRenderer), "blendShapes.second"),
                Is.EquivalentTo(new[]{B("child1", typeof(SkinnedMeshRenderer), "blendShapes.first", 0)}));
        }

        [Test]
        public void RecordMovePropertyTwiceTest()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child1Component = child1.AddComponent<SkinnedMeshRenderer>();

            var builder = new ObjectMappingBuilder<DummyPropInfo>(root);
            builder.RecordMoveProperty(child1Component, "blendShapes.test", "blendShapes.changed0");
            builder.RecordMoveProperty(child1Component, "blendShapes.changed0", "blendShapes.changed");

            var built = builder.BuildObjectMapping();

            var rootMapper = built.CreateAnimationMapper(root);

            Assert.That(
                rootMapper.MapBinding("child1", typeof(SkinnedMeshRenderer), "blendShapes.test"),
                Is.EquivalentTo(new[]{B("child1", typeof(SkinnedMeshRenderer), "blendShapes.changed", 0)}));
            Assert.That(
                rootMapper.MapBinding("child1", typeof(SkinnedMeshRenderer), "blendShapes.changed0"),
                Is.EquivalentTo(new[]{B("child1", typeof(SkinnedMeshRenderer), "blendShapes.changed", 0)}));
        }

        [Test]
        public void RecordRemovePropertyTest()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child1Component = child1.AddComponent<SkinnedMeshRenderer>();
            var child2 = Utils.NewGameObject("child2", root.transform);
            var child2Component = child2.AddComponent<SkinnedMeshRenderer>();

            var builder = new ObjectMappingBuilder<DummyPropInfo>(root);
            builder.RecordRemoveProperty(child1Component, "blendShapes.test");

            var built = builder.BuildObjectMapping();

            var rootMapper = built.CreateAnimationMapper(root);

            // should not affect to other component
            Assert.That(rootMapper.MapBinding("child2", typeof(SkinnedMeshRenderer), "blendShapes.test"),
                Is.Null);
            
            // but should affect to component
            Assert.That(rootMapper.MapBinding("child1", typeof(SkinnedMeshRenderer), "blendShapes.test"),
                Is.Empty);
        }

        [Test]
        public void RecordMovePropertyThenComponentThenPropertyTest()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child2 = Utils.NewGameObject("child2", root.transform);
            var child1Component = child1.AddComponent<SkinnedMeshRenderer>();
            var child2Component = child2.AddComponent<SkinnedMeshRenderer>();

            var builder = new ObjectMappingBuilder<DummyPropInfo>(root);
            builder.RecordMoveProperty(child2Component, "blendShapes.child2", "blendShapes.child2Changed");
            builder.RecordMoveProperty(child1Component, "blendShapes.child1", "blendShapes.child1Changed");
            builder.RecordMergeComponent(child1Component, child2Component);
            builder.RecordMoveProperty(child2Component, "blendShapes.moved", "blendShapes.movedChanged");

            var built = builder.BuildObjectMapping();

            var rootMapper = built.CreateAnimationMapper(root);

            Assert.That(
                rootMapper.MapBinding("child1", typeof(SkinnedMeshRenderer), "blendShapes.child1"),
                Is.EquivalentTo(new[] { B("child2", typeof(SkinnedMeshRenderer), "blendShapes.child1Changed", 0) }));
            Assert.That(
                rootMapper.MapBinding("child1", typeof(SkinnedMeshRenderer), "blendShapes.child2"),
                Is.EquivalentTo(new[] { B("child2", typeof(SkinnedMeshRenderer), "blendShapes.child2", 0) }));
            Assert.That(
                rootMapper.MapBinding("child1", typeof(SkinnedMeshRenderer), "blendShapes.child1Other"),
                Is.EquivalentTo(new[] { B("child2", typeof(SkinnedMeshRenderer), "blendShapes.child1Other", 0) }));
            Assert.That(
                rootMapper.MapBinding("child1", typeof(SkinnedMeshRenderer), "blendShapes.moved"),
                Is.EquivalentTo(new[] { B("child2", typeof(SkinnedMeshRenderer), "blendShapes.movedChanged", 0) }));

            Assert.That(rootMapper.MapBinding("child2", typeof(SkinnedMeshRenderer), "blendShapes.child1"),
                Is.Null);
            Assert.That(
                rootMapper.MapBinding("child2", typeof(SkinnedMeshRenderer), "blendShapes.child2"),
                Is.EquivalentTo(new[] { B("child2", typeof(SkinnedMeshRenderer), "blendShapes.child2Changed", 0) }));

            Assert.That(rootMapper.MapBinding("child2", typeof(SkinnedMeshRenderer), "blendShapes.child2Other"),
                Is.Null);
            Assert.That(
                rootMapper.MapBinding("child2", typeof(SkinnedMeshRenderer), "blendShapes.moved"),
                Is.EquivalentTo(new[] { B("child2", typeof(SkinnedMeshRenderer), "blendShapes.movedChanged", 0) }));
        }

        [Test]
        public void RecordMovePropertyThenGameObjectThenPropertyTest()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child11 = Utils.NewGameObject("child11", child1.transform);
            var child11Component = child11.AddComponent<SkinnedMeshRenderer>();
            var child2 = Utils.NewGameObject("child2", root.transform);

            var builder = new ObjectMappingBuilder<DummyPropInfo>(root);
            builder.RecordMoveProperty(child11Component, "blendShapes.child11", "blendShapes.child11Changed");
            child11.transform.parent = child2.transform;
            builder.RecordMoveProperty(child11Component, "blendShapes.moved", "blendShapes.movedChanged");

            var built = builder.BuildObjectMapping();
            
            var rootMapper = built.CreateAnimationMapper(root);

            Assert.That(
                rootMapper.MapBinding("child1/child11", typeof(SkinnedMeshRenderer), "blendShapes.child11"),
                Is.EquivalentTo(new[]{B("child2/child11", typeof(SkinnedMeshRenderer), "blendShapes.child11Changed", 0)}));
            Assert.That(
                rootMapper.MapBinding("child1/child11", typeof(SkinnedMeshRenderer), "blendShapes.moved"),
                Is.EquivalentTo(new[]{B("child2/child11", typeof(SkinnedMeshRenderer), "blendShapes.movedChanged", 0)}));

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

            var builder = new ObjectMappingBuilder<DummyPropInfo>(root);
            builder.RecordRemoveProperty(child1Component, Props.EnabledFor(child1Component));
            builder.RecordMergeComponent(child1Component, child2Component);
            Object.DestroyImmediate(child1Component);
            var child1ComponentId = child1Component.GetInstanceID();

            var built = builder.BuildObjectMapping();

            var rootMapper = built.CreateAnimationMapper(root);

            Assert.That(rootMapper.MapBinding("child1", typeof(SkinnedMeshRenderer), Props.EnabledFor(typeof(SkinnedMeshRenderer))),
                Is.Empty);

            Assert.That(rootMapper.MapBinding("child2", typeof(SkinnedMeshRenderer), Props.EnabledFor(typeof(SkinnedMeshRenderer))),
                Is.Null);

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

            var builder = new ObjectMappingBuilder<DummyPropInfo>(root);
            builder.RecordMoveProperty(child11Component, "blendShapes.child11", "blendShapes.child11Changed");
            child11.transform.parent = child2.transform;
            builder.RecordMoveProperty(child11Component, "blendShapes.moved", "blendShapes.movedChanged");

            var built = builder.BuildObjectMapping();
            
            var rootMapper = built.CreateAnimationMapper(root);

            Assert.That(
                rootMapper.MapBinding("child1/child11", typeof(SkinnedMeshRenderer), "blendShapes.child11"),
                Is.EquivalentTo(new[]{B("child2/child11", typeof(SkinnedMeshRenderer), "blendShapes.child11Changed", 0)}));
            Assert.That(
                rootMapper.MapBinding("child1/child11", typeof(SkinnedMeshRenderer), "blendShapes.moved"),
                Is.EquivalentTo(new[]{B("child2/child11", typeof(SkinnedMeshRenderer), "blendShapes.movedChanged", 0)}));

            Assert.That(built.MapComponentInstance(child11Component.GetInstanceID(), out var component), Is.False);
        }

        [Test]
        public void MovePropertyOfGameObject()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child11 = Utils.NewGameObject("child11", child1.transform);

            var builder = new ObjectMappingBuilder<DummyPropInfo>(root);
            builder.RecordMoveProperty(child11, Props.IsActive, child1, Props.IsActive);

            var built = builder.BuildObjectMapping();
            
            var rootMapper = built.CreateAnimationMapper(root);

            Assert.That(
                rootMapper.MapBinding("child1/child11", typeof(GameObject), Props.IsActive),
                Is.EquivalentTo(new [] {B("child1", typeof(GameObject), Props.IsActive, 0)}));
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

            var builder = new ObjectMappingBuilder<DummyPropInfo>(root);
            builder.RecordCopyProperty(child11, Props.IsActive,
                child12, Props.IsActive);
            builder.RecordCopyProperty(child11, Props.IsActive,
                child13, Props.IsActive);
            builder.RecordCopyProperty(child12, Props.IsActive,
                child14, Props.IsActive);

            var built = builder.BuildObjectMapping();

            var rootMapper = built.CreateAnimationMapper(root);

            Assert.That(
                rootMapper.MapBinding("child1/child11", typeof(GameObject), Props.IsActive),
                Is.EquivalentTo(new[]
                {
                    B("child1/child11", typeof(GameObject), Props.IsActive, 0),
                    B("child1/child12", typeof(GameObject), Props.IsActive, 0),
                    B("child1/child13", typeof(GameObject), Props.IsActive, 0),
                    B("child1/child14", typeof(GameObject), Props.IsActive, 0),
                }));

            Assert.That(
                rootMapper.MapBinding("child1/child12", typeof(GameObject), Props.IsActive),
                Is.EquivalentTo(new[]
                {
                    B("child1/child12", typeof(GameObject), Props.IsActive, 0),
                    B("child1/child14", typeof(GameObject), Props.IsActive, 0),
                }));
        }

        [Test]
        public void CopyAndDestroyOriginal()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child11 = Utils.NewGameObject("child11", child1.transform);
            var child12 = Utils.NewGameObject("child12", child1.transform);
            var child13 = Utils.NewGameObject("child13", child1.transform);
            var child14 = Utils.NewGameObject("child14", child1.transform);

            var builder = new ObjectMappingBuilder<DummyPropInfo>(root);
            builder.RecordCopyProperty(child11, Props.IsActive,
                child12, Props.IsActive);
            builder.RecordCopyProperty(child11, Props.IsActive,
                child13, Props.IsActive);
            builder.RecordCopyProperty(child12, Props.IsActive,
                child14, Props.IsActive);

            DestroyTracker.DestroyImmediate(child11);

            var built = builder.BuildObjectMapping();

            var rootMapper = built.CreateAnimationMapper(root);

            Assert.That(
                rootMapper.MapBinding("child1/child11", typeof(GameObject), Props.IsActive),
                Is.EquivalentTo(new[]
                {
                    B("child1/child12", typeof(GameObject), Props.IsActive, 0),
                    B("child1/child13", typeof(GameObject), Props.IsActive, 0),
                    B("child1/child14", typeof(GameObject), Props.IsActive, 0),
                }));

            Assert.That(
                rootMapper.MapBinding("child1/child12", typeof(GameObject), Props.IsActive),
                Is.EquivalentTo(new[]
                {
                    B("child1/child12", typeof(GameObject), Props.IsActive, 0),
                    B("child1/child14", typeof(GameObject), Props.IsActive, 0),
                }));
        }

        [Test]
        public void EnabledOfAnimatorTest()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child1Animator = child1.AddComponent<Animator>();

            var builder = new ObjectMappingBuilder<DummyPropInfo>(root);
            child1.name = "child2";

            var built = builder.BuildObjectMapping();

            var rootMapper = built.CreateAnimationMapper(root);
            Assert.That(
                rootMapper.MapBinding("child1", typeof(Animator), "PropertyAnimation"),
                Is.EquivalentTo(new[]{B("child2", typeof(Animator), "PropertyAnimation", 0)}));

            Assert.That(
                rootMapper.MapBinding("child1", typeof(Behaviour), "m_Enabled"),
                Is.EquivalentTo(new[]{B("child2", typeof(Behaviour), "m_Enabled", 0)}));

        }

        [Test]
        public void MultipleObjectOnSingleObject()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child1Collider = child1.AddComponent<BoxCollider>();
            var child2 = Utils.NewGameObject("child2", root.transform);
            var child2Collider0 = child2.AddComponent<BoxCollider>();
            var child2Collider1 = child2.AddComponent<BoxCollider>();

            var builder = new ObjectMappingBuilder<DummyPropInfo>(root);

            builder.RecordCopyProperty(child1Collider, "m_Enabled", child2Collider1, "m_Enabled");

            var built = builder.BuildObjectMapping();

            var rootMapper = built.CreateAnimationMapper(root);
            Assert.That(
                rootMapper.MapBinding("child1", typeof(BoxCollider), "m_Enabled"),
                Is.EquivalentTo(new[]
                {
                    B("child1", typeof(BoxCollider), "m_Enabled", 0),
                    B("child2", typeof(BoxCollider), "m_Enabled", 1),
                }));
        }

        private static (string, Type, string, int) B(string path, Type type, string prop, int index) => (path, type, prop, index);
    }
}
