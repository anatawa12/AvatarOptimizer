using UnityEditor;
using UnityEngine;

#if AAO_VRCSDK3_AVATARS
using VRC.SDK3.Avatars.Components;
#endif

namespace Anatawa12.AvatarOptimizer
{
    [InitializeOnLoad]
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
