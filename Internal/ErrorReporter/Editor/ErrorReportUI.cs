using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
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

        private const int RefreshDelayTime = 500;
        private Stopwatch DelayTimer = new Stopwatch();

        private void ScheduleRender()
        {
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

            var header = new HeaderBox(activeAvatar);
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

    class HeaderBox : Box
    {
        [CanBeNull] private PopupField<AvatarReport> _field;
        [CanBeNull] private AvatarReport _defaultValue;

        [CanBeNull]
        public AvatarReport DefaultValue
        {
            get => _defaultValue;
            set
            {
                _defaultValue = value;
                UpdateList();
            }
        }

        public event Action<AvatarReport> SelectionChanged;

        public HeaderBox(AvatarReport defaultValue)
        {
            AddToClassList("avatarHeader");
            _defaultValue = defaultValue;
            UpdateList();
        }

        public void UpdateList()
        {
            var list = BuildReport.CurrentReport.avatars;
            if (DefaultValue != null && !list.Contains(DefaultValue))
                list.Add(DefaultValue);
            list.Reverse();

            if (list.Count == 0)
            {
                Clear();
                Add(new Label("No avatars for Error Report"));
                _field = null;
            }
            else
            {
                AvatarReport value;
                if (_field != null && list.Contains(_field.value))
                    value = _field.value;
                else if (DefaultValue != null)
                    value = DefaultValue;
                else
                    value = list.First();

                _field = new PopupField<AvatarReport>(list, value,
                    x => x.objectRef.name,
                    x => x.objectRef.name);

                _field.RegisterValueChangedCallback(e => { SelectionChanged?.Invoke(e.newValue); });

                Clear();
                Add(new Label("Error report for "));
                Add(_field);
            }
        }
    }
}
