using nadena.dev.ndmf;

namespace Anatawa12.AvatarOptimizer
{
    internal static class BuildLog
    {
        public static void LogInfo(string code, params object[] args) =>
            ErrorReport.ReportError(AAOL10N.Localizer, ErrorSeverity.Information, code, args);

        public static void LogWarning(string code, params object[] args) =>
            ErrorReport.ReportError(AAOL10N.Localizer, ErrorSeverity.NonFatal, code, args);

        public static void LogError(string code, params object[] args) =>
            ErrorReport.ReportError(AAOL10N.Localizer, ErrorSeverity.Error, code, args);
    }
}
