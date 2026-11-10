using UnityEditor;

namespace Anatawa12.AvatarOptimizer
{
    abstract class AvatarTagComponentEditorBase : Editor
    {
        public sealed override void OnInspectorGUI()
        {
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
