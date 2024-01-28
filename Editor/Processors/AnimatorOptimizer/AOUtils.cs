using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor.Animations;

namespace Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer
{
    static class AOUtils
    {
        public static IEnumerable<AnimatorState> AllStates([CanBeNull] AnimatorStateMachine stateMachine)
        {
            if (stateMachine == null) yield break;
            foreach (var state in stateMachine.states)
                yield return state.state;

            foreach (var child in stateMachine.stateMachines)
            foreach (var state in AllStates(child.stateMachine))
                yield return state;
        }

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
            foreach (var transition in AllTransitions(child.stateMachine))
                yield return transition;
        }
    }
}
