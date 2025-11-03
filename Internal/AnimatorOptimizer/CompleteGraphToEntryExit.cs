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
    using IntRange = Range<int, RangeIntTrait>;
    using IntRangeSet = RangeSet<int, RangeIntTrait>;
    using FloatRange = Range<float, RangeFloatTrait>;
    using FloatRangeSet = RangeSet<float, RangeFloatTrait>;

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
            if (settings.SkipCompleteGraphToEntryExit) return; // feature disabled

            var state = context.GetState<AnimatorOptimizerState>();
            Execute(state, controller);
        }
        

        public static void Execute(AnimatorOptimizerState state, AOAnimatorController controller)
        {
            var typeByName = controller.parameters.ToDictionary(x => x.name, x => x.type);

            // first, collect transformable layers
            var layers = controller.layers;
            foreach (var layer in layers)
            {
                if (CanConvert(layer))
                {
                    DoConvert(layer, typeByName);
                }
            }
        }

        private static bool CanConvert(AOAnimatorControllerLayer layer)
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
            public static readonly AnimatorConditionComparator Instance = new();
            public bool Equals(AnimatorCondition x, AnimatorCondition y) => x.mode == y.mode && x.parameter == y.parameter && Equals(x.threshold, y.threshold);

            public int GetHashCode(AnimatorCondition obj) => HashCode.Combine(obj.mode, obj.parameter, obj.threshold);
        }

        private static void DoConvert(AOAnimatorControllerLayer layer,
            Dictionary<string, AnimatorControllerParameterType> typeByName)
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

                var exitTransitions = OptimizeCondition(transitonByTargetState.Where(x => x.Key != state).Select(x => x.Value.conditions).ToList(), typeByName)
                    .Select(conditions => new AnimatorStateTransition()
                    {
                        isExit = true,
                        destinationState = null,
                        destinationStateMachine = null,

                        duration = referenceTransition.duration,
                        hasFixedDuration = referenceTransition.hasFixedDuration,
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

            stateMachine.entryTransitions = entryTransitions.ToArray();
        }

        // both result and input are (condition[AND])[OR], so outer loop is OR, inner loop is AND
        public static List<AnimatorCondition[]> OptimizeCondition(List<AnimatorCondition[]> conditions,
            Dictionary<string, AnimatorControllerParameterType> typeByName)
        {
            // We do optimization by merge conditions one property by one property.
            var parameterNames = conditions.SelectMany(c => c.Select(cond => cond.parameter)).Distinct().ToArray();

            foreach (var parameterName in parameterNames)
            {
                // Unable to optimize if parameter type is unknown
                if (!typeByName.TryGetValue(parameterName, out var parameterType)) continue;
                // ensure the type is known type
                if (parameterType is not (AnimatorControllerParameterType.Float or AnimatorControllerParameterType.Bool or AnimatorControllerParameterType.Int or AnimatorControllerParameterType.Trigger)) continue;

                    // group by conditions except for parameterName
                var groups = conditions.GroupBy(conds =>
                    conds.Where(c => c.parameter != parameterName)
                        .ToEqualsHashSet(AnimatorConditionEqualityComparer.Instance));
                var newConditions = new List<AnimatorCondition[]>();

                foreach (var group in groups)
                {
                    var otherConditions = group.Key;
                    var thisConditions = group.Select(c => c.Where(cond => cond.parameter == parameterName).ToArray()).ToList();
                    // We need to reduce the number of thisConditions by merging them.
                    // e.g. (param == 1) OR (param == 2) => param is > 0 AND < 3
                    // e.g. (param > 1) OR (param > 2) => param > 1
                    // e.g. (param > 1) OR (param < 0) => no condition
                    // e.g., param is true OR param is false => no condition
                    if (thisConditions.Any(x => x.Length == 0))
                    {
                        // If empty condition (always true) exists, whole condition is always true.
                        newConditions.Add(otherConditions.ToArray());
                    }
                    else
                    {
                        newConditions.AddRange((parameterType switch
                        {
                            AnimatorControllerParameterType.Float => OptimizeFloatConditions(thisConditions),
                            AnimatorControllerParameterType.Int => OptimizeIntConditions(thisConditions),
                            AnimatorControllerParameterType.Bool => OptimizeBoolConditions(thisConditions),
                            AnimatorControllerParameterType.Trigger => OptimizeTriggerConditions(thisConditions),
                            _ => throw new ArgumentOutOfRangeException()
                        }).Select(x => x.Concat(otherConditions).ToArray()));
                    }
                }

                conditions = newConditions;
            }

            // TODO: optimize the condition
            return conditions;
        }

        public static List<AnimatorCondition[]> OptimizeFloatConditions(List<AnimatorCondition[]> conditions)
        {
            // We convert each conditions to set of ranges, then merge them.
            var ranges = FloatRangeSet.Union(conditions.Select(RangesUtil.FloatRangeSetFromConditions));

            if (ranges.IsEmpty()) return new List<AnimatorCondition[]>();

            var parameter = conditions.SelectMany(x => x).FirstOrDefault().parameter;
            // convert back to conditions
            return ranges.ToConditions(parameter);
        }

        public static List<AnimatorCondition[]> OptimizeIntConditions(List<AnimatorCondition[]> conditions)
        {
            // convert each conjunction to a set of integer ranges (may be multiple ranges due to NotEqual)
            var allRangesSet = IntRangeSet.Union(conditions.Select(RangesUtil.IntRangeSetFromConditions));

            // flatten ranges from all conjunctions, then sort and merge adjacent/overlapping ranges
            if (allRangesSet.IsEmpty()) return new List<AnimatorCondition[]>();

            // convert merged ranges back to AnimatorCondition[] arrays; parameter name from input conditions
            var parameter = conditions.SelectMany(x => x).FirstOrDefault().parameter;
            return allRangesSet.ToConditions(parameter);
        }

        public static List<AnimatorCondition[]> OptimizeBoolConditions(List<AnimatorCondition[]> conditions)
        {
            var parameter = conditions.SelectMany(x => x).First().parameter;

            var allNever = conditions.All(conds =>
                conds.Any(c => c.mode == AnimatorConditionMode.If) &&
                conds.Any(c => c.mode == AnimatorConditionMode.IfNot));
            if (allNever) return new List<AnimatorCondition[]>(); // never true

            var hasTrue = conditions.Any(conds => conds.Any(c => c.mode == AnimatorConditionMode.If));
            var hasFalse = conditions.Any(conds => conds.Any(c => c.mode == AnimatorConditionMode.IfNot));
            return (hasTrue, hasFalse) switch
            {
                // both true and false exists, whole condition is always true
                (true, true) => new List<AnimatorCondition[]> { Array.Empty<AnimatorCondition>() },
                // only true exists
                (true, false) => new List<AnimatorCondition[]> { new[] { IfCondition(parameter) } },
                // only false exists
                (false, true) => new List<AnimatorCondition[]> { new[] { IfNotCondition(parameter) } },
                // never true
                (false, false) => new List<AnimatorCondition[]>()
            };
        }

        public static List<AnimatorCondition[]> OptimizeTriggerConditions(List<AnimatorCondition[]> conditions)
        {
            // The only valid condition for trigger is If
            if (!conditions.All(conds => conds.All(c => c.mode is AnimatorConditionMode.If)))
                return conditions;
            var parameter = conditions.SelectMany(x => x).First().parameter;
            if (conditions.Any(x => x.Length == 0)) // always true
                return new List<AnimatorCondition[]> { Array.Empty<AnimatorCondition>() };
            return conditions.Count > 0 ? new List<AnimatorCondition[]> { new[] { IfCondition(parameter) } } : new List<AnimatorCondition[]>();
        }

        // new helper for integer ranges and OptimizeIntConditions implementation

        // utilities
        static AnimatorCondition AnimatorCondition(string parameter, AnimatorConditionMode mode, float threshold = 0) =>
            new()
            {
                parameter = parameter,
                mode = mode,
                threshold = threshold,
            };
        static AnimatorCondition GreaterCondition(string parameter, float threshold) => AnimatorCondition(parameter, AnimatorConditionMode.Greater, threshold);
        static AnimatorCondition LessCondition(string parameter, float threshold) => AnimatorCondition(parameter, AnimatorConditionMode.Less, threshold);
        static AnimatorCondition EqualsCondition(string parameter, float threshold) => AnimatorCondition(parameter, AnimatorConditionMode.Equals, threshold);
        static AnimatorCondition NotEqualsCondition(string parameter, float threshold) => AnimatorCondition(parameter, AnimatorConditionMode.NotEqual, threshold);
        static AnimatorCondition IfCondition(string parameter) => AnimatorCondition(parameter, AnimatorConditionMode.If);
        static AnimatorCondition IfNotCondition(string parameter) => AnimatorCondition(parameter, AnimatorConditionMode.IfNot);

        public class AnimatorConditionEqualityComparer : IEqualityComparer<AnimatorCondition>
        {
            public static AnimatorConditionEqualityComparer Instance = new();
            public bool Equals(AnimatorCondition x, AnimatorCondition y) => x.mode == y.mode && x.parameter == y.parameter && Equals(x.threshold, y.threshold);
            public int GetHashCode(AnimatorCondition obj) => HashCode.Combine(obj.mode, obj.parameter, obj.threshold);
        }
    }
}
