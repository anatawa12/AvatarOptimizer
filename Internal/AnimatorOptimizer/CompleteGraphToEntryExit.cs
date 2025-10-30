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
            // ensure the all conditions are valid. Float conditions only has Less and Greater modes.
            if (!conditions.All(conds => conds.All(c => c.mode is AnimatorConditionMode.Less or AnimatorConditionMode.Greater)))
                return conditions;
            // We convert each conditions to set of ranges, then merge them.
            var ranges = conditions.Select(conds =>
            {
                var range = FloatOpenRange.Entire;
                foreach (var cond in conds)
                {
                    range = cond.mode switch
                    {
                        AnimatorConditionMode.Less => range.Intersect(FloatOpenRange.LessThan(cond.threshold)),
                        AnimatorConditionMode.Greater => range.Intersect(FloatOpenRange.GreaterThan(cond.threshold)),
                        _ => throw new ArgumentOutOfRangeException()
                    };
                }
                return range;
            }).Where(r => !r.IsEmpty()).ToList();

            if (ranges.Count == 0) return new List<AnimatorCondition[]>();

            ranges.Sort((a, b) => (a.Min, b.Min) switch
            {
                (null, null) => 0,
                (null, _) => -1,
                (_, null) => 1,
                ({} minA, {} minB) => minA.CompareTo(minB)
            });

            // merge ranges as possible
            var mergedRanges = new List<FloatOpenRange>();
            mergedRanges.Add(ranges[0]);
            for (int i = 1; i < ranges.Count; i++)
            {
                var current = ranges[i];
                var lastMerged = mergedRanges[^1];
                if (lastMerged.Union(current) is { } union)
                {
                    // two ranges are adjacent or overlapping, can be merged
                    mergedRanges[^1] = union;
                }
                else
                {
                    // two ranges are disjoint, cannot be merged
                    mergedRanges.Add(current);
                }
            }

            var parameter = conditions.SelectMany(x => x).FirstOrDefault().parameter;
            // convert back to conditions
            return mergedRanges.Where(range => !range.IsEmpty()).Select(range => range.ToConditions(parameter)).ToList();
        }

        public static List<AnimatorCondition[]> OptimizeIntConditions(List<AnimatorCondition[]> thisConditions)
        {
            // accepted modes for int optimization: Equals, NotEqual, Greater, Less
            if (!thisConditions.All(conds => conds.All(c =>
                c.mode is AnimatorConditionMode.Equals or AnimatorConditionMode.NotEqual or
                          AnimatorConditionMode.Greater or AnimatorConditionMode.Less)))
                return thisConditions;

            // convert each conjunction to a set of integer ranges (may be multiple ranges due to NotEqual)
            var allRanges = new List<IntClosedRange>();
            foreach (var conds in thisConditions)
            {
                // start with entire integer domain
                var current = new List<IntClosedRange> { IntClosedRange.Entire };

                foreach (var c in conds)
                {
                    int t = (int)c.threshold;
                    current = (c.mode switch
                    {
                        AnimatorConditionMode.Equals => current.Select(r => r.Intersect(IntClosedRange.Point(t))),
                        AnimatorConditionMode.NotEqual => current.SelectMany(r => r.ExcludeValue(t)),
                        // param > t => allowed ints >= t+1
                        AnimatorConditionMode.Greater => current.Select(r => r.Intersect(IntClosedRange.FromMin(t + 1))),
                        // param < t => allowed ints <= t-1
                        AnimatorConditionMode.Less => current.Select(r => r.Intersect(IntClosedRange.FromMax(t - 1))),
                        _ => throw new ArgumentOutOfRangeException()
                    }).Where(intersect => !intersect.IsEmpty()).ToList();

                    // No ranges left, the whole conjunction is unsatisfiable
                    if (current.Count == 0) break;
                }

                allRanges.AddRange(current);
            }

            // flatten ranges from all conjunctions, then sort and merge adjacent/overlapping ranges
            if (allRanges.Count == 0) return new List<AnimatorCondition[]>();

            allRanges.Sort((a, b) => a.Min.CompareTo(b.Min));

            var merged = new List<IntClosedRange> { allRanges[0] };
            for (var i = 1; i < allRanges.Count; i++)
            {
                var cur = allRanges[i];
                var last = merged[^1];
                if (last.Union(cur) is { } union)
                {
                    merged[^1] = union;
                }
                else
                {
                    merged.Add(cur);
                }
            }

            // We need to convert list of range to list of range with list of holes.
            // a < x < b || b + 1 < x < c  => a < x < c with hole b since it's likely smaller number of conditions.
            // We allow up to two connected values as a holes
            // In other words, a < x < b || b + 2 < x < c will be a < x < c with hole b and b + 1, but
            // a < x < b || b + 3 < x < c will remain as is.
            var finalRanges = new List<(IntClosedRange, List<int> holes)>();
            foreach (var range in merged)
            {
                if (finalRanges.Count == 0)
                {
                    finalRanges.Add((range, new List<int>()));
                    continue;
                }

                var (lastRange, holes) = finalRanges[^1];
                // check if we can merge current range into lastRange with holes
                if (range.Min - lastRange.Max > 0 && range.Min - lastRange.Max <= 3)
                {
                    // can merge
                    // add holes for the gap
                    for (int v = lastRange.Max + 1; v < range.Min; v++)
                    {
                        holes.Add(v);
                    }
                    // update last range to cover current range
                    finalRanges[^1] = (new IntClosedRange(lastRange.Min, range.Max), holes);
                }
                else
                {
                    // cannot merge, just add
                    finalRanges.Add((range, new List<int>()));
                }
            }

            // convert merged ranges back to AnimatorCondition[] arrays; parameter name from input conditions
            var parameter = thisConditions.SelectMany(x => x).FirstOrDefault().parameter;
            return finalRanges.Select(tuple =>
            {
                var (range, holes) = tuple;
                // create range and add NotEquals for holes
                return range.ToConditions(parameter).Concat(holes.Select(h => NotEqualsCondition(parameter, h))).ToArray();
            }).ToList();
        }

        public static List<AnimatorCondition[]> OptimizeBoolConditions(List<AnimatorCondition[]> conditions)
        {
            // The only valid conditions for bool are If (true) and IfNot (false)
            if (!conditions.All(conds => conds.All(c => c.mode is AnimatorConditionMode.If or AnimatorConditionMode.IfNot)))
                return conditions;
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

        public struct FloatOpenRange : IEquatable<FloatOpenRange>
        {
            // each value is exclusive, null means unbounded (infinity + 1)
            public float? Min;
            public float? Max;

            public FloatOpenRange(float? min = null, float? max = null)
            {
                Min = min;
                Max = max;
            }

            public static FloatOpenRange Empty => new(0f, 0f);
            public static FloatOpenRange Entire => default;
            public static FloatOpenRange LessThan(float value) => new(max: value);
            public static FloatOpenRange GreaterThan(float value) => new(min: value);

            public readonly bool IsEmpty() => Min.HasValue && Max.HasValue && Min.Value >= Max.Value;

            public readonly FloatOpenRange Intersect(FloatOpenRange other)
            {
                var result = this;
                if (other.Min is { } minOther) result.Min = Min is { } minSelf ? Mathf.Max(minSelf, minOther) : minOther;
                if (other.Max is { } maxOther) result.Max = Max is { } maxSelf ? Mathf.Min(maxSelf, maxOther) : maxOther;
                return result;
            }

            public readonly FloatOpenRange? Union(FloatOpenRange other)
            {
                // union-ing empty range and another will result another
                if (IsEmpty()) return other;
                if (other.IsEmpty()) return this;

                // check if two ranges are adjacent or overlapping
                if (Intersect(other).IsEmpty()) return null;

                var result = this;
                result.Min = (this.Min, other.Min) is ({} minSelf, {} minOther) ? Mathf.Min(minSelf, minOther) : null;
                result.Max = (this.Max, other.Max) is ({} maxSelf, {} maxOther) ? Mathf.Max(maxSelf, maxOther) : null;
                return result;
            }

            public readonly bool Equals(FloatOpenRange other) => 
                IsEmpty() && other.IsEmpty() || Nullable.Equals(Min, other.Min) && Nullable.Equals(Max, other.Max);

            public readonly override bool Equals(object? obj) => obj is FloatOpenRange other && Equals(other);
            public readonly override int GetHashCode() => IsEmpty() ? 0 : HashCode.Combine(Min, Max);
            public static bool operator ==(FloatOpenRange left, FloatOpenRange right) => left.Equals(right);
            public static bool operator !=(FloatOpenRange left, FloatOpenRange right) => !left.Equals(right);

            public override string ToString() => IsEmpty() ? "Empty" : $"({Min?.ToString() ?? "none"}, {Max?.ToString() ?? "none"})";

            public readonly AnimatorCondition[] ToConditions(string parameter) => (Min, Max) switch
            {
                (null, null) => Array.Empty<AnimatorCondition>(),
                ({} min, null) => new[] { GreaterCondition(parameter, min) },
                (null, {} max) => new[] { LessCondition(parameter, max) },
                ({} min, {} max) => new[]
                {
                    GreaterCondition(parameter, min),
                    LessCondition(parameter, max),
                },
            };
        }

        // new helper for integer ranges and OptimizeIntConditions implementation
        public struct IntClosedRange : IEquatable<IntClosedRange>
        {
            // inclusive bounds; use sentinels to represent unbounded
            private const int NEG_INF = int.MinValue;
            private const int POS_INF = int.MaxValue;

            public int Min; // inclusive
            public int Max; // inclusive

            public IntClosedRange(int min, int max)
            {
                Min = min;
                Max = max;
            }

            public static IntClosedRange Empty => new(1, 0); // Min > Max => empty
            public static IntClosedRange Entire => new(NEG_INF, POS_INF);
            public static IntClosedRange FromMin(int min) => new(min, POS_INF);
            public static IntClosedRange FromMax(int max) => new(NEG_INF, max);
            public static IntClosedRange Point(int v) => new(v, v);

            public bool IsEmpty() => Min > Max;

            public IntClosedRange Intersect(IntClosedRange other) => new(Math.Max(Min, other.Min), Math.Min(Max, other.Max));

            // subtract single value v; may split range into up to two ranges
            public IEnumerable<IntClosedRange> ExcludeValue(int v)
            {
                if (v == NEG_INF) return new[] { new IntClosedRange(Math.Max(Min, v + 1), Max) }.Where(r => !r.IsEmpty()); 
                if (v == POS_INF) return new[] { new IntClosedRange(Min, Math.Min(Max, v - 1)) }.Where(r => !r.IsEmpty()); 
                return new[]
                {
                    new IntClosedRange(Min, Math.Min(Max, v - 1)),
                    new IntClosedRange(Math.Max(Min, v + 1), Max),
                }.Where(r => !r.IsEmpty());
            }

            // try union: if adjacent or overlapping, return union; otherwise null
            public IntClosedRange? Union(IntClosedRange other)
            {
                if (IsEmpty()) return other;
                if (other.IsEmpty()) return this;

                // disjoint with gap > 0
                if (Max != POS_INF && other.Min != NEG_INF && (long)Max + 1 < other.Min) return null;
                if (other.Max != POS_INF && Min != NEG_INF && (long)other.Max + 1 < Min) return null;

                var min = Math.Min(Min, other.Min);
                var max = Math.Max(Max, other.Max);
                return new IntClosedRange(min, max);
            }

            public bool Equals(IntClosedRange other) =>
                IsEmpty() && other.IsEmpty() || Min == other.Min && Max == other.Max;

            public override bool Equals(object? obj) => obj is IntClosedRange other && Equals(other);
            public override int GetHashCode() => IsEmpty() ? 0 : HashCode.Combine(Min, Max);
            public static bool operator ==(IntClosedRange left, IntClosedRange right) => left.Equals(right);
            public static bool operator !=(IntClosedRange left, IntClosedRange right) => !left.Equals(right);

            public override string ToString() => IsEmpty() ? "Empty" : $"[{Min}, {Max}]";

            // convert to animator conditions for a given parameter name
            public AnimatorCondition[] ToConditions(string parameter)
            {
                var hasMin = Min != NEG_INF;
                var hasMax = Max != POS_INF;

                return (hasMin, hasMax) switch
                {
                    (false, false) => Array.Empty<AnimatorCondition>(),
                    (true, false) => new[] { GreaterCondition(parameter, Min - 1) },
                    (false, true) => new[] { LessCondition(parameter, Max + 1f) },
                    (true, true) => Min == Max
                        ? new[] { EqualsCondition(parameter, Min) }
                        : new[] { GreaterCondition(parameter, Min - 1f), LessCondition(parameter, Max + 1f) }
                };
            }
        }

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
