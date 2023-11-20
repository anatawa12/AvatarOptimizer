#if AAO_VRCSDK3_AVATARS
using System.Linq;
using Anatawa12.AvatarOptimizer.AnimatorParsers;
using Anatawa12.AvatarOptimizer.ErrorReporting;
using nadena.dev.ndmf;
using UnityEngine;
using VRC.Dynamics;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    internal class OptimizePhysBone : TraceAndOptimizePass<OptimizePhysBone>
    {
        public override string DisplayName => "T&O: Optimize PhysBone";

        protected override void Execute(BuildContext context, TraceAndOptimizeState state)
        {
            if (!state.OptimizePhysBone) return;

            if (!state.SkipIsAnimatedOptimization)
                IsAnimatedOptimization(context);
        }

        private void IsAnimatedOptimization(BuildContext context)
        {
            BuildReport.ReportingObjects(context.GetComponents<VRCPhysBoneBase>(), physBone =>
            {
                var isAnimated = physBone.GetAffectedTransforms()
                    .Select(transform => context.GetAnimationComponent(transform))
                    .Any(animation => IsAnimatedExternally(physBone, animation));
                if (physBone.isAnimated && !isAnimated)
                {
                    physBone.isAnimated = false;
                    Debug.Log($"Optimized IsAnimated for {physBone.name}", physBone);
                }
                // TODO: add warning if physBone.isAnimated is false and isAnimated is true?
            });
        }

        private static bool IsAnimatedExternally(VRCPhysBoneBase physBone, AnimationComponentInfo animation)
        {
            foreach (var transformProperty in TransformProperties)
            {
                if (!animation.TryGetFloat(transformProperty, out var property)) continue;
                foreach (var modificationSource in property.Sources)
                    if (!(modificationSource is ComponentAnimationSource componentSource) ||
                        componentSource.Component != physBone)
                        return true;
            }

            return false;
        }

        private static readonly string[] TransformProperties =
        {
            "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w",
            "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z", 
            "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z", 
            "localEulerAnglesRaw.x", "localEulerAnglesRaw.y", "localEulerAnglesRaw.z"
        };
    }
}
#endif
