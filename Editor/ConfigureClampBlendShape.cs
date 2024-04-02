using UnityEditor;
using UnityEditor.Callbacks;

namespace Anatawa12.AvatarOptimizer
{
    [InitializeOnLoad]
    internal static class ConfigureClampBlendShape
    {
        private const string MenuName = "Tools/Avatar Optimizer/Configure Clamp BlendShape Weight";
        private const string ForceMenuName = "Tools/Avatar Optimizer/Force Configure Clamp BlendShape Weight Now";
        private const string SettingName = "com.anatawa12.avatar-optimizer.configure-clamp-blendshape-weight.enabled";

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(SettingName, true);
            set => EditorPrefs.SetBool(SettingName, value);
        }

        [MenuItem(MenuName)]
        private static void ToggleGenerateOnPlay()
        {
            Enabled = !Enabled;
            Menu.SetChecked(MenuName, Enabled);
        }

        [MenuItem(ForceMenuName)]
        private static void Force()
        {
            PlayerSettings.legacyClampBlendShapeWeights = EditorApplication.isPlayingOrWillChangePlaymode;
        }

        static ConfigureClampBlendShape()
        {
            EditorApplication.delayCall += () => Menu.SetChecked(MenuName, Enabled);
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += OnStateChanged;
        }

        private static void OnStateChanged(PlayModeStateChange obj)
        {
            switch (obj)
            {
                case PlayModeStateChange.ExitingEditMode:
                    if (Enabled)
                        PlayerSettings.legacyClampBlendShapeWeights = true;
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    if (Enabled)
                        PlayerSettings.legacyClampBlendShapeWeights = false;
                    break;
            }
        }

        private static int counter = -1;

        private static void Update()
        {
            if (counter < 0) return;
            if (counter > 0)
            {
                counter--;
                return;
            }

            counter--;

            if (Enabled && !EditorApplication.isPlayingOrWillChangePlaymode)
                PlayerSettings.legacyClampBlendShapeWeights = false;
        }

        [DidReloadScripts(int.MaxValue)]
        private static void DidReloadScripts()
        {
            // we reset legacyClampBlendShapeWeights to false just after assembly reload
            // since VRCSDSK will set it to true
            counter = 2;
        }
    }
}
