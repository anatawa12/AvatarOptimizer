#if AAO_VRCSDK3_AVATARS

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;
using VRC.Dynamics;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class ReplaceEndBoneWithEndpointPositionProcessor : Pass<ReplaceEndBoneWithEndpointPositionProcessor>
    {
        public override string DisplayName => "ReplaceEndBoneWithEndpointPositionProcessor";

        private const float TOLERANCE = 0.00001f;

        protected override void Execute(BuildContext context) => ExecuteImpl(context);
        public static void ExecuteImpl(BuildContext context)
        {
            var replacers = context.GetComponents<ReplaceEndBoneWithEndpointPosition>();
            if (replacers.Length == 0) return;

            var overlappedPhysBones = GetOverlappedPhysBones(context.GetComponents<VRCPhysBoneBase>());
            var componentInfos = context.Extension<GCComponentInfoContext>();
            
            foreach (var replacer in replacers)
            {
                using (ErrorReport.WithContextObject(replacer))
                {
                    Process(context, replacer, componentInfos, overlappedPhysBones);
                }
                DestroyTracker.DestroyImmediate(replacer);
            }
        }

        private static void Process(
            BuildContext context,
            ReplaceEndBoneWithEndpointPosition replacer, 
            GCComponentInfoContext componentInfos, 
            HashSet<VRCPhysBoneBase> overlappedPhysBones)
        {
            var physbones = replacer.GetComponents<VRCPhysBoneBase>();
            if (physbones.Length == 0) return;

            foreach (var physbone in physbones)
            {
                var leafBones = physbone.GetAffectedLeafBones().ToHashSet();

                if (!CanReplace(replacer, physbone, overlappedPhysBones, leafBones, out var replacementPosition)) continue;

                foreach (var leafBone in leafBones)
                {
                    physbone.ignoreTransforms.Add(leafBone);
                }
                physbone.endpointPosition = replacementPosition;

                // Remove PB <=> Transform dependencies in the hope that MergeBone will be applied
                var pbInfo = componentInfos.GetInfo(physbone);
                foreach (var leafBone in leafBones)
                {
                    pbInfo.RemoveDependencyType(leafBone, GCComponentInfo.DependencyType.PhysBone);
                    componentInfos.GetInfo(leafBone).RemoveDependencyType(physbone, GCComponentInfo.DependencyType.Normal);
                }
                
                // Remove PhysBone mutations on leafBones in the hope that MergeBone will be applied
                foreach (var leafBone in leafBones)
                {
                    var animationInfo = context.GetAnimationComponent(leafBone);
                    animationInfo.GetFloatNode("m_LocalPosition.x").Remove(physbone);
                    animationInfo.GetFloatNode("m_LocalPosition.y").Remove(physbone);
                    animationInfo.GetFloatNode("m_LocalPosition.z").Remove(physbone);
                    animationInfo.GetFloatNode("m_LocalRotation.x").Remove(physbone);
                    animationInfo.GetFloatNode("m_LocalRotation.y").Remove(physbone);
                    animationInfo.GetFloatNode("m_LocalRotation.z").Remove(physbone);
                    animationInfo.GetFloatNode("m_LocalRotation.w").Remove(physbone);
                }
            }
        }

        private static bool CanReplace(ReplaceEndBoneWithEndpointPosition replacer, VRCPhysBoneBase physbone, HashSet<VRCPhysBoneBase> overlappedPhysBones, HashSet<Transform> leafBones, out Vector3 replacementPosition)
        {
            replacementPosition = default;

            if (leafBones.Count == 0) return false;
            if (!ValidatePhysBone(physbone, leafBones)) return false;

            var localPositions = leafBones.Select(x => x.localPosition);
            switch (replacer.kind)
            {
                case ReplaceEndBoneWithEndpointPositionKind.Average:
                    replacementPosition = GetAvaragePosition(localPositions);
                    if (!AreApproximatelyEqualPosition(localPositions, replacementPosition))
                    {
                        BuildLog.LogWarning("ReplaceEndBoneWithEndpointPosition:validation:inequivalentPositions", physbone);
                    }
                    break;
                case ReplaceEndBoneWithEndpointPositionKind.Override:
                    // Manual replacement: User is responsible for correctness, so no warnings are issued here.
                    replacementPosition = replacer.overridePosition;
                    break;
                default:
                    throw new InvalidOperationException($"Invalid kind: {replacer.kind}");
            }

            return leafBones.All(ValidateLeafBone);

            bool ValidatePhysBone(VRCPhysBoneBase physbone, HashSet<Transform> leafBones)
            {
                if (overlappedPhysBones.Contains(physbone))
                {
                    // just warning
                    BuildLog.LogWarning("ReplaceEndBoneWithEndpointPosition:validation:overlappedPhysBone", physbone); 
                }
                if (physbone.endpointPosition != Vector3.zero)
                {
                    BuildLog.LogError("ReplaceEndBoneWithEndpointPosition:validation:endpointPositionAlreadySet", physbone);
                    return false;
                }
                if (!IsSafeMultiChild(physbone, leafBones))
                {
                    BuildLog.LogError("ReplaceEndBoneWithEndpointPosition:validation:unsafeMultiChild", physbone);
                    return false;
                }
                return true;
            }

            bool ValidateLeafBone(Transform leafBone)
            {
                return true;
            }
        }

        public static HashSet<VRCPhysBoneBase> GetOverlappedPhysBones(VRCPhysBoneBase[] physBones)
        {
            var transformToPhysBones = new Dictionary<Transform, List<VRCPhysBoneBase>>();
            foreach (var physbone in physBones)
            {
                foreach (var t in physbone.GetAffectedTransforms())
                {
                    if (!transformToPhysBones.TryGetValue(t, out var list))
                    {
                        list = new List<VRCPhysBoneBase>();
                        transformToPhysBones[t] = list;
                    }
                    list.Add(physbone);
                }
            }

            var overlappedPhysBones = new HashSet<VRCPhysBoneBase>();
            foreach (var physBoneList in transformToPhysBones.Values)
            {
                if (physBoneList.Count > 1)
                {
                    overlappedPhysBones.UnionWith(physBoneList);
                }
            }

            return overlappedPhysBones;
        }

        public static Vector3 GetAvaragePosition(IEnumerable<Vector3> positions)
        {
            return positions.Aggregate(Vector3.zero, (current, position) => current + position) / positions.Count();
        }

        public static bool AreApproximatelyEqualPosition(IEnumerable<Vector3> positions, Vector3 position)
        {
            foreach (var pos in positions)
            {
                if (Vector3.Distance(position, pos) > TOLERANCE)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsBoneLengthChange(VRCPhysBoneBase physBone)
        {
            var anyGrabbingAllowed = IsAnyGrabbingAllowed(physBone.allowGrabbing, physBone.grabFilter);
            var anyLengthChange = physBone.maxStretch != 0f || physBone.maxSquish != 0f;
            var stretchMotion = physBone.stretchMotion != 0f;
            return physBone.version switch
            {
                VRCPhysBoneBase.Version.Version_1_0 => anyGrabbingAllowed && anyLengthChange,
                VRCPhysBoneBase.Version.Version_1_1 => (anyGrabbingAllowed || stretchMotion) && anyLengthChange,
                _ => throw new InvalidOperationException($"Invalid version: {physBone.version}"),
            };
        }

        private static bool IsAnyGrabbingAllowed(VRCPhysBoneBase.AdvancedBool allow, VRCPhysBoneBase.PermissionFilter filter)
        {
            return allow switch
            {
                VRCPhysBoneBase.AdvancedBool.True => true,
                VRCPhysBoneBase.AdvancedBool.False => false,
                VRCPhysBoneBase.AdvancedBool.Other => filter.allowSelf || filter.allowOthers,
                _ => throw new InvalidOperationException($"Invalid allow: {allow}"),
            };
        }

        public static bool IsSafeMultiChild(VRCPhysBoneBase physBoneBase, HashSet<Transform> leafBones)
        {
            var rootBone = physBoneBase.GetTarget();
            var multiChildType = physBoneBase.multiChildType;

            var ignores = new HashSet<Transform>(physBoneBase.ignoreTransforms);

            var queue = new Queue<Transform>();
            queue.Enqueue(rootBone);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                
                var children = current.DirectChildrenEnumerable()
                                    .Where(t => !ignores.Contains(t))
                                    .ToList();

                if (children.Count > 1) // fork bone
                {
                    if (!IsSafeMultiChild(current, children, multiChildType, leafBones))
                    {
                        return false;
                    }
                }

                foreach (var child in children)
                {
                    queue.Enqueue(child);
                }
            }

            return true;

            static bool IsSafeMultiChild(
                Transform forkBone,
                List<Transform> children,
                VRCPhysBoneBase.MultiChildType multiChildType,
                HashSet<Transform> leafBones)
            {
                switch (multiChildType)
                {
                    case VRCPhysBoneBase.MultiChildType.Ignore:
                        // If after this transformation, it is no longer a Multi Child, that's not allowed.
                        var afterRemoval = children.Where(t => !leafBones.Contains(t));
                        if (afterRemoval.Count() < 2)
                        {
                            return false;
                        }
                        break;
                    case VRCPhysBoneBase.MultiChildType.First:
                        // If the first child in multi child is being removed, that's not allowed.
                        if (leafBones.Contains(children[0]))
                        {
                            return false;
                        }
                        break;
                    case VRCPhysBoneBase.MultiChildType.Average:
                        // If any children being averaged are to be removed, the average position will change.
                        if (children.Any(leafBones.Contains))
                        {
                            return false;
                        }
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid multiChildType: {multiChildType}");
                }
                return true;
            }
        }
    }
}

#endif
