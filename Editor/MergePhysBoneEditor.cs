using System;
using System.Linq;
using CustomLocalization4EditorExtension;
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

        private readonly SaveVersionDrawer _saveVersion = new SaveVersionDrawer();
        private SerializedProperty _mergedComponentProp;
        private SerializedProperty _rootTransformProp;
        private SerializedProperty _forcesProp;
        private SerializedProperty _pullProp;
        private SerializedProperty _springProp;
        private SerializedProperty _stiffnessProp;
        private SerializedProperty _gravityProp;
        private SerializedProperty _gravityFalloffProp;
        private SerializedProperty _immobileProp;
        private SerializedProperty _limitsProp;
        private SerializedProperty _maxAngleXProp;
        private SerializedProperty _limitRotationProp;
        private SerializedProperty _maxAngleZProp;
        private SerializedProperty _radiusProp;
        private SerializedProperty _allowCollisionProp;
        private SerializedProperty _collidersProp;
        private SerializedProperty _allowGrabbingProp;
        private SerializedProperty _grabMovementProp;
        private SerializedProperty _allowPosingProp;
        private SerializedProperty _maxStretchProp;
        private SerializedProperty _isAnimatedProp;
        private SerializedProperty _componentsSetProp;
        private PrefabSafeSet.EditorUtil<VRCPhysBoneBase> _componentsSetEditorUtil;

        private void OnEnable()
        {
            var nestCount = PrefabSafeSet.PrefabSafeSetUtil.PrefabNestCount(serializedObject.targetObject);
            _mergedComponentProp = serializedObject.FindProperty("merged");
            _rootTransformProp = serializedObject.FindProperty("rootTransform");
            _forcesProp = serializedObject.FindProperty("forces");
            _pullProp = serializedObject.FindProperty("pull");
            _springProp = serializedObject.FindProperty("spring");
            _stiffnessProp = serializedObject.FindProperty("stiffness");
            _gravityProp = serializedObject.FindProperty("gravity");
            _gravityFalloffProp = serializedObject.FindProperty("gravityFalloff");
            _immobileProp = serializedObject.FindProperty("immobile");
            _limitsProp = serializedObject.FindProperty("limits");
            _maxAngleXProp = serializedObject.FindProperty("maxAngleX");
            _limitRotationProp = serializedObject.FindProperty("limitRotation");
            _maxAngleZProp = serializedObject.FindProperty("maxAngleZ");
            _radiusProp = serializedObject.FindProperty("radius");
            _allowCollisionProp = serializedObject.FindProperty("allowCollision");
            _collidersProp = serializedObject.FindProperty("colliders");
            _allowGrabbingProp = serializedObject.FindProperty("allowGrabbing");
            _grabMovementProp = serializedObject.FindProperty("grabMovement");
            _allowPosingProp = serializedObject.FindProperty("allowPosing");
            _maxStretchProp = serializedObject.FindProperty("maxStretch");
            _isAnimatedProp = serializedObject.FindProperty("isAnimated");
            _componentsSetProp = serializedObject.FindProperty(nameof(MergePhysBone.componentsSet));
            _componentsSetEditorUtil = PrefabSafeSet.EditorUtil<VRCPhysBoneBase>.Create(
                _componentsSetProp, nestCount, x => (VRCPhysBoneBase)x.objectReferenceValue,
                (x, v) => x.objectReferenceValue = v);
        }

        public override void OnInspectorGUI()
        {
            _saveVersion.Draw(serializedObject);
            EditorGUI.BeginDisabledGroup(_mergedComponentProp.objectReferenceValue != null);
            EditorGUILayout.PropertyField(_mergedComponentProp);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(_rootTransformProp);


            EditorGUILayout.LabelField("Overrides", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            // == Forces ==
            EditorGUILayout.LabelField("Forces", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_forcesProp);
            EditorGUI.BeginDisabledGroup(_forcesProp.boolValue);
            EditorGUILayout.PropertyField(_pullProp);
            EditorGUILayout.PropertyField(_springProp);
            EditorGUILayout.PropertyField(_stiffnessProp);
            EditorGUILayout.PropertyField(_gravityProp);
            EditorGUILayout.PropertyField(_gravityFalloffProp);
            EditorGUILayout.PropertyField(_immobileProp);
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;
            // == Limits ==
            EditorGUILayout.LabelField("Limits", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_limitsProp);
            EditorGUI.BeginDisabledGroup(_limitsProp.boolValue && _componentsSetEditorUtil.Count != 0);
            var physBoneBase = _componentsSetEditorUtil.Count != 0 ? _componentsSetEditorUtil.Values.First() : null;
            switch (physBoneBase != null ? physBoneBase.limitType : VRCPhysBoneBase.LimitType.None)
            {
                case VRCPhysBoneBase.LimitType.None:
                    break;
                case VRCPhysBoneBase.LimitType.Angle:
                case VRCPhysBoneBase.LimitType.Hinge:
                    EditorGUILayout.PropertyField(_maxAngleXProp,
                        new GUIContent(CL4EE.Tr("MergePhysBone:prop:Max Angle")));
                    EditorGUILayout.PropertyField(_limitRotationProp);
                    break;
                case VRCPhysBoneBase.LimitType.Polar:
                    EditorGUILayout.PropertyField(_maxAngleXProp);
                    EditorGUILayout.PropertyField(_maxAngleZProp);
                    EditorGUILayout.PropertyField(_limitRotationProp);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;
            // == Collision ==
            EditorGUILayout.LabelField("Collision", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_radiusProp);
            EditorGUILayout.PropertyField(_allowCollisionProp);
            EditorGUILayout.PropertyField(_collidersProp);
            EditorGUI.indentLevel--;
            // == Grab & Pose ==
            EditorGUILayout.LabelField("Collision", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_allowGrabbingProp);
            EditorGUILayout.PropertyField(_grabMovementProp);
            EditorGUILayout.PropertyField(_allowPosingProp);
            EditorGUILayout.PropertyField(_maxStretchProp);
            EditorGUI.indentLevel--;
            // == Others ==
            EditorGUILayout.LabelField("Others", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_isAnimatedProp);
            EditorGUI.indentLevel--;

            EditorGUI.indentLevel--;

            EditorGUILayout.PropertyField(_componentsSetProp);

            serializedObject.ApplyModifiedProperties();

            var differs = Processors.MergePhysBoneProcessor.CollectDifferentProps((MergePhysBone)target,
                ((MergePhysBone)target).componentsSet.GetAsSet());
            if (differs.Count != 0)
            {
                GUILayout.Label("The following properies are different", Style.ErrorStyle);
                foreach (var differ in differs)
                    GUILayout.Label($"  {differ}", Style.ErrorStyle);
            }
        }
    }
}
