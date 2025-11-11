#if AAO_VRCSDK3_AVATARS

using UnityEditor;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(ReplaceEndBoneWithEndpointPosition))]
    internal class ReplaceEndBoneWithEndpointPositionEditor : AvatarTagComponentEditorBase
    {
        SerializedProperty kindProperty = null!; // initialized in OnEnable
        SerializedProperty manualReplacementPositionProperty = null!; // initialized in OnEnable
        
        void OnEnable()
        {
            kindProperty = serializedObject.FindProperty(nameof(ReplaceEndBoneWithEndpointPosition.kind));
            manualReplacementPositionProperty = serializedObject.FindProperty(nameof(ReplaceEndBoneWithEndpointPosition.manualReplacementPosition));
        }

        protected override void OnInspectorGUIInner()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.PropertyField(kindProperty);
            if (kindProperty.enumValueIndex == (int)ReplaceEndBoneWithEndpointPositionKind.Manual)
            {
                EditorGUILayout.PropertyField(manualReplacementPositionProperty);
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif
