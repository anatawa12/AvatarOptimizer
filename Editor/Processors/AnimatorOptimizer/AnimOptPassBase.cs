using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer
{
    class AnimatorOptimizerState
    {
        private HashSet<AnimatorController> _contollers = new HashSet<AnimatorController>();
        public IEnumerable<AnimatorController> Controllers => _contollers;

        public void Add(AnimatorController cloned)
        {
            _contollers.Add(cloned);
        }
    }

    abstract class AnimOptPassBase<T> : TraceAndOptimizes.TraceAndOptimizePass<T> 
        where T : TraceAndOptimizes.TraceAndOptimizePass<T>, new()
    {
        public override string DisplayName => "T&O: AnimOpt: " + typeof(T).Name;

        protected sealed override void Execute(BuildContext context, TraceAndOptimizes.TraceAndOptimizeState state)
        {
            if (!state.AnimatorOptimizer.enabled) return;
            foreach (var controller in context.GetState<AnimatorOptimizerState>().Controllers)
            {
                Profiler.BeginSample("Apply to Animator");
                Execute(context, controller, state.AnimatorOptimizer);
                Profiler.EndSample();
            }
        }

        protected abstract void Execute(BuildContext context, AnimatorController controller, TraceAndOptimize.AnimatorOptimizer settings);
    }

    // This pass prepares animator optimizer
    // This pass does the following things:
    // - Collects all AnimatorController objects and save to state
    // - Clones AnimatorController and StateMachines to avoid modifying original AnimatorController if needed
    // - If the RuntimeAnimatorController is AnimatorOverrideController, convert it to AnimatorController
    class InitializeAnimatorOptimizer : TraceAndOptimizes.TraceAndOptimizePass<InitializeAnimatorOptimizer>
    {
        public override string DisplayName => "AnimOpt: Initialize";

        protected override void Execute(BuildContext context, TraceAndOptimizes.TraceAndOptimizeState state)
        {
            if (!state.AnimatorOptimizer.enabled) return;

            var animatorState = context.GetState<AnimatorOptimizerState>();
            
            var traceAndOptimizeState = context.GetState<TraceAndOptimizes.TraceAndOptimizeState>();
            if (!traceAndOptimizeState.Enabled) return;
            if (!traceAndOptimizeState.AnimatorOptimizer.enabled) return;

            foreach (var component in context.AvatarRootObject.GetComponents<Component>())
            {
                using (var serializedObject = new SerializedObject(component))
                {
                    foreach (var property in serializedObject.ObjectReferenceProperties())
                    {
                        if (property.objectReferenceValue is RuntimeAnimatorController runtimeController)
                        {
                            var cloned = AnimatorControllerCloner.Clone(context, runtimeController);
                            animatorState.Add(cloned);
                            property.objectReferenceValue = cloned;
                        }
                    }

                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }
    }

    class AnimatorControllerCloner : DeepCloneHelper
    {
        [NotNull] private readonly BuildContext _context;
        [CanBeNull] private readonly IReadOnlyDictionary<AnimationClip,AnimationClip> _mapping;

        private AnimatorControllerCloner([NotNull] BuildContext context,
            [CanBeNull] IReadOnlyDictionary<AnimationClip, AnimationClip> mapping)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapping = mapping;
        }

        public static AnimatorController Clone([NotNull] BuildContext context,
            [NotNull] RuntimeAnimatorController runtimeController)
        {
            var (controller, mapping) = AnimatorParsersV2.AnimatorParser.GetControllerAndOverrides(runtimeController);

            return new AnimatorControllerCloner(context, mapping).MapObject(controller);
        }

        protected override Object CustomClone(Object o)
        {
            if (o is AnimationClip clip)
            {
                if (_mapping != null && _mapping.TryGetValue(clip, out var mapped))
                    return mapped;
            }

            if (_context.IsTemporaryAsset(o))
            {
                RegisterNotCloned(o);

                using (var so = new SerializedObject(o))
                {
                    foreach (var prop in so.ObjectReferenceProperties())
                        prop.objectReferenceValue = DeepClone(prop.objectReferenceValue);

                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            return null;
        }

        protected override ComponentSupport GetComponentSupport(Object o)
        {
            switch (o)
            {
                case AnimatorController _:
                case AnimatorStateMachine _:
                case AnimatorState _:
                case AnimatorTransitionBase _:
                case StateMachineBehaviour _:
                case Motion _ :
                    return ComponentSupport.Clone;

                // should not reach this case
                case RuntimeAnimatorController _:
                    return ComponentSupport.Unsupported;

                default:
                    return ComponentSupport.NoClone;
            }
        }
    }
}
