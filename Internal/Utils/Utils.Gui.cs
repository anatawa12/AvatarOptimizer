using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer;

public partial class Utils
{
    private static int PopupSuggestionHash = nameof(PopupSuggestionHash).GetHashCode();

    public static T? PopupSuggestion<T>(
        T[] suggestions,
        GUIContent[] suggestionLabels,
        params GUILayoutOption[] options) =>
        PopupSuggestion(suggestions, suggestionLabels, EditorStyles.popup, options);

    public static T? PopupSuggestion<T>(
        T[] suggestions,
        GUIContent[] suggestionLabels,
        GUIStyle style,
        params GUILayoutOption[] options) =>
        PopupSuggestionInternal(() => (suggestionLabels, suggestions), style, options);

    public static T? PopupSuggestion<T>(
        T[] suggestions,
        Converter<T, GUIContent> label,
        params GUILayoutOption[] options) =>
        PopupSuggestionInternal(() => (Array.ConvertAll(suggestions, label.Invoke), suggestions), EditorStyles.popup, options);

    public static T? PopupSuggestion<T>(
        T[] suggestions,
        Converter<T, GUIContent> label,
        GUIStyle style,
        params GUILayoutOption[] options) =>
        PopupSuggestionInternal(() => (Array.ConvertAll(suggestions, label.Invoke), suggestions), style, options);

    public static T? PopupSuggestion<T>(
        T[] suggestions,
        Func<T, string> label,
        params GUILayoutOption[] options) =>
        PopupSuggestionInternal(() => (Array.ConvertAll(suggestions, l => new GUIContent(label(l))), suggestions), EditorStyles.popup, options);

    public static T? PopupSuggestion<T>(
        T[] suggestions,
        Func<T, string> label,
        GUIStyle style,
        params GUILayoutOption[] options) =>
        PopupSuggestionInternal(() => (Array.ConvertAll(suggestions, l => new GUIContent(label(l))), suggestions), style, options);

    public static T? PopupSuggestion<T>(
        Func<T[]> suggestions,
        Converter<T, GUIContent> label,
        params GUILayoutOption[] options) =>
        PopupSuggestion(suggestions, label, EditorStyles.popup, options);
    
    public static T? PopupSuggestion<T>(
        Func<T[]> suggestions,
        Converter<T, GUIContent> label,
        GUIStyle style,
        params GUILayoutOption[] options) =>
        PopupSuggestionInternal(() =>
        {
            var suggestionValues = suggestions();
            return (Array.ConvertAll(suggestionValues, label.Invoke), suggestionValues);
        }, style, options);

    public static T? PopupSuggestion<T>(
        Func<T[]> suggestions,
        Func<T, string> label,
        params GUILayoutOption[] options) =>
        PopupSuggestion(suggestions, label, EditorStyles.popup, options);

    public static T? PopupSuggestion<T>(
        Func<T[]> suggestions,
        Func<T, string> label,
        GUIStyle style,
        params GUILayoutOption[] options) =>
        PopupSuggestionInternal(() =>
        {
            var suggestionValues = suggestions();
            return (Array.ConvertAll(suggestionValues, l => new GUIContent(label(l))), suggestionValues);
        }, style, options);

    internal static T? PopupSuggestionInternal<T>(
        Func<(GUIContent[], T[])> popupValues, 
        GUIStyle style,
        params GUILayoutOption[] options
    ) {
        return PopupSuggestionInternal(EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, style, options), popupValues, style);
    }

    private static T? PopupSuggestionInternal<T>(
        Rect position,
        Func<(GUIContent[], T[])> popupValues,
        GUIStyle style
    )
    {
        int controlId = GUIUtility.GetControlID(PopupSuggestionHash, FocusType.Keyboard, position);
        return PopupSuggestionImpl(position, controlId, popupValues, style);
    }

    internal static T? PopupSuggestionImpl<T>(
        Rect position,
        int controlID,
        Func<(GUIContent[], T[])> popupValues,
        GUIStyle style)
    {
        var result = PopupCallbackInfo.GetSelectedValueForControl<T>(controlID);
        GUIContent content;
        content = GUIContent.none;

        Event current = Event.current;
        switch (current.type)
        {
            case EventType.MouseDown:
                if (current.button == 0 && position.Contains(current.mousePosition))
                {
                    if (Application.platform == RuntimePlatform.OSXEditor)
                        position.y -= 19;
                    var (labels, values) = popupValues();
                    PopupCallbackInfo.instance = new PopupCallbackInfo(controlID, values);
                    EditorUtility.DisplayCustomMenu(position, labels, -1,
                        PopupCallbackInfo.instance.SetEnumValueDelegate, null, true);
                    GUIUtility.keyboardControl = controlID;
                    current.Use();
                    break;
                }

                break;
            case EventType.KeyDown:
                if (current.MainActionKeyForControl(controlID))
                {
                    if (Application.platform == RuntimePlatform.OSXEditor)
                        position.y -= 19;
                    var (labels, values) = popupValues();
                    PopupCallbackInfo.instance = new PopupCallbackInfo(controlID, values);
                    EditorUtility.DisplayCustomMenu(position, labels, -1,
                        PopupCallbackInfo.instance.SetEnumValueDelegate, null);
                    current.Use();
                    break;
                }

                break;
            case EventType.Repaint:
                Font font = style.font;
                style.Draw(position, content, controlID, false,
                    position.Contains(Event.current.mousePosition));
                style.font = font;
                break;
        }

        return result;
    }

    internal sealed class PopupCallbackInfo
    {
        public static PopupCallbackInfo? instance;
        internal const string PopupSuggestionChangedMessage = "PopupSuggestionChanged";
        private readonly int _controlID = 0;
        private int selectedIndex = 0;
        private readonly Object sourceView;
        private readonly Array _values;

        public PopupCallbackInfo(int controlID, Array values)
        {
            _controlID = controlID;
            _values = values;
            sourceView = GUIViewReflect.Current;
        }

        public static T? GetSelectedValueForControl<T>(int controlID)
        {
            Event current = Event.current;
            if (current.type == EventType.ExecuteCommand && current.commandName == PopupSuggestionChangedMessage)
            {
                if (instance == null)
                {
                    Debug.LogError("PopupSuggestion menu has no instance");
                    return default;
                }

                if (instance._controlID == controlID)
                {
                    GUI.changed = true;
                    var result = ((T[])instance._values)[instance.selectedIndex];
                    instance = null;
                    current.Use();
                    return result;
                }
            }

            return default;
        }

        internal void SetEnumValueDelegate(object userData, string[] options, int selected)
        {
            selectedIndex = selected;
            if (!sourceView)
                return;
            GUIViewReflect.SendEvent(sourceView, EditorGUIUtility.CommandEvent(PopupSuggestionChangedMessage));
        }
    }

    internal static bool MainActionKeyForControl(this Event evt, int controlId)
    {
        if (GUIUtility.keyboardControl != controlId)
            return false;
        bool flag = evt.alt || evt.shift || evt.command || evt.control;
        return evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.Space ||
                                                 evt.keyCode == KeyCode.Return ||
                                                 evt.keyCode == KeyCode.KeypadEnter) && !flag;
    }

    private static class GUIViewReflect
    {
        private static Func<Object>? _getCurrent;

        public static Object Current
        {
            get
            {
                if (_getCurrent == null)
                {
                    var guiView = typeof(EditorGUI).Assembly.GetType("UnityEditor.GUIView");
                    var getCurrentMethod = guiView.GetProperty("current").GetGetMethod();
                    _getCurrent = (Func<Object>)Delegate.CreateDelegate(typeof(Func<Object>), getCurrentMethod);
                }

                return _getCurrent();
            }
        }

        private static MethodInfo? _sendEventMethod;

        public static void SendEvent(Object view, Event @event)
        {
            if (_sendEventMethod == null)
            {
                var guiView = typeof(EditorGUI).Assembly.GetType("UnityEditor.GUIView");
                _sendEventMethod = guiView.GetMethod("SendEvent", 
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(Event) },
                    null);
            }

            _sendEventMethod.Invoke(view, new object[] { @event });
        }
    }
}
