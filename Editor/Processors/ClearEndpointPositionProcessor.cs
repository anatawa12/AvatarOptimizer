using Anatawa12.AvatarOptimizer.ErrorReporting;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class ClearEndpointPositionProcessor
    {
        public void Process(OptimizerSession session)
        {
            BuildReport.ReportingObjects(session.GetComponents<ClearEndpointPosition>(),
                component => BuildReport.ReportingObjects(component.GetComponents<VRCPhysBoneBase>(), Process));
        }
        
        public static void Process(VRCPhysBoneBase pb)
        {
            if (pb.endpointPosition == Vector3.zero) return;
            WalkChildrenAndSetEndpoint(pb.GetTarget(), pb);
            pb.endpointPosition = Vector3.zero;
            EditorUtility.SetDirty(pb);
        }

        internal static bool WalkChildrenAndSetEndpoint(Transform target, VRCPhysBoneBase physBone)
        {
            if (physBone.ignoreTransforms.Contains(target))
                return false;
            var childCount = 0;
            for (var i = 0; i < target.childCount; i++)
                if (WalkChildrenAndSetEndpoint(target.GetChild(i), physBone))
                    childCount++;
            if (childCount == 0)
            {
                var go = new GameObject($"{target.name}_EndPhysBone");
                go.transform.parent = target;
                go.transform.localPosition = physBone.endpointPosition;
            }
            return true;
        }
    }
}
