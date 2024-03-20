using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Anatawa12.AvatarOptimizer
{
    public static class AnimatorControllerInfoUtils
    {
        [NotNull]
        [ItemNotNull]
        public static IEnumerable<AnimatorStateInfo> GetStatesRecursively([CanBeNull] this AnimatorStateMachineInfo stateMachine)
        {
            if (stateMachine == null) return Array.Empty<AnimatorStateInfo>();
            if (stateMachine.StateMachines.Count == 0)
            {
                // fast path: no sub state machines so just return states
                return stateMachine.States;
            }

            // slow path: do recursively
            return GetStatesRecursivelySlow(stateMachine);
        }

        private static IEnumerable<AnimatorStateInfo> GetStatesRecursivelySlow([NotNull] AnimatorStateMachineInfo stateMachine)
        {
            var queue = new Queue<AnimatorStateMachineInfo>();
            queue.Enqueue(stateMachine);

            while (queue.Count != 0)
            {
                var current = queue.Dequeue();
                foreach (var state in current.States)
                    yield return state;

                foreach (var subStateMachine in current.StateMachines)
                    queue.Enqueue(subStateMachine);
            }
        }
    }
}