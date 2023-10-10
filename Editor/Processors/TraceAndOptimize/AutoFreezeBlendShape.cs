using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    class AutoFreezeBlendShape
    {
        private readonly ImmutableModificationsContainer _modifications;
        private readonly OptimizerSession _session;
        private readonly HashSet<GameObject> _exclusions;

        public AutoFreezeBlendShape(ImmutableModificationsContainer modifications, OptimizerSession session,
            HashSet<GameObject> exclusions)
        {
            _modifications = modifications;
            _session = session;
            _exclusions = exclusions;
        }

        public void Process()
        {
            // first optimization: unused blend shapes
            foreach (var skinnedMeshRenderer in _session.GetComponents<SkinnedMeshRenderer>())
            {
                var mesh = skinnedMeshRenderer.sharedMesh;

                // skip SMR without mesh
                if (!mesh) continue;
                if (_exclusions.Contains(skinnedMeshRenderer.gameObject)) continue; // manual exclusiton

                var modifies = _modifications.GetModifiedProperties(skinnedMeshRenderer);
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

            ComputePreserveBlendShapes(_session.PreserveBlendShapes);

            // second optimization: remove meaningless blendShapes
            foreach (var skinnedMeshRenderer in _session.GetComponents<SkinnedMeshRenderer>())
            {
                if (_exclusions.Contains(skinnedMeshRenderer.gameObject)) continue; // manual exclusion
                skinnedMeshRenderer.gameObject.GetOrAddComponent<FreezeBlendShape>();
                skinnedMeshRenderer.gameObject.GetOrAddComponent<InternalAutoFreezeMeaninglessBlendShape>();
            }
        }

        private void ComputePreserveBlendShapes(Dictionary<SkinnedMeshRenderer, HashSet<string>> preserveBlendShapes)
        {
            // some BlendShapes manipulated by VRC Avatar Descriptor must exists
            var descriptor = _session.GetRootComponent<VRCAvatarDescriptor>();
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
