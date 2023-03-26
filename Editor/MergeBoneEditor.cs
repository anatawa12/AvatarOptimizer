using CustomLocalization4EditorExtension;
using UnityEditor;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(MergeBone))]
    public class MergeBoneEditor : Editor
    {
        private SaveVersionDrawer _saveVersion = new SaveVersionDrawer();
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(CL4EE.Tr("MergeBone:description"), MessageType.None);
            _saveVersion.Draw(serializedObject);
        }
    }
}
