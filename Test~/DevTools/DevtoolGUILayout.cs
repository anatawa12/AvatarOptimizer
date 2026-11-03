using UnityEditor;
using UnityEngine;

namespace AvatarOptimizer.DevTools
{
    public static partial class DevtoolGUILayout
    {
        public static string FilePath(string filePath, params GUILayoutOption[] options) => DevtoolGUI.FilePath(EditorGUILayout.GetControlRect(false, 18f, options), filePath, options);
        public static string FilePath(string field, string filePath, params GUILayoutOption[] options) => DevtoolGUI.FilePath(EditorGUILayout.GetControlRect(true, 18f, options), field, filePath, options);
    }

    public static partial class DevtoolGUI
    {
        public static string FilePath(Rect position, string filePath, GUILayoutOption[] options)
            => FilePath(position, label: "", filePath, options);

        public static string FilePath(Rect position, string label, string filePath, GUILayoutOption[] options)
        {
            position = EditorGUI.PrefixLabel(position, new GUIContent(label));

            const int buttonWidth = 50;
            var pathPosition = position;
            pathPosition.width -= buttonWidth + 1;
            var buttonPosition = position;
            buttonPosition.x = pathPosition.xMax + 1;
            buttonPosition.width = buttonWidth;

            GUI.Label(pathPosition, filePath);
            if (GUI.Button(buttonPosition, "Select"))
            {
                filePath = EditorUtility.OpenFilePanel("Select", filePath, "");
            }

            return filePath;
        }
    }
}