using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer
{
    public class AnimatorOptimizerState
    {
        private readonly List<AOAnimatorController> _contollers = new();
        public IEnumerable<AOAnimatorController> Controllers => _contollers;

        public void Add(AOAnimatorController wrapper) => _contollers.Add(wrapper);

        private readonly Dictionary<AnimationClip, bool> _isTimeDependentClipCache = new();
        public bool IsTimeDependentClip(AnimationClip clip)
        {
            if (_isTimeDependentClipCache.TryGetValue(clip, out var result))
                return result;
            return _isTimeDependentClipCache[clip] = IsTimeDependentClipImpl(clip);
        }

        private static bool IsTimeDependentClipImpl(AnimationClip clip)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);

                // one frame is time independent
                if (curve.length <= 1) continue;

                foreach (var (preKey, postKey) in curve.keys.ZipWithNext())
                {
                    var preWeighted = preKey.weightedMode == WeightedMode.Out || preKey.weightedMode == WeightedMode.Both;
                    var postWeighted = postKey.weightedMode == WeightedMode.In || postKey.weightedMode == WeightedMode.Both;

                    if (preKey.value.CompareTo(postKey.value) != 0) return true;
                    // it's constant
                    if (float.IsInfinity(preKey.outWeight) || float.IsInfinity(postKey.inTangent)) continue;
                    if (preKey.outTangent == 0 && postKey.inTangent == 0) continue;
                    if (preWeighted && postWeighted && preKey.outWeight == 0 && postKey.inWeight == 0) continue;
                    return true;
                }
            }

            return false;
        }
    }

    public abstract class AnimOptPassBase<T> : TraceAndOptimizePass<T> where T : TraceAndOptimizePass<T>, new()
    {
        protected sealed override void Execute(BuildContext context, TraceAndOptimizeState state)
        {
            foreach (var controller in context.GetState<AnimatorOptimizerState>().Controllers)
            {
                Profiler.BeginSample("Apply to Animator");
                Execute(context, controller, state);
                Profiler.EndSample();
            }
        }

        private protected abstract void Execute(BuildContext context, AOAnimatorController controller,
            TraceAndOptimizeState settings);
    }
}
