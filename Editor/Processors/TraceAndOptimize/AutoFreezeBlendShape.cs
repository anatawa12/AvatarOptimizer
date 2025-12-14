using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;

#if AAO_VRCSDK3_AVATARS
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
#endif

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    internal class AutoFreezeNonAnimatedBlendShape : TraceAndOptimizePass<AutoFreezeNonAnimatedBlendShape>
    {
        public override string DisplayName => "T&O: Automatically Freeze Non-Animated BlendShapes";
        protected override bool Enabled(TraceAndOptimizeState state) => state.FreezingNonAnimatedBlendShape;

        protected override void Execute(BuildContext context, TraceAndOptimizeState state)
        {
            var mergeBlendShapeMergeSkinnedMeshSources = new HashSet<SkinnedMeshRenderer>();
            foreach (var mergeSkinnedMesh in context.GetComponents<MergeSkinnedMesh>())
            {
                if (mergeSkinnedMesh.blendShapeMode == MergeSkinnedMesh.BlendShapeMode.MergeSameName)
                    mergeBlendShapeMergeSkinnedMeshSources.UnionWith(mergeSkinnedMesh.renderersSet.GetAsSet());
            }

            // first optimization: unused BlendShapes
            foreach (var skinnedMeshRenderer in context.GetComponents<SkinnedMeshRenderer>())
            {
                if (state.Exclusions.Contains(skinnedMeshRenderer.gameObject)) continue; // manual exclusiton
                if (skinnedMeshRenderer.TryGetComponent<Cloth>(out _)) continue; // cloth is too complex https://github.com/anatawa12/AvatarOptimizer/issues/1027
                if (mergeBlendShapeMergeSkinnedMeshSources.Contains(skinnedMeshRenderer)) continue; // skip if it will be merged
                skinnedMeshRenderer.gameObject.GetOrAddComponent<FreezeBlendShape>();
                skinnedMeshRenderer.gameObject.GetOrAddComponent<InternalAutoFreezeNonAnimatedBlendShapes>();
            }
        }
    }

    internal class AutoFreezeConstantlyAnimatedBlendShape : TraceAndOptimizePass<AutoFreezeConstantlyAnimatedBlendShape>
    {
        public override string DisplayName => "T&O: Automatically Freeze Constantly Animated BlendShapes";
        protected override bool Enabled(TraceAndOptimizeState state) => state.FreezingMeaninglessBlendShape;

        protected override void Execute(BuildContext context, TraceAndOptimizeState state)
        {
            foreach (var skinnedMeshRenderer in context.GetComponents<SkinnedMeshRenderer>())
            {
                if (state.Exclusions.Contains(skinnedMeshRenderer.gameObject)) continue; // manual exclusion
                if (skinnedMeshRenderer.TryGetComponent<Cloth>(out _)) continue; // cloth is too complex https://github.com/anatawa12/AvatarOptimizer/issues/1027
                skinnedMeshRenderer.gameObject.GetOrAddComponent<FreezeBlendShape>();
                skinnedMeshRenderer.gameObject.GetOrAddComponent<InternalAutoFreezeMeaninglessBlendShape>();
            }
        }
    }
}
