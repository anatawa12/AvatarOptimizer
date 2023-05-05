using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.ErrorReporting
{
    [Serializable]
    internal class AvatarReport
    {
        [SerializeField] internal ObjectRef objectRef;
        [SerializeField] internal bool successful;
        [SerializeField] internal List<ErrorLog> logs = new List<ErrorLog>();
    }

    [InitializeOnLoad]
    [Serializable]
    public class BuildReport
    {
        private const string Path = "Library/com.anatawa12.error-reporting.json";

        private static BuildReport _report;
        private Stack<Object> _references = new Stack<Object>();

        [SerializeField] internal List<AvatarReport> avatars = new List<AvatarReport>();

        internal ConditionalWeakTable<VRCAvatarDescriptor, AvatarReport> AvatarsByObject =
            new ConditionalWeakTable<VRCAvatarDescriptor, AvatarReport>();
        private AvatarReport CurrentAvatar { get; set; }

        internal static BuildReport CurrentReport
        {
            get
            {
                if (_report == null) _report = LoadReport() ?? new BuildReport();
                return _report;
            }
        }

        static BuildReport()
        {
            EditorApplication.playModeStateChanged += change =>
            {
                switch (change)
                {
                    case PlayModeStateChange.ExitingEditMode:
                        // TODO - skip if we're doing a VRCSDK build
                        _report = new BuildReport();
                        break;
                }
            };
            Utils.GetCurrentReportActiveReferences = () => CurrentReport.GetActiveReferences();
        }

        private static BuildReport LoadReport()
        {
            try
            {
                var data = File.ReadAllText(Path);
                return JsonUtility.FromJson<BuildReport>(data);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        internal static void SaveReport()
        {
            var report = CurrentReport;
            var json = JsonUtility.ToJson(report);

            File.WriteAllText(Path, json);

            ErrorReportUI.reloadErrorReport();
        }

        private class AvatarReportScope : IDisposable
        {
            public void Dispose()
            {
                var successful = CurrentReport.CurrentAvatar.successful;
                CurrentReport.CurrentAvatar = null;
                BuildReport.SaveReport();
                ErrorReportUI.MaybeOpenErrorReportUI();
                if (!successful) throw new Exception("Avatar processing failed");
            }
        }

        public static IDisposable ReportingOnAvatar(VRCAvatarDescriptor descriptor)
        {
            if (descriptor != null)
            {
                if (!CurrentReport.AvatarsByObject.TryGetValue(descriptor, out var report))
                {
                    Debug.LogWarning("Reporting on Avatar is called before ErrorReporting Initializer Processor");
                    report = CurrentReport.Initialize(descriptor);
                }
                CurrentReport.CurrentAvatar = report;
            }

            return new AvatarReportScope();
        }

        internal AvatarReport Initialize(VRCAvatarDescriptor descriptor)
        {
            if (descriptor == null) return null;

            AvatarReport report = new AvatarReport();
            report.objectRef = new ObjectRef(descriptor.gameObject);
            avatars.Add(report);
            report.successful = true;

            report.logs.AddRange(ComponentValidation.ValidateAll(descriptor.gameObject));

            AvatarsByObject.Add(descriptor, report);
            return report;
        }

        internal static void Log(ReportLevel level, string code, object[] strings, params Object[] objects)
        {
            ErrorLog errorLog =
                new ErrorLog(level, code, strings: strings.Select(s => s.ToString()).ToArray(), objects);

            var avatarReport = CurrentReport.CurrentAvatar;
            if (avatarReport == null)
            {
                Debug.LogWarning("Error logged when not processing an avatar: " + errorLog);
                return;
            }

            avatarReport.logs.Add(errorLog);
        }

        internal static void LogFatal(string code, object[] strings, params Object[] objects)
        {
            Log(ReportLevel.Error, code, strings: strings, objects: objects);
            if (CurrentReport.CurrentAvatar != null)
            {
                CurrentReport.CurrentAvatar.successful = false;
            }
            else
            {
                throw new Exception("Fatal error without error reporting scope");
            }
        }

        internal static void LogException(Exception e, string additionalStackTrace = "")
        {
            var avatarReport = CurrentReport.CurrentAvatar;
            if (avatarReport == null)
            {
                Debug.LogException(e);
                return;
            }
            else
            {
                avatarReport.logs.Add(new ErrorLog(e, additionalStackTrace));
            }
        }

        public static T ReportingObject<T>(UnityEngine.Object obj, Func<T> action)
        {
            if (obj != null) CurrentReport._references.Push(obj);
            try
            {
                return action();
            }
            catch (Exception e)
            {
                var additionalStackTrace = string.Join("\n", Environment.StackTrace.Split('\n').Skip(1)) + "\n";
                LogException(e, additionalStackTrace);
                return default;
            }
            finally
            {
                if (obj != null) CurrentReport._references.Pop();
            }
        }

        public static void ReportingObject(UnityEngine.Object obj, Action action)
        {
            ReportingObject(obj, () =>
            {
                action();
                return true;
            });
        }

        public static void ReportingObjects<T>(IEnumerable<T> objs, Action<T> action) where T : Object
        {
            foreach (var obj in objs)
                ReportingObject(obj, () => action(obj));
        }

        internal IEnumerable<ObjectRef> GetActiveReferences()
        {
            return _references.Select(o => new ObjectRef(o));
        }

        public static void Clear()
        {
            _report = new BuildReport();
        }
    }
}
