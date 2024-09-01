using System;
using System.Collections.Generic;
using nadena.dev.ndmf;

namespace Anatawa12.AvatarOptimizer.Test
{
    public static class LogTestUtility
    {
        public static void Test(Action<LogTestScope> test)
        {
            var scope = new LogTestScope();

            var errors = ErrorReport.CaptureErrors(() => test(scope));

            scope.CheckErrors(errors);
        }
    }

    public class LogTestScope
    {
        private HashSet<(ErrorSeverity, string)> expectingErrors = new();

        public void ExpectError(ErrorSeverity severity, string code)
        {
            expectingErrors.Add((severity, code));
        }

        public void CheckErrors(List<ErrorContext> errors)
        {
            foreach (var errorContext in errors)
            {
                var simpleError = (SimpleError)errorContext.TheError;

                if (!expectingErrors.Contains((simpleError.Severity, simpleError.TitleKey)))
                {
                    throw new Exception($"unexpected error: {simpleError.TitleKey}");
                }
            }
        }
    }
}