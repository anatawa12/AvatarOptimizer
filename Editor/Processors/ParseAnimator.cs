using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using Anatawa12.AvatarOptimizer.ndmf;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class ParseAnimator : Pass<ParseAnimator>
    {
        public override string DisplayName => "Parse Animator";

        protected override void Execute(BuildContext context)
        {
            // do not parse Animator if there are no AAO components in the project to avoid warnings
            if (!context.GetState<AAOEnabled>().Enabled) return;

            var traceAndOptimize = context.GetState<TraceAndOptimizes.TraceAndOptimizeState>();
            var parser = new AnimatorParser(traceAndOptimize.MmdWorldCompatibility);
            var modifications = parser.GatherAnimationModifications(context);
            context.GetState<MaterialAnimationWeightZeroEffectHotFixState>().SkinnedMeshAnimations =
                parser.SkinnedMeshAnimations;
            context.Extension<ObjectMappingContext>()
                .MappingBuilder
                .ImportModifications(modifications);
        }
    }

    // AAO 1.7 hotfix. see https://github.com/anatawa12/AvatarOptimizer/issues/1244
    class MaterialAnimationWeightZeroEffectHotFixState
    {
        public Dictionary<SkinnedMeshRenderer, HashSet<string>> SkinnedMeshAnimations = new Dictionary<SkinnedMeshRenderer, HashSet<string>>();
    }
}
