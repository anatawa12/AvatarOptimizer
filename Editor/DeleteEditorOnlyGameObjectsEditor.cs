using CustomLocalization4EditorExtension;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(DeleteEditorOnlyGameObjects))]
    class DeleteEditorOnlyGameObjectsEditor : AvatarGlobalComponentEditor
    {
        protected override string Description => CL4EE.Tr("DeleteEditorOnlyGameObjects:description");
    }
}
