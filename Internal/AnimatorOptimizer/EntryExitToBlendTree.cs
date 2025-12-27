using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;
using nadena.dev.ndmf;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

#if AAO_VRCSDK3_AVATARS
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
#endif

namespace Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer
{
    using IntRangeSet = RangeSet<int, RangeIntTrait>;
    using FloatRange = Range<float, RangeFloatTrait>;
    using FloatRangeSet = RangeSet<float, RangeFloatTrait>;

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
    [RunsOnPlatforms(WellKnownPlatforms.VRChatAvatar30)] // EntryExit to BlendTree optimization heavily depends on VRChat's behavior
    public class EntryExitToBlendTree : AnimOptPassBase<EntryExitToBlendTree>
    {
        private static CachedGuidLoader<AnimationClip> _emptyClip = "ce6c609e7fd58444d9d59e98296eed35";

        public override string DisplayName => "Animator Optimizer: Entry-Exit to BlendTree";
        protected override bool Enabled(TraceAndOptimizeState state) => state.EntryExitToBlendTree;

        private protected override void Execute(BuildContext context, AOAnimatorController controller,
            TraceAndOptimizeState settings) =>
            Execute(context.GetState<AnimatorOptimizerState>(), controller);

        public static void Execute(AnimatorOptimizerState state, AOAnimatorController controller)
        {
            var parameterType = controller.parameters.ToDictionary(x => x.name, x => x.type);

            // first, collect transformable layers
            var layers = controller.layers;
            var convertInfos = new ConvertibleLayerInfo?[layers.Length];
            var layerByParameter = new Dictionary<string, List<int>>();
            for (var i = 0; i < layers.Length; i++)
            {
                var info = TryParseDiamondLayer(layers[i], state, parameterType);
                info ??= TryParseLinearLayer(layers[i], state, parameterType);
                convertInfos[i] = info;
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
                if (convertInfos[i] is not { } info) continue;
                var layer = layers[i];
                DoConvert(info, layer);
            }

            Predicate<string> needsConvert = parameter => layerByParameter.ContainsKey(parameter) && parameterType[parameter] != AnimatorControllerParameterType.Float;

            var parameters = controller.parameters;
            
            // Track parameter type changes for driver correction
            var parameterTypeChanges = new Dictionary<string, AnimatorControllerParameterType>();
            foreach (var parameter in parameters)
                if (needsConvert(parameter.name))
                    parameterTypeChanges[parameter.name] = parameter.type;
            
            // Change parameter types to float
            foreach (ref var parameter in parameters.AsSpan())
                if (layerByParameter.ContainsKey(parameter.name))
                    parameter.type = AnimatorControllerParameterType.Float;

#if AAO_VRCSDK3_AVATARS
            // Correct parameter drivers to preserve behavior after type change
            CorrectParameterDrivers(controller, parameterTypeChanges, ref parameters);
#endif

            T[] ConvertTransitions<T>(T[] transitions, Func<T, AnimatorCondition[], T> clone)
                where T : AnimatorTransitionBase
            {
                if (transitions.Length == 0) return transitions;

                var entryTransitions = new LinkedList<T>(transitions);
                for (var cursor = entryTransitions.First; cursor != null; cursor = cursor.Next)
                {
                    if (!NeedsConversion(cursor.Value.conditions, needsConvert)) continue;
                    var conditions = cursor.Value.conditions;
                    var newConditions = ConvertIntOrBoolConditionsToFloat(conditions, needsConvert);
                    var toRemove = cursor;
                    foreach (var animatorConditions in FlattenConditions(newConditions))
                        cursor = entryTransitions.AddAfter(cursor, clone(toRemove.Value, animatorConditions));
                    entryTransitions.Remove(toRemove);
                }

                return entryTransitions.ToArray();
            }

            AnimatorTransition CloneTransition(AnimatorTransition transition, AnimatorCondition[] conditions) => new()
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
                AnimatorCondition[] conditions) => new()
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
                        var transitions = stateMachine.GetStateMachineTransitions(child.stateMachine);
                        if (transitions.Length == 0) continue;
                        stateMachine.SetStateMachineTransitions(child.stateMachine,
                            ConvertTransitions(transitions, CloneTransition));
                    }
                }
            }

            controller.parameters = parameters;
        }

        /// <summary>
        /// Parses diamond entry-exit state machine like the following:
        /// 
        /// <code>
        ///                    +---------------+
        ///                    | Default State |
        ///                 /  +---------------+ \
        ///                /   +---------------+  \      
        ///   +----------+  /  |   2nd State   | \  +----------+
        ///   |  Entry   |     +---------------+    |   Exit   |
        ///   +----------+            ...           +----------+
        ///                 \         ...         /        
        ///                    +---------------+ 
        ///                    |   nth State   |
        ///                    +---------------+ 
        /// </code>
        /// </summary>
        private static ConvertibleLayerInfo? TryParseDiamondLayer(AOAnimatorControllerLayer layer,
            AnimatorOptimizerState optimizerState, Dictionary<string, AnimatorControllerParameterType> parameterType)
        {
            if (!CheckForBasicStateCondition(layer, optimizerState, out var timeMotionParameter)) return null;

            if (layer is not
                {
                    stateMachine:
                    {
                        anyStateTransitions: { Length: 0 },
                        stateMachines: { Length: 0 },
                        defaultState: { } defaultState,
                        states: { Length: >= 2 } states,
                        entryTransitions: { Length: >= 1 } entryTransitions,
                    }
                })
                return null;

            // check for conditions of entry transitions

            // 1st check: all transitions are about the same single parameter
            var conditionParameter = entryTransitions.SelectMany(x => x.conditions).Select(x => x.parameter).DistinctSingleOrDefaultIfNoneOrMultiple();
            if (conditionParameter == null) return null; // no entry transitions or multiple parameters used in entry transitions

            // 2nd check: transitions are focusing on state
            foreach (var entryTransition in entryTransitions)
            {
                if (entryTransition is not
                    {
                        isExit: false,
                        destinationStateMachine: null,
                        destinationState: not null,
                        conditions: not null,
                    }) return null;
            }

            // 3rd check: parameter is int or bool
            if (!parameterType.TryGetValue(conditionParameter, out var paramType)) return null; // parameter type is not int nor bool

            switch (paramType)
            {
                case AnimatorControllerParameterType.Float:
                    return ProcessLayerByType<FloatRangeSet, FloatSetTrait>(conditionParameter, timeMotionParameter, defaultState, states, entryTransitions);
                case AnimatorControllerParameterType.Int:
                    return ProcessLayerByType<IntRangeSet, IntSetTrait>(conditionParameter, timeMotionParameter, defaultState, states, entryTransitions);
                case AnimatorControllerParameterType.Bool: 
                    return ProcessLayerByType<BoolSet, BoolSetTrait>(conditionParameter, timeMotionParameter, defaultState, states, entryTransitions);
                default: return null;
            }
            
        }

        static ConvertibleLayerInfo? ProcessLayerByType<TRangeSet, TTrait>(
            string conditionParameter,
            string? timeMotionParameter,
            AnimatorState defaultState,
            ChildAnimatorState[] states,
            AnimatorTransition[] entryTransitions)
            where TRangeSet : struct, IRangeSet<TRangeSet>
            where TTrait : struct, ISetTrait<TRangeSet>
        {
            {
                {
                    var allRanges = default(TTrait).Empty;
                    var stateRanges = new Dictionary<AnimatorState, TRangeSet>();
                    foreach (var entryTransition in entryTransitions)
                    {
                        var range = default(TTrait).SetFromConditions(entryTransition.conditions!);
                        var state = entryTransition.destinationState!;

                        if (!allRanges.Intersect(range).IsEmpty()) return null; // duplicated range

                        stateRanges.TryGetValue(state, out var values);
                        stateRanges[state] = values.Union(range);
                        allRanges = allRanges.Union(values);
                    }

                    // check there are no states without entry transition (including default transition).
                    stateRanges.TryAdd(defaultState, default(TTrait).Empty);
                    if (stateRanges.Count != states.Length) return null;

                    // check for transitions
                    foreach (var childStateInfo in states)
                    {
                        var state = childStateInfo.state;
                        var transitions = state.transitions;
                        // basic transition check: all transitions are exit transitions without blending
                        var allConditions = new AnimatorCondition[transitions.Length][];
                        for (var i = 0; i < transitions.Length; i++)
                        {
                            var transition = transitions[i];
                            if (transition is not
                                {
                                    isExit: true,
                                    solo: false,
                                    mute: false,
                                    destinationState: null,
                                    destinationStateMachine: null,
                                    conditions: { } conditions,

                                    hasExitTime: false,
                                    duration: 0,
                                    offset: 0,
                                    // since duration is zero, interruption should not be happened
                                }) return null;
                            allConditions[i] = conditions;
                        }

                        // transition condition check.
                        if (defaultState == state)
                        {
                            // for default state, we check if we exit the default state if the value is any other states value.
                            // We allow too relaxed condition for exiting default state since it will re-enter the default state.
                            var exitValues = allRanges;
                            exitValues = exitValues.Exclude(stateRanges[state]);

                            foreach (var conditions in allConditions)
                            {
                                // conditions with parameters other than conditionParameter can be false
                                if (conditions.Any(c => c.parameter != conditionParameter)) continue;

                                exitValues = exitValues.Exclude(default(TTrait).SetFromConditions(conditions));
                            }

                            if (!exitValues.IsEmpty()) return null;
                        }
                        else
                        {
                            // for other states, it have to leave state iff value is not any of current value

                            // conditions with parameters other than conditionParameter can be false
                            if (allConditions.Any(conditions => conditions.Any(c => c.parameter != conditionParameter)))
                                return null; // non-conditionParameter condition found

                            var exitingValues =
                                default(TTrait).Union(allConditions.Select(default(TTrait).SetFromConditions));
                            var expectedToExitRange = stateRanges[state].Complement();
                            if (!exitingValues.Equals(expectedToExitRange)) return null;
                        }
                    }

                    return new ConvertibleLayerInfo(conditionParameter, defaultState,
                        stateRanges.ToDictionary(p => p.Key, p => default(TTrait).ConvertToFloatRangeSet(p.Value)),
                        timeMotionParameter);
                }
            }
        }

        /// <summary>
        /// Parses linear entry-exit state machine like the following:
        /// This pattern can only support exactly two states, one of which is the default state and the other is the second state.
        ///
        /// <code>
        /// +----------+       +-----------+       +-----------+       +----------+
        /// |  Entry   |  ==>  | 1st State |  ==>  | 2nd state |  ==>  |   Exit   |
        /// +----------+       +-----------+       +-----------+       +----------+
        /// </code>
        /// </summary>
        private static ConvertibleLayerInfo? TryParseLinearLayer(AOAnimatorControllerLayer layer,
            AnimatorOptimizerState optimizerState,
            Dictionary<string, AnimatorControllerParameterType> parameterType)
        {
            if (!CheckForBasicStateCondition(layer, optimizerState, out var timeMotionParameter)) return null;

            if (layer is not
                {
                    stateMachine:
                    {
                        anyStateTransitions: { Length: 0 },
                        stateMachines: { Length: 0 },
                        defaultState: { } defaultState,
                        states: { Length: 2 } states,
                        entryTransitions: { Length: 0 },
                    }
                })
                return null;

            // prerequirements of statemachine
            if (!states.Any(x => x.state == defaultState)) return null; // default state must be one of the states
            var anotherState = states.First(x => x.state != defaultState).state;

            // basic transition check: all transitions does not have exit time, duration, and not solo nor mute.
            if (!defaultState.transitions.Concat(anotherState.transitions).All(t => t is
                {
                    solo: false,
                    mute: false,

                    hasExitTime: false,
                    duration: 0,
                    offset: 0,
                    // since duration is zero, interruption should not be happened
                }))
            {
                return null;
            }

            var conditionParameter = defaultState.transitions.Concat(anotherState.transitions).SelectMany(x => x.conditions).Select(x => x.parameter).DistinctSingleOrDefaultIfNoneOrMultiple();
            if (conditionParameter == null) return null; // no entry transitions or multiple parameters used in entry transitions

            if (!parameterType.TryGetValue(conditionParameter, out var paramType)) return null; // parameter type is not int nor bool

            switch (paramType)
            {
                case AnimatorControllerParameterType.Float:
                    return TryParseLinearLayerByType<FloatRangeSet, FloatSetTrait>(conditionParameter, timeMotionParameter, defaultState, anotherState);
                case AnimatorControllerParameterType.Int:
                    return TryParseLinearLayerByType<IntRangeSet, IntSetTrait>(conditionParameter, timeMotionParameter, defaultState, anotherState);
                case AnimatorControllerParameterType.Bool:
                    return TryParseLinearLayerByType<BoolSet, BoolSetTrait>(conditionParameter, timeMotionParameter, defaultState, anotherState);
                default:
                    return null;
            }
        }

        private static ConvertibleLayerInfo? TryParseLinearLayerByType<TRangeSet, TTrait>(
            string conditionParameter,
            string? timeMotionParameter,
            AnimatorState defaultState,
            AnimatorState anotherState)
            where TRangeSet : struct, IRangeSet<TRangeSet>
            where TTrait : struct, ISetTrait<TRangeSet>
        {
            // Check default => another state transition.
            foreach (var defaultStateTransition in defaultState.transitions)
            {
                if (defaultStateTransition is not
                    {
                        isExit: false,
                        destinationStateMachine: null,
                        destinationState: var dest,
                        conditions: not null,
                    })
                    return null;

                if (dest != anotherState) return null; // default state must have transition to the 'another state'
            }

            var anotherStateValues = default(TTrait).Empty;
            foreach (var defaultStateTransition in defaultState.transitions)
            {
                var conditions = defaultStateTransition.conditions!;
                anotherStateValues = anotherStateValues.Union(default(TTrait).SetFromConditions(conditions));
            }

            // check another => exit transition
            {
                var transitions = anotherState.transitions;
                // basic transition check: all transitions are exit transitions without blending
                var allConditions = new AnimatorCondition[transitions.Length][];
                for (var i = 0; i < transitions.Length; i++)
                {
                    var transition = transitions[i];
                    if (transition is not
                        {
                            // target
                            isExit: true,
                            destinationState: null,
                            destinationStateMachine: null,
                            // conditions
                            conditions: { } conditions,
                        }) return null;
                    allConditions[i] = conditions;
                }

                // transition condition check.
                if (allConditions.Any(conditions => conditions.Any(c => c.parameter != conditionParameter))) return null;

                var expectedToExitRange = anotherStateValues.Complement();
                var exitingValues = default(TTrait).Union(allConditions.Select(default(TTrait).SetFromConditions));
                if (!exitingValues.Equals(expectedToExitRange)) return null;
            }

            return new ConvertibleLayerInfo(conditionParameter, defaultState, new Dictionary<AnimatorState, FloatRangeSet> {{anotherState, default(TTrait).ConvertToFloatRangeSet(anotherStateValues)}}, timeMotionParameter);
        }

        private static bool CheckForBasicStateCondition(AOAnimatorControllerLayer layer, AnimatorOptimizerState optimizerState, out string? timeMotionParameter)
        {
            timeMotionParameter = null;
            if (layer is not
                {
                    IsSynced: false,
                    IsSyncedToOtherLayer: false,
                    stateMachine:
                    {
                        states: { Length: >= 2 } states,
                    }
                })
                return false;

            // check for each states
            // we have to check
            // - write defaults are same in the layer
            // - if write defaults is off, check animating properties are same between layers
            // - exit transitions are correct for that state
            // - there are no other transitions
            // - there are no behaviors

            // check write defaults and animating properties
            if (states[0].state.writeDefaultValues)
            {
                for (var index = 1; index < states.Length; index++)
                {
                    // check WD
                    if (states[index] is not { state: { writeDefaultValues: true } }) return false;
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

            // In previous version of AAO, we denied motion with time dependency.
            // However, it seems unity's BlendTree can handle motion with time dependency correctly.
            // BlendTree does change the length of the motion field depending on the weight.
            // If there is two motion with 1s and 2s length respectively,
            // when weight for first motion is 0.0, the length of blend tree is 2s,
            // when weight for second motion is 0.0, the length of blend tree is 1s.
            // The BlendTree after this optimization will never have weight other than 0.0 for motions,
            // so the length of the BlendTree will always be same as the motion which has weight.
            // So if the motion has time dependency, the behavior is same as expecte if motion time is used.
            // Please note that we cannot optimize if there is no motion time parameter because
            // in original state machine, the 'time' is reset to 0 when entering state,
            // but in BlendTree, the 'time' is never reset.

            foreach (var childStateInfo in states)
            {
                // TODO: for linear animation, we can simulate motion time with 1d blend tree
                // https://github.com/anatawa12/AvatarOptimizer/issues/861

                if (childStateInfo is not
                    {
                        state:
                        {
                            behaviours: { Length: 0 },
                            motion: var motion,
                        },
                    }) return false;

                if (childStateInfo.state.timeParameterActive)
                {
                    // we allow any motion including time dependency motion if timeParameter is assigned
                    // however, all timeParameter must be same
                    if (timeMotionParameter == null)
                    {
                        timeMotionParameter = childStateInfo.state.timeParameter ?? "";
                    }
                    else
                    {
                        if (timeMotionParameter != childStateInfo.state.timeParameter) return false;
                    }
                }
                else
                {
                    // if timeParameter is not assigned, motion must not have time dependency
                    foreach (var clip in ACUtils.AllClips(motion))
                        if (optimizerState.IsTimeDependentClip(clip))
                            return false;
                }
            }

            return true;
        }

        class ConvertibleLayerInfo
        {
            public readonly string ParameterName;
            public readonly AnimatorState DefaultState;
            public readonly Dictionary<AnimatorState, FloatRangeSet> RangeForStates;
            public readonly string? TimeMotionParameter;

            public ConvertibleLayerInfo(string parameterName, AnimatorState defaultState,
                Dictionary<AnimatorState, FloatRangeSet> rangeForStates,
                string? timeMotionParameter)
            {
                ParameterName = parameterName;
                DefaultState = defaultState;
                RangeForStates = rangeForStates; 
                TimeMotionParameter = timeMotionParameter;
            }


            public IEnumerable<string> Parameters => new[] { ParameterName };
        }

        private static HashSet<EditorCurveBinding>? CollectAnimatingProperties(Motion? motion)
        {
            switch (motion)
            {
                case AnimationClip clip:
                    return clip.GetBindings();
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
            var valueForStates = info.RangeForStates;
            valueForStates.Remove(info.DefaultState); // default states are proceed always so remove from this list
            var defaultMotion = info.DefaultState.motion ? info.DefaultState.motion : _emptyClip.Value;

            var usedRanges = FloatRangeSet.Union(valueForStates.Values);
            var rangeForDefault = usedRanges.Complement();

            var statesWithRange = new List<(FloatRange range, Motion motion)>();
            foreach (var (state, rangeSet) in valueForStates)
            foreach (var range in rangeSet.Ranges)
                statesWithRange.Add((range, state.motion ? state.motion : _emptyClip.Value));
            foreach (var range in rangeForDefault.Ranges)
                statesWithRange.Add((range, defaultMotion));

            statesWithRange.Sort((a, b) => a.range.MinInclusive.CompareTo(b.range.MinInclusive));

            var children = new List<ChildMotion>();

            foreach (var ((prevRange, prevMotion), (nextRange, nextMoton)) in statesWithRange.ZipWithNext())
            {
                if (!prevRange.MinInclusive.Equals(prevRange.MaxInclusive))
                    children.Add(CreateChild(prevRange.MaxInclusive, prevMotion));
                children.Add(CreateChild(nextRange.MinInclusive, nextMoton));
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
                // since it's 1d BlendTree, it's always be normalized wo WD off will not cause weird behavior.
                // changing to WD on will cause problem with WD off-based animators
                writeDefaultValues = info.DefaultState.writeDefaultValues,
                timeParameterActive = info.TimeMotionParameter != null,
                timeParameter = info.TimeMotionParameter,
            };

            layer.stateMachine!.states = new[]
            {
                new ChildAnimatorState()
                {
                    position = Vector3.zero,
                    state = newState,
                }
            };

            layer.stateMachine.entryTransitions = Array.Empty<AnimatorTransition>();
            layer.stateMachine.defaultState = newState;

            ChildMotion CreateChild(float value, Motion motion) => new()
            {
                motion = motion,
                timeScale = 1,
                threshold = value,
                directBlendParameter = "",
            };
        }

        public static IEnumerable<AnimatorCondition[]> FlattenConditions(List<AnimatorCondition[]> conditions)
        {
            var indices = new int[conditions.Count];

            while (true)
            {
                var result = new AnimatorCondition[conditions.Count];
                for (var i = 0; i < conditions.Count; i++)
                    result[i] = conditions[i][indices[i]];

                yield return result;

                for (var i = 0; i < conditions.Count; i++)
                {
                    indices[i]++;
                    if (indices[i] < conditions[i].Length) break;
                    indices[i] = 0;
                    if (i == conditions.Count - 1) yield break;
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
        public static List<AnimatorCondition[]> ConvertIntOrBoolConditionsToFloat(AnimatorCondition[] condition,
            Predicate<string> shouldConvert)
        {
            var result = new List<AnimatorCondition[]>();
            foreach (var cond in condition)
                if (shouldConvert(cond.parameter))
                    result.AddRange(ConvertIntOrBoolConditionToFloat(cond));
                else
                    result.Add(new[] { cond });
            return result;
        }

        // AnimatorCondition[and][or]
        public static List<AnimatorCondition[]> ConvertIntOrBoolConditionToFloat(AnimatorCondition condition)
        {
            var rangeSet = condition.mode switch
            {
                AnimatorConditionMode.If => BoolSet.FromValue(true).ToFloatRangeSet(),
                AnimatorConditionMode.IfNot => BoolSet.FromValue(false).ToFloatRangeSet(),
                AnimatorConditionMode.Greater or AnimatorConditionMode.Less or AnimatorConditionMode.Equals
                    or AnimatorConditionMode.NotEqual => RangesUtil.IntRangeSetFromConditions(new[] { condition }).ToFloatRangeSet(),
                _ => throw new ArgumentOutOfRangeException()
            };
            return RangesToFloatConditions(rangeSet, condition.parameter);
        }

        // AnimatorCondition[and][or]
        private static List<AnimatorCondition[]> RangesToFloatConditions(FloatRangeSet rangeSet, string parameterName)
        {
            var ranges = rangeSet.Ranges.ToArray();
            if (ranges.Length == 1)
            {
                var range = ranges[0];

                var result = new List<AnimatorCondition[]>();
                if (range.MinExclusive is { } minExclusive)
                    result.Add(new[]
                    {
                        new AnimatorCondition
                        {
                            parameter = parameterName,
                            mode = AnimatorConditionMode.Greater,
                            threshold = minExclusive,
                        }
                    });
                if (range.MaxExclusive is { } maxExclusive)
                    result.Add(new[]
                    {
                        new AnimatorCondition
                        {
                            parameter = parameterName,
                            mode = AnimatorConditionMode.Less,
                            threshold = maxExclusive,
                        }
                    });
                return result;
            }

            if (ranges.Length == 2)
            {
                var range0 = ranges[0];
                var range1 = ranges[1];
                Utils.Assert(range0.MinInclusive.Equals(float.NegativeInfinity));
                Utils.Assert(range1.MaxInclusive.Equals(float.PositiveInfinity));
                return new List<AnimatorCondition[]>()
                {
                    new []
                    {
                        new AnimatorCondition
                        {
                            parameter = parameterName,
                            mode = AnimatorConditionMode.Less,
                            threshold = range0.MaxExclusive!.Value,
                        },
                        new AnimatorCondition
                        {
                            parameter = parameterName,
                            mode = AnimatorConditionMode.Greater,
                            threshold = range1.MinExclusive!.Value,
                        },
                    },
                };
            }
            else
            {
                throw new InvalidOperationException("Cannot convert range set with more than two ranges to conditions");
            }
        }

#if AAO_VRCSDK3_AVATARS
        /// <summary>
        /// Corrects VRChat parameter drivers when parameter types are changed from bool/int to float.
        /// This preserves the original behavior by creating intermediate parameters with the original type.
        /// Based on NDMF PR #693: https://github.com/bdunderscore/ndmf/pull/693
        /// </summary>
        private static void CorrectParameterDrivers(AOAnimatorController controller,
            Dictionary<string, AnimatorControllerParameterType> parameterTypeChanges,
            ref AnimatorControllerParameter[] parameters)
        {
            if (parameterTypeChanges.Count == 0) return;

            var parametersToAdd = new List<AnimatorControllerParameter>();

            foreach (var layer in controller.layers)
            {
                if (layer.stateMachine == null) continue;

                foreach (var stateMachine in ACUtils.AllStateMachines(layer.stateMachine))
                {
                    // Check state machine behaviors
                    if (stateMachine.behaviours != null)
                    {
                        foreach (var behaviour in stateMachine.behaviours)
                        {
                            if (behaviour is VRCAvatarParameterDriver driver)
                            {
                                CorrectDriverParameters(driver, parameterTypeChanges, parametersToAdd);
                            }
                        }
                    }

                    // Check state behaviors
                    foreach (var childState in stateMachine.states)
                    {
                        if (childState.state.behaviours != null)
                        {
                            foreach (var behaviour in childState.state.behaviours)
                            {
                                if (behaviour is VRCAvatarParameterDriver driver)
                                {
                                    CorrectDriverParameters(driver, parameterTypeChanges, parametersToAdd);
                                }
                            }
                        }
                    }
                }
            }

            // Add any new temporary parameters that were created
            if (parametersToAdd.Count > 0)
            {
                parameters = parameters.Concat(parametersToAdd).ToArray();
            }
        }

        private static int _tempParameterCounter = 0;
        
        private static void CorrectDriverParameters(
            VRCAvatarParameterDriver driver,
            Dictionary<string, AnimatorControllerParameterType> parameterTypeChanges,
            List<AnimatorControllerParameter> parametersToAdd)
        {
            var originalParameters = driver.parameters;
            if (originalParameters == null || originalParameters.Count == 0) return;
            
            var newParameters = new List<VRC_AvatarParameterDriver.Parameter>();

            foreach (var param in originalParameters)
            {
                if (string.IsNullOrEmpty(param.name))
                {
                    // Keep parameters with no name as-is
                    newParameters.Add(param);
                    continue;
                }
                
                if (parameterTypeChanges.TryGetValue(param.name, out var oldType))
                {
                    // This parameter was changed from bool/int to float
                    // Create an intermediate parameter with the original type
                    var tmpName = $"__AAO_tmp_{param.name}_{System.Threading.Interlocked.Increment(ref _tempParameterCounter)}";

                    // Add the temporary parameter
                    parametersToAdd.Add(new AnimatorControllerParameter
                    {
                        name = tmpName,
                        type = oldType,
                        defaultFloat = 0,
                        defaultInt = 0,
                        defaultBool = false
                    });

                    // Create a new parameter that sets the temporary parameter
                    var tmpParam = new VRC_AvatarParameterDriver.Parameter
                    {
                        name = tmpName,
                        type = param.type,
                        value = param.value,
                        valueMin = param.valueMin,
                        valueMax = param.valueMax,
                        chance = param.chance,
                        convertRange = param.convertRange,
                        destMin = param.destMin,
                        destMax = param.destMax,
                        source = param.source
                    };

                    // Create a copy parameter that copies the temp to the final float parameter
                    var copyParam = new VRC_AvatarParameterDriver.Parameter
                    {
                        name = param.name,
                        type = VRC_AvatarParameterDriver.ChangeType.Copy,
                        source = tmpName
                    };

                    newParameters.Add(tmpParam);
                    newParameters.Add(copyParam);
                }
                else
                {
                    // Parameter wasn't changed, keep as-is
                    newParameters.Add(param);
                }
            }

            driver.parameters = newParameters;
        }
#endif
    }
}
