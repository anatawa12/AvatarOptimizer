using System;

namespace Anatawa12.AvatarOptimizer.ErrorReporting
{
    public static class ErrorReporterRuntime
    {
        public static event Action OnChangeAction;

        public static void TriggerChange()
        {
            OnChangeAction?.Invoke();
        }
    }
}
