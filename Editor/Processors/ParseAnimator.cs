using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using Anatawa12.AvatarOptimizer.ndmf;
using nadena.dev.ndmf;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class ParseAnimator : Pass<ParseAnimator>
    {
        public override string DisplayName => "Parse Animator";

        protected override void Execute(BuildContext context)
        {
            // do not parse Animator if there are no AAO components in the project to avoid warnings
            if (!context.GetState<AAOEnabled>().Enabled) return;
            RunPass(context);
        }

        public static void RunPass(BuildContext context)
        {
            var traceAndOptimize = context.GetState<TraceAndOptimizes.TraceAndOptimizeState>();
            var modifications = new AnimatorParser(traceAndOptimize.MmdWorldCompatibility)
                .GatherAnimationModifications(context);
            context.Extension<ObjectMappingContext>()
                .MappingBuilder!
                .ImportModifications(modifications);
            BugReportHelper.Context.Current?.AnimatorParserResult(context, modifications);
        }
    }
}
