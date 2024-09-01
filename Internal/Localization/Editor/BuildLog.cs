#define ENABLE_TEST

using System;
using nadena.dev.ndmf;

namespace Anatawa12.AvatarOptimizer
{
    public static class BuildLog
    {
        public static void LogInfo(string code, params object[] args) =>
            ReportError(ErrorSeverity.Information, code, args);

        public static void LogWarning(string code, params object[] args) =>
            ReportError(ErrorSeverity.NonFatal, code, args);

        public static void LogError(string code, params object[] args) =>
            ReportError(ErrorSeverity.Error, code, args);

        public static void LogErrorWithAutoFix(string code, Action autoFix, params object[] args)
        {
#if ENABLE_TEST
            BuildLogTestSupport.ReportError(ErrorSeverity.Error, code, args);
#endif
            ErrorReport.ReportError(new ErrorWithAutoFix(ErrorSeverity.Error, code, autoFix, args));
        }

        private static void ReportError(ErrorSeverity severity, string code, params object[] args)
        {
#if ENABLE_TEST
            BuildLogTestSupport.ReportError(severity, code, args);
#endif
            ErrorReport.ReportError(AAOL10N.Localizer, severity, code, args);
        }
    }
}

#if ENABLE_TEST
namespace Anatawa12.AvatarOptimizer
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    public static class BuildLogTestSupport
    {
        private static BuildLogTestScope? _scope;

        public static void ReportError(ErrorSeverity severity, string code, object[] args)
        {
            _scope?.ReportError(severity, code, args);
        }

        public static BuildLogTestScope CaptureLog()
        {
            if (_scope != null)
                throw new InvalidOperationException("already in capture log scope");

            return _scope = new BuildLogTestScope();
        }

        public class BuildLogTestScope : IDisposable
        {
            private ErrorSeverity allowedSeverity = ErrorSeverity.Information;

            private HashSet<(ErrorSeverity, string)> expectingErrors = new();

            private List<(ErrorSeverity, string, object[])> unexpectedErrors = new();

            public void ExpectError(ErrorSeverity severity, string code)
            {
                expectingErrors.Add((severity, code));
            }

            internal void ReportError(ErrorSeverity severity, string code, object[] args)
            {
                if (severity > allowedSeverity)
                    if (!expectingErrors.Remove((severity, code)))
                        unexpectedErrors.Add((severity, code, args));

                Debug.Log($"Error: {severity} {code} {string.Join(", ", args)}");
            }

            void IDisposable.Dispose()
            {
                if (expectingErrors.Count != 0)
                {
                    throw new InvalidOperationException(
                        "there are unreported errors: " +
                        string.Join(", ",
                            expectingErrors.Select(x => $"{x.Item1} {x.Item2}")));
                }

                if (unexpectedErrors.Count != 0)
                {
                    throw new InvalidOperationException("there are unexpected errors!");
                }

                _scope = null;
            }
        }
    }
}
#endif
