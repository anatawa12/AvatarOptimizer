using Anatawa12.AvatarOptimizer.ErrorReporting;
using CustomLocalization4EditorExtension;
using UnityEditor;
using UnityEngine;

#if AAO_VRCSDK3_AVATARS
using nadena.dev.ndmf;
using VRC.Core;
using VRC.SDK3.Avatars.Components;
#endif

namespace Anatawa12.AvatarOptimizer
{
    abstract class AvatarGlobalComponentEditorBase : AvatarTagComponentEditorBase
    {
        static AvatarGlobalComponentEditorBase()
        {
            ComponentValidation.RegisterValidator<AvatarGlobalComponent>(component =>
            {
#if AAO_VRCSDK3_AVATARS
                if (!component.GetComponent<VRCAvatarDescriptor>())
                    BuildReport.LogError("AvatarGlobalComponent:NotOnAvatarDescriptor");
#else
                if (!nadena.dev.ndmf.runtime.RuntimeUtil.IsAvatarRoot(component.transform))
                    BuildReport.LogError("AvatarGlobalComponent:NotOnAvatarDescriptor");
#endif
            });
        }
        protected override void OnInspectorGUIInner()
        {
#if AAO_VRCSDK3_AVATARS
            if (!((Component)serializedObject.targetObject).GetComponent<VRCAvatarDescriptor>())
                EditorGUILayout.HelpBox(CL4EE.Tr("AvatarGlobalComponent:NotOnAvatarDescriptor"),
                    MessageType.Error);
#else
            if (!nadena.dev.ndmf.runtime.RuntimeUtil.IsAvatarRoot(((Component)serializedObject.targetObject).transform))
                EditorGUILayout.HelpBox(CL4EE.Tr("AvatarGlobalComponent:NotOnAvatarRoot"),
                    MessageType.Error);
#endif
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
