using CustomLocalization4EditorExtension;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(DeleteEditorOnlyGameObjects))]
    class DeleteEditorOnlyGameObjectsEditor : AvatarTagComponentEditorBase
    {
        protected override string Description => CL4EE.Tr("DeleteEditorOnlyGameObjects:description");

        protected override void OnInspectorGUIInner()
        {
            if (!((Component)serializedObject.targetObject).GetComponent<VRCAvatarDescriptor>())
                EditorGUILayout.HelpBox(CL4EE.Tr("DeleteEditorOnlyGameObjects:NotOnAvatarDescriptor"),
                    MessageType.Error);
        }
    }
}
