using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

using static Anatawa12.AvatarOptimizer.Test.AnimatorControllerGeneratorStatics;

namespace Anatawa12.AvatarOptimizer.Test
{
    public static class AnimatorControllerGeneratorStatics
    {
        public static AnimationClipBuilder BuildAnimationClip(string name) => new(name);
        public static ChildAnimatorState NewChildState(AnimatorState state) => new() { state = state };
        public static ChildAnimatorState NewChildState(string name, Motion motion) => 
            NewChildState(new AnimatorState { name = name, motion = motion });
        
        public static AnimatorStateMachine NewAnimatorStateMachine(params ChildAnimatorState[] states) => 
            new() { states = states };

        public static AnimatorControllerLayer NewLayer(string name, AnimatorStateMachine stateMachine) => 
            new() { name = name, stateMachine = stateMachine };

        public static AnimatorControllerLayer NewLayer(string name,
            params ChildAnimatorState[] states) =>
            NewLayer(name, NewAnimatorStateMachine(states));

        public static AnimatorControllerBuilder BuildAnimatorController(string name) => new(name);
    }

    public class AnimatorControllerBuilder
    {
        private AnimatorController _controller;

        public AnimatorControllerBuilder(string name)
        {
            _controller = new AnimatorController { name = name };
        }

        public AnimatorControllerBuilder AddLayer(string name, Action<AnimatorStateMachineBuilder> action)
        {
            var builder = new AnimatorStateMachineBuilder(name);
            action(builder);
            _controller.AddLayer(new AnimatorControllerLayer { name = name, stateMachine = builder.Build() });
            return this;
        }

        public AnimatorController Build() => _controller;
    }

    public class AnimatorStateMachineBuilder
    {
        private AnimatorStateMachine _stateMachine;

        public AnimatorStateMachineBuilder(string name)
        {
            _stateMachine = new AnimatorStateMachine { name = name };
        }

        public AnimatorStateMachineBuilder NewClipState(string name, Action<AnimationClipBuilder> action)
        {
            var builder = new AnimationClipBuilder(name);
            action(builder);
            var states = _stateMachine.states;
            ArrayUtility.Add(ref states,
                new ChildAnimatorState { state = new AnimatorState { name = name, motion = builder.Build() } });
            _stateMachine.states = states;

            return this;
        }

        public AnimatorStateMachine Build() => _stateMachine;
    }


    public class AnimationClipBuilder
    {
        private AnimationClip _clip;

        public AnimationClipBuilder(string name)
        {
            _clip = new AnimationClip { name = name };
        }

        public AnimationClipBuilder AddPropertyBinding(string path, Type type, string propertyName, AnimationCurve curve)
        {
            _clip.SetCurve(path, type, propertyName, curve);
            return this;
        }

        public AnimationClipBuilder AddPropertyBinding(string path, Type type, string propertyName, params Keyframe[] keyframes) => 
            AddPropertyBinding(path, type, propertyName, new AnimationCurve(keyframes));

        public AnimationClip Build() => _clip;
    }
}