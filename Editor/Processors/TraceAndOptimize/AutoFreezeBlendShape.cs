using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using nadena.dev.ndmf;
using UnityEngine;

#if AAO_VRCSDK3_AVATARS
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
#endif

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    internal class AutoFreezeBlendShape : TraceAndOptimizePass<AutoFreezeBlendShape>
    {
        public override string DisplayName => "T&O: AutoFreezeBlendShape";

        protected override void Execute(BuildContext context, TraceAndOptimizeState state)
        {
            if (!state.OptimizeBlendShape) return;

            if (!state.SkipFreezingNonAnimatedBlendShape)
                FreezeNonAnimatedBlendShapes(context, state);
            if (!state.SkipFreezingMeaninglessBlendShape)
                FreezeMeaninglessBlendShapes(context, state);
        }

        void FreezeNonAnimatedBlendShapes(BuildContext context, TraceAndOptimizeState state)
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

        void FreezeMeaninglessBlendShapes(BuildContext context, TraceAndOptimizeState state) {
            ComputePreserveBlendShapes(context, state.PreserveBlendShapes);

            // second optimization: remove meaningless blendShapes
            foreach (var skinnedMeshRenderer in context.GetComponents<SkinnedMeshRenderer>())
            {
                if (state.Exclusions.Contains(skinnedMeshRenderer.gameObject)) continue; // manual exclusion
                if (skinnedMeshRenderer.TryGetComponent<Cloth>(out _)) continue; // cloth is too complex https://github.com/anatawa12/AvatarOptimizer/issues/1027
                skinnedMeshRenderer.gameObject.GetOrAddComponent<FreezeBlendShape>();
                skinnedMeshRenderer.gameObject.GetOrAddComponent<InternalAutoFreezeMeaninglessBlendShape>();
            }
        }

        private void ComputePreserveBlendShapes(BuildContext context, Dictionary<SkinnedMeshRenderer, HashSet<string>> preserveBlendShapes)
        {
#if AAO_VRCSDK3_AVATARS
            // some BlendShapes manipulated by VRC Avatar Descriptor must exists
            var descriptor = context.AvatarDescriptor;
            if (descriptor)
            {
                switch (descriptor.lipSync)
                {
                    case VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape when descriptor.VisemeSkinnedMesh != null:
                    {
                        var skinnedMeshRenderer = descriptor.VisemeSkinnedMesh;
                        if (!preserveBlendShapes.TryGetValue(skinnedMeshRenderer, out var set))
                            preserveBlendShapes.Add(skinnedMeshRenderer, set = new HashSet<string>());
                        set.UnionWith(descriptor.VisemeBlendShapes);
                        break;
                    }
                    case VRC_AvatarDescriptor.LipSyncStyle.JawFlapBlendShape when descriptor.VisemeSkinnedMesh != null:
                    {
                        var skinnedMeshRenderer = descriptor.VisemeSkinnedMesh;
                        if (!preserveBlendShapes.TryGetValue(skinnedMeshRenderer, out var set))
                            preserveBlendShapes.Add(skinnedMeshRenderer, set = new HashSet<string>());
                        set.Add(descriptor.MouthOpenBlendShapeName);
                        break;
                    }
                }

                if (descriptor.enableEyeLook)
                {
                    switch (descriptor.customEyeLookSettings.eyelidType)
                    {
                        case VRCAvatarDescriptor.EyelidType.None:
                            break;
                        case VRCAvatarDescriptor.EyelidType.Bones:
                            break;
                        case VRCAvatarDescriptor.EyelidType.Blendshapes
                            when descriptor.customEyeLookSettings.eyelidsSkinnedMesh != null:
                        {
                            var skinnedMeshRenderer = descriptor.customEyeLookSettings.eyelidsSkinnedMesh;
                            if (!preserveBlendShapes.TryGetValue(skinnedMeshRenderer, out var set))
                                preserveBlendShapes.Add(skinnedMeshRenderer, set = new HashSet<string>());

                            var mesh = skinnedMeshRenderer.sharedMesh;
                            set.UnionWith(
                                from index in descriptor.customEyeLookSettings.eyelidsBlendshapes
                                where 0 <= index && index < mesh.blendShapeCount
                                select mesh.GetBlendShapeName(index)
                            );
                        }
                            break;
                    }
                }
            }
#endif
        }
    }
}
