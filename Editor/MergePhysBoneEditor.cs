using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;

namespace Anatawa12.Merger
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
            var mergedComponentProp = serializedObject.FindProperty("mergedComponent");
            EditorGUI.BeginDisabledGroup(mergedComponentProp.objectReferenceValue != null);
            EditorGUILayout.PropertyField(mergedComponentProp);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.LabelField("Overrides", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            // == Forces ==
            EditorGUILayout.LabelField("Forces", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("integrationType"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pull"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("spring"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stiffness"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("gravity"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("gravityFalloff"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("immobile"));
            EditorGUI.indentLevel--;
            // == Limits ==
            EditorGUILayout.LabelField("Limits", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("limitType"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxAngleX"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxAngleZ"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("limitRotation"));
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
            var componentsProp = serializedObject.FindProperty("components");

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

            if (componentsProp.AsEnumerable().ZipWithNext()
                .Any(x => !IsSamePhysBone(new SerializedObject(x.Item1.objectReferenceValue),
                    new SerializedObject(x.Item2.objectReferenceValue))))
            {
                GUILayout.Label("Some Component has different", Style.ErrorStyle);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private bool Eq(SerializedObject a, SerializedObject b, string prop) =>
            serializedObject.FindProperty(prop).boolValue ||
            SerializedProperty.DataEquals(a.FindProperty(prop), b.FindProperty(prop));

        private bool EqCurve(SerializedObject a, SerializedObject b, string prop) =>
            serializedObject.FindProperty(prop).boolValue ||
            SerializedProperty.DataEquals(a.FindProperty(prop), b.FindProperty(prop)) ||
            SerializedProperty.DataEquals(a.FindProperty(prop + "Curve"), b.FindProperty(prop + "Curve"));

        private bool IsSamePhysBone(SerializedObject a, SerializedObject b)
        {
            // === Transforms ===
            // Root Transform: ignore: we'll merge them
            // Ignore Transforms: ignore: we'll merge them
            // Endpoint position: ignore: we'll replace with zero and insert end bone instead
            // Multi Child Type: ignore: Must be 'Ignore'
            // == Forces ==
            if (!Eq(a, b, nameof(VRCPhysBoneBase.integrationType))) return false;
            if (!EqCurve(a, b, nameof(VRCPhysBoneBase.pull))) return false;
            if (!EqCurve(a, b, nameof(VRCPhysBoneBase.spring))) return false;
            if (!EqCurve(a, b, nameof(VRCPhysBoneBase.stiffness))) return false;
            if (!EqCurve(a, b, nameof(VRCPhysBoneBase.gravity))) return false;
            if (!EqCurve(a, b, nameof(VRCPhysBoneBase.gravityFalloff))) return false;
            if (!Eq(a, b, nameof(VRCPhysBoneBase.immobileType))) return false;
            if (!EqCurve(a, b, nameof(VRCPhysBoneBase.immobile))) return false;
            // == Limits ==
            if (!Eq(a, b, nameof(VRCPhysBoneBase.limitType))) return false;
            switch ((VRCPhysBoneBase.LimitType) a.FindProperty(nameof(VRCPhysBoneBase.limitType)).enumValueIndex)
            {
                case VRCPhysBoneBase.LimitType.None:
                    break;
                case VRCPhysBoneBase.LimitType.Angle:
                case VRCPhysBoneBase.LimitType.Hinge:
                    if (!EqCurve(a, b, nameof(VRCPhysBoneBase.maxAngleXCurve))) return false;
                    //if (!EqCurve(a, b, nameof(VRCPhysBoneBase.maxAngleZCurve))) return false;
                    if (!Eq(a, b, nameof(VRCPhysBoneBase.limitRotation))) return false;
                    if (!Eq(a, b, nameof(VRCPhysBoneBase.limitRotationXCurve))) return false;
                    if (!Eq(a, b, nameof(VRCPhysBoneBase.limitRotationYCurve))) return false;
                    if (!Eq(a, b, nameof(VRCPhysBoneBase.limitRotationZCurve))) return false;
                    break;
                case VRCPhysBoneBase.LimitType.Polar:
                    if (!EqCurve(a, b, nameof(VRCPhysBoneBase.maxAngleXCurve))) return false;
                    if (!EqCurve(a, b, nameof(VRCPhysBoneBase.maxAngleZCurve))) return false;
                    if (!Eq(a, b, nameof(VRCPhysBoneBase.limitRotation))) return false;
                    if (!Eq(a, b, nameof(VRCPhysBoneBase.limitRotationXCurve))) return false;
                    if (!Eq(a, b, nameof(VRCPhysBoneBase.limitRotationYCurve))) return false;
                    if (!Eq(a, b, nameof(VRCPhysBoneBase.limitRotationZCurve))) return false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            // == Collision ==
            if (!EqCurve(a, b, nameof(VRCPhysBoneBase.radius))) return false;
            if (!Eq(a, b, nameof(VRCPhysBoneBase.allowCollision))) return false;
            if (!EqSet(a.FindProperty(nameof(VRCPhysBoneBase.colliders)).AsEnumerable().Select(x => x.objectReferenceValue), 
                    b.FindProperty(nameof(VRCPhysBoneBase.colliders)).AsEnumerable().Select(x => x.objectReferenceValue))) return false;
            // == Grab & Pose ==
            if (!Eq(a, b, nameof(VRCPhysBoneBase.allowGrabbing))) return false;
            if (!Eq(a, b, nameof(VRCPhysBoneBase.allowPosing))) return false;
            if (!Eq(a, b, nameof(VRCPhysBoneBase.grabMovement))) return false;
            if (!EqCurve(a, b, nameof(VRCPhysBoneBase.maxStretch))) return false;
            // == Options ==
            // Parameter: ignore: must be empty
            // Is Animated: ignore: we can merge them.
            // Gizmos: ignore: it should not affect actual behaviour
            return true;
        }

        private bool EqSet<T>(IEnumerable<T> a, IEnumerable<T> b) => 
            new HashSet<T>(a).SetEquals(b);
    }
}
