using System;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(MergePhysBone))]
    internal class MergePhysBoneEditor : Editor
    {
        private static class Style
        {
            public static readonly GUIStyle ErrorStyle = new GUIStyle
            {
                normal = { textColor = Color.red },
                wordWrap = false,
            };

            public static readonly GUIStyle WarningStyle = new GUIStyle
            {
                normal = { textColor = Color.yellow },
                wordWrap = false,
            };
        }

        public override void OnInspectorGUI()
        {
            var mergedComponentProp = serializedObject.FindProperty("merged");
            EditorGUI.BeginDisabledGroup(mergedComponentProp.objectReferenceValue != null);
            EditorGUILayout.PropertyField(mergedComponentProp);
            EditorGUI.EndDisabledGroup();
            
            var rootTransformProp = serializedObject.FindProperty("rootTransform");
            EditorGUILayout.PropertyField(rootTransformProp);

            SerializedProperty forcesProp, limitsProp;
            var componentsProp = serializedObject.FindProperty("components");

            EditorGUILayout.LabelField("Overrides", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            // == Forces ==
            EditorGUILayout.LabelField("Forces", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(forcesProp = serializedObject.FindProperty("forces"));
            EditorGUI.BeginDisabledGroup(forcesProp.boolValue);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pull"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("spring"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stiffness"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("gravity"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("gravityFalloff"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("immobile"));
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;
            // == Limits ==
            EditorGUILayout.LabelField("Limits", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(limitsProp = serializedObject.FindProperty("limits"));
            EditorGUI.BeginDisabledGroup(limitsProp.boolValue && componentsProp.arraySize != 0);
            var physBoneBase = (VRCPhysBoneBase)(componentsProp.arraySize != 0
                ? componentsProp.GetArrayElementAtIndex(0).objectReferenceValue
                : null);
            switch (physBoneBase != null ? physBoneBase.limitType : VRCPhysBoneBase.LimitType.None)
            {
                case VRCPhysBoneBase.LimitType.None:
                    break;
                case VRCPhysBoneBase.LimitType.Angle:
                case VRCPhysBoneBase.LimitType.Hinge:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("maxAngleX"), new GUIContent("Max Angle"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("limitRotation"));
                    break;
                case VRCPhysBoneBase.LimitType.Polar:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("maxAngleX"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("maxAngleZ"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("limitRotation"));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;
            // == Collision ==
            EditorGUILayout.LabelField("Collision", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("radius"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("allowCollision"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("colliders"));
            EditorGUI.indentLevel--;
            // == Grab & Pose ==
            EditorGUILayout.LabelField("Collision", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("allowGrabbing"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("grabMovement"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("allowPosing"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxStretch"));
            EditorGUI.indentLevel--;
            // == Others ==
            EditorGUILayout.LabelField("Others", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isAnimated"));
            EditorGUI.indentLevel--;

            EditorGUI.indentLevel--;

            EditorGUILayout.LabelField("Components:", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            for (var i = 0; i < componentsProp.arraySize; i++)
            {
                var elementProp = componentsProp.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(elementProp);

                if (elementProp.objectReferenceValue == null)
                {
                    componentsProp.DeleteArrayElementAtIndex(i);
                    i--;
                }
                else if (elementProp.objectReferenceValue is VRCPhysBoneBase bone)
                {
                    if (rootTransformProp.objectReferenceValue is Transform transform && !bone.GetTarget().IsChildOf(transform))
                        GUILayout.Label("RootTransform is not valid", Style.ErrorStyle);
                    if (bone.multiChildType != VRCPhysBoneBase.MultiChildType.Ignore)
                        GUILayout.Label("Multi child type must be Ignore", Style.ErrorStyle);
                    if (bone.parameter != "")
                        GUILayout.Label("You cannot use individual parameter", Style.WarningStyle);
                }
            }

            var toAdd = (VRCPhysBoneBase)EditorGUILayout.ObjectField($"Element {componentsProp.arraySize}", null,
                typeof(VRCPhysBoneBase), true);
            EditorGUI.indentLevel--;
            if (toAdd != null)
            {
                componentsProp.arraySize += 1;
                componentsProp.GetArrayElementAtIndex(componentsProp.arraySize - 1).objectReferenceValue = toAdd;
            }
            serializedObject.ApplyModifiedProperties();

            var differs = Processors.MergePhysBoneProcessor.CollectDifferentProps((MergePhysBone)target);
            if (differs.Count != 0)
            {
                GUILayout.Label("The following properies are different", Style.ErrorStyle);
                foreach (var differ in differs)
                    GUILayout.Label($"  {differ}", Style.ErrorStyle);
            }
        }
    }
}
