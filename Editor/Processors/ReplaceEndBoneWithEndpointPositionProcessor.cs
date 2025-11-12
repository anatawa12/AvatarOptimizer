#if AAO_VRCSDK3_AVATARS

using System;
using System.Collections.Generic;
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
            foreach (var replacer in context.GetComponents<ReplaceEndBoneWithEndpointPosition>())
            {
                using (ErrorReport.WithContextObject(replacer))
                {
                    Process(replacer);
                }
                DestroyTracker.DestroyImmediate(replacer);
            }
        }

        private static void Process(ReplaceEndBoneWithEndpointPosition replacer)
        {
            var physbones = replacer.GetComponents<VRCPhysBoneBase>();
            if (physbones.Length == 0) return;

            foreach (var physbone in physbones)
            {
                var endBones = physbone.GetAffectedLeafBones();

                if (!ValidatePhysBone(physbone, endBones)) continue;

                var localPositions = endBones.Select(x => x.localPosition);

                var replacementPosition = replacer.kind switch
                {
                    ReplaceEndBoneWithEndpointPositionKind.Average => GetAvaragePosition(localPositions),
                    ReplaceEndBoneWithEndpointPositionKind.Manual => replacer.manualReplacementPosition,
                    _ => throw new InvalidOperationException($"Invalid kind: {replacer.kind}"),
                };
                if (!AreApproximatelyEqualPosition(localPositions, replacementPosition))
                {
                    BuildLog.LogWarning("ReplaceEndBoneWithEndpointPosition:validation:inequivalentPositions", physbone);
                }

                foreach (var endbone in endBones)
                {
                    if (!ValidateEndBone(endbone)) continue;

                    if (!endbone.gameObject.TryGetComponent<MergeBone>(out _))
                        endbone.gameObject.AddComponent<MergeBone>().avoidNameConflict = true;
                }
                physbone.endpointPosition = replacementPosition;
            }
            
            bool ValidatePhysBone(VRCPhysBoneBase physbone, IEnumerable<Transform> endBones)
            {
                if (physbone.endpointPosition != Vector3.zero)
                {
                    BuildLog.LogWarning("ReplaceEndBoneWithEndpointPosition:validation:endpointPositionAlreadySet", physbone);
                    return false;
                }
                if (!IsSafeMultiChild(physbone, endBones))
                {
                    BuildLog.LogWarning("ReplaceEndBoneWithEndpointPosition:validation:unsafeMultiChild", physbone);
                    return true; // just warning
                }
                return true;
            }

            bool ValidateEndBone(Transform endBone)
            {
                if (endBone.GetComponents<Component>().Length != 1) // except transform
                {
                    BuildLog.LogWarning("ReplaceEndBoneWithEndpointPosition:validation:endBoneHasComponents", endBone);
                    return true; // just warning
                }
                return true;
            }
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

        public static bool IsSafeMultiChild(VRCPhysBoneBase physBoneBase, IEnumerable<Transform> leafBones)
        {
            var rootBone = physBoneBase.GetTarget();
            var multiChildType = physBoneBase.multiChildType;

            var ignores = new HashSet<Transform>(physBoneBase.ignoreTransforms);
            var leafBoneSet = new HashSet<Transform>(leafBones);

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
                    if (!IsSafeMultiChild(current, children, multiChildType, leafBoneSet))
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
                HashSet<Transform> leafBoneSet)
            {
                switch (multiChildType)
                {
                    case VRCPhysBoneBase.MultiChildType.Ignore:
                        // If after this transformation, it is no longer a Multi Child, that's not allowed.
                        var afterRemoval = children.Where(t => !leafBoneSet.Contains(t));
                        if (afterRemoval.Count() < 2)
                        {
                            return false;
                        }
                        break;
                    case VRCPhysBoneBase.MultiChildType.First:
                        // If the first child in multi child is being removed, that's not allowed.
                        if (leafBoneSet.Contains(children[0]))
                        {
                            return false;
                        }
                        break;
                    case VRCPhysBoneBase.MultiChildType.Average:
                        // If any children being averaged are to be removed, the average position will change.
                        if (children.Any(leafBoneSet.Contains))
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
