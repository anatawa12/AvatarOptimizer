#if AAO_VRCSDK3_AVATARS

using System;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class ClearEndpointPositionProcessor : Pass<ClearEndpointPositionProcessor>
    {
        public override string DisplayName => "ClearEndpointPosition";

        protected override void Execute(BuildContext context)
        {
            foreach (var component in context.GetComponents<ClearEndpointPosition>())
            foreach (var vrcPhysBoneBase in component.GetComponents<VRCPhysBoneBase>())
                using (ErrorReport.WithContextObject(vrcPhysBoneBase))
                    Process(vrcPhysBoneBase);
        }

        public static void Process(VRCPhysBoneBase pb)
        {
            CreateEndBones(pb, (name, parent, localPosition) =>
            {
                new GameObject(name) { transform = { localPosition = localPosition } }
                    .transform.SetParent(parent, worldPositionStays: false);
            });
            pb.endpointPosition = Vector3.zero;
            EditorUtility.SetDirty(pb);
        }

        public static void CreateEndBones(VRCPhysBoneBase pb, Action<string, Transform, Vector3> createEndBone)
        {
            if (pb.endpointPosition == Vector3.zero) return;
            WalkChildrenAndSetEndpoint(pb.GetTarget(), pb, createEndBone);
        }

        internal static bool WalkChildrenAndSetEndpoint(Transform target, VRCPhysBoneBase physBone,
            Action<string, Transform, Vector3> createEndBone)
        {
            if (physBone.ignoreTransforms.Contains(target))
                return false;
            var childCount = 0;
            for (var i = 0; i < target.childCount; i++)
                if (WalkChildrenAndSetEndpoint(target.GetChild(i), physBone, createEndBone))
                    childCount++;
            if (childCount == 0)
                createEndBone($"{target.name}_EndPhysBone", target, physBone.endpointPosition);

            return true;
        }
    }
}

#endif
