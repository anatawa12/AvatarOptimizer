using Anatawa12.AvatarOptimizer.Processors;
using CustomLocalization4EditorExtension;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(ClearEndpointPosition))]
    internal class ClearEndpointPositionEditor : Editor
    {
        private readonly SaveVersionDrawer _saveVersion = new SaveVersionDrawer();
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(CL4EE.Tr("ClearEndpointPosition:description"), MessageType.None);

            _saveVersion.Draw(serializedObject);

            if (GUILayout.Button(CL4EE.Tr("ClearEndpointPosition:button:Apply and Remove Component")))
            {
                ClearEndpointPositionProcessor.Process(((Component)target).GetComponent<VRCPhysBoneBase>());
                DestroyImmediate(target);
            }
        }
    }
}
