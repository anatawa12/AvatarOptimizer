using UnityEditor;
using UnityEngine;
using VRC.Dynamics;

namespace Anatawa12.Merger
{
    internal class ClearEndpointPosition : EditorWindow
    {
        private VRCPhysBoneBase _physBoneBase;

        private void OnGUI()
        {
            _physBoneBase = (VRCPhysBoneBase)EditorGUILayout.ObjectField(_physBoneBase, typeof(VRCPhysBoneBase), true);
            EditorGUI.BeginDisabledGroup(!_physBoneBase);
            if (GUILayout.Button("Do Clear"))
            {
                Clear(_physBoneBase);
            }
            EditorGUI.EndDisabledGroup();
        }

        private static void Clear(VRCPhysBoneBase physBoneBase)
        {
            if (physBoneBase.endpointPosition != Vector3.zero)
            {
                MergePhysBone.WalkChildrenAndSetEndpoint(
                    physBoneBase.rootTransform ? physBoneBase.rootTransform : physBoneBase.transform, physBoneBase);
                physBoneBase.endpointPosition = Vector3.zero;
            }
        }

        [MenuItem("Tools/Merger/Clear Endpoint Position")]
        public static void Open() => CreateWindow<ClearEndpointPosition>();
    }
}
