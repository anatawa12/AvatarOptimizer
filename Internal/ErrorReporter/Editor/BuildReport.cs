using System;
using System.Collections;
using System.Collections.Generic;
using nadena.dev.ndmf;
using nadena.dev.ndmf.localization;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.ErrorReporting
{
    public static class BuildReport
    {
        public static void LogInfo(string code, params object[] args) =>
            ErrorReport.ReportError(new InlineError(ErrorSeverity.Information, code, args));

        public static void LogWarning(string code, params object[] args) =>
            ErrorReport.ReportError(new InlineError(ErrorSeverity.NonFatal, code, args));

        public static void LogError(string code, params object[] args) =>
            ErrorReport.ReportError(new InlineError(ErrorSeverity.Error, code, args));
    }

    internal class InlineError : SimpleError, IError
    {
        private static Localizer _localizer = new Localizer("en-US", () => new List<LocalizationAsset>()
        {
            // en.po
            AssetDatabase.LoadAssetAtPath<LocalizationAsset>(
                AssetDatabase.GUIDToAssetPath("f9d382355a0a485980e8e7271bca53d7")),
            // ja.po
            AssetDatabase.LoadAssetAtPath<LocalizationAsset>(
                AssetDatabase.GUIDToAssetPath("feed5ac7cc024b9e92e46f7e2dbdbe82")),
        });

        private readonly string _key;

        public InlineError(ErrorSeverity errorSeverity, string key, params object[] args)
        {
            Localizer = _localizer;
            Severity = errorSeverity;
            // https://github.com/bdunderscore/ndmf/issues/99
            // https://github.com/bdunderscore/ndmf/issues/98
            _key = key;

            DetailsSubst = Array.ConvertAll(args, o => o?.ToString());
            Flatten(args, _references);
        }

        protected override string DetailsKey => _key;

        private static void Flatten(object arg, List<ObjectReference> list)
        {
            // https://github.com/bdunderscore/ndmf/issues/95
            // https://github.com/bdunderscore/ndmf/issues/96
            if (arg is ObjectReference or)
                list.Add(or);
            else if (arg is Object uo)
                list.Add(ObjectRegistry.GetReference(uo));
            else if (arg is IContextProvider provider)
                Flatten(provider.ProvideContext(), list);
            else if (arg is IEnumerable enumerable)
                foreach (var value in enumerable)
                    Flatten(value, list);
        }

        protected override Localizer Localizer { get; }
        public override ErrorSeverity Severity { get; }
        protected override string TitleKey { get; }

        protected override string[] DetailsSubst { get; }

        protected override string[] HintSubst => DetailsSubst;
    }

    public interface IContextProvider
    {
        object ProvideContext();
    }
}
