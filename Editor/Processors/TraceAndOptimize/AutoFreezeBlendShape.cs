using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    internal class AutoFreezeBlendShape : Pass<AutoFreezeBlendShape>
    {
        public override string DisplayName => "T&O: AutoFreezeBlendShape";

        protected override void Execute(BuildContext context)
        {
            var state = context.GetState<TraceAndOptimizeState>();
            if (!state.FreezeBlendShape) return;

            if (!state.SkipFreezingNonAnimatedBlendShape)
                FreezeNonAnimatedBlendShapes(context, state);
            if (!state.SkipFreezingMeaninglessBlendShape)
                FreezeMeaninglessBlendShapes(context, state);
        }

        void FreezeNonAnimatedBlendShapes(BuildContext context, TraceAndOptimizeState state)
        {
            // first optimization: unused blend shapes
            foreach (var skinnedMeshRenderer in context.GetComponents<SkinnedMeshRenderer>())
            {
                var mesh = skinnedMeshRenderer.sharedMesh;

                // skip SMR without mesh
                if (!mesh) continue;
                if (state.Exclusions.Contains(skinnedMeshRenderer.gameObject)) continue; // manual exclusiton

                var modifies = state.Modifications.GetModifiedProperties(skinnedMeshRenderer);
                var blendShapeValues = Enumerable.Range(0, mesh.blendShapeCount)
                    .Select(i => skinnedMeshRenderer.GetBlendShapeWeight(i)).ToArray();
                var notChanged = Enumerable.Range(0, mesh.blendShapeCount)
                    .Select(i => mesh.GetBlendShapeName(i))
                    .Where((name, i) =>
                    {
                        if (!modifies.TryGetValue($"blendShape.{name}", out var prop)) return true;

                        switch (prop.State)
                        {
                            case AnimationProperty.PropertyState.ConstantAlways:
                                blendShapeValues[i] = prop.ConstValue;
                                return true;
                            case AnimationProperty.PropertyState.ConstantPartially:
                                return prop.ConstValue.CompareTo(blendShapeValues[i]) == 0;
                            case AnimationProperty.PropertyState.Variable:
                                return false;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    })
                    .ToArray();

                if (notChanged.Length == 0) continue;

                for (var i = 0; i < blendShapeValues.Length; i++)
                    skinnedMeshRenderer.SetBlendShapeWeight(i, blendShapeValues[i]);
                EditorUtility.SetDirty(skinnedMeshRenderer);

                var freeze = skinnedMeshRenderer.gameObject.GetOrAddComponent<FreezeBlendShape>();
                var serialized = new SerializedObject(freeze);
                var editorUtil = PrefabSafeSet.EditorUtil<string>.Create(
                    serialized.FindProperty(nameof(FreezeBlendShape.shapeKeysSet)),
                    0, p => p.stringValue, (p, v) => p.stringValue = v);
                foreach (var shape in notChanged)
                    editorUtil.GetElementOf(shape).EnsureAdded();
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        void FreezeMeaninglessBlendShapes(BuildContext context, TraceAndOptimizeState state) {
            ComputePreserveBlendShapes(context, state.PreserveBlendShapes);

            // second optimization: remove meaningless blendShapes
            foreach (var skinnedMeshRenderer in context.GetComponents<SkinnedMeshRenderer>())
            {
                if (state.Exclusions.Contains(skinnedMeshRenderer.gameObject)) continue; // manual exclusion
                skinnedMeshRenderer.gameObject.GetOrAddComponent<FreezeBlendShape>();
                skinnedMeshRenderer.gameObject.GetOrAddComponent<InternalAutoFreezeMeaninglessBlendShape>();
            }
        }

        private void ComputePreserveBlendShapes(BuildContext context, Dictionary<SkinnedMeshRenderer, HashSet<string>> preserveBlendShapes)
        {
            // some BlendShapes manipulated by VRC Avatar Descriptor must exists
            var descriptor = context.AvatarDescriptor;
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
    }
}
