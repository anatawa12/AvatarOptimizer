using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    abstract class AvatarTagComponentEditorBase : Editor
    {
        public sealed override void OnInspectorGUI()
        {
            ShowVersionInfo();

            AAOL10N.DrawLanguagePicker();

            var description = Description;
            if (!string.IsNullOrEmpty(description))
                EditorGUILayout.HelpBox(description, MessageType.None);

            OnInspectorGUIInner();

#if VRCSDK_AVATARS_NO_RESET_GUI_STATE_BUG
            // Workarounds https://feedback.vrchat.com/sdk-bug-reports/p/3106-use-all-tracking-keys-toggle-may-be-unclickable-and-the-left-half-of-field
            EditorGUIUtility.labelWidth = 0;
#endif
        }

        private static class Styles
        {
            public static readonly GUIStyle VersionText = new GUIStyle(GUI.skin.label)
            {
                fontSize = (int)(EditorStyles.label.fontSize * 1.2f),
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
            };
            public static readonly GUIStyle CheckForUpdateStatus = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
            };
            public static readonly GUIStyle CheckForUpdateButton = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(15, 15, 5, 5),
                margin = new RectOffset(5, 5, 5, 5),
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
            };
        }

        private static void ShowVersionInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(string.Format(AAOL10N.Tr("CheckForUpdate:inspector:CurrentVersion"), CheckForUpdate.Checker.CurrentVersionName), Styles.VersionText);
            if (CheckForUpdate.Checker.Checking)
            {
                GUILayout.Label(AAOL10N.Tr("CheckForUpdate:inspector:CheckingForUpdate"), Styles.CheckForUpdateStatus);
            }
            else if (CheckForUpdate.MenuItems.CheckForUpdateEnabled)
            {
                if (CheckForUpdate.Checker.OutOfDate)
                {
                    GUILayout.Label(string.Format(AAOL10N.Tr("CheckForUpdate:inspector:OutOfDate"), CheckForUpdate.Checker.LatestVersionName), Styles.CheckForUpdateStatus);
                }
                else
                {
                    GUILayout.Label(AAOL10N.Tr("CheckForUpdate:inspector:UsingLatest"), Styles.CheckForUpdateStatus);
                    if (CheckForUpdateButton(AAOL10N.Tr("CheckForUpdate:inspector:CheckForUpdateNow")))
                    {
                        CheckForUpdate.MenuItems.CheckForUpdateEnabled = true;
                        CheckForUpdate.Checker.DoCheckForUpdate(force: true);
                    }
                }
            }
            else
            {
                GUILayout.Label(AAOL10N.Tr("CheckForUpdate:inspector:CheckForUpdateDisabled"),
                    Styles.CheckForUpdateStatus);
                if (CheckForUpdateButton(AAOL10N.Tr("CheckForUpdate:inspector:EnableCheckForUpdate")))
                {
                    CheckForUpdate.MenuItems.CheckForUpdateEnabled = true;
                    CheckForUpdate.Checker.DoCheckForUpdate(force: true);
                }
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
            return;

            bool CheckForUpdateButton(string text)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                var result = GUILayout.Button(text, Styles.CheckForUpdateButton);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                return result;
            }
        }

        private string? _descriptionKey;
        protected virtual string? Description
        {
            get
            {
                if (_descriptionKey == null)
                    _descriptionKey = $"{serializedObject.targetObject.GetType().Name}:description";
                return AAOL10N.TryTr(_descriptionKey);
            }
        }

        protected abstract void OnInspectorGUIInner();
    }

    [CustomEditor(typeof(AvatarTagComponent), true)]
    class DefaultAvatarTagComponentEditor : AvatarTagComponentEditorBase
    {
        protected override void OnInspectorGUIInner()
        {
            serializedObject.UpdateIfRequiredOrScript();
            var iterator = serializedObject.GetIterator();

            var enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                if ("m_Script" != iterator.propertyPath)
                    EditorGUILayout.PropertyField(iterator, true);
                enterChildren = false;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
