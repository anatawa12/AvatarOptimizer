using UnityEditor;

namespace Anatawa12.AvatarOptimizer.CheckForUpdate
{
    [InitializeOnLoad]
    internal class MenuItems
    {
        private const string ToggleCheckForUpdate = "Tools/Avatar Optimizer/Check for Update/Enabled";
        private const string ForceCheckForBetaRelease = "Tools/Avatar Optimizer/Check for Update/Check for Prerelease";
        private const string ToggleSettingName = "com.anatawa12.avatar-optimizer.check-for-update.enabled";
        private const string BetaSettingName = "com.anatawa12.avatar-optimizer.check-for-update.beta";

        public static bool CheckForUpdateEnabled
        {
            get => EditorPrefs.GetBool(ToggleSettingName, true);
            private set => EditorPrefs.SetBool(ToggleSettingName, value);
        }

        public static bool ForceBetaChannel
        {
            get => EditorPrefs.GetBool(BetaSettingName, false);
            private set => EditorPrefs.SetBool(BetaSettingName, value);
        }

        [MenuItem(ToggleCheckForUpdate)]
        private static void ToggleGenerateOnPlay()
        {
            CheckForUpdateEnabled = !CheckForUpdateEnabled;
            Menu.SetChecked(ToggleCheckForUpdate, CheckForUpdateEnabled);
        }

        [MenuItem(ForceCheckForBetaRelease)]
        private static void ToggleBetaChannel()
        {
            ForceBetaChannel = !ForceBetaChannel;
            Menu.SetChecked(ForceCheckForBetaRelease, ForceBetaChannel);
        }
        
        static MenuItems()
        {
            EditorApplication.delayCall += () => Menu.SetChecked(ToggleCheckForUpdate, CheckForUpdateEnabled);
            EditorApplication.delayCall += () => Menu.SetChecked(ForceCheckForBetaRelease, ForceBetaChannel);
        }
    }
}
