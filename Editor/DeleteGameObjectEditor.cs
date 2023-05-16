using UnityEditor;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(DeleteGameObject))]
    internal class DeleteGameObjectEditor : AvatarTagComponentEditorBase
    {
        protected override void OnInspectorGUIInner()
        {
            EditorGUILayout.HelpBox(
                "DeleteGameObject is removed in v0.4.0. \n" +
                "use EditorOnly tag instead", MessageType.Error);
        }
    }
}
