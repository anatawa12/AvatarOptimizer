using Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer;
using NUnit.Framework;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorOptimizer
{
    // https://github.com/anatawa12/AvatarOptimizer/issues/969
    public class Issue969Test : AnimatorOptimizerTestBase
    {
        private AnimatorOptimizerState _state = new();
        public override string TestName => "EntryExitToBlendTree/Issue969";

        // FX_3.controller
        // - Entry からデフォルトステート以外のステートに Equals で遷移
        // - デフォルトステートから Exit に Equals で遷移 (デフォルトステート以外のステートの個数分)
        // - デフォルトステート以外のステートから Exit に NotEquals で遷移
        [Test]
        public void MultipleEqualsExitFromDefaultAndNonEqualsExitFromNonDefault()
        {
            var controller = LoadCloneAnimatorController("FX_3");
            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));
            Assert.That(controller.layers[0].stateMachine.states.Length, Is.EqualTo(1));
        }

        // FX_4.controller
        // - Entry からデフォルトステート以外のステートに Equals で遷移
        // - Entry からデフォルトステートに空の Conditions で遷移 (↑より優先度低)
        // - デフォルトステートから Exit に Equals で遷移 (デフォルトステート以外のステートの個数分)
        // - デフォルトステート以外のステートから Exit に NotEquals で遷移
        [Test]
        public void NoConditionEntryToDefault()
        {
            var controller = LoadCloneAnimatorController("FX_4");
            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));
            Assert.That(controller.layers[0].stateMachine.states.Length, Is.EqualTo(1));
        }

        // FX_1.controller
        // - Entry からデフォルトステート以外のステートに Equals で遷移
        // - 全てのステートから Exit に NotEquals で遷移
        [Test]
        public void RelaxDefaultToExitCondition()
        {
            var controller = LoadCloneAnimatorController("FX_1");
            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));
            Assert.That(controller.layers[0].stateMachine.states.Length, Is.EqualTo(1));
        }

        // FX_0.controller
        // - Entry から全てのステートに Equals で遷移
        // - 全てのステートから Exit に NotEquals で遷移
        // In other words, RelaxDefaultToExitCondition(FX_1) with entry transition to default state
        [Test]
        public void FX0()
        {
            var controller = LoadCloneAnimatorController("FX_0");
            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));
            Assert.That(controller.layers[0].stateMachine.states.Length, Is.EqualTo(1));
        }

        // FX_2.controller
        // - Entry からデフォルトステート以外のステートに Equals で遷移
        // - Entry からデフォルトステートに空の Conditions で遷移 (↑より優先度低)
        // - 全てのステートから Exit に NotEquals で遷移
        // In other words, combination of NoConditionEntryToDefault(FX_4) and
        //   RelaxDefaultToExitCondition(FX_1)
        [Test]
        public void FX2()
        {
            var controller = LoadCloneAnimatorController("FX_2");
            EntryExitToBlendTree.Execute(_state, new AOAnimatorController(controller));
            Assert.That(controller.layers[0].stateMachine.states.Length, Is.EqualTo(1));
        }
    }
}