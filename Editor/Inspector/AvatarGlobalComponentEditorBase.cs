using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    abstract class AvatarGlobalComponentEditorBase : AvatarTagComponentEditorBase
    {
        protected override void OnInspectorGUIInner()
        {
            if (!nadena.dev.ndmf.runtime.RuntimeUtil.IsAvatarRoot(((Component)serializedObject.targetObject).transform))
                EditorGUILayout.HelpBox(AAOL10N.Tr("AvatarGlobalComponent:NotOnAvatarRoot"),
                    MessageType.Error);
        }
    }

    [CustomEditor(typeof(AvatarGlobalComponent), true)]
    class AvatarGlobalComponentEditor : AvatarGlobalComponentEditorBase
    {
        protected override void OnInspectorGUIInner()
        {
            base.OnInspectorGUIInner();
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
