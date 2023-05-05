// this file is generated with .generate.ts

using System;
using System.Reflection;

namespace Anatawa12.AvatarOptimizer.ErrorReporting
{
    public partial class ErrorLog
    {
        public static ErrorLog Validation(string code, string[] strings, params object[] args)
            => new ErrorLog(ReportLevel.Validation, code, strings, args, Assembly.GetCallingAssembly());

        public static ErrorLog Validation(string code, params object[] args)
            => new ErrorLog(ReportLevel.Validation, code, Array.Empty<string>(), args, Assembly.GetCallingAssembly());

        public static ErrorLog Info(string code, string[] strings, params object[] args)
            => new ErrorLog(ReportLevel.Info, code, strings, args, Assembly.GetCallingAssembly());

        public static ErrorLog Info(string code, params object[] args)
            => new ErrorLog(ReportLevel.Info, code, Array.Empty<string>(), args, Assembly.GetCallingAssembly());

        public static ErrorLog Warning(string code, string[] strings, params object[] args)
            => new ErrorLog(ReportLevel.Warning, code, strings, args, Assembly.GetCallingAssembly());

        public static ErrorLog Warning(string code, params object[] args)
            => new ErrorLog(ReportLevel.Warning, code, Array.Empty<string>(), args, Assembly.GetCallingAssembly());

        public static ErrorLog Error(string code, string[] strings, params object[] args)
            => new ErrorLog(ReportLevel.Error, code, strings, args, Assembly.GetCallingAssembly());

        public static ErrorLog Error(string code, params object[] args)
            => new ErrorLog(ReportLevel.Error, code, Array.Empty<string>(), args, Assembly.GetCallingAssembly());

    }
}
