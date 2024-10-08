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
        // header
        {
            var (original, changed, button) = DivideToTwo(EditorGUILayout.GetControlRect());

            GUI.Label(original, "Original", EditorStyles.boldLabel);
            GUI.Label(changed, "Changed", EditorStyles.boldLabel);

            var content = new GUIContent("+");
            if (EditorGUI.DropdownButton(button, content, FocusType.Passive, EditorStyles.miniButton))
            {
                var mesh = ((RenameBlendShape)target).GetComponent<SkinnedMeshRenderer>()?.sharedMesh;
                var blendShapeNames = mesh == null
                    ? Array.Empty<string>()
                    : Enumerable.Range(0, mesh.blendShapeCount).Select(mesh.GetBlendShapeName).ToArray();
                BlendShapePicker.Show(button, blendShapeNames, _nameMap, serializedObject);
                //_nameMap.Add()
            }
        }

        Action? deferredAction = null;

        foreach (var element in _nameMap.Elements)
        {
            var rect = EditorGUILayout.GetControlRect();
            using var propertyScope = new PropertyScope<string, string>(element, rect, GUIContent.none);
            using var disabledScope = new EditorGUI.DisabledScope(!element.Contains);

            var (original, changed, button) = DivideToTwo(rect);

            GUI.Label(original, element.Key);
            if (element.Contains)
            {
                element.Set(EditorGUI.TextField(changed, element.Value));

                if (GUI.Button(button, "-"))
                    deferredAction += () => element.Remove();
            }
            else
            {
                GUI.Label(changed, "(Removed)");
            }
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
