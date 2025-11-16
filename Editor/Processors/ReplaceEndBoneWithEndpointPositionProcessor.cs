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

        protected override void Execute(BuildContext context)
        {
            var replacers = context.GetComponents<ReplaceEndBoneWithEndpointPosition>();
            if (replacers.Length == 0) return;

            if (HasNestedPhysBone(context.GetComponents<VRCPhysBoneBase>(), out var nestedPhysBone))
            {
                BuildLog.LogError("ReplaceEndBoneWithEndpointPosition:validation:nestedPhysBone", nestedPhysBone);
                return;
            }

            var componentInfos = context.Extension<GCComponentInfoContext>();
            foreach (var replacer in replacers)
            {
                using (ErrorReport.WithContextObject(replacer))
                {
                    Process(replacer, componentInfos);
                }
                DestroyTracker.DestroyImmediate(replacer);
            }
        }

        private static void Process(ReplaceEndBoneWithEndpointPosition replacer, GCComponentInfoContext componentInfos)
        {
            var physbones = replacer.GetComponents<VRCPhysBoneBase>();
            if (physbones.Length == 0) return;

            foreach (var physbone in physbones)
            {
                if (!CanReplace(replacer, physbone, out var leafBones, out var replacementPosition)) continue;

                foreach (var leafBone in leafBones)
                {
                    physbone.ignoreTransforms.Add(leafBone);

                    componentInfos.GetInfo(leafBone).Dependencies.Remove(physbone);
                }
                physbone.endpointPosition = replacementPosition;
            }
        }

        private static bool CanReplace(ReplaceEndBoneWithEndpointPosition replacer, VRCPhysBoneBase physbone, out HashSet<Transform> leafBones, out Vector3 replacementPosition)
        {
            leafBones = physbone.GetAffectedLeafBones().ToHashSet();
            replacementPosition = default;

            if (leafBones.Count == 0) return false;
            if (!ValidatePhysBone(physbone, leafBones)) return false;

            var localPositions = leafBones.Select(x => x.localPosition);
            replacementPosition = replacer.kind switch
            {
                ReplaceEndBoneWithEndpointPositionKind.Average => GetAvaragePosition(localPositions),
                ReplaceEndBoneWithEndpointPositionKind.Manual => replacer.manualReplacementPosition,
                _ => throw new InvalidOperationException($"Invalid kind: {replacer.kind}"),
            };
            if (!AreApproximatelyEqualPosition(localPositions, replacementPosition))
            {
                BuildLog.LogWarning("ReplaceEndBoneWithEndpointPosition:validation:inequivalentPositions", physbone);
            }

            return leafBones.All(ValidateLeafBone);

            bool ValidatePhysBone(VRCPhysBoneBase physbone, HashSet<Transform> leafBones)
            {
                if (physbone.endpointPosition != Vector3.zero)
                {
                    BuildLog.LogError("ReplaceEndBoneWithEndpointPosition:validation:endpointPositionAlreadySet", physbone);
                    return false;
                }
                if (!IsSafeMultiChild(physbone, leafBones))
                {
                    BuildLog.LogWarning("ReplaceEndBoneWithEndpointPosition:validation:unsafeMultiChild", physbone);
                    return true; // just warning
                }
                return true;
            }

            bool ValidateLeafBone(Transform leafBone)
            {
                if (leafBone.GetComponents<Component>().Length != 1) // except transform
                {
                    BuildLog.LogWarning("ReplaceEndBoneWithEndpointPosition:validation:leafBoneHasComponents", leafBone);
                    return true; // just warning
                }
                return true;
            }
        }

        public static bool HasNestedPhysBone(VRCPhysBoneBase[] physBones, [NotNullWhen(true)] out VRCPhysBoneBase? nestedPhysBone)
        {
            nestedPhysBone = null;
            var allAffectedTransforms = new HashSet<Transform>();
            foreach (var physbone in physBones)
            {
                var affectedTransforms = physbone.GetAffectedTransforms();
                if (allAffectedTransforms.Overlaps(affectedTransforms))
                {
                    nestedPhysBone = physbone;
                    return true;
                }
                allAffectedTransforms.UnionWith(affectedTransforms);
            }
            return false;
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

        public static bool IsStretchMotionEnabled(VRCPhysBoneBase physBone)
        {
            return physBone.version switch
            {
                VRCPhysBoneBase.Version.Version_1_0 => physBone.maxStretch != 0f,
                VRCPhysBoneBase.Version.Version_1_1 => physBone.stretchMotion != 0f && (physBone.maxStretch != 0f || physBone.maxSquish != 0f),
                _ => throw new InvalidOperationException($"Invalid version: {physBone.version}"),
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
