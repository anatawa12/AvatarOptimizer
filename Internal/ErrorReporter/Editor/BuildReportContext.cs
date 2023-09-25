using System;
using nadena.dev.ndmf;
using UnityEditor;

namespace Anatawa12.AvatarOptimizer.ErrorReporting
{
    internal class BuildReportSharedState
    {
        public bool Initialized;
    }

    public class BuildReportContext : IExtensionContext
    {
        private IDisposable _scope;

        public void OnActivate(BuildContext context)
        {
            var state = context.GetState<BuildReportSharedState>();
            if (!state.Initialized)
            {
                state.Initialized = true;
                // If it's in unity editor, I assume building avatar.
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    BuildReport.Clear();

                BuildReport.CurrentReport.Initialize(context.AvatarDescriptor);
            }
            _scope = BuildReport.ReportingOnAvatar(context.AvatarDescriptor);
        }

        public void OnDeactivate(BuildContext context)
        {
            _scope.Dispose();
            _scope = null;
        }
    }
}
