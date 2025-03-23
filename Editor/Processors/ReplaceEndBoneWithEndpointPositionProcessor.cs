#if AAO_VRCSDK3_AVATARS

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

        private const float tolerance = 0.00001f;

        protected override void Execute(BuildContext context)
        {
            foreach (var replacer in context.GetComponents<ReplaceEndBoneWithEndpointPosition>())
            {
                if (replacer.replacementPosition == Vector3.zero)
                {
                    BuildLog.LogError("ReplaceEndBoneWithEndpointPosition:validation:meaninglessReplacementPosition", replacer);
                }
                foreach (var physbone in replacer.GetComponents<VRCPhysBoneBase>())
                {
                    using (ErrorReport.WithContextObject(physbone))
                        Process(replacer, physbone);
                }
                DestroyTracker.DestroyImmediate(replacer);
            }
        }

        private static void Process(ReplaceEndBoneWithEndpointPosition replacer, VRCPhysBoneBase physbone)
        {
            if (physbone.endpointPosition != Vector3.zero)
            {
                BuildLog.LogError("ReplaceEndBoneWithEndpointPosition:validation:endpointPositionNotZero", replacer);
            }

            var endBones = physbone.GetEndBones();
            
            if (endBones.Any(b => b.GetComponents<Component>().Length != 1)) // except transfrom
            {
                BuildLog.LogWarning("ReplaceEndBoneWithEndpointPosition:validation:endBoneHasComponents", replacer);
            }

            if (AreEndBonesEqualLocalPosition(endBones, out var position))
            {
                BuildLog.LogWarning("ReplaceEndBoneWithEndpointPosition:validation:endBonesNotEqualLocalPosition", replacer);
            }
            else if (Vector3.Distance(replacer.replacementPosition, position) > tolerance)
            {
                BuildLog.LogWarning("ReplaceEndBoneWithEndpointPosition:validation:incorrectReplacementPosition", replacer);
            }

            foreach (var endbone in endBones)
            {
                if (!endbone.gameObject.GetComponent<MergeBone>())
                    endbone.gameObject.AddComponent<MergeBone>().avoidNameConflict = true;
            }
            physbone.endpointPosition = replacer.replacementPosition;
        }

        public static bool AreEndBonesEqualLocalPosition(IEnumerable<Transform> endBones, out Vector3 position)
        {
            return AreApproximatelyEqualPosition(endBones.Select(x => x.localPosition), tolerance, out position);
        }

        private static bool AreApproximatelyEqualPosition(IEnumerable<Vector3> positions, float tolerance, out Vector3 position)
        {
            position = Vector3.zero;
            // average
            foreach (var pos in positions)
            {
                position += pos;
            }
            position /= positions.Count();

            foreach (var pos in positions)
            {
                if (Vector3.Distance(position, pos) > tolerance)
                {
                    return false;
                }
            }

            return true;
        }
    }
}

#endif
