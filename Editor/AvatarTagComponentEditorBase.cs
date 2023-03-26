using CustomLocalization4EditorExtension;
using UnityEditor;

namespace Anatawa12.AvatarOptimizer
{
    abstract class AvatarTagComponentEditorBase : Editor
    {
        private readonly SaveVersionDrawer _saveVersion = new SaveVersionDrawer();

        public sealed override void OnInspectorGUI()
        {
            CL4EE.DrawLanguagePicker();

            var description = Description;
            if (!string.IsNullOrEmpty(description))
                EditorGUILayout.HelpBox(description, MessageType.None);

            _saveVersion.Draw(serializedObject);

            OnInspectorGUIInner();
        }

        protected virtual string Description => null;
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
