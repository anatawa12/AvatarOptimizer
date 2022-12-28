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
                .Any(x => !IsSamePhysBone(x.Item1.objectReferenceValue as VRCPhysBoneBase,
                    x.Item2.objectReferenceValue as VRCPhysBoneBase)))
            {
                GUILayout.Label("Some Component has different", Style.ErrorStyle);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private bool Eq(float a, float b) => Mathf.Abs(a - b) < 0.00001f;
        private bool Eq(Vector3 a, Vector3 b) => (a - b).magnitude < 0.00001f;
        private bool Eq(AnimationCurve a, AnimationCurve b) => a.Equals(b);
        private bool Eq(float aFloat, AnimationCurve aCurve, float bFloat, AnimationCurve bCurve) => 
            Eq(aFloat, bFloat) && Eq(aCurve, bCurve);

        private bool IsSamePhysBone(VRCPhysBoneBase a, VRCPhysBoneBase b)
        {
            // === Transforms ===
            // Root Transform: ignore: we'll merge them
            // Ignore Transforms: ignore: we'll merge them
            // Endpoint position: ignore: we'll replace with zero and insert end bone instead
            // Multi Child Type: ignore: Must be 'Ignore'
            // == Forces ==
            if (a.integrationType != b.integrationType) return false;
            if (!Eq(a.pull, a.pullCurve, b.pull, b.pullCurve)) return false;
            if (!Eq(a.spring, a.springCurve, b.spring, b.springCurve)) return false;
            if (!Eq(a.stiffness, a.stiffnessCurve, b.stiffness, b.stiffnessCurve)) return false;
            if (!Eq(a.gravity, a.gravityCurve, b.gravity, b.gravityCurve)) return false;
            if (!Eq(a.gravityFalloff, a.gravityFalloffCurve, b.gravityFalloff, b.gravityFalloffCurve)) return false;
            if (a.immobileType != b.immobileType) return false;
            if (!Eq(a.immobile, a.immobileCurve, b.immobile, b.immobileCurve)) return false;
            // == Limits ==
            if (a.limitType != b.limitType) return false;
            switch (a.limitType)
            {
                case VRCPhysBoneBase.LimitType.None:
                    break;
                case VRCPhysBoneBase.LimitType.Angle:
                case VRCPhysBoneBase.LimitType.Hinge:
                    if (!Eq(a.maxAngleX, a.maxAngleXCurve, b.maxAngleX, b.maxAngleXCurve)) return false;
                    //if (!Eq(a.maxAngleZ, a.maxAngleZCurve, b.maxAngleZ, b.maxAngleZCurve)) return false;
                    if (!Eq(a.limitRotation, b.limitRotation)) return false;
                    if (!Eq(a.limitRotationXCurve, b.limitRotationXCurve)) return false;
                    if (!Eq(a.limitRotationYCurve, b.limitRotationYCurve)) return false;
                    if (!Eq(a.limitRotationZCurve, b.limitRotationZCurve)) return false;
                    break;
                case VRCPhysBoneBase.LimitType.Polar:
                    if (!Eq(a.maxAngleX, a.maxAngleXCurve, b.maxAngleX, b.maxAngleXCurve)) return false;
                    if (!Eq(a.maxAngleZ, a.maxAngleZCurve, b.maxAngleZ, b.maxAngleZCurve)) return false;
                    if (!Eq(a.limitRotation, b.limitRotation)) return false;
                    if (!Eq(a.limitRotationXCurve, b.limitRotationXCurve)) return false;
                    if (!Eq(a.limitRotationYCurve, b.limitRotationYCurve)) return false;
                    if (!Eq(a.limitRotationZCurve, b.limitRotationZCurve)) return false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            // == Collision ==
            if (!Eq(a.radius, a.radiusCurve, b.radius, b.radiusCurve)) return false;
            if (a.allowCollision != b.allowCollision) return false;
            if (!Eq(a.colliders, b.colliders)) return false;
            // == Grab & Pose ==
            if (a.allowGrabbing != b.allowGrabbing) return false;
            if (a.allowPosing != b.allowPosing) return false;
            if (!Eq(a.grabMovement, b.grabMovement)) return false;
            if (!Eq(a.maxStretch, a.maxStretchCurve, b.maxStretch, b.maxStretchCurve)) return false;
            // == Options ==
            // Parameter: ignore: must be empty
            // Is Animated: ignore: we can merge them.
            // Gizmos: ignore: it should not affect actual behaviour
            return true;
        }

        private bool Eq(IEnumerable<VRCPhysBoneColliderBase> a, IEnumerable<VRCPhysBoneColliderBase> b) => 
            new HashSet<VRCPhysBoneColliderBase>(a).SetEquals(b);
    }
}
