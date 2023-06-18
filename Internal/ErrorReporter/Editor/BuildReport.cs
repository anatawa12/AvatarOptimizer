using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
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
        }

        private static BuildReport LoadReport()
        {
            try
            {
                var data = File.ReadAllText(Path);
                return JsonUtility.FromJson<BuildReport>(data);
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static void SaveReport()
        {
            var report = CurrentReport;
            var json = JsonUtility.ToJson(report);

            File.WriteAllText(Path, json);

            ErrorReportUI.ReloadErrorReport();
        }

        private class AvatarReportScope : IDisposable
        {
            public void Dispose()
            {
                var avatar = CurrentReport.CurrentAvatar;
                CurrentReport.CurrentAvatar = null;
                var successful = avatar.successful;
                BuildReport.SaveReport();
                if (avatar.logs.Any())
                    ErrorReportUI.OpenErrorReportUIFor(avatar);
                else
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

        [CanBeNull]
        internal static ErrorLog Log(ReportLevel level, string code, params string[] strings)
        {
            for (var i = 0; i < strings.Length; i++)
                strings[i] = strings[i] ?? "";
            var errorLog = new ErrorLog(level, code, strings);

            var avatarReport = CurrentReport.CurrentAvatar;
            if (avatarReport == null)
            {
                Debug.LogWarning("Error logged when not processing an avatar: " + errorLog);
                return null;
            }

            avatarReport.logs.Add(errorLog);
            return errorLog;
        }

        [CanBeNull]
        public static ErrorLog LogFatal(string code, params string[] strings)
        {
            var log = Log(ReportLevel.Error, code, strings: strings);
            if (CurrentReport.CurrentAvatar != null)
            {
                CurrentReport.CurrentAvatar.successful = false;
            }
            else
            {
                throw new Exception("Fatal error without error reporting scope");
            }
            return log;
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
                ReportInternalError(e, 2);
                return default;
            }
            finally
            {
                if (obj != null) CurrentReport._references.Pop();
            }
        }

        public static void ReportInternalError(Exception exception) => ReportInternalError(exception, 2);

        private static void ReportInternalError(Exception exception, int strips)
        {
            var additionalStackTrace = string.Join("\n", 
                Environment.StackTrace.Split('\n').Skip(strips)) + "\n";
            LogException(exception, additionalStackTrace);
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

        public static void RemapPaths(string original, string cloned)
        {
            foreach (var av in CurrentReport.avatars)
            {
                av.objectRef = av.objectRef.Remap(original, cloned);

                foreach (var log in av.logs)
                {
                    log.referencedObjects = log.referencedObjects.Select(o => o.Remap(original, cloned)).ToList();
                }
            }

            ErrorReportUI.ReloadErrorReport();
        }
    }
}
