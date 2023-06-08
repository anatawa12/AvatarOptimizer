using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Anatawa12.AvatarOptimizer.ErrorReporting
{
    internal class ErrorReportUI : EditorWindow
    {
        internal static Action reloadErrorReport = () => { };

        [MenuItem("Tools/Avatar Optimizer/Show error report", false, 100)]
        public static void OpenErrorReportUI()
        {
            GetWindow<ErrorReportUI>().Show();
        }

        public static void MaybeOpenErrorReportUI()
        {
            if (BuildReport.CurrentReport.avatars.Any(av => av.logs.Count > 0))
            {
                OpenErrorReportUI();
            }
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Error Report");

            rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("com.anatawa12.avatar-optimizer.error-report"));
            RenderContent();

            reloadErrorReport = RenderContent;

            Selection.selectionChanged += ScheduleRender;
            EditorApplication.hierarchyChanged += ScheduleRender;
            ErrorReporterRuntime.OnChangeAction += ScheduleRender;
            //Localization.OnLangChange += RenderContent;
        }

        private void OnDisable()
        {
            reloadErrorReport = () => { };
            Selection.selectionChanged -= ScheduleRender;
            EditorApplication.hierarchyChanged -= ScheduleRender;
            ErrorReporterRuntime.OnChangeAction -= ScheduleRender;
            //Localization.OnLangChange -= RenderContent;
        }

        private readonly int RefreshDelayTime = 500;
        private Stopwatch DelayTimer = new Stopwatch();
        private bool RenderPending = false;

        private void ScheduleRender()
        {
            if (RenderPending) return;
            RenderPending = true;
            DelayTimer.Restart();
            EditorApplication.delayCall += StartRenderTimer;
        }

        private async void StartRenderTimer()
        {
            while (DelayTimer.ElapsedMilliseconds < RefreshDelayTime)
            {
                long remaining = RefreshDelayTime - DelayTimer.ElapsedMilliseconds;
                if (remaining > 0)
                {
                    await Task.Delay((int) remaining);
                }
            }

            RenderPending = false;
            RenderContent();
            Repaint();
        }

        private void RenderContent()
        {
            rootVisualElement.Clear();

            var root = new Box();
            root.Clear();
            root.name = "Root";
            rootVisualElement.Add(root);

            int reported = 0;

            AvatarReport activeAvatar = null;

            GameObject activeAvatarObject = null;
            if (Selection.gameObjects.Length == 1)
            {
                activeAvatarObject = Utils.FindAvatarInParents(Selection.activeGameObject.transform)?.gameObject;
                if (activeAvatarObject != null)
                {
                    var foundAvatarPath = Utils.RelativePath(null, activeAvatarObject);
                    activeAvatar = BuildReport.CurrentReport.avatars
                            .FirstOrDefault(av => av.objectRef.path == foundAvatarPath);
                }

                if (activeAvatar == null)
                {
                    activeAvatar = new AvatarReport();
                    activeAvatar.objectRef = new ObjectRef(activeAvatarObject);
                }
            }

            if (activeAvatar == null)
            {
                activeAvatar = BuildReport.CurrentReport.avatars.LastOrDefault();
            }

            var header = new Box();
            header.Add(new Label("Error report for "));
            var list = BuildReport.CurrentReport.avatars.ToList();
            if (!list.Contains(activeAvatar)) list.Add(activeAvatar);
            list.Reverse();
            var field = new PopupField<AvatarReport>(list, activeAvatar, 
                x => x.objectRef.name, 
                x => x.objectRef.name);
            header.Add(field);
            header.AddToClassList("avatarHeader");
            root.Add(header);


            if (activeAvatar != null)
            {
                var box = new ScrollView();
                var lookupCache = new ObjectRefLookupCache();
                reported++;
                var avBox = new Box();
                avBox.AddToClassList("avatarBox");

                List<ErrorLog> errorLogs = activeAvatar.logs
                    .Where(l => activeAvatarObject == null || l.reportLevel != ReportLevel.Validation).ToList();

                if (activeAvatarObject != null)
                {
                    activeAvatar.logs = errorLogs;

                    activeAvatar.logs.AddRange(ComponentValidation.ValidateAll(activeAvatarObject));
                }

                foreach (var ev in activeAvatar.logs)
                {
                    avBox.Add(new ErrorElement(ev, lookupCache));
                    avBox.Add(new ErrorElement(ev, lookupCache));
                    avBox.Add(new ErrorElement(ev, lookupCache));
                    avBox.Add(new ErrorElement(ev, lookupCache));
                    avBox.Add(new ErrorElement(ev, lookupCache));
                    avBox.Add(new ErrorElement(ev, lookupCache));
                    avBox.Add(new ErrorElement(ev, lookupCache));
                    avBox.Add(new ErrorElement(ev, lookupCache));
                    avBox.Add(new ErrorElement(ev, lookupCache));
                    avBox.Add(new ErrorElement(ev, lookupCache));
                    avBox.Add(new ErrorElement(ev, lookupCache));
                }

                activeAvatar.logs.Sort((a, b) => a.reportLevel.CompareTo(b.reportLevel));

                box.Add(avBox);
                root.Add(box);
            }

            /*
            if (reported == 0)
            {
                var container = new Box();
                container.name = "no-errors";
                container.Add(new Label("Nothing to report!"));
                root.Add(container);
            }
            */
        }
    }
}
