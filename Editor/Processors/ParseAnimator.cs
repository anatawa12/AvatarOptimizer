using Anatawa12.AvatarOptimizer.AnimatorParsers;
using nadena.dev.ndmf;

namespace Anatawa12.AvatarOptimizer.Processors
{
    class AnimatorState
    {
        public ImmutableModificationsContainer Modifications;
    }

    internal class ParseAnimator : Pass<ParseAnimator>
    {
        public override string DisplayName => "T&O: Parse Animator";

        protected override void Execute(BuildContext context)
        {
            var traceAndOptimize = context.GetState<TraceAndOptimizes.TraceAndOptimizeState>();
            var animatorState = context.GetState<AnimatorState>();
            var modifications = new AnimatorParser(traceAndOptimize.MmdWorldCompatibility)
                .GatherAnimationModifications(context);
            animatorState.Modifications = modifications;
            context.Extension<ObjectMappingContext>()
                .MappingBuilder
                .ImportModifications(modifications);
        }
    }
}