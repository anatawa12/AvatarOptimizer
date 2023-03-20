using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    // https://github.com/bdunderscore/modular-avatar/blob/db49e2e210bc070671af963ff89df853ae4514a5/Packages/nadena.dev.modular-avatar/Editor/ApplyOnPlay.cs#L54
    // Originally under MIT License
    // Copyright (c) 2022 bd_
    [InitializeOnLoad]
    internal static class ApplyOnPlay
    {
        private const string EnableMenuName = "Tools/Avatar Optimizer/Apply on Play";
        private const string EnableSettingName = "com.anatawa12.avatar-optimizer.apply-on-play";

        private const string GenerateMenuName = "Tools/Avatar Optimizer/Write to Asset on Play";
        private const string GenerateSettingName = "com.anatawa12.avatar-optimizer.write-on-play";

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(EnableSettingName, true);
            set => EditorPrefs.SetBool(EnableSettingName, value);
        }

        public static bool Generate
        {
            get => EditorPrefs.GetBool(GenerateSettingName, false);
            set => EditorPrefs.SetBool(GenerateSettingName, value);
        }

        /**
         * We need to process avatars before lyuma's av3 emulator wakes up and processes avatars; it does this in Awake,
         * so we have to do our processing in Awake as well. This seems to work fine when first entering play mode, but
         * if you subsequently enable an initially-disabled avatar, processing from within Awake causes an editor crash.
         *
         * To workaround this, we initially process in awake; then, after OnPlayModeStateChanged is invoked (ie, after
         * all initially-enabled components have Awake called), we switch to processing from Start instead.
         */
        private static RuntimeUtil.OnDemandSource _armedSource = RuntimeUtil.OnDemandSource.Awake;

        static ApplyOnPlay()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            RuntimeUtil.OnDemandProcessAvatar = MaybeProcessAvatar;
            EditorApplication.delayCall += () => Menu.SetChecked(EnableMenuName, Enabled);
            EditorApplication.delayCall += () => Menu.SetChecked(GenerateMenuName, Generate);
        }

        private static void MaybeProcessAvatar(RuntimeUtil.OnDemandSource source, MonoBehaviour component)
        {
            if (Enabled && source == _armedSource && component != null)
            {
                var avatar = RuntimeUtil.FindAvatarInParents(component.transform)
                if (avatar == null) return
                OptimizerProcessor.ProcessObject(new OptimizerSession(avatar.gameObject, Generate))
            }
        }

        [MenuItem(EnableMenuName)]
        private static void ToggleApplyOnPlay()
        {
            Enabled = !Enabled;
            Menu.SetChecked(EnableMenuName, Enabled);
        }

        [MenuItem(GenerateMenuName)]
        private static void ToggleGenerateOnPlay()
        {
            Generate = !Generate;
            Menu.SetChecked(GenerateMenuName, Generate);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange obj)
        {
            if (obj == PlayModeStateChange.EnteredPlayMode)
            {
                _armedSource = RuntimeUtil.OnDemandSource.Start;
            }
        }
    }
}
