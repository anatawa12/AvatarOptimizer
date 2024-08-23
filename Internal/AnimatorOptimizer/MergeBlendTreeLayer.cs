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
        private protected override void Execute(BuildContext context, AOAnimatorController controller,
            TraceAndOptimizeState settings)
        {
            if (settings.SkipMergeBlendTreeLayer) return;
            Execute(controller);
        }

        public static void Execute(AOAnimatorController controller, string? alwaysOneParameter = null)
        {
            var directBlendTrees = new List<(int layerIndex, BlendTree tree)>();

            var modifiedProperties = new HashSet<EditorCurveBinding>();

            alwaysOneParameter ??= $"AAO_AlwaysOne_{Guid.NewGuid()}";

            for (var i = controller.layers.Length - 1; i >= 0; i--)
            {
                var layer = controller.layers[i];

                var blendTree = GetSingleBlendTreeAsDirect(layer, alwaysOneParameter);

                if (blendTree != null)
                {
                    var blendTreeModified = ACUtils.AllClips(blendTree).Aggregate(new HashSet<EditorCurveBinding>(),
                        (set, clip) =>
                        {
                            set.UnionWith(AnimationUtility.GetCurveBindings(clip));
                            set.UnionWith(AnimationUtility.GetObjectReferenceCurveBindings(clip));
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

        private static BlendTree? GetSingleBlendTreeAsDirect(AOAnimatorControllerLayer layer, string alwaysOneParameter)
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
                            writeDefaultValues: true,
                            behaviours: { Length: 0 },
                            timeParameterActive: false,
                            motion: BlendTree { } blendTree,
                        })
                    {
                        bool isMergeableDirectBlendTree = blendTree.blendType == BlendTreeType.Direct;

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
                }
            }

            return null;
        }
    }
}
