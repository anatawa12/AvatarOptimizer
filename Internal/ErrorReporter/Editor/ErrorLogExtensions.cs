using System;
using VRC.SDK3.Avatars.Components;

namespace Anatawa12.AvatarOptimizer.ErrorReporting
{
    public static class ErrorLogExtensions
    {
        internal static void SaveReport()
        {
            var report = BuildReport.CurrentReport;
            //var json = JsonConvert.SerializeObject(report);

            //File.WriteAllText(Path, json);

            ErrorReportUI.reloadErrorReport();
        }

        private class AvatarReportScope : IDisposable
        {
            public void Dispose()
            {
                var successful = BuildReport.CurrentReport.CurrentAvatar.successful;
                BuildReport.CurrentReport.CurrentAvatar = null;
                SaveReport();
                if (!successful) throw new Exception("Avatar processing failed");
            }
        }

        static IDisposable ReportingOnAvatar(this BuildReport buildReport, VRCAvatarDescriptor descriptor)
        {
            if (descriptor != null)
            {
                AvatarReport report = new AvatarReport();
                report.objectRef = new ObjectRef(descriptor.gameObject);
                buildReport.Avatars.Add(report);
                buildReport.CurrentAvatar = report;
                buildReport.CurrentAvatar.successful = true;

                buildReport.CurrentAvatar.logs.AddRange(ComponentValidation.ValidateAll(descriptor.gameObject));
            }

            return new AvatarReportScope();
        }

    }
}
