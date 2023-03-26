using CustomLocalization4EditorExtension;
using UnityEditor;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(MergeBone))]
    class MergeBoneEditor : AvatarTagComponentEditorBase
    {
        protected override string Description => CL4EE.Tr("MergeBone:description");

        protected override void OnInspectorGUIInner()
        {
        }
    }
}
