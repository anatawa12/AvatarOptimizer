using Anatawa12.AvatarOptimizer.Processors;
using CustomLocalization4EditorExtension;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(ClearEndpointPosition))]
    internal class ClearEndpointPositionEditor : AvatarTagComponentEditorBase
    {
        protected override string Description => CL4EE.Tr("ClearEndpointPosition:description");

        protected override void OnInspectorGUIInner()
        {
            if (GUILayout.Button(CL4EE.Tr("ClearEndpointPosition:button:Apply and Remove Component")))
            {
                ClearEndpointPositionProcessor.Process(((Component)target).GetComponent<VRCPhysBoneBase>());
                DestroyImmediate(target);
            }
        }
    }
}
