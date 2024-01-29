#if LEGACY_MODULAR_AVATAR
using nadena.dev.ndmf.localization;
using UnityEditor;

namespace Anatawa12.AvatarOptimizer
{
    [InitializeOnLoad]
    internal static class LegacyModularAvatarWarning
    {
        static LegacyModularAvatarWarning()
        {
            if (SessionState.GetBool("com.anatawa12.legacy-ma-warning", false)) return;
            SessionState.SetBool("com.anatawa12.legacy-ma-warning", true);
            EditorApplication.delayCall += DisplayWarning;
        }

        private static void DisplayWarning()
        {
            for (;;)
            {
                var result = EditorUtility.DisplayDialog("AvatarOptimizer",
                    AAOL10N.Tr("LegacyModularAvatarWarning:message"),
                    "OK",
                    AAOL10N.Tr("LegacyModularAvatarWarning:readWithNextLocale"));
                if (result) return;

                LanguagePrefs.Language = AAOL10N.Tr("LegacyModularAvatarWarning:nextLocale");
            }
        }
    }
}
#endif
