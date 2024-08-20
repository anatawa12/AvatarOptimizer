using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    // The class describes the location of the animation curve
    internal sealed class AnimationLocation : IErrorContext
    {
        public Animator Component { get; }
        public int PlayableLayerIndex { get; }
        public int AnimationLayerIndex { get; }
        public AnimatorState AnimatorState { get; }
        public int[] BlendTreeLocation { get; }
        public AnimationCurve Curve { get; }
        public AnimationClip Clip { get; set; }

        public AnimationLocation(Animator component, int playableLayerIndex, int animationLayerIndex,
            AnimatorState state, int[]? blendTreeLocation, AnimationCurve curve,
            AnimationClip clip)
        {
            if (!component) throw new ArgumentNullException(nameof(component));
            if (!state) throw new ArgumentNullException(nameof(state));

            Component = component;
            PlayableLayerIndex = playableLayerIndex;
            AnimationLayerIndex = animationLayerIndex;
            AnimatorState = state;
            BlendTreeLocation = blendTreeLocation ?? Array.Empty<int>();
            Curve = curve ?? throw new ArgumentNullException(nameof(curve));
            Clip = clip;
        }

        public static IEnumerable<AnimationLocation> CollectAnimationLocation(RootPropModNode<float> node)
        {
            foreach (var animatorNode in node.ComponentNodes.OfType<AnimatorPropModNode<float>>())
            foreach (var playableNodeInfo in animatorNode.LayersReversed.WhileApplied())
            foreach (var animatorNodeInfo in playableNodeInfo.Node.LayersReversed.WhileApplied())
            foreach (var animatorStateNode in animatorNodeInfo.Node.Children)
            foreach (var animationLocation in CollectAnimationLocation(
                         animatorNode.Component, playableNodeInfo.LayerIndex, animatorNodeInfo.LayerIndex,
                         animatorStateNode.State, animatorStateNode.Node))
                yield return animationLocation;
        }

        public static IEnumerable<AnimationLocation> CollectAnimationLocation(Animator animator, int playableLayer,
            int animatorLayer,
            AnimatorState state, ImmutablePropModNode<float> node)
        {
            // fast path
            if (node is FloatAnimationCurveNode floatNode)
                return new[]
                {
                    new AnimationLocation(animator, playableLayer, animatorLayer,
                        state, null, floatNode.Curve, floatNode.Clip)
                };
            return CollectAnimationLocationSlow(animator, playableLayer, animatorLayer, state, node);
        }

        private static IEnumerable<AnimationLocation> CollectAnimationLocationSlow(Animator animator,
            int playableLayer, int animatorLayer, AnimatorState state, ImmutablePropModNode<float> node)
        {
            // slow path: recursively collect blend tree
            var queue = new Queue<(BlendTreeNode<float>, int[])>();
            queue.Enqueue(((BlendTreeNode<float>)node, Array.Empty<int>()));

            while (queue.Count != 0)
            {
                var (blendTree, location) = queue.Dequeue();

                foreach (var element in blendTree.Children)
                {
                    var newLocation = location;
                    ArrayUtility.Add(ref newLocation, element.Index);
                    switch (element.Node)
                    {
                        case FloatAnimationCurveNode floatNode:
                            yield return new AnimationLocation(animator, playableLayer, animatorLayer, state,
                                newLocation, floatNode.Curve, floatNode.Clip);
                            break;
                        case BlendTreeNode<float> childBlendTree:
                            queue.Enqueue((childBlendTree, newLocation));
                            break;
                        default:
                            throw new InvalidOperationException(
                                "Unexpected node type: " +
                                element.Node.GetType().FullName);
                    }
                }
            }
        }

        private bool Equals(AnimationLocation other) =>
            Equals(Component, other.Component) && PlayableLayerIndex == other.PlayableLayerIndex &&
            AnimationLayerIndex == other.AnimationLayerIndex && Equals(AnimatorState, other.AnimatorState) &&
            BlendTreeLocation.SequenceEqual(other.BlendTreeLocation) && Equals(Curve, other.Curve);

        public override bool Equals(object? obj) =>
            ReferenceEquals(this, obj) || obj is AnimationLocation other && Equals(other);

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Component);
            hashCode.Add(PlayableLayerIndex);
            hashCode.Add(AnimationLayerIndex);
            hashCode.Add(AnimatorState);
            foreach (var location in BlendTreeLocation)
                hashCode.Add(location);
            hashCode.Add(Curve);
            return hashCode.ToHashCode();
        }

        public IEnumerable<ObjectReference> ContextReferences => new[]
            { ObjectRegistry.GetReference(Component), ObjectRegistry.GetReference(Clip) };
    }
}
