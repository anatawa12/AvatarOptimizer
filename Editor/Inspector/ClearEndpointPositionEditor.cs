#if AAO_VRCSDK3_AVATARS

using Anatawa12.AvatarOptimizer.Processors;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(ClearEndpointPosition))]
    internal class ClearEndpointPositionEditor : AvatarTagComponentEditorBase
    {
        protected override void OnInspectorGUIInner()
        {
            if (GUILayout.Button(AAOL10N.Tr("ClearEndpointPosition:button:Apply and Remove Component")))
            {
                var pb = ((Component)target).GetComponent<VRCPhysBoneBase>();
                Undo.SetCurrentGroupName("Clear Endpoint Position");
                ClearEndpointPositionProcessor.CreateEndBones(pb, (name, parent, localPosition) =>
                {
                    var gameObject = new GameObject(name);
                    Undo.RegisterCreatedObjectUndo(gameObject, $"Create EndBone: {name}");
                    Undo.SetTransformParent(gameObject.transform, parent, $"Set EndBone Parent: {name}");
                    Undo.RecordObject(gameObject.transform, $"Set Position of EndBone: {name}");
                    gameObject.transform.localPosition = localPosition;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(gameObject.transform);
                });
                Undo.RecordObject(pb, "Set Endpoint Position to Zero");
                pb.endpointPosition = Vector3.zero;
                PrefabUtility.RecordPrefabInstancePropertyModifications(pb);
                Undo.DestroyObjectImmediate(target);
            }
        }
    }
}

#endif
