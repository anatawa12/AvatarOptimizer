using UnityEditor;

namespace Anatawa12.AvatarOptimizer
{
    internal class DebugConfigWindow : EditorWindow
    {
        [MenuItem("Tools/Avatar Optimizer/Debug Config Window")]
        static void Open() => GetWindow<DebugConfigWindow>("AAO Debug Config Window");

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "This window contains pc-global configuration for debugging AAO.\n" + 
                "This includes experimental features of AAO.\n" +
                "This window uses EditorPrefs so the configuration is shared between projects.", MessageType.None);

            FeatureFlag("com.anatawa12.avatar-optimizer.merge-bone-bindpose-optimization",
                "BindPose Optimization in MergeBone");
        }

        private void FeatureFlag(string id, string name)
        {
            var current = EditorPrefs.GetBool(id, false);
            var updated = EditorGUILayout.ToggleLeft(name, current);
            if (current != updated)
                EditorPrefs.SetBool(id, updated);
        }
    }
}