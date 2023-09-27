using System;
using Anatawa12.AvatarOptimizer.Processors;
using CustomLocalization4EditorExtension;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [Obsolete("Obsoleted by Trace and Optimize")]
    [CustomEditor(typeof(UnusedBonesByReferencesTool))]
    class UnusedBonesByReferencesToolEditor : AvatarGlobalComponentEditorBase
    {
        private SerializedProperty _preserveEndBone;
        private SerializedProperty _detectExtraChild;

        private void OnEnable()
        {
            _preserveEndBone = serializedObject.FindProperty(nameof(UnusedBonesByReferencesTool.preserveEndBone));
            _detectExtraChild = serializedObject.FindProperty(nameof(UnusedBonesByReferencesTool.detectExtraChild));
        }

        protected override void OnInspectorGUIInner()
        {
            EditorGUILayout.PropertyField(_preserveEndBone);
            EditorGUILayout.PropertyField(_detectExtraChild);

            EditorGUILayout.HelpBox(CL4EE.Tr("UnusedBonesByReferencesTool:suggestMigrate"), MessageType.Info);
            if (GUILayout.Button(CL4EE.Tr("UnusedBonesByReferencesTool:migrate")))
            {
                const string CONTENT_UNDO_NAME = "Migrate UnusedBonesByReferencesTool to Trace and Optimize";

                var component = (UnusedBonesByReferencesTool)target;

                // first, migrate configuration
                var traceAndOptimize = component.GetComponent<TraceAndOptimize>();
                if (!traceAndOptimize)
                    traceAndOptimize = Undo.AddComponent<TraceAndOptimize>(component.gameObject);
                Undo.RecordObject(traceAndOptimize, CONTENT_UNDO_NAME);

                traceAndOptimize.removeUnusedObjects = true;
                traceAndOptimize.advancedSettings.useLegacyGc = false;
                traceAndOptimize.preserveEndBone = component.preserveEndBone;
                PrefabUtility.RecordPrefabInstancePropertyModifications(traceAndOptimize);

                // then, remove EditorOnly from bones
                foreach (var boneReference in UnusedBonesByReferencesToolEarlyProcessor.BoneReference
                             .Make(component.transform))
                {
                    if (boneReference.Bone.CompareTag("EditorOnly"))
                    {
                        Undo.RecordObject(boneReference.Bone, CONTENT_UNDO_NAME);
                        boneReference.Bone.tag = "Untagged";
                        PrefabUtility.RecordPrefabInstancePropertyModifications(boneReference.Bone);
                    }
                }

                Undo.DestroyObjectImmediate(component);
                Undo.SetCurrentGroupName(CONTENT_UNDO_NAME);

                EditorUtility.DisplayDialog(CL4EE.Tr("UnusedBonesByReferencesTool:migrationFinished:title"),
                    CL4EE.Tr("UnusedBonesByReferencesTool:migrationFinished:description"),
                    "Ok");
            }
        }
    }
}