using System;
using nadena.dev.ndmf;

namespace Anatawa12.AvatarOptimizer
{
    public static class BuildLog
    {
        public static void LogInfo(string code, params object[] args) =>
            ErrorReport.ReportError(AAOL10N.Localizer, ErrorSeverity.Information, code, args);

        public static void LogWarning(string code, params object[] args) =>
            ErrorReport.ReportError(AAOL10N.Localizer, ErrorSeverity.NonFatal, code, args);

        public static void LogError(string code, params object[] args) =>
            ErrorReport.ReportError(AAOL10N.Localizer, ErrorSeverity.Error, code, args);

        public static void LogErrorWithAutoFix(string code, Action autoFix, params object[] args) =>
            ErrorReport.ReportError(new ErrorWithAutoFix(ErrorSeverity.Error, code, autoFix, args));

        public static ErrorWithAutoFix LogWarningWithAutoFix(string code, Action autoFix, params object[] args)
        {
            var error = new ErrorWithAutoFix(ErrorSeverity.NonFatal, code, autoFix, args);
            ErrorReport.ReportError(error);
            return error;
        }
    }
}
