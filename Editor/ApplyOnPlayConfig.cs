using UnityEditor;

namespace Anatawa12.AvatarOptimizer
{
    [InitializeOnLoad]
    internal static class ApplyOnPlayConfig
    {
        private const string GenerateMenuName = "Tools/Avatar Optimizer/Write to Asset on Play";
        private const string GenerateSettingName = "com.anatawa12.avatar-optimizer.write-on-play";

        public static bool Generate
        {
            get => EditorPrefs.GetBool(GenerateSettingName, false);
            set => EditorPrefs.SetBool(GenerateSettingName, value);
        }

        static ApplyOnPlayConfig()
        {
            EditorApplication.delayCall += () => Menu.SetChecked(GenerateMenuName, Generate);
        }

        [MenuItem(GenerateMenuName)]
        private static void ToggleGenerateOnPlay()
        {
            Generate = !Generate;
            Menu.SetChecked(GenerateMenuName, Generate);
        }
    }
}
