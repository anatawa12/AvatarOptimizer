using UnityEditor;

namespace Anatawa12.AvatarOptimizer
{
    // https://github.com/bdunderscore/modular-avatar/blob/db49e2e210bc070671af963ff89df853ae4514a5/Packages/nadena.dev.modular-avatar/Editor/ApplyOnPlay.cs#L54
    // Originally under MIT License
    // Copyright (c) 2022 bd_
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
