// Execute this script on the time changed this script.

console.log(`// this file is generated with .generate.ts`)
console.log(``)
console.log(`using System;`)
console.log(`using System.Reflection;`)
console.log(``)
console.log(`namespace Anatawa12.AvatarOptimizer.ErrorReporting`)
console.log(`{`)
console.log(`    public partial class ErrorLog`)
console.log(`    {`)
for (const level of ["Validation", "Info", "Warning", "Error"]) {
    console.log(`        public static ErrorLog ${level}(string code, string[] strings, params object[] args)`)
    console.log(`            => new ErrorLog(ReportLevel.${level}, code, strings, args, Assembly.GetCallingAssembly());`)
    console.log(``)
    console.log(`        public static ErrorLog ${level}(string code, params object[] args)`)
    console.log(`            => new ErrorLog(ReportLevel.${level}, code, Array.Empty<string>(), args, Assembly.GetCallingAssembly());`)
    console.log(``)
}
console.log(`    }`)
console.log(`}`)
