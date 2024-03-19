using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;
using JetBrains.Annotations;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer
{
    /// <summary>
    /// converts entry-exit state machine to 1d blend tree
    ///
    /// Currently this class only supports entry transition with 1 parameter equals condition. 
    /// </summary>

    // detailed explanation of current limitations
    // This optimization expects
    // (about state machine)
    // - there are no child state machine
    // - there are no state machine behaviour
    // (about transitions)
    // - all transitions are associated with same single parameter
    // - all states are connected from entry transition
    // - all states are connected to exit
    // - there are no other transitions except entry and exit
    // - all states will leave state to exit when parameter value become values not listed in entry transitions
    // (semantics)
    // - each state has corresponding parameter value for the parameter
    // (motion / state)
    // - all states have same write defaults value
    // - if write defaults is off, all states have same animating properties
    // - all states must not have motion time. you have to use 1d blend tree for gesture weight.
    class EntryExitToBlendTree : AnimOptPassBase<EntryExitToBlendTree>
    {
        protected override void Execute(BuildContext context, AOAnimatorController controller,
            TraceAndOptimizeState settings)
        {
            if (settings.SkipEntryExitToBlendTree) return; // feature disabled

            var state = context.GetState<AnimatorOptimizerState>();
            Execute(state, controller);
        }

        public static void Execute(AnimatorOptimizerState state, AOAnimatorController controller)
        {
            var intParameters = new HashSet<string>(controller.parameters
                .Where(x => x.type == AnimatorControllerParameterType.Int)
                .Select(x => x.name));

            // first, collect transformable layers
            var layers = controller.layers;
            var convertInfos = new ConvertibleLayerInfo[layers.Count()];
            var layerByParameter = new Dictionary<string, List<int>>();
            for (var i = 0; i < layers.Length; i++)
            {
                var info = convertInfos[i] = TryParseLayer(layers[i], state, intParameters);
                if (info != null)
                {
                    foreach (var parameter in info.Parameters)
                    {
                        if (!layerByParameter.TryGetValue(parameter, out var list))
                            layerByParameter.Add(parameter, list = new List<int>());
                        list.Add(i);
                    }
                }
            }

            // finally, convert layers & change type of parameters

            for (var i = 0; i < layers.Length; i++)
            {
                if (convertInfos[i] == null) continue;
                var info = convertInfos[i];
                var layer = layers[i];

                DoConvert(info, layer);
            }

            var parameters = controller.parameters;
            foreach (ref var parameter in parameters.AsSpan())
                if (layerByParameter.ContainsKey(parameter.name))
                    parameter.type = AnimatorControllerParameterType.Float;

            Predicate<string> needsConvert = parameter => layerByParameter.ContainsKey(parameter);

            T[] ConvertTransitions<T>(T[] transitions, Func<T, AnimatorCondition[], T> clone)
                where T : AnimatorTransitionBase
            {
                if (transitions.Length == 0) return transitions;

                var entryTransitions = new LinkedList<T>(transitions);
                for (var cursor = entryTransitions.First; cursor != null; cursor = cursor.Next)
                {
                    if (!NeedsConversion(cursor.Value.conditions, needsConvert)) continue;
                    var conditions = cursor.Value.conditions;
                    var newConditions = ConvertIntConditionsToFloat(conditions, needsConvert);
                    var toRemove = cursor;
                    foreach (var animatorConditions in FlattenConditions(newConditions))
                        cursor = entryTransitions.AddAfter(cursor, clone(toRemove.Value, animatorConditions));
                    entryTransitions.Remove(toRemove);
                }

                return entryTransitions.ToArray();
            }

            AnimatorTransition CloneTransition(AnimatorTransition transition, AnimatorCondition[] conditions) =>
                new AnimatorTransition
                {
                    name = transition.name,
                    conditions = conditions,
                    destinationStateMachine = transition.destinationStateMachine,
                    destinationState = transition.destinationState,
                    solo = transition.solo,
                    mute = transition.mute,
                    isExit = transition.isExit,
                };

            AnimatorStateTransition CloneStateTransition(AnimatorStateTransition transition,
                AnimatorCondition[] conditions) =>
                new AnimatorStateTransition
                {
                    name = transition.name,
                    conditions = conditions,
                    destinationStateMachine = transition.destinationStateMachine,
                    destinationState = transition.destinationState,
                    solo = transition.solo,
                    mute = transition.mute,
                    isExit = transition.isExit,
                    duration = transition.duration,
                    offset = transition.offset,
                    exitTime = transition.exitTime,
                    hasExitTime = transition.hasExitTime,
                    hasFixedDuration = transition.hasFixedDuration,
                    interruptionSource = transition.interruptionSource,
                    orderedInterruption = transition.orderedInterruption,
                    canTransitionToSelf = transition.canTransitionToSelf,
                };

            for (var layerI = 0; layerI < layers.Length; layerI++)
            {
                if (convertInfos[layerI] != null) continue;
                var layer = layers[layerI];
                if (layer.IsSynced) continue;

                foreach (var stateMachine in ACUtils.AllStateMachines(layer.stateMachine))
                {
                    stateMachine.entryTransitions = ConvertTransitions(stateMachine.entryTransitions, CloneTransition);
                    stateMachine.anyStateTransitions =
                        ConvertTransitions(stateMachine.anyStateTransitions, CloneStateTransition);
                    foreach (var animatorState in stateMachine.states)
                        animatorState.state.transitions =
                            ConvertTransitions(animatorState.state.transitions, CloneStateTransition);

                    foreach (var child in stateMachine.stateMachines)
                    {
                        stateMachine.SetStateMachineTransitions(child.stateMachine,
                            ConvertTransitions(stateMachine.GetStateMachineTransitions(child.stateMachine),
                                CloneTransition));
                    }
                }
            }

            controller.parameters = parameters;
        }

        [CanBeNull]
        private static ConvertibleLayerInfo TryParseLayer(AOAnimatorControllerLayer layer,
            AnimatorOptimizerState optimizerState, HashSet<string> intParameters)
        {
            if (layer.IsSynced || layer.IsSyncedToOtherLayer) return null; // synced layer is not supported
            if (!layer.stateMachine) return null;
            var stateMachine = layer.stateMachine;
            var states = stateMachine.states;

            if (stateMachine.anyStateTransitions.Length != 0) return null;
            if (stateMachine.stateMachines.Length != 0) return null;
            if (stateMachine.defaultState == null) return null;
            if (stateMachine.states.Length < 2) return null;

            // check for conditions of entry transitions

            string conditionParameter = null;
            var stateValues = new Dictionary<AnimatorState, HashSet<int>>();
            var allValues = new HashSet<int>();
            foreach (var entryTransition in stateMachine.entryTransitions)
            {
                if (entryTransition.destinationStateMachine != null) return null;
                if (entryTransition.destinationState == null) return null;
                if (entryTransition.conditions.Length != 1) return null;

                var condition = entryTransition.conditions[0];
                if (condition.mode != AnimatorConditionMode.Equals) return null;
                if (conditionParameter == null)
                {
                    if (!intParameters.Contains(condition.parameter)) return null; // non int parameter
                    conditionParameter = condition.parameter;
                }
                else
                {
                    if (condition.parameter != conditionParameter) return null;
                }

                var dest = entryTransition.destinationState;
                if (!stateValues.TryGetValue(dest, out var values))
                    stateValues.Add(dest, values = new HashSet<int>());
                // not finite makes casting to int undefined
                if (float.IsNaN(condition.threshold) || float.IsInfinity(condition.threshold)) return null;
                var value = (int)condition.threshold;
                if (allValues.Contains(value)) return null; // duplicated value
                values.Add(value);
                allValues.Add(value);
            }

            // check there are no states without entry transition.
            var defaultState = stateMachine.defaultState;
            if (stateValues.ContainsKey(defaultState))
            {
                if (stateValues.Count != states.Length) return null;
            }
            else
            {
                if (stateValues.Count != states.Length - 1) return null;
            }

            // check for each states
            // we have to check
            // - write defaults are same in the layer
            // - if write defaults is off, check animating properties are same between layers
            // - exit transitions are correct for that state
            // - there are no other transitions
            // - there are no behaviors
            bool? writeDefaults = null;
            HashSet<EditorCurveBinding> animatingProperties = null;
            foreach (var childStateInfo in states)
            {
                var state = childStateInfo.state;
                if (!state) return null;
                if (state.behaviours.Length != 0)
                    return null; // we cannot execute state machine behaviour in blend tree
                // TODO: for linear animation, we can simulate motion time with 1d blend tree
                // https://github.com/anatawa12/AvatarOptimizer/issues/861
                if (state.timeParameterActive) return null; // motion time is not allowed. 

                var motion = state.motion;

                // check WD and animating properties
                if (writeDefaults == null)
                {
                    // first state in the stateMachine
                    writeDefaults = state.writeDefaultValues;
                    if (!state.writeDefaultValues)
                    {
                        if (!motion) return null; // with WD=off, motion=None will cause broken animator
                        animatingProperties = CollectAnimatingProperties(motion);
                        if (animatingProperties == null) return null; // we found unsupported motion
                    }
                }
                else
                {
                    // other states: check with first state
                    if (state.writeDefaultValues != writeDefaults) return null;
                    if (!state.writeDefaultValues)
                    {
                        if (!motion) return null; // with WD=off, motion=None will cause broken animator
                        var newAnimatingProperties = CollectAnimatingProperties(motion);
                        if (newAnimatingProperties == null) return null; // we found unsupported motion
                        Debug.Assert(animatingProperties != null, nameof(animatingProperties) + " != null");
                        if (!animatingProperties.SetEquals(newAnimatingProperties)) return null;
                    }
                }

                // the clip is time dependant, we cannot convert it to blend tree
                foreach (var clip in ACUtils.AllClips(motion))
                    if (optimizerState.IsTimeDependentClip(clip))
                        return null;

                // check for transitions
                var transitions = state.transitions;

                // basic transition check: all transitions are exit transitions without blending
                foreach (var transition in transitions)
                {
                    // it's not a exit transition
                    if (!transition.isExit) return null;
                    // hasExitTime = true means changing condition may not change state immediately
                    if (transition.hasExitTime) return null;
                    // duration != 0 means has blending
                    if (transition.duration != 0) return null;
                }

                // transition condition check.
                if (defaultState == state)
                {
                    // for default state, 
                    HashSet<int> exitValues;
                    if (stateValues.TryGetValue(state, out var values))
                    {
                        exitValues = new HashSet<int>(allValues);
                        exitValues.ExceptWith(values);
                    }
                    else
                    {
                        exitValues = allValues;
                    }

                    // for default states, it have to leave state if any of exit values are set
                    // TODO: users can create condition like `< minValue` or `> maxValue` to leave state
                    // https://github.com/anatawa12/AvatarOptimizer/issues/862
                    if (!MultipleEqualsTransition()
                        && !NotEqualsTransition()) return null;

                    bool MultipleEqualsTransition()
                    {
                        if (transitions.Length != exitValues.Count) return false;
                        foreach (var transition in transitions)
                        {
                            if (transition.conditions.Length != 1) return false;
                            var condition = transition.conditions[0];
                            if (condition.mode != AnimatorConditionMode.Equals) return false;
                            if (condition.parameter != conditionParameter) return false;
                            var value = (int)condition.threshold;
                            if (!exitValues.Remove(value)) return false;
                        }

                        return false;
                    }

                    bool NotEqualsTransition()
                    {
                        if (!KnownParameterValues.GetIntValues(conditionParameter, out var possibleValues))
                            return false;
                        var possibleValuesSet = new HashSet<int>(possibleValues);
                        possibleValuesSet.ExceptWith(exitValues);

                        return PossibleValuesExitTransitionCheck(possibleValuesSet);
                    }
                }
                else
                {
                    // for other states, it have to leave state if value is not any of current value
                    // TODO: users can create condition like `< minValue` or `> maxValue` to leave state
                    // https://github.com/anatawa12/AvatarOptimizer/issues/862
                    var values = stateValues[state];
                    if (!PossibleValuesExitTransitionCheck(values)) return null;
                }

                bool PossibleValuesExitTransitionCheck(HashSet<int> values)
                {
                    if (transitions.Length != 1) return false;
                    var transition = transitions[0];
                    var conditions = transition.conditions;
                    if (conditions.Length != values.Count) return false;

                    values = new HashSet<int>(values);
                    foreach (var condition in conditions)
                    {
                        if (condition.mode != AnimatorConditionMode.NotEqual) return false;
                        if (condition.parameter != conditionParameter) return false;
                        var value = (int)condition.threshold;
                        if (!values.Remove(value)) return false;
                    }

                    return true;
                }
            }

            return new ConvertibleLayerInfo(conditionParameter, defaultState, stateValues);
        }

        class ConvertibleLayerInfo
        {
            public readonly string ParameterName;
            public readonly AnimatorState DefaultState;
            public readonly Dictionary<AnimatorState, HashSet<int>> ValueForStates;

            public ConvertibleLayerInfo(string parameterName, AnimatorState defaultState,
                Dictionary<AnimatorState, HashSet<int>> valueForStates)
            {
                ParameterName = parameterName;
                DefaultState = defaultState;
                ValueForStates = valueForStates;
            }

            public IEnumerable<string> Parameters => new[] { ParameterName };
        }

        [CanBeNull]
        private static HashSet<EditorCurveBinding> CollectAnimatingProperties(Motion motion)
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

        private static void DoConvert(ConvertibleLayerInfo info, AOAnimatorControllerLayer layer)
        {
            var valueForStates = info.ValueForStates;
            valueForStates.Remove(info.DefaultState); // default states are proceed always so remove from this list
            var defaultMotion = info.DefaultState.motion;

            var states = new List<(int value, Motion motion)>();

            foreach (var (state, values) in valueForStates)
            foreach (var value in values)
                states.Add((value, state.motion));

            // sort increasing order
            states.Sort((x, y) => x.value.CompareTo(y.value));

            var children = new List<ChildMotion>();

            void AddFrames(int before, Motion beforeMotion, int after, Motion afterMotion)
            {
                var threshold = before + 0.5f;
                var rounded = Mathf.Round(threshold);
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (rounded == after)
                {
                    Debug.Assert((int)Mathf.Round(Utils.PreviousFloat(threshold)) == before);
                    // in this case, .5 will go the motion so default is one before .5
                    children.Add(CreateChild(Utils.PreviousFloat(threshold), beforeMotion));
                    children.Add(CreateChild(threshold, afterMotion));
                }
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                else if (rounded == before)
                {
                    Debug.Assert((int)Mathf.Round(Utils.NextFloat(threshold)) == after);
                    // in this case, .5 will go to default motion so default is .5
                    children.Add(CreateChild(threshold, beforeMotion));
                    children.Add(CreateChild(Utils.NextFloat(threshold), afterMotion));
                }
                else
                {
                    throw new InvalidOperationException("unexpected: rounding x - 0.5 is not x - 1 or x");
                }
            }

            {
                // first frame: add defaultMotion before first state
                var (value, motion) = states[0];
                AddFrames(value - 1, defaultMotion,
                    value, motion);
            }

            for (var i = 1; i < states.Count; i++)
            {
                // other frames: add motions if needed
                var (prevValue, prevMotion) = states[i - 1];
                var (currentValue, currentMotion) = states[i];

                if (prevMotion != currentMotion)
                    AddFrames(prevValue, prevMotion,
                        currentValue, currentMotion);
            }

            {
                // last frame: add last state to defaultMotion
                var (value, motion) = states[states.Count - 1];
                AddFrames(value, motion,
                    value + 1, defaultMotion);
            }

            var blendTree = new BlendTree
            {
                blendType = BlendTreeType.Simple1D,
                blendParameter = info.ParameterName,
                useAutomaticThresholds = false,
                minThreshold = children.First().threshold,
                maxThreshold = children.Last().threshold,
                name = $"EntryExit to BlendTree by AAO for {layer.name}",
                children = children.ToArray(),
            };

            var newState = new AnimatorState()
            {
                name = $"EntryExit to BlendTree by AAO for {layer.name}",
                motion = blendTree,
                writeDefaultValues = true, // WD on to avoid unexpected blendtree behaviour
            };

            layer.stateMachine.states = new ChildAnimatorState[]
            {
                new ChildAnimatorState()
                {
                    position = Vector3.zero,
                    state = newState,
                }
            };

            layer.stateMachine.entryTransitions = Array.Empty<AnimatorTransition>();
            layer.stateMachine.defaultState = newState;

            ChildMotion CreateChild(float value, Motion motion) =>
                new ChildMotion
                {
                    motion = motion,
                    timeScale = 1,
                    threshold = value,
                    directBlendParameter = "",
                };
        }

        public static IEnumerable<AnimatorCondition[]> FlattenConditions(AnimatorCondition[][] conditions)
        {
            var indices = new int[conditions.Length];

            while (true)
            {
                var result = new AnimatorCondition[conditions.Length];
                for (var i = 0; i < conditions.Length; i++)
                    result[i] = conditions[i][indices[i]];

                yield return result;

                for (var i = 0; i < conditions.Length; i++)
                {
                    indices[i]++;
                    if (indices[i] < conditions[i].Length) break;
                    indices[i] = 0;
                    if (i == conditions.Length - 1) yield break;
                }
            }
        }

        public static bool NeedsConversion(AnimatorCondition[] conditions, Predicate<string> shouldConvert)
        {
            foreach (var condition in conditions)
                if (shouldConvert(condition.parameter))
                    return true;
            return false;
        }

        // AnimatorCondition[and][or]
        public static AnimatorCondition[][] ConvertIntConditionsToFloat(AnimatorCondition[] condition,
            Predicate<string> shouldConvert)
        {
            var result = new List<AnimatorCondition[]>();
            foreach (var cond in condition)
                if (shouldConvert(cond.parameter))
                    result.AddRange(ConvertIntConditionToFloat(cond));
                else
                    result.Add(new[] { cond });
            return result.ToArray();
        }

        // AnimatorCondition[and][or]
        public static AnimatorCondition[][] ConvertIntConditionToFloat(AnimatorCondition condition)
        {
            switch (condition.mode)
            {
                case AnimatorConditionMode.Greater:
                {
                    var thresholdRound = Mathf.Round(condition.threshold);
                    var threshold = thresholdRound - 0.5f;
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (Mathf.Round(threshold) == thresholdRound)
                        threshold = Utils.PreviousFloat(threshold);

                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    Debug.Assert(Mathf.Round(threshold) == thresholdRound - 1);
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    Debug.Assert(Mathf.Round(Utils.NextFloat(threshold)) == thresholdRound);

                    return new[]
                    {
                        new[]
                        {
                            new AnimatorCondition()
                            {
                                mode = AnimatorConditionMode.Greater,
                                parameter = condition.parameter,
                                threshold = threshold,
                            },
                        },
                    };
                }
                case AnimatorConditionMode.Less:
                {
                    var thresholdRound = Mathf.Round(condition.threshold);
                    var threshold = thresholdRound + 0.5f;
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (Mathf.Round(threshold) == thresholdRound)
                        threshold = Utils.NextFloat(threshold);

                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    Debug.Assert(Mathf.Round(threshold) == thresholdRound + 1);
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    Debug.Assert(Mathf.Round(Utils.PreviousFloat(threshold)) == thresholdRound);

                    return new[]
                    {
                        new[]
                        {
                            new AnimatorCondition()
                            {
                                mode = AnimatorConditionMode.Less,
                                parameter = condition.parameter,
                                threshold = threshold,
                            },
                        },
                    };
                }
                case AnimatorConditionMode.Equals:
                {
                    var thresholdRound = Mathf.Round(condition.threshold);
                    var lowerThreshold = thresholdRound - 0.5f;
                    var upperThreshold = thresholdRound + 0.5f;

                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (Mathf.Round(lowerThreshold) == thresholdRound)
                        lowerThreshold = Utils.PreviousFloat(lowerThreshold);
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (Mathf.Round(upperThreshold) == thresholdRound)
                        upperThreshold = Utils.NextFloat(upperThreshold);

                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    Debug.Assert(Mathf.Round(lowerThreshold) == thresholdRound - 1);
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    Debug.Assert(Mathf.Round(Utils.NextFloat(lowerThreshold)) == thresholdRound);
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    Debug.Assert(Mathf.Round(Utils.PreviousFloat(upperThreshold)) == thresholdRound);
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    Debug.Assert(Mathf.Round(upperThreshold) == thresholdRound + 1);

                    // AND condition
                    return new[]
                    {
                        new[]
                        {
                            new AnimatorCondition()
                            {
                                mode = AnimatorConditionMode.Greater,
                                parameter = condition.parameter,
                                threshold = lowerThreshold,
                            },
                        },
                        new[]
                        {
                            new AnimatorCondition()
                            {
                                mode = AnimatorConditionMode.Less,
                                parameter = condition.parameter,
                                threshold = upperThreshold,
                            },
                        },
                    };
                }
                case AnimatorConditionMode.NotEqual:
                {
                    var thresholdRound = Mathf.Round(condition.threshold);
                    var lowerThreshold = thresholdRound - 0.5f;
                    var upperThreshold = thresholdRound + 0.5f;

                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (Mathf.Round(lowerThreshold) != thresholdRound)
                        lowerThreshold = Utils.NextFloat(lowerThreshold);
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (Mathf.Round(upperThreshold) != thresholdRound)
                        upperThreshold = Utils.PreviousFloat(upperThreshold);

                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    Debug.Assert(Mathf.Round(Utils.PreviousFloat(lowerThreshold)) == thresholdRound - 1);
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    Debug.Assert(Mathf.Round(lowerThreshold) == thresholdRound);
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    Debug.Assert(Mathf.Round(upperThreshold) == thresholdRound);
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    Debug.Assert(Mathf.Round(Utils.NextFloat(upperThreshold)) == thresholdRound + 1);

                    // OR condition
                    return new[]
                    {
                        new[]
                        {
                            new AnimatorCondition()
                            {
                                mode = AnimatorConditionMode.Less,
                                parameter = condition.parameter,
                                threshold = lowerThreshold,
                            },
                            new AnimatorCondition()
                            {
                                mode = AnimatorConditionMode.Greater,
                                parameter = condition.parameter,
                                threshold = upperThreshold,
                            },
                        },
                    };
                }

                // for bool
                case AnimatorConditionMode.If:
                case AnimatorConditionMode.IfNot:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}