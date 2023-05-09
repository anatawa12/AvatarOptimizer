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
            foreach (var component in session.GetComponents<ClearEndpointPosition>())
                BuildReport.ReportingObject(component, () => Process(component.GetComponent<VRCPhysBoneBase>()));
        }
        
        public static void Process(VRCPhysBoneBase pb)
        {
            if (pb.endpointPosition == Vector3.zero) return;
            WalkChildrenAndSetEndpoint(pb.GetTarget(), pb);
            pb.endpointPosition = Vector3.zero;
            EditorUtility.SetDirty(pb);
        }

        internal static void WalkChildrenAndSetEndpoint(Transform target, VRCPhysBoneBase physBone)
        {
            if (physBone.ignoreTransforms.Contains(target))
                return;
            if (target.childCount == 0)
            {
                var go = new GameObject($"{target.name}_EndPhysBone");
                go.transform.parent = target;
                go.transform.localPosition = physBone.endpointPosition;
                return;
            }
            for (var i = 0; i < target.childCount; i++)
                WalkChildrenAndSetEndpoint(target.GetChild(i), physBone);
        }
    }
}
