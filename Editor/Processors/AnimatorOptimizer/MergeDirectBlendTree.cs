using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;
using JetBrains.Annotations;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;

namespace Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer
{
    internal class MergeDirectBlendTree : AnimOptPassBase<MergeDirectBlendTree>
    {
        protected override void Execute(BuildContext context, AOAnimatorController controller,
            TraceAndOptimizeState settings)
        {
            if (!settings.SkipMergeDirectBlendTreeLayers) return;
            Execute(controller);
        }

        public static void Execute(AOAnimatorController controller)
        {
            var directBlendTrees = new List<(int layerIndex, BlendTree tree)>();

            var modifiedProperties = new HashSet<EditorCurveBinding>();

            for (var i = controller.layers.Length - 1; i >= 0; i--)
            {
                var layer = controller.layers[i];

                var blendTree = GetSingleDirectBlendTree(layer);

                if (blendTree != null)
                {
                    var blendTreeModified = ACUtils.AllClips(blendTree).Aggregate(new HashSet<EditorCurveBinding>(), (set, clip) =>
                    {
                        modifiedProperties.UnionWith(AnimationUtility.GetCurveBindings(clip));
                        modifiedProperties.UnionWith(AnimationUtility.GetObjectReferenceCurveBindings(clip));
                        return set;
                    });
                    // nothing is animated in higher priority layer
                    if (!blendTreeModified.Any(modifiedProperties.Contains))
                         directBlendTrees.Add((i, blendTree));

                    modifiedProperties.UnionWith(blendTreeModified);
                }
                else
                {
                    foreach (var motion in layer.GetMotions())
                    foreach (var clip in ACUtils.AllClips(motion))
                    {
                        modifiedProperties.UnionWith(AnimationUtility.GetCurveBindings(clip));
                        modifiedProperties.UnionWith(AnimationUtility.GetObjectReferenceCurveBindings(clip));
                    }
                }
            }

            // if we found only one, leave it as is
            if (directBlendTrees.Count < 2) return;

            directBlendTrees.Reverse();

            // create merged layer
            var newLayer = controller.AddLayer("Merged Direct BlendTrees");
            var newState = new AnimatorState { name = "Merged Direct BlendTrees" };
            newLayer.stateMachine.states = new[] { new ChildAnimatorState { state = newState } };
            newLayer.stateMachine.defaultState = newState;
            newLayer.defaultWeight = 1f;

            var newBlendTree = new BlendTree() { name = "Merged Direct BlendTrees" };
            newState.motion = newBlendTree;
            newBlendTree.children = directBlendTrees.SelectMany(x => x.tree.children).ToArray();

            // clear original layers
            foreach (var (layerIndex, _) in directBlendTrees)
            {
                var layer = controller.layers[layerIndex];
                layer.stateMachine.states = Array.Empty<ChildAnimatorState>();
                layer.stateMachine.defaultState = null;
                layer.defaultWeight = 0f;
                layer.WeightChange = AnimatorWeightChange.NotChanged;
            }
        }

        [CanBeNull]
        private static BlendTree GetSingleDirectBlendTree(AOAnimatorControllerLayer layer)
        {
            if (layer.IsSyncedToOtherLayer || layer.IsSynced) return null;
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (layer.defaultWeight != 1) return null;
            if (layer.WeightChange != AnimatorWeightChange.NotChanged &&
                layer.WeightChange != AnimatorWeightChange.AlwaysOne) return null;
            if (!layer.stateMachine) return null;
            var stateMachine = layer.stateMachine;

            if (stateMachine.states.Length != 1) return null;
            if (stateMachine.stateMachines.Length != 0) return null;

            var state = stateMachine.states[0].state;
            if (stateMachine.defaultState != state) return null;

            // state configuration
            if (!state.writeDefaultValues) return null;
            if (state.behaviours.Length != 0) return null;
            if (state.timeParameterActive) return null;
            if (!(state.motion is BlendTree blendTree)) return null;

            // validate blend tree
            if (blendTree.blendType != BlendTreeType.Direct) return null;
            return blendTree;
        }
    }
}