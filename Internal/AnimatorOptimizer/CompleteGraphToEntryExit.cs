using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer
{
    /// <summary>
    /// Converts Complete Graph state machine to Entry-Exit state machine
    ///
    /// In later pass, Entry Exit might be converted to 1D BlendTree. 
    /// </summary>

    // detailed explanation of current limitations
    // This optimization expects
    // (about state machine)
    // - there are no child state machine
    // (about transitions)
    // - all states are connected each other (complete graph)
    // - all transitions targeting a state has same conditions
    // - all transitions from a state has same 'transition settings' (hasExitTime, exitTime, duration, offset, interruptionSource)
    // - there is no transition except for complete graph transitions
    // - there is no any state / entry transition
    // This optimization allows:
    public class CompleteGraphToEntryExit : AnimOptPassBase<CompleteGraphToEntryExit>
    {
        private protected override void Execute(BuildContext context, AOAnimatorController controller,
            TraceAndOptimizeState settings)
        {
            if (settings.SkipAnyStateToEntryExit) return; // feature disabled

            var state = context.GetState<AnimatorOptimizerState>();
            Execute(state, controller);
        }
        

        public static void Execute(AnimatorOptimizerState state, AOAnimatorController controller)
        {
            var boolParameters = new HashSet<string>(controller.parameters
                .Where(x => x.type is AnimatorControllerParameterType.Bool)
                .Select(x => x.name));

            // first, collect transformable layers
            var layers = controller.layers;
            foreach (var layer in layers)
            {
                if (CanConvert(layer, state, controller.RootGameObjectOrNull, boolParameters))
                {
                    DoConvert(layer);
                }
            }
        }

        private static bool CanConvert(AOAnimatorControllerLayer layer,
            AnimatorOptimizerState optimizerState,
            GameObject? rootGameObject,
            HashSet<string> boolParameters)
        {
            // basic check
            if (layer is not
                {
                    IsSynced: false,
                    IsSyncedToOtherLayer: false,
                    stateMachine:
                    {
                        anyStateTransitions: { Length: 0 },
                        entryTransitions: { Length: 0 },

                        stateMachines: { Length: 0 },
                        states: { Length: >= 2 } states,
                    }
                })
                return false;
            var childStates = states.Select(x => x.state).ToArray();

            var transitonByTargetState = new Dictionary<AnimatorState, List<AnimatorStateTransition>>();
            foreach (var child in childStates)
            {
                var currentChildTargets = new HashSet<AnimatorState>();
                currentChildTargets.Add(child);

                AnimatorStateTransition? firstTransition = null;

                foreach (var transition in child.transitions)
                {
                    if (transition.isExit) continue; // exit transition
                    if (transition.destinationState == null && transition.destinationStateMachine == null) return false; // bad transition
                    if (transition.destinationState != null && transition.destinationStateMachine != null) return false; // bad transition
                    if (transition.solo) continue; // unsupported
                    if (transition.mute) continue; // unsupported
                    if (transition.destinationState == null) return false; // has state machine transition
                    if (transition.destinationState == child) continue; // self-transition, allowed

                    if (firstTransition == null)
                    { 
                        firstTransition = transition;
                    }
                    else
                    {
                        if (
                            !Equals(transition.duration, firstTransition.duration) ||
                            !Equals(transition.offset, firstTransition.offset) ||
                            transition.interruptionSource != firstTransition.interruptionSource ||
                            transition.orderedInterruption != firstTransition.orderedInterruption ||
                            !Equals(transition.exitTime, firstTransition.exitTime) ||
                            transition.hasExitTime != firstTransition.hasExitTime ||
                            transition.hasFixedDuration != firstTransition.hasFixedDuration)
                            return false; // different transition settings for the same source
                    }

                    if (!transitonByTargetState.TryGetValue(transition.destinationState, out var list))
                        transitonByTargetState.Add(transition.destinationState, list = new ());
                    list.Add(transition);

                    currentChildTargets.Add(transition.destinationState);
                }

                if (!currentChildTargets.SetEquals(childStates)) return false; // incomplete graph
            }

            foreach (var (_, transitions) in transitonByTargetState)
            {
                var firstConditions = transitions[0].conditions;
                if (transitions.Any(t => !t.conditions.SequenceEqual(firstConditions, AnimatorConditionComparator.Instance)))
                    return false; // different conditions for same target
            }

            // it seems we can convert this layer
            return true;
        }

        private class AnimatorConditionComparator : IEqualityComparer<AnimatorCondition>
        {
            public static AnimatorConditionComparator Instance = new();
            public bool Equals(AnimatorCondition x, AnimatorCondition y) => x.mode == y.mode && x.parameter == y.parameter && Equals(x.threshold, y.threshold);

            public int GetHashCode(AnimatorCondition obj) => HashCode.Combine(obj.mode, obj.parameter, obj.threshold);
        }

        private static void DoConvert(AOAnimatorControllerLayer layer)
        {
            var stateMachine = layer.stateMachine!;

            var states = stateMachine.states.Select(x => x.state).ToArray();
            
            var transitonByTargetState = new Dictionary<AnimatorState, AnimatorStateTransition>();
            foreach (var child in states)
                foreach (var transition in child.transitions)
                    transitonByTargetState.TryAdd(transition.destinationState, transition);

            // Each state will:
            // - Have entry transition with conditions same as transitonByTargetState[self]
            // - Have exit transition with 'either' condition of transitonByTargetState. Should be optimized, unless there is no benefits.
            // - May have self-transition if there is any self-transition in original transitions.
            var entryTransitions = new List<AnimatorTransition>();

            foreach (var state in states)
            {
                var originalTransition = transitonByTargetState[state];

                // entry transition
                entryTransitions.Add(new AnimatorTransition()
                {
                    conditions = originalTransition.conditions,
                    destinationState = state,
                });

                var referenceTransition = state.transitions.First(x => x.destinationState != state);

                var exitTransitions = OptimizeCondition(transitonByTargetState.Values.Select(x => x.conditions).ToArray())
                    .Select(conditions => new AnimatorStateTransition()
                    {
                        isExit = true,
                        destinationState = null,
                        destinationStateMachine = null,

                        duration = referenceTransition.duration,
                        offset = referenceTransition.offset,
                        interruptionSource = referenceTransition.interruptionSource,
                        orderedInterruption = referenceTransition.orderedInterruption,
                        hasExitTime = referenceTransition.hasExitTime,
                        exitTime = referenceTransition.exitTime,
                        conditions = conditions,
                    });

                var selfTransitions = state.transitions.Where(x => x.destinationState == state);

                state.transitions = exitTransitions.Concat(selfTransitions).ToArray();
            }
        }

        private static AnimatorCondition[][] OptimizeCondition(AnimatorCondition[][] conditions)
        {
            // TODO: optimize the condition
            return conditions;
        }
    }
}
