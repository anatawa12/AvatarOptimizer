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
                if (!ValidatePhysBone(physbone)) continue;

                var endBones = physbone.GetEndBones();
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

                    if (!endbone.gameObject.GetComponent<MergeBone>())
                        endbone.gameObject.AddComponent<MergeBone>().avoidNameConflict = true;
                }
                physbone.endpointPosition = replacementPosition;
            }
            
            bool ValidatePhysBone(VRCPhysBoneBase physbone)
            {
                if (physbone.endpointPosition != Vector3.zero)
                {
                    BuildLog.LogWarning("ReplaceEndBoneWithEndpointPosition:validation:endpointPositionAlreadySet", physbone);
                    return false;
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
    }
}

#endif
