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
    // - if write defaults is off, all states have same animating properties
    public class AnyStateToEntryExit : AnimOptPassBase<AnyStateToEntryExit>
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
            // This optimization only works if root game object is known
            if (!controller.HasKnownRootGameObject) return;

            var boolParameters = new HashSet<string>(controller.parameters
                .Where(x => x.type is AnimatorControllerParameterType.Bool)
                .Select(x => x.name));

            // first, collect transformable layers
            var layers = controller.layers;
            foreach (var layer in layers)
            {
                if (CanConvert(layer, state, controller.RootGameObject, boolParameters))
                {
                    DoConvert(layer);
                }
            }
        }

        private static bool CanConvert(AOAnimatorControllerLayer layer,
            AnimatorOptimizerState optimizerState,
            GameObject rootGameObject,
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

                        // TODO: we can support exit time, duration, or offset if their settings are same in all transitions
                        hasExitTime: false,
                        duration: 0,
                        offset: 0,
                    })
                    return false;

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
                if (states[0].state.motion == null) return false; // with WD=off, motion=None will cause broken animator
                var expectAnimatingProperties = CollectAnimatingProperties(states[0].state.motion);
                if (expectAnimatingProperties == null) return false; // we found unsupported motion

                for (var index = 1; index < states.Length; index++)
                {
                    // check WD and animating properties
                    var childStateInfo = states[index];
                    if (childStateInfo is not { state: { motion: var motion, writeDefaultValues: false } })
                        return false;

                    if (motion == null) return false; // with WD=off, motion=None will cause broken animator
                    var newAnimatingProperties = CollectAnimatingProperties(motion);
                    if (newAnimatingProperties == null) return false; // we found unsupported motion
                    if (!newAnimatingProperties.SetEquals(expectAnimatingProperties)) return false;
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

            // default state must be no-op to current avatar
            {
                var defaultMotion = defaultState.motion;
                if (defaultMotion == null)
                {
                    // this means default state is WD-on and no motion
                    // therefore, this is no-op
                }
                else
                {
                    // in other cases, we have to check if it's no-op
                    var defaultClip = defaultMotion as AnimationClip;
                    if (defaultClip == null)
                        return false; // blendTree is unlikely to be no-op so we reject it // TODO: support blendTree
                    if (optimizerState.IsTimeDependentClip(defaultClip))
                        return false; // time dependent clip is not no-op

                    if (!IsNoop(defaultClip, rootGameObject)) return false;
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

            {
                var isBoolean = anyStateTransitions.Any(x =>
                    x.conditions.Any(y => y.mode is AnimatorConditionMode.If or AnimatorConditionMode.IfNot));

                AnimatorStateTransition MakeExitTransition(AnimatorCondition[] conditions) =>
                    new()
                    {
                        isExit = true,
                        mute = false,
                        solo = false,

                        canTransitionToSelf = false,

                        hasExitTime = false,
                        duration = 0,
                        offset = 0,
                        conditions = conditions
                    };

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
                else
                {
                    var targetParameter = anyStateTransitions[0].conditions[0].parameter;
                    if (!KnownParameterValues.GetIntValues(targetParameter, out var allValues))
                        throw new InvalidOperationException("unknown parameter");

                    var valuePerState = states.ToDictionary(x => x.state, _ => new HashSet<int>(allValues));

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

        // requirement: clip is not time dependent
        private static bool IsNoop(AnimationClip defaultClip, GameObject rootGameObject)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(defaultClip))
            {
                var curve = AnimationUtility.GetEditorCurve(defaultClip, binding);
                if (curve.length < 1) return false; // invalid curve
                var value = curve[0].value;
                var component = AnimationUtility.GetAnimatedObject(rootGameObject, binding);
                if (component == null) continue; // target is not exist: no-op

                var currentValue = GetCurrentValue(rootGameObject, binding);

                if (!value.Equals(currentValue)) return false; // not no-op
            }

            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(defaultClip))
            {
                var curve = AnimationUtility.GetObjectReferenceCurve(defaultClip, binding);
                if (curve.Length < 1) return false; // invalid curve
                var value = curve[0].value;
                var component = AnimationUtility.GetAnimatedObject(rootGameObject, binding);
                if (component == null) continue; // target is not exist: no-op

                var currentValue = GetCurrentValue(rootGameObject, binding);

                switch (currentValue, value)
                {
                    case (null, null):
                        break; // same value; no-op

                    case (null, not null): // either is null
                    case (not null, null): // either is null
                    case (not Object, _): // different type
                        return false;
                    case (Object obj, var obj2):
                        if (obj.GetInstanceID() != obj2.GetInstanceID()) return false;
                        break;
                }
            }

            return true;
        }

        private static object? GetCurrentValue(GameObject rootGameObject, EditorCurveBinding curveBinding)
        {
            // for some types, we can't get current value
            if (curveBinding.type == typeof(Animator)) return null;

            var method = typeof(Editor).Assembly.GetType("UnityEditorInternal.CurveBindingUtility")
                .GetMethod("GetCurrentValue",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic
                    , null, new[] { typeof(GameObject), typeof(EditorCurveBinding) }, null);
            if (method == null) throw new InvalidOperationException("method not found");
            return method.Invoke(null, new object[] { rootGameObject, curveBinding });
        }


        private static HashSet<EditorCurveBinding>? CollectAnimatingProperties(Motion? motion)
        {
            switch (motion)
            {
                case AnimationClip clip:
                    var result = new HashSet<EditorCurveBinding>();
                    result.UnionWith(AnimationUtility.GetCurveBindings(clip));
                    result.UnionWith(AnimationUtility.GetObjectReferenceCurveBindings(clip));
                    return result;
                case BlendTree tree:
                    if (tree.blendType == BlendTreeType.Direct) return null;
                    if (tree.children.Length == 0) return null; // unknown
                    var props = CollectAnimatingProperties(tree.children[0].motion);
                    if (props == null) return null;

                    for (var i = 1; i < tree.children.Length; i++)
                    {
                        var childProps = CollectAnimatingProperties(tree.children[i].motion);
                        if (childProps == null) return null;
                        if (!props.SetEquals(childProps)) return null;
                    }

                    return props;
                default:
                    return null;
            }
        }
    }
}
