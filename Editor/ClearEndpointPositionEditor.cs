using Anatawa12.AvatarOptimizer.Processors;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(ClearEndpointPosition))]
    internal class ClearEndpointPositionEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("You clear Endpoint Position with _end bones.");
            EditorGUILayout.LabelField("This can be useful for MergeBone component");

            if (GUILayout.Button("Apply and Remove Component"))
            {
                ClearEndpointPositionProcessor.Process((ClearEndpointPosition)target);
                DestroyImmediate(target);
            }
        }
    }
}
