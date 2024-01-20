using System;
using System.Linq;
using CustomLocalization4EditorExtension;
using nadena.dev.ndmf;
using nadena.dev.ndmf.localization;
using Debug = System.Diagnostics.Debug;

namespace Anatawa12.AvatarOptimizer
{
    internal static class BuildLog
    {
        public static void LogInfo(string code, params object[] args) =>
            ErrorReport.ReportError(_localizer, ErrorSeverity.Information, code, args);

        public static void LogWarning(string code, params object[] args) =>
            ErrorReport.ReportError(_localizer, ErrorSeverity.NonFatal, code, args);

        public static void LogError(string code, params object[] args) =>
            ErrorReport.ReportError(_localizer, ErrorSeverity.Error, code, args);

        
        private static Localizer _localizer = new Localizer("en-US", () =>
        {
            var localization = CL4EE.GetLocalization();
            Debug.Assert(localization != null, nameof(localization) + " != null");
            return localization.LocalizationByIsoCode.Values
                .Select(locale => (locale.LocaleIsoCode, (Func<string, string>)locale.TryGetLocalizedString))
                .ToList();
        });
    }
}
