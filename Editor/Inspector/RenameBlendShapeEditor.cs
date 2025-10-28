using System;
using System.Linq;
using Anatawa12.AvatarOptimizer.PrefabSafeMap;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer;

[CustomEditor(typeof(RenameBlendShape))]
internal class RenameBlendShapeEditor : AvatarTagComponentEditorBase
{
    private PSMEditorUtil<string, string> _nameMap = default!; // initialized in OnEnable

    private void OnEnable()
    {
        _nameMap = PSMEditorUtil<string, string>.Create(
            serializedObject.FindProperty(nameof(RenameBlendShape.nameMap)),
            x => x.stringValue, (x, y) => x.stringValue = y,
            x => x.stringValue, (x, y) => x.stringValue = y);
    }

    protected override void OnInspectorGUIInner()
    {
        var component = (RenameBlendShape)target;
        var shapes = EditSkinnedMeshComponentUtil.GetBlendShapes(component.GetComponent<SkinnedMeshRenderer>(), component);
        // header
        {
            var (original, changed, button) = DivideToTwo(EditorGUI.IndentedRect(EditorGUILayout.GetControlRect()));

            GUI.Label(original, AAOL10N.Tr("RenameBlendShape:original"), EditorStyles.boldLabel);
            GUI.Label(changed, AAOL10N.Tr("RenameBlendShape:changed"), EditorStyles.boldLabel);

            var content = new GUIContent("+");
            if (EditorGUI.DropdownButton(button, content, FocusType.Passive, EditorStyles.miniButton))
            {
                var shapeNames = shapes
                    .Select(x => x.name)
                    .Where(name => _nameMap.GetElementOf(name)?.Contains != true)
                    .ToArray();
                BlendShapePicker.Show(button, shapeNames, _nameMap, serializedObject);
            }
        }


        var duplicatedNames = shapes
            .Select(x => x.name)
            .Where(name => _nameMap.GetElementOf(name)?.Contains != true)
            .Concat(_nameMap.Entries.Select(x => x.Value).Where(x => !string.IsNullOrEmpty(x)))
            .GroupBy(x => x)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToHashSet();

        Action? deferredAction = null;
        var hasEmptyError = false;
        var hasDuplicatedWarning = duplicatedNames.Any();

        foreach (var element in _nameMap.Elements)
        {
            var rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
            using var propertyScope = new PropertyScope<string, string>(element, rect, GUIContent.none);
            using var disabledScope = new EditorGUI.DisabledScope(!element.Contains);

            var (original, changed, button) = DivideToTwo(rect);
            GUI.Label(original, element.Key);
            if (element.Contains)
            {
                var prevColor = GUI.color;
                var tooltip = "";
                if (string.IsNullOrEmpty(element.Value))
                {
                    hasEmptyError = true;
                    GUI.color = new Color(1, 0.5f, 0.5f);
                    tooltip = AAOL10N.Tr("RenameBlendShape:error:empty-name-this");
                }
                else if (element.Value != null && duplicatedNames.Contains(element.Value))
                {
                    hasEmptyError = true;
                    GUI.color = new Color(1, 0.5f, 0.5f);
                    tooltip = AAOL10N.Tr("RenameBlendShape:warning:name-conflict-this");
                }

                GUI.Label(changed, new GUIContent("", tooltip));
                element.Set(GUI.TextField(changed, element.Value));
                GUI.color = prevColor;

                if (GUI.Button(button, "-"))
                    deferredAction += () => element.EnsureRemoved();
            }
            else
            {
                GUI.Label(changed, AAOL10N.Tr("RenameBlendShape:removed"));
            }
        }

        if (hasEmptyError)
        {
            EditorGUILayout.HelpBox(AAOL10N.Tr("RenameBlendShape:error:empty-name-some"), MessageType.Error);
        }

        if (hasDuplicatedWarning)
        {
            EditorGUILayout.HelpBox(AAOL10N.Tr("RenameBlendShape:warning:name-conflict-some"), MessageType.Warning);
        }

        deferredAction?.Invoke();

        serializedObject.ApplyModifiedProperties();

        return;

        (Rect, Rect, Rect) DivideToTwo(Rect rect)
        {
            const float space = 1;

            var width = rect.width;

            var buttonWidth = rect.height; // square button
            width -= buttonWidth + space;

            var fieldWidth = (width - space) / 2;

            var original = rect with { width = fieldWidth };
            var changed = rect with { x = original.xMax + space, width = fieldWidth };
            var button = rect with { x = changed.xMax + space, width = buttonWidth };

            return (original, changed, button);
        }
    }

    class BlendShapePicker : EditorWindow
    {
        private string[] _names = Array.Empty<string>();
        private PSMEditorUtil<string,string> _nameMap = default!; // initialized in Show
        private SerializedObject _serializedObject = default!; // initialized in Show
        private Vector2 _scrollPosition;

        private void OnGUI()
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            foreach (var name in _names)
            {
                if (GUILayout.Button(name))
                {
                    _serializedObject.Update();
                    _nameMap.Add(name, "");
                    _serializedObject.ApplyModifiedProperties();
                    Close();
                }
            }
            GUILayout.EndScrollView();
        }

        private static void CloseAllOpenWindows()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<BlendShapePicker>())
            {
                try
                {
                    window.Close();
                }
                catch
                {
                    DestroyImmediate(window);
                }
            }
        }

        [DidReloadScripts]
        private static void OnScriptReload() => CloseAllOpenWindows();

        public static void Show(Rect rect, string[] names, PSMEditorUtil<string,string> nameMap, SerializedObject serializedObject)
        {
            CloseAllOpenWindows();
            var window = CreateInstance<BlendShapePicker>();
            var screenRect = GUIUtility.GUIToScreenRect(rect);
            screenRect.xMin -= 80;
            window._names = names ?? Array.Empty<string>();
            window._nameMap = nameMap;
            window._serializedObject = serializedObject;
            window.ShowAsDropDown(screenRect, new Vector2(200, 200));
        }
    }
}
