using UnityEditor;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(MergeBone))]
    public class MergeBoneEditor : Editor
    {
        private SaveVersionDrawer _saveVersion = new SaveVersionDrawer();
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("You will remove this GameObject and merge bone to parent");
            _saveVersion.Draw(serializedObject);
        }
    }
}
