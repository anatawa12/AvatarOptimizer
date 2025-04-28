using nadena.dev.ndmf;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Test
{
    public class ApplyObjectMappingTest
    {
        [Test]
        public void AvatarMask()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child11 = Utils.NewGameObject("child11", child1.transform);
            var builder = new ObjectMappingBuilder<DummyPropInfo>(root);

            child11.name = "child12";

            var built = builder.BuildObjectMapping();

            var rootMapper = new AnimatorControllerMapper(built.CreateAnimationMapper(root), new BuildContext(root, null));

            var avatarMask = new AvatarMask();
            avatarMask.transformCount = 1;
            avatarMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
            avatarMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg, false);
            avatarMask.SetTransformPath(0, "child1/child11");
            
            var animatorController = new AnimatorController();
            animatorController.AddLayer(new AnimatorControllerLayer()
            {
                name = "layer",
                avatarMask = avatarMask,
                stateMachine = new AnimatorStateMachine() { name = "layer" },
            });

            rootMapper.FixAnimatorController(animatorController);
            Assert.That(animatorController.layers[0].avatarMask.GetTransformPath(0), 
                Is.EqualTo("child1/child12"));
            Assert.That(avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Head), Is.True);
            Assert.That(avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg), Is.False);
        }

        [Test]
        public void PreserveAnimationLength()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child11 = Utils.NewGameObject("child11", child1.transform);
            var builder = new ObjectMappingBuilder<DummyPropInfo>(root);

            Object.DestroyImmediate(child11);

            var built = builder.BuildObjectMapping();

            var rootMapper = new AnimatorControllerMapper(built.CreateAnimationMapper(root), new BuildContext(root, null));

            var animatorController = new AnimatorController();
            var layer = new AnimatorControllerLayer()
            {
                name = "layer",
                stateMachine = new AnimatorStateMachine() { name = "layer" },
            };
            var state = layer.stateMachine.AddState("theState");
            var clip = new AnimationClip();
            clip.SetCurve("child1/child11", typeof(GameObject), Props.IsActive, AnimationCurve.Constant(0, 0.3f, 1));
            state.motion = clip;
            animatorController.AddLayer(layer);

            rootMapper.FixAnimatorController(animatorController);

            var mappedClip = animatorController.layers[0].stateMachine.states[0].state.motion as AnimationClip;
            Assert.That(mappedClip, Is.Not.Null);
            
            Assert.That(mappedClip.length, Is.EqualTo(0.3f));
            Assert.That(AnimationUtility.GetCurveBindings(mappedClip)[0].path,
                Contains.Substring("AvatarOptimizerClipLengthDummy"));
        }

        [Test]
        public void PreserveProxyAnimation()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child11 = Utils.NewGameObject("child11", child1.transform);
            var builder = new ObjectMappingBuilder<DummyPropInfo>(root);

            Object.DestroyImmediate(child11);

            var built = builder.BuildObjectMapping();

            var rootMapper = new AnimatorControllerMapper(built.CreateAnimationMapper(root), new BuildContext(root, null));

            var animatorController = new AnimatorController();
            var layer = new AnimatorControllerLayer()
            {
                name = "layer",
                stateMachine = new AnimatorStateMachine() { name = "layer" },
            };
            var state = layer.stateMachine.AddState("theState");
            var clip = new AnimationClip();
            clip.SetCurve("child1/child11", typeof(GameObject), Props.IsActive, AnimationCurve.Constant(0, 0.3f, 1));
            state.motion = clip;

            var proxyMotion =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    AssetDatabase.GUIDToAssetPath("806c242c97b686d4bac4ad50defd1fdb"));
            state = layer.stateMachine.AddState("afk");
            state.motion = proxyMotion;

            animatorController.AddLayer(layer);

            rootMapper.FixAnimatorController(animatorController);

            // ensure non-proxy mapped
            var mappedClip = animatorController.layers[0].stateMachine.states[0].state.motion as AnimationClip;
            Assert.That(mappedClip, Is.Not.EqualTo(clip));

            mappedClip = animatorController.layers[0].stateMachine.states[1].state.motion as AnimationClip;
            Assert.That(mappedClip, Is.EqualTo(proxyMotion));
        }

        [Test]
        public void ProcessSyncedLayer()
        {
            var root = new GameObject();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child11 = Utils.NewGameObject("child11", child1.transform);
            var builder = new ObjectMappingBuilder<DummyPropInfo>(root);

            Object.DestroyImmediate(child11);

            var built = builder.BuildObjectMapping();

            var rootMapper = new AnimatorControllerMapper(built.CreateAnimationMapper(root), new BuildContext(root, null));

            var animatorController = new AnimatorController();

            var layer = new AnimatorControllerLayer
            {
                name = "layer",
                stateMachine = new AnimatorStateMachine { name = "layer" },
            };
            var state = layer.stateMachine.AddState("theState");
            var clip = new AnimationClip();
            clip.SetCurve("child1/child11", typeof(GameObject), Props.IsActive, AnimationCurve.Constant(0, 0.3f, 1));
            state.motion = clip;
            animatorController.AddLayer(layer);

            var syncedLayer = new AnimatorControllerLayer
            {
                name = "syncedLayer",
                stateMachine = new AnimatorStateMachine { name = "syncedLayer" },
                syncedLayerIndex = 0,
            };
            var syncedClip = new AnimationClip();
            syncedClip.SetCurve("child1/child11", typeof(GameObject), Props.IsActive, AnimationCurve.Constant(0, 0.3f, 3));
            syncedLayer.SetOverrideMotion(state, syncedClip);
            animatorController.AddLayer(layer);

            rootMapper.FixAnimatorController(animatorController);

            // ensure non-proxy mapped
            var mappedClip = animatorController.layers[0].stateMachine.states[0].state.motion as AnimationClip;
            Assert.That(mappedClip, Is.Not.EqualTo(clip));
            
            var syncedMappedClip = animatorController.layers[1].GetOverrideMotion(state);
            Assert.That(syncedMappedClip, Is.Not.EqualTo(syncedClip));
        }
    }
}
