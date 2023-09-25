using System;
using System.Linq;
using JetBrains.Annotations;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.ErrorReporting
{
    internal class BuildReportSharedState
    {
        [CanBeNull] public AvatarReport Report;
    }

    public class BuildReportContext : IExtensionContext
    {
        public void OnActivate(BuildContext context)
        {
            var state = context.GetState<BuildReportSharedState>();
            var descriptor = context.AvatarDescriptor;
            if (descriptor == null) throw new Exception();
            var report = state.Report;
            if (state.Report == null)
            {
                // If it's in unity editor, I assume building avatar.
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    BuildReport.Clear();
                state.Report = report = BuildReport.CurrentReport.Initialize(descriptor);
            }

            BuildReport.CurrentReport.CurrentAvatar = report;
        }

        public void OnDeactivate(BuildContext context)
        {
            var avatar = BuildReport.CurrentReport.CurrentAvatar;
            BuildReport.CurrentReport.CurrentAvatar = null;
            var successful = avatar.successful;
            BuildReport.SaveReport();
            if (avatar.logs.Any())
                ErrorReportUI.OpenErrorReportUIFor(avatar);
            else
                ErrorReportUI.MaybeOpenErrorReportUI();
            if (!successful) throw new Exception("Avatar processing failed");
        }
    }
}
