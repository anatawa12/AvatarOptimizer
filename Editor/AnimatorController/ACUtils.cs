using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor.Animations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    static partial class ACUtils
    {
        [ItemNotNull]
        [NotNull]
        public static IEnumerable<AnimatorStateMachine> AllStateMachines([CanBeNull] AnimatorStateMachine stateMachine)
        {
            if (stateMachine == null) yield break;
            yield return stateMachine;

            foreach (var child in stateMachine.stateMachines)
            foreach (var machine in AllStateMachines(child.stateMachine))
                yield return machine;
        }

        [ItemNotNull]
        [NotNull]
        public static IEnumerable<AnimatorState> AllStates([CanBeNull] AnimatorStateMachine stateMachine)
        {
            if (stateMachine == null) yield break;
            foreach (var state in stateMachine.states)
                yield return state.state;

            foreach (var child in stateMachine.stateMachines)
            foreach (var state in AllStates(child.stateMachine))
                yield return state;
        }

        [ItemNotNull]
        [NotNull]
        public static IEnumerable<AnimatorTransitionBase> AllTransitions([CanBeNull] AnimatorStateMachine stateMachine)
        {
            if (stateMachine == null) yield break;

            foreach (var transition in stateMachine.entryTransitions)
                yield return transition;
            foreach (var transition in stateMachine.anyStateTransitions)
                yield return transition;

            foreach (var state in stateMachine.states)
            foreach (var transition in state.state.transitions)
                yield return transition;

            foreach (var child in stateMachine.stateMachines)
            {
                foreach (var transition in stateMachine.GetStateMachineTransitions(child.stateMachine))
                    yield return transition;
                foreach (var transition in AllTransitions(child.stateMachine))
                    yield return transition;
            }
        }

        [NotNull]
        [ItemNotNull]
        public static IEnumerable<AnimationClip> AllClips([CanBeNull] Motion motion)
        {
            switch (motion)
            {
                case null:
                    yield break;
                case AnimationClip clip:
                    yield return clip;
                    break;
                case BlendTree blendTree:
                    foreach (var child in blendTree.children)
                    foreach (var clip in AllClips(child.motion))
                        yield return clip;
                    break;
            }
        }

        [NotNull]
        [ItemNotNull]
        public static IEnumerable<StateMachineBehaviour> StateMachineBehaviours(
            [NotNull] RuntimeAnimatorController runtimeController)
        {
            var (controller, _) = GetControllerAndOverrides(runtimeController);

            foreach (var layer in controller.layers)
            {
                if (layer.syncedLayerIndex == -1)
                    foreach (var behaviour in StateMachineBehaviours(layer.stateMachine))
                        yield return behaviour;
                else
                    foreach (var state in AllStates(controller.layers[layer.syncedLayerIndex].stateMachine))
                    foreach (var behaviour in layer.GetOverrideBehaviours(state))
                        yield return behaviour;
            }
        }

        [NotNull]
        [ItemNotNull]
        public static IEnumerable<StateMachineBehaviour> StateMachineBehaviours([NotNull] AnimatorStateMachine stateMachineIn)
        {
            var queue = new Queue<AnimatorStateMachine>();
            queue.Enqueue(stateMachineIn);

            while (queue.Count != 0)
            {
                var stateMachine = queue.Dequeue();
                foreach (var behaviour in stateMachine.behaviours)
                    if (behaviour != null)
                        yield return behaviour;
                foreach (var state in stateMachine.states)
                foreach (var behaviour in state.state.behaviours)
                    if (behaviour != null)
                        yield return behaviour;

                foreach (var childStateMachine in stateMachine.stateMachines)
                    queue.Enqueue(childStateMachine.stateMachine);
            }
        }

        public static int ComputeLayerCount([NotNull] this RuntimeAnimatorController controller)
        {
            while (controller is AnimatorOverrideController overrideController)
                controller = overrideController.runtimeAnimatorController;
            return ((AnimatorController)controller).layers.Length;
        }
    }
}
