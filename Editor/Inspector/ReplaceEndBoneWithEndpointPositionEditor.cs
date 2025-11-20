#if AAO_VRCSDK3_AVATARS

using UnityEditor;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(ReplaceEndBoneWithEndpointPosition))]
    internal class ReplaceEndBoneWithEndpointPositionEditor : AvatarTagComponentEditorBase
    {
        SerializedProperty kindProperty = null!; // initialized in OnEnable
        SerializedProperty overridePositionProperty = null!; // initialized in OnEnable
        
        void OnEnable()
        {
            kindProperty = serializedObject.FindProperty(nameof(ReplaceEndBoneWithEndpointPosition.kind));
            overridePositionProperty = serializedObject.FindProperty(nameof(ReplaceEndBoneWithEndpointPosition.overridePosition));
        }

        protected override void OnInspectorGUIInner()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.PropertyField(kindProperty);
            using (new EditorGUI.DisabledScope(kindProperty.enumValueIndex != (int)ReplaceEndBoneWithEndpointPositionKind.Override))
            {
                EditorGUILayout.PropertyField(overridePositionProperty);
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif
