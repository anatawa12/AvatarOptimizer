using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer
{
    public class MergeBlendTreeLayer : AnimOptPassBase<MergeBlendTreeLayer>
    {
        public override string DisplayName => "Animator Optimizer: Merge BlendTree Layers";
        protected override bool Enabled(TraceAndOptimizeState state) => state.MergeBlendTreeLayer;

        private protected override void Execute(BuildContext context, AOAnimatorController controller,
            TraceAndOptimizeState settings) =>
            Execute(context.GetState<AnimatorOptimizerState>(), controller);

        public static void Execute(AnimatorOptimizerState state, AOAnimatorController controller,
            string? alwaysOneParameter = null)
        {
            var directBlendTrees = new List<(int layerIndex, BlendTree tree)>();

            var modifiedProperties = new HashSet<EditorCurveBinding>();

            alwaysOneParameter ??= $"AAO_AlwaysOne_{Guid.NewGuid()}";

            for (var i = controller.layers.Length - 1; i >= 0; i--)
            {
                var layer = controller.layers[i];

                var blendTree = GetSingleBlendTreeAsDirect(layer, state, alwaysOneParameter);

                if (blendTree != null)
                {
                    var blendTreeModified = blendTree.GetAllBindings();
                    // nothing is animated in higher priority layer
                    if (!blendTreeModified.Any(modifiedProperties.Contains))
                    {
                        Debug.Log($"MergeBlendTreeLayer: Merging layer {layer.name} (index {i}) of {controller.name} with blend tree {blendTree.name}");
                        directBlendTrees.Add((i, blendTree));
                    }

                    modifiedProperties.UnionWith(blendTreeModified);
                }
                else
                {
                    foreach (var motion in layer.GetMotions())
                        modifiedProperties.UnionWith(motion.GetAllBindings());
                }
            }

            // if we found only one, leave it as is
            if (directBlendTrees.Count < 2) return;

            directBlendTrees.Reverse();

            // create merged layer
            var theLayer = controller.layers[directBlendTrees.Last().layerIndex];
            theLayer.name = "AAO BlendTree MergedLayer";
            var newState = new AnimatorState { name = "Merged Direct BlendTrees" };
            theLayer.stateMachine!.states = new[] { new ChildAnimatorState { state = newState } };
            theLayer.stateMachine.defaultState = newState;
            theLayer.defaultWeight = 1f;

            var newBlendTree = new BlendTree() { name = "Merged Direct BlendTrees" };
            newState.motion = newBlendTree;
            newBlendTree.blendType = BlendTreeType.Direct;
            newBlendTree.children = directBlendTrees.SelectMany(x => x.tree.children).ToArray();

            if (newBlendTree.children.Any(x => x.directBlendParameter == alwaysOneParameter))
            {
                controller.parameters = controller.parameters
                    .Append(new AnimatorControllerParameter()
                    {
                        name = alwaysOneParameter,
                        type = AnimatorControllerParameterType.Float,
                        defaultFloat = 1,
                    })
                    .ToArray();
            }

            // clear original layers
            foreach (var (layerIndex, _) in directBlendTrees.SkipLast(1))
            {
                var layer = controller.layers[layerIndex];
                layer.stateMachine!.states = Array.Empty<ChildAnimatorState>();
                layer.stateMachine.defaultState = null;
                layer.defaultWeight = 0f;
                layer.WeightChange = AnimatorWeightChange.NotChanged;
            }
        }

        private static BlendTree? GetSingleBlendTreeAsDirect(AOAnimatorControllerLayer layer,
            AnimatorOptimizerState optimizerState, string alwaysOneParameter)
        {
            if (layer is
                {
                    IsSyncedToOtherLayer: false,
                    IsSynced: false,
                    avatarMask: null,
                    IsOverride: true,
                    defaultWeight: 1,
                    WeightChange: AnimatorWeightChange.NotChanged or AnimatorWeightChange.AlwaysOne,
                    stateMachine:
                    {
                        stateMachines: { Length: 0 },
                        states: { Length: 1 } states,
                    } stateMachine
                })
            {
                var state = states[0].state;
                if (stateMachine.defaultState == state)
                {
                    if (state is
                        {
                            behaviours: { Length: 0 },
                            timeParameterActive: false,
                            // Those settings are per-state settings so we cannot keep them in merged state.
                            // They affect humanoid animations even if the clip is not time dependent.
                            // https://github.com/anatawa12/AvatarOptimizer/issues/1829
                            mirrorParameterActive: false,
                            mirror: false,
                            iKOnFeet: false,
                            motion: BlendTree { } blendTree,
                        })
                    {
                        // All children of Direct BlendTree share the normalized time, and the duration of the
                        // Direct BlendTree is the sum of 'weight * length' of each child. Therefore, merging
                        // BlendTrees changes the playback speed of time dependent clips.
                        // See BlendTreeTimeBehaviorChecks for the behavior.
                        // We also cannot keep speed, cycle offset and their parameters of the state in merged state,
                        // but they do not affect time independent clips so we don't have to check them.
                        // https://github.com/anatawa12/AvatarOptimizer/issues/1829
                        foreach (var clip in ACUtils.AllClips(blendTree))
                            if (optimizerState.IsTimeDependentClip(clip))
                                return null;

                        if (state.writeDefaultValues)
                        {
                            var isMergeableDirectBlendTree = blendTree.blendType == BlendTreeType.Direct;

                            using (var serialized = new SerializedObject(blendTree))
                                if (serialized.FindProperty("m_NormalizedBlendValues").boolValue)
                                    isMergeableDirectBlendTree = false;

                            if (isMergeableDirectBlendTree)
                                return blendTree;

                            return new()
                            {
                                blendType = BlendTreeType.Direct,
                                children = new ChildMotion[]
                                {
                                    new()
                                    {
                                        directBlendParameter = alwaysOneParameter,
                                        motion = blendTree,
                                        timeScale = 1,
                                    }
                                },
                            };
                        }
                        else
                        {
                            // for write defaults off, wight sum must be 1
                            if (ACUtils.AllBlendTrees(blendTree).All(allBlendTree =>
                                    allBlendTree.blendType is BlendTreeType.Simple1D 
                                        or BlendTreeType.SimpleDirectional2D
                                        or BlendTreeType.FreeformDirectional2D 
                                        or BlendTreeType.FreeformCartesian2D))
                            {
                                // verify target properties are same
                                var clips = ACUtils.AllClips(blendTree).ToHashSet();
                                var modifiedProperties = clips.GetAllBindings();

                                if (clips.Any(animationClip =>
                                        !modifiedProperties.SetEquals(animationClip.GetAllBindings())))
                                    return null;

                                return new()
                                {
                                    blendType = BlendTreeType.Direct,
                                    children = new ChildMotion[]
                                    {
                                        new()
                                        {
                                            directBlendParameter = alwaysOneParameter,
                                            motion = blendTree,
                                            timeScale = 1,
                                        }
                                    },
                                };
                            }
                            return null;
                        }
                    }
                }
            }

            return null;
        }
    }
}
