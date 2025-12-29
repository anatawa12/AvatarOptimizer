using System;
using System.Collections.Generic;
using System.Collections;
using nadena.dev.ndmf;
using nadena.dev.ndmf.localization;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer
{
    public class ErrorWithAutoFix : SimpleError
    {
        internal readonly Action AutoFix;
        public override Localizer Localizer => AAOL10N.Localizer;
        public override string TitleKey { get; }
        public override ErrorSeverity Severity { get; }
        private string[] _subst;
        public string AutoFixKey = "ErrorReporter:autoFix";
        
        public ErrorWithAutoFix(ErrorSeverity errorSeverity, string key, Action autoFix, params object[] args)
        {
            AutoFix = autoFix;
            Severity = errorSeverity;
            TitleKey = key;

            List<string> substitutions = new List<string>();
            AddContext(args, substitutions);
            _subst = substitutions.ToArray();
        }

        private void AddContext(IEnumerable args, List<string> substitutions)
        {
            foreach (var arg in args)
            {
                if (arg == null)
                {
                    substitutions.Add("<missing>");
                }
                else if (arg is string s)
                {
                    // string is IEnumerable, so we have to special case this
                    substitutions.Add(s);
                }
                else if (arg is ObjectReference or)
                {
                    AddReference(or);
                    substitutions.Add(or.ToString());
                }
                else if (arg is Object uo)
                {
                    var objectReference = ObjectRegistry.GetReference(uo);
                    AddReference(objectReference);
                    substitutions.Add(objectReference.ToString());
                }
                else if (arg is IEnumerable e)
                {
                    AddContext(e, substitutions);
                }
                else if (arg is IErrorContext ec)
                {
                    AddContext(ec.ContextReferences, substitutions);
                }
                else
                {
                    substitutions.Add(arg.ToString());
                }
            }
        }

        public override VisualElement CreateVisualElement(ErrorReport report)
        {
            var element = base.CreateVisualElement(report);
            element[0].Add(new Button(() => { AutoFix(); }) {text = AAOL10N.Tr(AutoFixKey)});
            return element;
        }

        public override string[] TitleSubst => _subst;
        public override string[] DetailsSubst => _subst;
        public override string[] HintSubst => _subst;
    }
}
