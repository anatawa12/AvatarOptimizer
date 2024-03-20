using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.Animations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    internal struct ParameterMap
    {
        private Dictionary<string, AnimatorControllerParameterInfo> _map;

        [NotNull]
        public AnimatorControllerParameterInfo this[string name]
        {
            get
            {
                if (_map == null) throw new InvalidOperationException("map is not initialized");
                if (!_map.TryGetValue(name, out var value))
                    throw new KeyNotFoundException($"invalid AnimatorController: parameter {name} not found");
                return value;
            }
        }

        public void Add([NotNull] AnimatorControllerParameterInfo parameter)
        {
            if (_map == null) _map = new Dictionary<string, AnimatorControllerParameterInfo>();
            _map[parameter.Name] = parameter;
        }
    }

    /// <summary>
    /// This class wraps the AnimatorController to provide more information about the controller.
    /// </summary>
    public class AnimatorControllerInfo
    {
        private readonly AnimatorController _original;

        public readonly string Name;
        [NotNull] [ItemNotNull] public readonly List<AnimatorControllerParameterInfo> Parameters;
        [NotNull] [ItemNotNull] public readonly List<AnimatorControllerLayerInfoBase> Layers;

        public AnimatorControllerInfo(AnimatorController controller)
        {
            _original = controller;
            Name = controller.name;
            Parameters = controller.parameters.Select(x => new AnimatorControllerParameterInfo(x)).ToList();

            var parameters = new ParameterMap();
            foreach (var parameter in Parameters)
                parameters.Add(parameter);

            var isSyncSource = new BitArray(controller.layers.Length);
            foreach (var layer in controller.layers)
                if (layer.syncedLayerIndex != -1)
                    isSyncSource[layer.syncedLayerIndex] = true;

            var layers = new AnimatorControllerLayerInfoBase[controller.layers.Length];

            for (var i = 0; i < controller.layers.Length; i++)
                if (controller.layers[i].syncedLayerIndex == -1)
                    layers[i] = isSyncSource[i] ? (AnimatorControllerLayerInfoBase)
                        new SyncSourceAnimatorControllerLayerInfo(controller.layers[i], parameters) :
                        new OrphanAnimatorControllerLayerInfo(controller.layers[i], parameters);

            for (var i = 0; i < controller.layers.Length; i++)
                if (controller.layers[i].syncedLayerIndex != -1)
                    layers[i] = new SyncedAnimatorControllerLayerInfo(controller.layers[i], layers);

            Layers = layers.ToList();
        }

        public AnimatorController ToController()
        {
            var controller = new AnimatorController
            {
                name = Name,
            };
            controller.parameters = Parameters.Select(x => x.ToParameter()).ToArray();
            controller.layers = Layers.Select(x => x.ToAnimatorControllerLayer()).ToArray();
            //TODO VVVV
            return controller;
        }
    }

    public class AnimatorControllerParameterInfo
    {
        // original data
        public readonly string Name;

        public AnimatorControllerParameterType CurrentType
        {
            get;
        }

        // int: the value
        // float: Bits conversion
        // bool: false: 0, true: others
        private int _defaultValue;

        public AnimatorControllerParameterInfo(AnimatorControllerParameter parameter)
        {
            Name = parameter.name;
            CurrentType = parameter.type;
            switch (CurrentType)
            {
                case AnimatorControllerParameterType.Float:
                    _defaultValue = Utils.SingleToInt32Bits(parameter.defaultFloat);
                    break;
                case AnimatorControllerParameterType.Int:
                    _defaultValue = parameter.defaultInt;
                    break;
                case AnimatorControllerParameterType.Bool:
                    _defaultValue = parameter.defaultBool ? 1 : 0;
                    break;
                case AnimatorControllerParameterType.Trigger:
                    _defaultValue = parameter.defaultBool ? 1 : 0;
                    // lock as bool since trigger cannot be simulated with other types
                    _boolLocks++;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public AnimatorControllerParameter ToParameter()
        {
            CheckTypeMatch();
            return new AnimatorControllerParameter()
            {
                name = Name,
                type = CurrentType,
                defaultFloat = CurrentType == AnimatorControllerParameterType.Float
                    ? Utils.Int32BitsToSingle(_defaultValue)
                    : 0,
                defaultInt = CurrentType == AnimatorControllerParameterType.Int ? _defaultValue : 0,
                defaultBool = CurrentType == AnimatorControllerParameterType.Bool && _defaultValue != 0,
            };
        }

        internal void CheckTypeMatch()
        {
            switch (CurrentType)
            {
                case AnimatorControllerParameterType.Float:
                    if (_boolUsers > 0 || _intUsers > 0)
                        throw new InvalidOperationException($"float parameter {Name} is used as int or bool");
                    break;
                case AnimatorControllerParameterType.Int:
                    if (_boolUsers > 0 || _floatUsers > 0)
                        throw new InvalidOperationException($"int parameter {Name} is used as float or bool");
                    break;
                case AnimatorControllerParameterType.Bool:
                case AnimatorControllerParameterType.Trigger:
                    if (_intUsers > 0 || _floatUsers > 0)
                        throw new InvalidOperationException($"bool parameter {Name} is used as int or float");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private int _floatLocks;
        private int _boolLocks;
        private int _intLocks;

        private int _floatUsers;
        private int _boolUsers;
        private int _intUsers;

        private static void Lock(ref int @lock, int otherLock1, int otherLock2, int otherUsers1, int otherUsers2)
        {
            if (otherUsers1 > 0 || otherUsers2 > 0)
                throw new InvalidOperationException("cannot lock when used as another type");
            if (otherLock1 > 0 || otherLock2 > 0)
                throw new InvalidOperationException("cannot lock when locked as another type");
            @lock++;
        }

        private static void Unlock(ref int @lock)
        {
            if (@lock <= 0)
                throw new InvalidOperationException("cannot unlock when not locked");
            @lock--;
        }

        private static void Use(ref int users, int otherLock1, int otherLock2)
        {
            if (otherLock1 > 0 || otherLock2 > 0)
                throw new InvalidOperationException("cannot use when locked as another type");
            users++;
        }

        private static void Unuse(ref int users)
        {
            if (users <= 0)
                throw new InvalidOperationException("cannot unuse when not used");
            users--;
        }

        internal void LockAsFloat() => Lock(ref _floatLocks, _boolLocks, _intLocks, _boolUsers, _intUsers);
        internal void LockAsBool() => Lock(ref _boolLocks, _floatLocks, _intLocks, _floatUsers, _intUsers);
        internal void LockAsInt() => Lock(ref _intLocks, _boolLocks, _floatLocks, _boolUsers, _floatUsers);

        internal void UnlockFloat() => Unlock(ref _floatLocks);
        internal void UnlockBool() => Unlock(ref _boolLocks);
        internal void UnlockInt() => Unlock(ref _intLocks);

        internal void UseAsFloat() => Use(ref _floatUsers, _boolLocks, _intLocks);
        internal void UseAsBool() => Use(ref _boolUsers, _floatLocks, _intLocks);
        internal void UseAsInt() => Use(ref _intUsers, _boolLocks, _floatLocks);

        internal void UnuseFloat() => Unuse(ref _floatUsers);
        internal void UnuseBool() => Unuse(ref _boolUsers);
        internal void UnuseInt() => Unuse(ref _intUsers);
    }

    public abstract class AnimatorControllerLayerInfoBase
    {
        public readonly string Name;
        [CanBeNull] public AvatarMask Mask;
        public bool IsBaseLayer { get; internal set; }
        public bool IsSyncedToOtherLayer { get; internal set; }
        public float DefaultWeight { get; }
        public AnimatorLayerBlendingMode BlendingMode { get; }
        public bool IKPass { get; }

        public bool IsOverride => BlendingMode == AnimatorLayerBlendingMode.Override;

        protected AnimatorControllerLayerInfoBase(AnimatorControllerLayer layer)
        {
            Name = layer.name;
            Mask = layer.avatarMask;
            DefaultWeight = layer.defaultWeight;
            BlendingMode = layer.blendingMode;
            IKPass = layer.iKPass;
        }

        public virtual AnimatorControllerLayer ToAnimatorControllerLayer() =>
            new AnimatorControllerLayer
            {
                name = Name,
                avatarMask = Mask,
                defaultWeight = DefaultWeight,
                blendingMode = BlendingMode,
                iKPass = IKPass,
            };
    }

    /// <summary>
    /// The animator controller layer which does not synced to any other layers.
    /// Such a animator controller layer is easy to optimize.
    /// </summary>
    public class OrphanAnimatorControllerLayerInfo : AnimatorControllerLayerInfoBase
    {
        [NotNull] public AnimatorStateMachineInfo StateMachine;

        internal OrphanAnimatorControllerLayerInfo(AnimatorControllerLayer layer, ParameterMap map) : base(layer)
        {
            StateMachine = new AnimatorStateMachineInfo(layer.stateMachine, map);
        }

        public override AnimatorControllerLayer ToAnimatorControllerLayer()
        {
            var result =  base.ToAnimatorControllerLayer();
            result.stateMachine = StateMachine.ToStateMachine();
            return result;
        }
    }

    /// <summary>
    /// The animator controller layer which synced to other layers.
    /// Semantically this layer is same as <see cref="OrphanAnimatorControllerLayerInfo"/> but it is harder to optimize
    /// and I think I will forget to check if synced or not so I separated class.
    /// </summary>
    public class SyncSourceAnimatorControllerLayerInfo : AnimatorControllerLayerInfoBase
    {
        [NotNull] public AnimatorStateMachineInfo StateMachine;

        internal SyncSourceAnimatorControllerLayerInfo(AnimatorControllerLayer layer, ParameterMap map) : base(layer)
        {
            StateMachine = new AnimatorStateMachineInfo(layer.stateMachine, map);
        }

        public override AnimatorControllerLayer ToAnimatorControllerLayer()
        {
            var result =  base.ToAnimatorControllerLayer();
            result.stateMachine = StateMachine.ToStateMachine();
            return result;
        }
    }

    /// <summary>
    /// The animator controller layer which synced from other layers.
    /// </summary>
    public class SyncedAnimatorControllerLayerInfo : AnimatorControllerLayerInfoBase
    {
        public SyncSourceAnimatorControllerLayerInfo SyncSourceLayer { get; internal set; }

        [NotNull] public Dictionary<AnimatorStateInfo, Motion> MotionOverrides =
            new Dictionary<AnimatorStateInfo, Motion>();

        [NotNull] public Dictionary<AnimatorStateInfo, List<StateMachineBehaviour>> BehaviorOverrides =
            new Dictionary<AnimatorStateInfo, List<StateMachineBehaviour>>();

        public bool SyncedLayerAffectsTiming { get; internal set; }

        public SyncedAnimatorControllerLayerInfo(AnimatorControllerLayer layer,
            AnimatorControllerLayerInfoBase[] layers) : base(layer)
        {
            if (layer.syncedLayerIndex < 0 || layer.syncedLayerIndex >= layers.Length)
                throw new ArgumentOutOfRangeException(nameof(layer), "syncedLayerIndex is invalid");

            SyncSourceLayer = (SyncSourceAnimatorControllerLayerInfo) layers[layer.syncedLayerIndex];
            SyncedLayerAffectsTiming = layer.syncedLayerAffectsTiming;

            // overrides
            foreach (var stateInfo in SyncSourceLayer.StateMachine.GetStatesRecursively())
            {
                var motion = layer.GetOverrideMotion(stateInfo.Original);
                if (motion != null) MotionOverrides[stateInfo] = motion;
                var behaviors = layer.GetOverrideBehaviours(stateInfo.Original);
                if (behaviors != null && behaviors.Length != 0) BehaviorOverrides[stateInfo] = behaviors.ToList();
            }
        }

        public override AnimatorControllerLayer ToAnimatorControllerLayer()
        {
            var result =  base.ToAnimatorControllerLayer();
            result.stateMachine = new AnimatorStateMachine();
            foreach (var (stateInfo, motion) in MotionOverrides)
                result.SetOverrideMotion(stateInfo.Original, motion);
            foreach (var (stateInfo, behaviors) in BehaviorOverrides)
                result.SetOverrideBehaviours(stateInfo.Original, behaviors.ToArray());
            return result;
        }
    }

    public abstract class AnimatorTransitionDestination
    {
    }

    public class AnimatorStateMachineInfo : AnimatorTransitionDestination
    {
        public readonly string Name;
        [NotNull] [ItemNotNull] public readonly List<AnimatorStateInfo> States;
        [NotNull] [ItemNotNull] public readonly List<AnimatorStateMachineInfo> StateMachines;
        [NotNull] [ItemNotNull] public readonly LinkedList<AnimatorStateTransitionInfo> AnyStateTransitions;
        [NotNull] [ItemNotNull] public readonly LinkedList<AnimatorTransitionInfo> EntryTransitions;
        [NotNull] [ItemNotNull] public readonly List<StateMachineBehaviour> Behaviours;
        [CanBeNull] public AnimatorStateInfo DefaultState; // null if no states in this state machine

        public readonly Vector3 AnyStatePosition;
        public readonly Vector3 EntryPosition;
        public readonly Vector3 ExitPosition;
        public readonly Vector3 ParentStateMachinePosition;

        // copoed from parent AnimatorStateMachine
        [NotNull] [ItemNotNull] public readonly LinkedList<AnimatorTransitionInfo> Transitions;
        public readonly Vector3 Position;

        internal AnimatorStateMachineInfo(AnimatorStateMachine layerStateMachine, ParameterMap map)
        {
            var stateMapping = new Dictionary<AnimatorState, AnimatorStateInfo>();
            var stateMachineMapping = new Dictionary<AnimatorStateMachine, AnimatorStateMachineInfo>();

            foreach (var childAnimatorState in layerStateMachine.states)
                stateMapping[childAnimatorState.state] = new AnimatorStateInfo(childAnimatorState, map);

            foreach (var childAnimatorState in layerStateMachine.stateMachines)
                stateMachineMapping[childAnimatorState.stateMachine] =
                    new AnimatorStateMachineInfo(childAnimatorState, map);

            Name = layerStateMachine.name;
            States = layerStateMachine.states.Select(x => stateMapping[x.state]).ToList();
            StateMachines = layerStateMachine.stateMachines.Select(x => stateMachineMapping[x.stateMachine]).ToList();

            AnyStateTransitions = new LinkedList<AnimatorStateTransitionInfo>();
            EntryTransitions = new LinkedList<AnimatorTransitionInfo>();
            Behaviours = layerStateMachine.behaviours.ToList();
            DefaultState = stateMapping[layerStateMachine.defaultState];

            foreach (var transition in layerStateMachine.anyStateTransitions)
                AnyStateTransitions.AddLast(new AnimatorStateTransitionInfo(transition,
                    map, stateMapping, stateMachineMapping));

            foreach (var animatorTransition in layerStateMachine.entryTransitions)
                EntryTransitions.AddLast(new AnimatorTransitionInfo(animatorTransition,
                    map, stateMapping, stateMachineMapping));

            for (var i = 0; i < layerStateMachine.states.Length; i++)
            {
                var state = layerStateMachine.states[i];
                var stateInfo = States[i];
                foreach (var transition in state.state.transitions)
                    stateInfo.Transitions.AddLast(new AnimatorStateTransitionInfo(transition,
                        map, stateMapping, stateMachineMapping));
            }

            AnyStatePosition = layerStateMachine.anyStatePosition;
            EntryPosition = layerStateMachine.entryPosition;
            ExitPosition = layerStateMachine.exitPosition;
            ParentStateMachinePosition = layerStateMachine.parentStateMachinePosition;

            Transitions = new LinkedList<AnimatorTransitionInfo>();
        }

        private AnimatorStateMachineInfo(ChildAnimatorStateMachine layerStateMachine, ParameterMap map)
            : this(layerStateMachine.stateMachine, map)
        {
            Position = layerStateMachine.position;
        }

        public AnimatorStateMachine ToStateMachine()
        {
            throw new NotImplementedException();
        }
    }

    public class AnimatorStateInfo : AnimatorTransitionDestination
    {
        public readonly string Name;
        public readonly float Speed;
        public readonly float CycleOffset;
        [NotNull] [ItemNotNull] public readonly LinkedList<AnimatorStateTransitionInfo> Transitions;
        [NotNull] [ItemNotNull] public readonly List<StateMachineBehaviour> Behaviours;
        public readonly bool IKOnFeet;
        public readonly bool WriteDefaults;
        public readonly bool Mirror;
        [CanBeNull] public readonly Motion Motion;
        [NotNull] public string Tag;

        // memo: float except for mirror, which is bool
        // null if parameter control is not active
        [CanBeNull] public AnimatorControllerParameterInfo SpeedParameter;
        [CanBeNull] public AnimatorControllerParameterInfo MirrorParameter;
        [CanBeNull] public AnimatorControllerParameterInfo CycleOffsetParameter;
        [CanBeNull] public AnimatorControllerParameterInfo MotionTimeParameter;

        // copied from parent AnimatorStateMachine
        public readonly Vector3 Position;

        internal readonly AnimatorState Original;

        internal AnimatorStateInfo(ChildAnimatorState childState, ParameterMap map)
        {
            Original = childState.state;

            var state = childState.state;
            Name = state.name;
            Speed = state.speed;
            CycleOffset = state.cycleOffset;
            IKOnFeet = state.iKOnFeet;
            WriteDefaults = state.writeDefaultValues;
            Mirror = state.mirror;
            Motion = state.motion;
            Tag = state.tag;

            Transitions = new LinkedList<AnimatorStateTransitionInfo>();
            Behaviours = state.behaviours.ToList();

            SpeedParameter = state.speedParameterActive ? map[state.speedParameter] : null;
            MirrorParameter = state.mirrorParameterActive ? map[state.mirrorParameter] : null;
            CycleOffsetParameter = state.cycleOffsetParameterActive ? map[state.cycleOffsetParameter] : null;
            MotionTimeParameter = state.timeParameterActive ? map[state.timeParameter] : null;

            Position = childState.position;

            SpeedParameter?.LockAsFloat();
            MirrorParameter?.LockAsBool();
            CycleOffsetParameter?.LockAsFloat();
            MotionTimeParameter?.LockAsFloat();
        }
    }

    public abstract class AnimatorTransitionInfoBase : IDisposable
    {
        private AnimatorConditionInfo[] _conditions;

        [ItemNotNull]
        [NotNull]
        public AnimatorConditionInfo[] Conditions
        {
            get => _conditions;
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (value == _conditions) return;
                if (_conditions != null)
                    foreach (var condition in _conditions)
                        condition.Dispose();
                _conditions = value;
                foreach (var condition in _conditions) condition.Activate();
            }
        }

        // Null means exit transition
        [CanBeNull] public AnimatorTransitionDestination Destination;
        public bool Solo;
        public bool Mute;

        internal AnimatorTransitionInfoBase(AnimatorTransitionBase transition, ParameterMap map,
            Dictionary<AnimatorState, AnimatorStateInfo> stateMapping,
            Dictionary<AnimatorStateMachine, AnimatorStateMachineInfo> stateMachineMapping)
        {
            Conditions = transition.conditions.Select(x => new AnimatorConditionInfo(x, map)).ToArray();
            if (!transition.destinationStateMachine && !transition.destinationState && !transition.isExit)
                throw new Exception("Invalid transition: no destination are set");
            if (transition.destinationStateMachine && transition.destinationState
                || transition.destinationStateMachine && transition.isExit
                || transition.destinationState && transition.isExit)
                throw new Exception("Invalid transition: multiple destination are set");
            if (transition.isExit)
                Destination = null;
            else if (transition.destinationStateMachine)
                Destination = stateMachineMapping[transition.destinationStateMachine];
            else
                Destination = stateMapping[transition.destinationState];

            Solo = transition.solo;
            Mute = transition.mute;
        }

        public void Dispose()
        {
            if (_conditions == null) return;
            foreach (var condition in _conditions) condition.Dispose();
        }
    }

    public sealed class AnimatorStateTransitionInfo : AnimatorTransitionInfoBase
    {
        public float Duration;
        public float Offset;

        public float? ExitTime;

        // if true, Duration is in seconds, if false, Duration is in percent
        public bool HasFixedDuration;
        public TransitionInterruptionSource InterruptionSource;
        public bool OrderedInterruption;
        public bool CanTransitionToSelf; // only affects on AnyState transition

        internal AnimatorStateTransitionInfo(AnimatorStateTransition transition, ParameterMap map,
            Dictionary<AnimatorState, AnimatorStateInfo> stateMapping,
            Dictionary<AnimatorStateMachine, AnimatorStateMachineInfo> stateMachineMapping) :
            base(transition, map, stateMapping, stateMachineMapping)
        {
            Duration = transition.duration;
            Offset = transition.offset;
            ExitTime = transition.hasExitTime ? transition.exitTime : (float?)null;
            HasFixedDuration = transition.hasFixedDuration;
            InterruptionSource = transition.interruptionSource;
            OrderedInterruption = transition.orderedInterruption;
            CanTransitionToSelf = transition.canTransitionToSelf;
        }
    }

    public sealed class AnimatorTransitionInfo : AnimatorTransitionInfoBase
    {
        internal AnimatorTransitionInfo(AnimatorTransitionBase transition, ParameterMap map,
            Dictionary<AnimatorState, AnimatorStateInfo> stateMapping,
            Dictionary<AnimatorStateMachine, AnimatorStateMachineInfo> stateMachineMapping) : base(transition, map,
            stateMapping, stateMachineMapping)
        {
        }
    }

    public class AnimatorConditionInfo
    {
        [NotNull] public readonly AnimatorControllerParameterInfo Parameter;
        public readonly float Threshold;
        public readonly ConditionType Type;

        internal AnimatorConditionInfo(AnimatorCondition condition, ParameterMap map)
        {
            Parameter = map[condition.parameter];
            Threshold = condition.threshold;

            // there is no recursive pattern match in C# 7.x
            switch (Parameter.CurrentType)
            {
                case AnimatorControllerParameterType.Float:
                    switch (condition.mode)
                    {
                        case AnimatorConditionMode.Greater:
                            Type = ConditionType.IfGreaterFloat;
                            break;
                        case AnimatorConditionMode.Less:
                            Type = ConditionType.IfLessFloat;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(condition),
                                "condition mode is invalid for float");
                    }

                    break;
                case AnimatorControllerParameterType.Int:
                    switch (condition.mode)
                    {
                        case AnimatorConditionMode.Greater:
                            Type = ConditionType.IfGreaterInt;
                            break;
                        case AnimatorConditionMode.Less:
                            Type = ConditionType.IfLessInt;
                            break;
                        case AnimatorConditionMode.Equals:
                            Type = ConditionType.IfEqualInt;
                            break;
                        case AnimatorConditionMode.NotEqual:
                            Type = ConditionType.IfNotEqualInt;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(condition),
                                "condition mode is invalid for int");
                    }

                    break;
                case AnimatorControllerParameterType.Bool:
                    switch (condition.mode)
                    {
                        case AnimatorConditionMode.If:
                            Type = ConditionType.IfTrue;
                            break;
                        case AnimatorConditionMode.IfNot:
                            Type = ConditionType.IfFalse;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(condition),
                                "condition mode is invalid for bool");
                    }

                    break;
                case AnimatorControllerParameterType.Trigger:
                    switch (condition.mode)
                    {
                        case AnimatorConditionMode.If:
                            Type = ConditionType.IfTrue;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(condition),
                                "condition mode is invalid for trigger");
                    }

                    break;
                default:
                    throw new InvalidOperationException($"logic failure: bad parameter type for {condition.parameter}");
            }
        }

        public void Activate()
        {
            switch (Type)
            {
                case ConditionType.IfTrue:
                case ConditionType.IfFalse:
                    Parameter.UseAsBool();
                    break;
                case ConditionType.IfGreaterInt:
                case ConditionType.IfLessInt:
                case ConditionType.IfEqualInt:
                case ConditionType.IfNotEqualInt:
                    Parameter.UseAsInt();
                    break;
                case ConditionType.IfGreaterFloat:
                case ConditionType.IfLessFloat:
                    Parameter.UseAsFloat();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Dispose()
        {
            switch (Type)
            {
                case ConditionType.IfTrue:
                case ConditionType.IfFalse:
                    Parameter.UnuseBool();
                    break;
                case ConditionType.IfGreaterInt:
                case ConditionType.IfLessInt:
                case ConditionType.IfEqualInt:
                case ConditionType.IfNotEqualInt:
                    Parameter.UnuseInt();
                    break;
                case ConditionType.IfGreaterFloat:
                case ConditionType.IfLessFloat:
                    Parameter.UnuseFloat();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public enum ConditionType
    {
        IfTrue,
        IfFalse,
        IfGreaterInt,
        IfLessInt,
        IfEqualInt,
        IfNotEqualInt,
        IfGreaterFloat,
        IfLessFloat,
    }
}