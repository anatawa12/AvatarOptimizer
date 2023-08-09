using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = System.Diagnostics.Debug;

namespace Anatawa12.AvatarOptimizer.ErrorReporting
{
    internal class ErrorReportUI : EditorWindow
    {
        internal static void ReloadErrorReport() => ReloadErrorReportEvent?.Invoke();
        private static event Action ReloadErrorReportEvent;

        [MenuItem("Tools/Avatar Optimizer/Show error report", false, 100)]
        public static void OpenErrorReportUI()
        {
            GetWindow<ErrorReportUI>().Show();
        }

        public static void OpenErrorReportUIFor(AvatarReport avatar)
        {
            var window = GetWindow<ErrorReportUI>();
            window.Show();
            if (window._header != null)
                window._header.Value = avatar;
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

            ReloadErrorReportEvent += RenderContent;

            Selection.selectionChanged += ScheduleRender;
            EditorApplication.hierarchyChanged += ScheduleRender;
            ErrorReporterRuntime.OnChangeAction += ScheduleRender;
            //Localization.OnLangChange += RenderContent;
        }

        private void OnDisable()
        {
            ReloadErrorReportEvent -= RenderContent;
            Selection.selectionChanged -= ScheduleRender;
            EditorApplication.hierarchyChanged -= ScheduleRender;
            ErrorReporterRuntime.OnChangeAction -= ScheduleRender;
            //Localization.OnLangChange -= RenderContent;
        }

        private const int RefreshDelayTime = 500;
        [CanBeNull] private Stopwatch _delayTimer;
        [CanBeNull] private HeaderBox _header;
        [CanBeNull] private GameObject _defaultAvatarObject;

        private void ScheduleRender()
        {
            if (_delayTimer == null)
            {
                _delayTimer = Stopwatch.StartNew();
                EditorApplication.delayCall += StartRenderTimer;
            }
            else
            {
                _delayTimer.Restart();
            }
        }

        private async void StartRenderTimer()
        {
            Debug.Assert(_delayTimer != null, nameof(_delayTimer) + " != null");
            while (_delayTimer.ElapsedMilliseconds < RefreshDelayTime)
            {
                long remaining = RefreshDelayTime - _delayTimer.ElapsedMilliseconds;
                if (remaining > 0)
                {
                    await Task.Delay((int) remaining);
                }
            }

            _delayTimer = null;
            RenderContent();
            Repaint();
        }

        private void CreateGUI()
        {
            var root = new Box();
            root.Clear();
            root.name = "Root";
            rootVisualElement.Add(root);

            var (activeAvatar, activeAvatarObject) = GetDefaultAvatar();
            _defaultAvatarObject = activeAvatarObject;

            _header = new HeaderBox(activeAvatar);
            root.Add(_header);

            var box = new ScrollView();
            root.Add(box);
            if (_header.Value != null)
                UpdateErrorList(box, _header.Value);
            _header.SelectionChanged += x => UpdateErrorList(box, x);
        }

        private (AvatarReport activeAvatar, GameObject activeAvatarObject) GetDefaultAvatar()
        {
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

                if (activeAvatarObject != null && activeAvatar == null)
                {
                    activeAvatar = new AvatarReport();
                    activeAvatar.objectRef = new ObjectRef(activeAvatarObject);
                }
            }

            return (activeAvatar, activeAvatarObject);
        }

        private void UpdateErrorList(VisualElement box, [NotNull] AvatarReport activeAvatar)
        {
            Debug.Assert(_header != null, nameof(_header) + " != null");
            var activeAvatarObject = activeAvatar == _header.DefaultValue ? _defaultAvatarObject : null;
            var lookupCache = new ObjectRefLookupCache();
            var avBox = new Box();
            avBox.AddToClassList("avatarBox");

            var errorLogs = activeAvatar.logs.ToList();

            if (activeAvatarObject != null)
            {
                errorLogs.RemoveAll(l => l.reportLevel == ReportLevel.Validation);

                activeAvatar.logs = errorLogs;

                activeAvatar.logs.AddRange(ComponentValidation.ValidateAll(activeAvatarObject));
            }

            activeAvatar.logs.Sort((a, b) => a.reportLevel.CompareTo(b.reportLevel));

            if (activeAvatar.logs.Count == 0)
            {
                var container = new Box();
                container.name = "no-errors";
                container.Add(new Label("Nothing to report!"));
                avBox.Add(container);
            }
            else
            {
                foreach (var ev in activeAvatar.logs)
                {
                    avBox.Add(new ErrorElement(ev, lookupCache));
                }
            }

            box.Clear();
            box.Add(avBox);
        }

        private void RenderContent()
        {
            var (activeAvatar, activeAvatarObject) = GetDefaultAvatar();
            _defaultAvatarObject = activeAvatarObject;
            if (_header != null)
                _header.DefaultValue = activeAvatar;
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
                var oldDefaultValue = _defaultValue;
                _defaultValue = value;
                UpdateList(oldDefaultValue: oldDefaultValue);
            }
        }

        [CanBeNull] public AvatarReport Value
        {
            get => _field?.value;
            set => UpdateList(value);
        }

        public event Action<AvatarReport> SelectionChanged;

        public HeaderBox(AvatarReport defaultValue)
        {
            AddToClassList("avatarHeader");
            _defaultValue = defaultValue;
            UpdateList();
        }

        public void UpdateList(
            AvatarReport setValue = null,
            AvatarReport oldDefaultValue = null
        )
        {
            var list = BuildReport.CurrentReport.avatars.ToList();
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
                AvatarReport oldValue = _field?.value;
                AvatarReport value;
                if (setValue != null)
                    value = setValue;
                else if (DefaultValue != null && oldDefaultValue == _field?.value)
                    value = DefaultValue;
                else if (_field != null && list.Contains(_field.value))
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

                if (oldValue != value || setValue != null) SelectionChanged?.Invoke(value);
            }
            MarkDirtyRepaint();
        }
    }
}
