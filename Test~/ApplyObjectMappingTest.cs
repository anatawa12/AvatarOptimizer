using NUnit.Framework;
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
            var builder = new ObjectMappingBuilder(root);

            child11.name = "child12";

            var built = builder.BuildObjectMapping();

            var rootMapper = new AnimatorControllerMapper(built.CreateAnimationMapper(root));

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

            var mappedController = rootMapper.MapAnimatorController(animatorController);
            Assert.That(mappedController, Is.Not.EqualTo(animatorController));
            Assert.That(mappedController.layers[0].avatarMask.GetTransformPath(0), 
                Is.EqualTo("child1/child12"));
            Assert.That(avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Head), Is.True);
            Assert.That(avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg), Is.False);
        }
    }
}