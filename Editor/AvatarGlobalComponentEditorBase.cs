using Anatawa12.AvatarOptimizer.ErrorReporting;
using CustomLocalization4EditorExtension;
using UnityEditor;
using UnityEngine;
using VRC.Core;
using VRC.SDK3.Avatars.Components;

namespace Anatawa12.AvatarOptimizer
{
    [InitializeOnLoad]
    abstract class AvatarGlobalComponentEditorBase : AvatarTagComponentEditorBase
    {
        static AvatarGlobalComponentEditorBase()
        {
            ComponentValidation.RegisterValidator<AvatarGlobalComponent>(component =>
            {
                if (!component.GetComponent<VRCAvatarDescriptor>())
                    return new[] { ErrorLog.Error("AvatarGlobalComponent:NotOnAvatarDescriptor", component) };
                return null;
            });
        }

        protected override void OnInspectorGUIInner()
        {
            if (!((Component)serializedObject.targetObject).GetComponent<VRCAvatarDescriptor>())
                EditorGUILayout.HelpBox(CL4EE.Tr("AvatarGlobalComponent:NotOnAvatarDescriptor"),
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
