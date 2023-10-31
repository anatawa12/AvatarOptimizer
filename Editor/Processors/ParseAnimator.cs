using Anatawa12.AvatarOptimizer.AnimatorParsers;
using nadena.dev.ndmf;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class ParseAnimator : Pass<ParseAnimator>
    {
        public override string DisplayName => "T&O: Parse Animator";

        protected override void Execute(BuildContext context)
        {
            var traceAndOptimize = context.GetState<TraceAndOptimizes.TraceAndOptimizeState>();
            var modifications = new AnimatorParser(traceAndOptimize.MmdWorldCompatibility)
                .GatherAnimationModifications(context);
            context.Extension<ObjectMappingContext>()
                .MappingBuilder
                .ImportModifications(modifications);
        }
    }
}