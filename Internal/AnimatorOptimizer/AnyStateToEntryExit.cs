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
    /// Converts AnyState state machine to Entry-Exit state machine
    ///
    /// This optimization only works if all possible values of a parameter are known.
    ///
    /// In later pass, Entry Exit might be converted to 1D BlendTree. 
    /// </summary>

    // detailed explanation of current limitations
    // This optimization expects
    // (about state machine)
    // - there are no child state machine
    // - there are no state machine behaviour
    // (about transitions)
    // - all transitions are associated with same single parameter
    // - all states are connected from any state
    // - all any state transitions has transition to self disabled
    // - there is no transition except for any state
    // - all states will leave state to exit when parameter value become values not listed in entry transitions
    // (semantics)
    // - each state has corresponding parameter value for the parameter
    // - default state has no-op to the current state of avatar
    // (motion / state)
    // - all states have same write defaults value
    public class AnyStateToEntryExit : AnimOptPassBase<AnyStateToEntryExit>
    {
        private protected override void Execute(BuildContext context, AOAnimatorController controller,
            TraceAndOptimizeState settings)
        {
            if (!settings.AnyStateToEntryExit) return; // feature disabled

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
                        anyStateTransitions: { Length: >= 1 } anyStateTransitions,
                        stateMachines: { Length: 0 },
                        defaultState: { } defaultState,
                        states: { Length: >= 2 } states,
                        entryTransitions.Length: 0,
                    }
                })
                return false;

            // check for transitions
            var foundStates = new HashSet<AnimatorState>();

            var hasExitTime = anyStateTransitions[0].hasExitTime;
            var exitTime = anyStateTransitions[0].exitTime;
            var fixedDuration = anyStateTransitions[0].hasFixedDuration;
            var duration = anyStateTransitions[0].duration;
            var offset = anyStateTransitions[0].offset;

            foreach (var anyStateTransition in anyStateTransitions)
            {
                if (anyStateTransition is not
                    {
                        isExit: false,
                        mute: false,
                        solo: false,
                        destinationStateMachine: null,
                        destinationState: { } dest,
                        conditions.Length: >= 1,

                        canTransitionToSelf: false, // TODO: we may remove this limitation with infinity loop entry-exit
                    })
                    return false;

                if (anyStateTransition.hasExitTime != hasExitTime) return false;
                if (hasExitTime)
                    if (!Mathf.Approximately(anyStateTransition.exitTime, exitTime)) return false;
                if (anyStateTransition.hasFixedDuration != fixedDuration) return false;
                if (!Mathf.Approximately(anyStateTransition.duration, duration)) return false;
                if (!Mathf.Approximately(anyStateTransition.offset, offset)) return false;

                foundStates.Add(dest);
            }

            if (!foundStates.SetEquals(states.Select(x => x.state))) return false;

            // check for conditions
            // one of condition must be satisfied at any time
            // this is something like tautology check
            var allConditions = anyStateTransitions.Select(x => x.conditions).ToArray();
            if (!IsTautology(allConditions, boolParameters)) return false;

            // check for each state
            // we have to check
            // - write defaults are same in the layer
            // - if write defaults is off, check animating properties are same between layers

            // check write defaults and animating properties
            if (states[0].state.writeDefaultValues)
            {
                for (var index = 1; index < states.Length; index++)
                {
                    // check WD
                    if (states[index] is not { state.writeDefaultValues: true }) return false;
                }
            }
            else
            {
                for (var index = 1; index < states.Length; index++)
                {
                    // check WD
                    if (states[index] is not { state.writeDefaultValues: false })
                        return false;
                }
            }

            foreach (var childStateInfo in states)
            {
                if (childStateInfo is not
                    {
                        state:
                        {
                            transitions.Length: 0, // no transitions other than any state
                        },
                    }) return false;
            }

            // First one tick of default state should not change the behavior of layers
            if (defaultState.writeDefaultValues)
            {
                // if WD=on, all states will affects same set of properties thanks to WD
                // so we can skip this check
            }
            else
            {
                // in WD=off, we have to check non-nop properties of default animation are animated by all states
                var defaultClip = defaultState.motion as AnimationClip;
                if (defaultClip == null) return false; // WD=off and no motion is weird so we reject it

                static bool IsMeaningfulBinding(AnimationClip clip, EditorCurveBinding binding, GameObject? rootGameObject)
                {
                    if (rootGameObject == null) return false; // we can't check no-op without rootGameObject
                    var component = Utils.GetAnimatedObject(rootGameObject, binding);
                    if (component == null) return true; // target is not exist: no-op

                    if (binding.isPPtrCurve)
                    {
                        var curve = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                        if (curve.Length < 1) return false; // invalid curve
                        var value = curve[0].value; // we check the first frame

                        var currentValue = GetCurrentValue(rootGameObject, binding);

                        switch (currentValue, value)
                        {
                            case (null, null):
                                return true; // same value; no-op

                            case (null, not null): // either is null
                            case (not null, null): // either is null
                            case (not Object, _): // different type
                                return false;

                            case (Object obj, var obj2):
                                return obj.GetInstanceID() == obj2.GetInstanceID();
                        }
                    }
                    else
                    {
                        var curve = AnimationUtility.GetEditorCurve(clip, binding);
                        var value = curve.Evaluate(0);
                        var currentValue = GetCurrentValue(rootGameObject, binding);
                        return value.Equals(currentValue);
                    }

                }

                var meaningfulDefaultStateCurveBindings = AnimationUtility.GetCurveBindings(defaultClip)
                    .Where(x => !IsMeaningfulBinding(defaultClip, x, rootGameObject))
                    .ToArray();
                var meaningfulDefaultStateObjectReferenceCurveBindings = AnimationUtility.GetObjectReferenceCurveBindings(defaultClip)
                    .Where(x => !IsMeaningfulBinding(defaultClip, x, rootGameObject))
                    .ToArray();

                if (meaningfulDefaultStateCurveBindings.Length != 0
                    || meaningfulDefaultStateObjectReferenceCurveBindings.Length != 0)
                {
                    foreach (var childStateInfo in states)
                    {
                        if (childStateInfo.state == defaultState) continue; // default state is always ok
                        foreach (var animationClip in ACUtils.AllClipsMayNull(childStateInfo.state.motion))
                        {
                            if (animationClip == null) return false; // null clip with WD=off

                            foreach (var binding in meaningfulDefaultStateCurveBindings)
                            {
                                if (AnimationUtility.GetEditorCurve(animationClip, binding) == null)
                                    return false; // meaningful curve is not animated
                            }

                            foreach (var binding in meaningfulDefaultStateObjectReferenceCurveBindings)
                            {
                                if (AnimationUtility.GetObjectReferenceCurve(animationClip, binding) == null)
                                    return false; // meaningful curve is not animated
                            }
                        }
                    }
                }
            }

            // it seems we can convert this layer
            return true;
        }

        private static void DoConvert(AOAnimatorControllerLayer layer)
        {
            var stateMachine = layer.stateMachine!;
            var anyStateTransitions = stateMachine.anyStateTransitions;
            var states = stateMachine.states;

            // First, generate entry transition for each state
            {
                var entryTransitions = new AnimatorTransition[anyStateTransitions.Length];

                for (var i = 0; i < anyStateTransitions.Length; i++)
                {
                    var anyStateTransition = anyStateTransitions[i];
                    var dest = anyStateTransition.destinationState;
                    var conditions = anyStateTransition.conditions;

                    var transition = new AnimatorTransition
                    {
                        destinationState = dest,
                        conditions = conditions,
                    };

                    entryTransitions[i] = transition;
                }

                stateMachine.entryTransitions = entryTransitions;
            }

            // then, create exit transitions
            // We only support simple case where condition parameter is only one and it's bool or known int parameter
            // we use this limitation to generate exit conditions
            // (this limitation is implemented in IsTautology method)

            // for general case, we have to check all possible transitions
            // exit condition are !any(entryConditionsForTheState)
            // however, entry conditions are (!any(priorTransitionConditions) && entryConditions)
            // therefore, actual exit condition !any(!any(priorTransitionConditions) && entryConditions)

            var baseTransition = anyStateTransitions[0];
            AnimatorStateTransition MakeExitTransition(AnimatorCondition[] conditions) =>
                new()
                {
                    isExit = true,
                    mute = false,
                    solo = false,

                    canTransitionToSelf = false,

                    hasExitTime = baseTransition.hasExitTime,
                    exitTime = baseTransition.exitTime,
                    hasFixedDuration = baseTransition.hasFixedDuration,
                    duration = baseTransition.duration,
                    offset = baseTransition.offset,
                    conditions = conditions
                };


            {
                var isBoolean = anyStateTransitions.Any(x =>
                    x.conditions.Any(y => y.mode is AnimatorConditionMode.If or AnimatorConditionMode.IfNot));

                if (isBoolean)
                {
                    var valuePerState = states.ToDictionary(x => x.state, _ => new List<bool>());

                    var canEntryWithTrue = true;
                    var canEntryWithFalse = true;

                    foreach (var anyStateTransition in anyStateTransitions)
                    {
                        var destState = anyStateTransition.destinationState;

                        var isTrue = anyStateTransition.conditions.Single().mode == AnimatorConditionMode.If;
                        if (isTrue && canEntryWithTrue)
                            valuePerState[destState].Add(true);
                        else if (!isTrue && canEntryWithFalse)
                            valuePerState[destState].Add(false);

                        if (isTrue) canEntryWithTrue = false;
                        else canEntryWithFalse = true;
                    }

                    var baseCondition = anyStateTransitions[0].conditions[0];
                    foreach (var (state, values) in valuePerState)
                    {
                        var isTrue = values.Any(x => x);
                        var isFalse = values.Any(x => !x);
                        switch (isTrue, isFalse)
                        {
                            case (true, false):
                                // this state is true state so we should set false exit
                                state.transitions = new[]
                                {
                                    MakeExitTransition(new[]
                                        { baseCondition with { mode = AnimatorConditionMode.IfNot } })
                                };
                                break;
                            case (false, true):
                                // this state is false state so we should set true exit
                                state.transitions = new[]
                                {
                                    MakeExitTransition(new[] { baseCondition with { mode = AnimatorConditionMode.If } })
                                };
                                break;
                            case (false, false):
                            case (true, true):
                                // this state is both true and false state so we don't have to set exit
                                break;
                        }
                    }
                }
                else // int
                {
                    var targetParameter = anyStateTransitions[0].conditions[0].parameter;
                    if (!KnownParameterValues.GetIntValues(targetParameter, out var allValues))
                        throw new InvalidOperationException("unknown parameter");

                    var valuePerState = states.ToDictionary(x => x.state, _ => new HashSet<int>());

                    {
                        var entryValues = new HashSet<int>(allValues);
                        foreach (var anyStateTransition in anyStateTransitions)
                        {
                            var destState = anyStateTransition.destinationState;

                            var thisCondValue = new HashSet<int>(allValues);
                            foreach (var condition in anyStateTransition.conditions)
                            {
                                switch (condition.mode)
                                {
                                    case AnimatorConditionMode.Equals:
                                        thisCondValue.IntersectWith(new[] { (int)condition.threshold });
                                        break;
                                    case AnimatorConditionMode.NotEqual:
                                        thisCondValue.ExceptWith(new[] { (int)condition.threshold });
                                        break;
                                    default:
                                        throw new InvalidOperationException("unexpected mode");
                                }
                            }

                            thisCondValue.IntersectWith(entryValues);
                            entryValues.ExceptWith(thisCondValue);

                            valuePerState[destState].UnionWith(thisCondValue);
                        }

                        // assert all values are covered
                        if (entryValues.Count != 0) throw new InvalidOperationException("not all values are covered");
                    }

                    var baseCondition = anyStateTransitions.SelectMany(x => x.conditions).First();

                    foreach (var (state, valuesForState) in valuePerState)
                    {
                        if (valuesForState.Count == 0) continue; // this state is unreachable

                        var conditionCountWithNotEqual = valuesForState.Count;
                        var conditionCountWithEqual = allValues.Length - conditionCountWithNotEqual;

                        AnimatorStateTransition[] transitions;

                        if (conditionCountWithEqual < conditionCountWithNotEqual)
                        {
                            transitions = new AnimatorStateTransition[conditionCountWithEqual];
                            var i = 0;
                            foreach (var value in allValues.Except(valuesForState))
                                transitions[i++] = MakeExitTransition(new[]
                                    { baseCondition with { mode = AnimatorConditionMode.Equals, threshold = value } });
                        }
                        else
                        {
                            transitions = new AnimatorStateTransition[conditionCountWithNotEqual];
                            var i = 0;
                            foreach (var value in valuesForState)
                                transitions[i++] = MakeExitTransition(new[]
                                {
                                    baseCondition with { mode = AnimatorConditionMode.NotEqual, threshold = value }
                                });
                        }

                        state.transitions = transitions;
                    }
                }
            }

            // finally remove any state transitions
            stateMachine.anyStateTransitions = Array.Empty<AnimatorStateTransition>();
        }

        private static bool IsTautology(AnimatorCondition[][] allConditions, HashSet<string> boolParameters)
        {
            // for simplicity, we only check known int parameters or bool parameters
            // if we found unknown parameter, we assume them as not tautology condition
            // In addition, we only allow single parameter for condition.
            // This is likely to happen in real world with gesture animations or RISv4 animations 
            // TODO: support more complex pattern
            // note: this limit is used in DoConvert implementation so when relax we have to update DoConvert implementation

            var parameters = allConditions.SelectMany(x => x).Select(x => x.parameter).Distinct().ToArray();
            if (parameters.Length != 1) return false; // multiple parameters are not supported
            var targetParameter = parameters[0];

            if (boolParameters.Contains(targetParameter))
            {
                // check if there is condition for true and false
                var foundTrue = false;
                var foundFalse = false;
                foreach (var andCondition in allConditions)
                {
                    if (andCondition is not { Length: 1 }) return false; // only one condition is allowed
                    var condition = andCondition[0];
                    switch (condition.mode)
                    {
                        case AnimatorConditionMode.If:
                            foundTrue = true;
                            break;
                        case AnimatorConditionMode.IfNot:
                            foundFalse = true;
                            break;
                        default:
                            return false; // unknown mode
                    }
                }

                if (foundTrue && foundFalse) return true;
                return false;
            }
            else if (KnownParameterValues.GetIntValues(targetParameter, out var values))
            {
                // check if all values are covered
                var missingValues = new HashSet<int>(values);

                foreach (var andCondition in allConditions)
                {
                    var thisCondValue = new HashSet<int>(values);
                    foreach (var condition in andCondition)
                    {
                        switch (condition.mode)
                        {
                            case AnimatorConditionMode.Equals:
                                thisCondValue.IntersectWith(new[] { (int)condition.threshold });
                                break;
                            case AnimatorConditionMode.NotEqual:
                                thisCondValue.ExceptWith(new[] { (int)condition.threshold });
                                break;
                            default:
                                return false; // unexpected
                        }
                    }

                    missingValues.ExceptWith(thisCondValue);
                }

                // if all values are covered, it's tautology
                return missingValues.Count == 0;
            }
            else
            {
                // unknown parameter; it's too hard to check
                return false;
            }
        }

        private static object? GetCurrentValue(GameObject? rootGameObject, EditorCurveBinding curveBinding)
        {
            // for some types, we can't get current value
            if (curveBinding.type == typeof(Animator)) return null;
            if (rootGameObject == null) return null;

            var method = typeof(Editor).Assembly.GetType("UnityEditorInternal.CurveBindingUtility")
                .GetMethod("GetCurrentValue",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic
                    , null, new[] { typeof(GameObject), typeof(EditorCurveBinding) }, null);
            if (method == null) throw new InvalidOperationException("method not found");
            return method.Invoke(null, new object[] { rootGameObject, curveBinding });
        }
    }
}
