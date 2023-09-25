#if LEGACY_MODULAR_AVATAR
using System.Diagnostics;
using CustomLocalization4EditorExtension;
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
            var localization = CL4EE.GetLocalization();
            Debug.Assert(localization != null, nameof(localization) + " != null");
            for (;;)
            {
                var result = EditorUtility.DisplayDialog("AvatarOptimizer",
                    localization.Tr("LegacyModularAvatarWarning:message"),
                    "OK",
                    localization.Tr("LegacyModularAvatarWarning:readWithNextLocale"));
                if (result) return;

                localization.CurrentLocaleCode = localization.Tr("LegacyModularAvatarWarning:nextLocale");
            }
        }
    }
}
#endif
