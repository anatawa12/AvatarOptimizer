using System;
using System.Collections.Generic;
using System.Linq;
using CustomLocalization4EditorExtension;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;
using Debug = System.Diagnostics.Debug;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(MergePhysBone))]
    internal class MergePhysBoneEditor : AvatarTagComponentEditorBase
    {
        private SerializedObject _mergedPhysBone;
        private MergePhysBoneEditorRenderer _renderer;
        private SerializedProperty _makeParent;
        private SerializedProperty _componentsSetProp;

        private void OnEnable()
        {
            _renderer = new MergePhysBoneEditorRenderer(serializedObject);
            _mergedPhysBone = new SerializedObject(serializedObject.FindProperty("merged").objectReferenceValue);
            _makeParent = serializedObject.FindProperty("makeParent");
            _componentsSetProp = serializedObject.FindProperty(nameof(MergePhysBone.componentsSet));
        }

        protected override void OnInspectorGUIInner()
        {
            _mergedPhysBone.Update();

            EditorGUILayout.PropertyField(_makeParent);
            if (_makeParent.boolValue && ((Component)target).transform.childCount != 0)
                EditorGUILayout.HelpBox(CL4EE.Tr("MergePhysBone:error:makeParentWithChildren"), MessageType.Error);

            EditorGUILayout.PropertyField(_componentsSetProp);

            // draw custom editor
            _renderer.DoProcess();

            serializedObject.ApplyModifiedProperties();
            _mergedPhysBone.ApplyModifiedProperties();
        }
    }

    internal abstract class MergePhysBoneEditorModificationUtils
    {
        // for historical reasons, InconsistentNaming
        // ReSharper disable InconsistentNaming
        // ReSharper disable MemberCanBePrivate.Global
        protected SerializedObject _sourcePhysBone;
        protected readonly SerializedObject _mergedPhysBone;
        protected readonly SerializedProperty _version;
        protected readonly SerializedProperty _integrationTypeProp;
        protected readonly SerializedProperty _pullProp;
        protected readonly SerializedProperty _springProp;
        protected readonly SerializedProperty _stiffnessProp;
        protected readonly SerializedProperty _gravityProp;
        protected readonly SerializedProperty _gravityFalloffProp;
        protected readonly SerializedProperty _immobileTypeProp;
        protected readonly SerializedProperty _immobileProp;
        protected readonly SerializedProperty _limitsProp;
        protected readonly SerializedProperty _maxAngleXProp;
        protected readonly SerializedProperty _limitRotationProp;
        protected readonly SerializedProperty _maxAngleZProp;
        protected readonly SerializedProperty _radiusProp;
        protected readonly SerializedProperty _allowCollisionProp;
        protected readonly SerializedProperty _collidersProp;
        protected readonly SerializedProperty _allowGrabbingProp;
        protected readonly SerializedProperty _grabMovementProp;
        protected readonly SerializedProperty _allowPosingProp;
        protected readonly SerializedProperty _stretchMotion;
        protected readonly SerializedProperty _maxStretchProp;
        protected readonly SerializedProperty _maxSquish;
        protected readonly SerializedProperty _snapToHandProp;
        protected readonly SerializedProperty _isAnimatedProp;
        protected readonly SerializedProperty _resetWhenDisabledProp;
        protected readonly PrefabSafeSet.EditorUtil<VRCPhysBoneBase> _componentsSetEditorUtil;
        // ReSharper restore InconsistentNaming
        // ReSharper restore MemberCanBePrivate.Global

        public MergePhysBoneEditorModificationUtils(SerializedObject serializedObject)
        {
            var nestCount = PrefabSafeSet.PrefabSafeSetUtil.PrefabNestCount(serializedObject.targetObject);
            _mergedPhysBone = new SerializedObject(serializedObject.FindProperty("merged").objectReferenceValue);
            _version = serializedObject.FindProperty("version");
            _integrationTypeProp = serializedObject.FindProperty("integrationType");
            _pullProp = serializedObject.FindProperty("pull");
            _springProp = serializedObject.FindProperty("spring");
            _stiffnessProp = serializedObject.FindProperty("stiffness");
            _gravityProp = serializedObject.FindProperty("gravity");
            _gravityFalloffProp = serializedObject.FindProperty("gravityFalloff");
            _immobileTypeProp = serializedObject.FindProperty("immobileType");
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
            _stretchMotion = serializedObject.FindProperty("stretchMotion");
            _maxStretchProp = serializedObject.FindProperty("maxStretch");
            _maxSquish = serializedObject.FindProperty("maxSquish");
            _snapToHandProp = serializedObject.FindProperty("snapToHand");
            _isAnimatedProp = serializedObject.FindProperty("isAnimated");
            _resetWhenDisabledProp = serializedObject.FindProperty("resetWhenDisabled");
            var componentsSetProp = serializedObject.FindProperty(nameof(MergePhysBone.componentsSet));
            _componentsSetEditorUtil = PrefabSafeSet.EditorUtil<VRCPhysBoneBase>.Create(
                componentsSetProp, nestCount, x => (VRCPhysBoneBase)x.objectReferenceValue,
                (x, v) => x.objectReferenceValue = v);
        }

        public void DoProcess()
        {
            _mergedPhysBone.Update();

            var sourcePysBone = _componentsSetEditorUtil.Values.FirstOrDefault();
            _sourcePhysBone = sourcePysBone == null
                ? null
                : new SerializedObject(_componentsSetEditorUtil.Values.Cast<Object>().ToArray());

            if (_sourcePhysBone == null)
            {
                NoSource();
            }
            else
            {
                SerializedObject GetPb(SerializedProperty prop) => prop.boolValue ? _mergedPhysBone : _sourcePhysBone;

                BeginPbConfig();

                PbVersionProp("Version", "version", _version);

                var version = (VRCPhysBoneBase.Version)GetPb(_version).FindProperty("version").enumValueIndex;

                switch (version)
                {
                    case VRCPhysBoneBase.Version.Version_1_0:
                    case VRCPhysBoneBase.Version.Version_1_1:
                        break;
                    default:
                        UnsupportedPbVersion();
                        break;
                }

                bool CheckMinVersion(VRCPhysBoneBase.Version require) => version >= require;

                // == Transform ==
                if (BeginSection("Transform", "transforms"))
                {
                    TransformSection();
                }

                // == Forces ==
                if (NextSection("Forces", "forces"))
                {
                    PbProp("Integration Type", "integrationType", _integrationTypeProp);
                    var isSimplified = GetPb(_integrationTypeProp).FindProperty("integrationType").enumValueIndex == 0;
                    PbCurveProp("Pull", "pull", "pullCurve", _pullProp);
                    if (isSimplified)
                    {
                        PbCurveProp("Spring", "spring", "springCurve", _springProp, _integrationTypeProp);
                    }
                    else
                    {
                        PbCurveProp("Momentum", "spring", "springCurve", _springProp, _integrationTypeProp);
                        PbCurveProp("Stiffness", "stiffness", "stiffnessCurve", _stiffnessProp, _integrationTypeProp);
                    }

                    PbCurveProp("Gravity", "gravity", "gravityCurve", _gravityProp);
                    PbCurveProp("Gravity Falloff", "gravityFalloff", "gravityFalloffCurve", _gravityFalloffProp);
                    PbProp("Immobile Type", "immobileType", _immobileTypeProp);
                    PbCurveProp("Immobile", "immobile", "immobileCurve", _immobileProp);
                }

                // == Limits ==
                if (NextSection("Limits", "limits"))
                {
                    PbProp("Limit Type", "limitType", _limitsProp);
                    var limitType =
                        (VRCPhysBoneBase.LimitType)GetPb(_limitsProp).FindProperty("limitType").enumValueIndex;

                    switch (limitType)
                    {
                        case VRCPhysBoneBase.LimitType.None:
                            break;
                        case VRCPhysBoneBase.LimitType.Angle:
                        case VRCPhysBoneBase.LimitType.Hinge:
                            PbCurveProp("Max Angle", "maxAngleX", "maxAngleXCurve", _maxAngleXProp, _limitsProp);
                            Pb3DCurveProp("Rotation", "limitRotation",
                                "Pitch", "limitRotationXCurve",
                                "Roll", "limitRotationYCurve",
                                "Yaw", "limitRotationZCurve",
                                _limitRotationProp, _limitsProp);
                            break;
                        case VRCPhysBoneBase.LimitType.Polar:
                            PbCurveProp("Max Angle X", "maxAngleX", "maxAngleXCurve", _maxAngleXProp, _limitsProp);
                            PbCurveProp("Max Angle Z", "maxAngleZ", "maxAngleZCurve", _maxAngleZProp, _limitsProp);
                            Pb3DCurveProp("Rotation", "limitRotation",
                                "Pitch", "limitRotationXCurve",
                                "Roll", "limitRotationYCurve",
                                "Yaw", "limitRotationZCurve",
                                _limitRotationProp, _limitsProp);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                // == Collision ==
                if (NextSection("Collision", "collision"))
                {
                    PbCurveProp("Radius", "radius", "radiusCurve", _radiusProp);
                    PbPermissionProp("Allow Collision", "allowCollision", "collisionFilter", _allowCollisionProp);
                    ColliderProp("Colliders", "colliders", _collidersProp);
                }

                // == Stretch & Squish ==
                if (NextSection("Stretch & Squish", "stretch--squish"))
                {
                    if (CheckMinVersion(VRCPhysBoneBase.Version.Version_1_1))
                        PbCurveProp("Stretch Motion", "stretchMotion", "stretchMotionCurve", _stretchMotion);
                    PbCurveProp("Max Stretch", "maxStretch", "maxStretchCurve", _maxStretchProp);
                    if (CheckMinVersion(VRCPhysBoneBase.Version.Version_1_1))
                        PbCurveProp("Max Squish", "maxSquish", "maxSquishCurve", _maxSquish);
                }

                // == Grab & Pose ==
                if (NextSection("Grab & Pose", "grab--pose"))
                {
                    PbPermissionProp("Allow Grabbing", "allowGrabbing", "grabFilter", _allowGrabbingProp);
                    PbPermissionProp("Allow Posing", "allowPosing", "poseFilter", _allowPosingProp);
                    PbProp("Grab Movement", "grabMovement", _grabMovementProp);
                    PbProp("Snap To Hand", "snapToHand", _snapToHandProp);
                }

                // == Options ==
                if (NextSection("Options", "options"))
                {
                    OptionParameter();
                    OptionIsAnimated();
                    PbProp("Reset When Disabled", "resetWhenDisabled", _resetWhenDisabledProp);
                }

                EndSection();

                EndPbConfig();
            }

            _mergedPhysBone.ApplyModifiedProperties();
        }

        protected abstract void BeginPbConfig();

        protected abstract bool BeginSection(string name, string docTag);
        protected abstract void EndSection();

        protected abstract void EndPbConfig();

        private bool NextSection(string name, string docTag)
        {
            EndSection();
            return BeginSection(name, docTag);
        }

        protected abstract void NoSource();

        protected abstract void TransformSection();
        protected abstract void OptionParameter();
        protected abstract void OptionIsAnimated();

        protected abstract void UnsupportedPbVersion();

        protected abstract void PbVersionProp([NotNull] string label,
            [NotNull] string pbPropName,
            [NotNull] SerializedProperty overridePropName,
            [ItemNotNull] [NotNull] params SerializedProperty[] overrides);

        protected abstract void PbProp([NotNull] string label,
            [NotNull] string pbPropName,
            [NotNull] SerializedProperty overridePropName,
            [ItemNotNull] [NotNull] params SerializedProperty[] overrides);

        protected abstract void PbCurveProp([NotNull] string label,
            [NotNull] string pbPropName,
            [NotNull] string pbCurvePropName,
            [NotNull] SerializedProperty overridePropName,
            [ItemNotNull] [NotNull] params SerializedProperty[] overrides);

        protected abstract void PbPermissionProp([NotNull] string label,
            [NotNull] string pbPropName,
            [NotNull] string pbFilterPropName,
            [NotNull] SerializedProperty overridePropName,
            [ItemNotNull] [NotNull] params SerializedProperty[] overrides);

        protected abstract void Pb3DCurveProp([NotNull] string label,
            [NotNull] string pbPropName,
            [NotNull] string pbXCurveLabel, [NotNull] string pbXCurvePropName,
            [NotNull] string pbYCurveLabel, [NotNull] string pbYCurvePropName,
            [NotNull] string pbZCurveLabel, [NotNull] string pbZCurvePropName,
            [NotNull] SerializedProperty overridePropName,
            [ItemNotNull] [NotNull] params SerializedProperty[] overrides);

        protected abstract void ColliderProp([NotNull] string label,
            [NotNull] string pbProp,
            [NotNull] SerializedProperty overrideProp);
    }

    sealed class MergePhysBoneEditorRenderer : MergePhysBoneEditorModificationUtils
    {
        public MergePhysBoneEditorRenderer(SerializedObject serializedObject) : base(serializedObject)
        {
        }

        private readonly Dictionary<string, bool> _sectionFolds = new Dictionary<string, bool>();

        protected override void BeginPbConfig()
        {
            Utils.HorizontalLine();
        }

        protected override bool BeginSection(string name, string docTag) {
            if (!_sectionFolds.TryGetValue(name, out var open)) open = true;
            var rect = GUILayoutUtility.GetRect(EditorGUIUtility.fieldWidth, EditorGUIUtility.fieldWidth, 18f, 18f, EditorStyles.foldoutHeader);
            var (foldout, button) = SplitRect(rect, OverrideWidth);
            open = EditorGUI.Foldout(foldout, open, name, EditorStyles.foldoutHeader);
            _sectionFolds[name] = open;
            if (GUI.Button(button, "?"))
                Application.OpenURL("https://docs.vrchat.com/docs/physbones#" + docTag);
            EditorGUI.indentLevel++;
            return open;
        }

        protected override void EndSection() {
            EditorGUI.indentLevel--;
        }

        protected override void EndPbConfig()
        {
        }

        protected override void UnsupportedPbVersion()
        {
            EditorGUILayout.HelpBox(CL4EE.Tr("MergePhysBone:error:unsupportedPbVersion"), MessageType.Error);
        }

        protected override void NoSource() {
            EditorGUILayout.HelpBox(CL4EE.Tr("MergePhysBone:error:noSources"), MessageType.Error);
        }

        protected override void TransformSection() {
            EditorGUILayout.LabelField("Root Transform", "Auto Generated");
            EditorGUILayout.LabelField("Ignore Transforms", "Automatically Merged");
            EditorGUILayout.LabelField("Endpoint Position", "Cleared to zero");
            EditorGUILayout.LabelField("Multi Child Type", "Must be Ignore");
            var multiChildType = _sourcePhysBone.FindProperty("multiChildType");
            if (multiChildType.enumValueIndex != 0 || multiChildType.hasMultipleDifferentValues)
                EditorGUILayout.HelpBox(CL4EE.Tr("MergePhysBone:error:multiChildType"), MessageType.Error);
        }
        protected override void OptionParameter() {
            EditorGUILayout.PropertyField(_mergedPhysBone.FindProperty("parameter"));
            EditorGUILayout.HelpBox("See VRCPhysBone editor's text OR docs for more info about Parameter.",
                MessageType.Info);
        }
        protected override void OptionIsAnimated() {
            EditorGUILayout.PropertyField(_isAnimatedProp);
        }

        const float OverrideWidth = 30f;
        const float CurveButtonWidth = 20f;

        private (Rect restRect, Rect fixedRect) SplitRect(Rect propRect, float width)
        {
            var restRect = propRect;
            restRect.width -= EditorGUIUtility.standardVerticalSpacing + width;
            var fixedRect = propRect;
            fixedRect.x = restRect.xMax + EditorGUIUtility.standardVerticalSpacing;
            fixedRect.width = width;
            return (restRect, fixedRect);
        }

        private (Rect restRect, Rect fixedRect0, Rect fixedRect1) SplitRect(Rect propRect, float width0, float width1)
        {
            var (tmp, fixedRect1) = SplitRect(propRect, width1);
            var (restRect, fixedRect0) = SplitRect(tmp, width0);
            return (restRect, fixedRect0, fixedRect1);
        }

        
        bool IsCurveWithValue(SerializedProperty prop) =>
            prop.animationCurveValue != null && prop.animationCurveValue.length > 0;

        protected override void PbVersionProp(string label, 
            string pbPropName, 
            SerializedProperty overrideProp,
            params SerializedProperty[] overrides)
        {
            var labelContent = new GUIContent(label);
            var forceOverride = overrides.Any(x => x.boolValue);

            var (valueRect, buttonRect, overrideRect) =
                SplitRect(EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight), CurveButtonWidth,
                    OverrideWidth);

            if (forceOverride || overrideProp.boolValue)
            {
                // Override mode

                renderer(_mergedPhysBone);

                if (forceOverride)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    PopupNoIndent(overrideRect, 1, CopyOverride);
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    EditorGUI.BeginProperty(overrideRect, null, overrideProp);
                    var selected = PopupNoIndent(overrideRect, 1, CopyOverride);
                    if (selected != 1)
                        overrideProp.boolValue = false;
                    EditorGUI.EndProperty();
                }
            }
            else
            {
                // Copy mode
                EditorGUI.BeginDisabledGroup(true);
                var differ = renderer(_sourcePhysBone);
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginProperty(overrideRect, null, overrideProp);
                var selected = PopupNoIndent(overrideRect, 0, CopyOverride);
                if (selected != 0)
                    overrideProp.boolValue = true;
                EditorGUI.EndProperty();

                if (differ)
                {
                    EditorGUILayout.HelpBox(
                        "The value is differ between two or more sources. " +
                        "You have to set same value OR override this property", 
                        MessageType.Error);
                }
            }
            
            const string docURL = "https://docs.vrchat.com/docs/physbones#versions";

            if (GUI.Button(buttonRect, "?"))
            {
                Application.OpenURL(docURL);
            }

            bool renderer(SerializedObject obj)
            {
                var prop = obj.FindProperty(pbPropName);
                var prevValue = prop.enumValueIndex;
                EditorGUI.PropertyField(valueRect, prop, labelContent);
                var newValue = prop.enumValueIndex;
                if (prevValue != newValue)
                {
                    switch (EditorUtility.DisplayDialogComplex(
                                CL4EE.Tr("MergePhysBone:dialog:versionInfo:title"), 
                                CL4EE.Tr("MergePhysBone:dialog:versionInfo:message"),
                                CL4EE.Tr("MergePhysBone:dialog:versionInfo:openDoc"), 
                                CL4EE.Tr("MergePhysBone:dialog:versionInfo:revert"),
                                CL4EE.Tr("MergePhysBone:dialog:versionInfo:continue")))
                    {
                        case 0:
                            Application.OpenURL(docURL);
                            break;
                        case 1:
                            prop.enumValueIndex = prevValue;
                            break;
                        case 2:
                            prop.enumValueIndex = newValue;
                            break;
                    }
                }
                return prop.hasMultipleDifferentValues;
            }
        }


        protected override void PbProp(string label, 
            string pbPropName, 
            SerializedProperty overridePropName,
            params SerializedProperty[] overrides)
        {
            PbPropImpl(label, overridePropName, overrides, (valueRect, obj, labelContent) =>
            {
                var prop = obj.FindProperty(pbPropName);
                EditorGUI.PropertyField(valueRect, prop, labelContent);
                return prop.hasMultipleDifferentValues;
            });
        }

        private void DrawCurveFieldWithButton(SerializedProperty curveProp, Rect buttonRect, Func<Rect> curveRect)
        {
            if (IsCurveWithValue(curveProp))
            {
                if (GUI.Button(buttonRect, "X"))
                {
                    curveProp.animationCurveValue = new AnimationCurve();
                }

                var rect = curveRect();
                EditorGUI.BeginProperty(rect, null, curveProp);
                EditorGUI.BeginChangeCheck();
                var cur = EditorGUI.CurveField(rect, " ", curveProp.animationCurveValue, Color.cyan,
                    new Rect(0.0f, 0.0f, 1f, 1f));
                if (EditorGUI.EndChangeCheck())
                    curveProp.animationCurveValue = cur;
                EditorGUI.EndProperty();
            }
            else
            {
                if (GUI.Button(buttonRect, "C"))
                {
                    var curve = new AnimationCurve();
                    curve.AddKey(new Keyframe(0.0f, 1f));
                    curve.AddKey(new Keyframe(1f, 1f));
                    curveProp.animationCurveValue = curve;
                }
            }
        }

        protected override void PbCurveProp(string label,
            string pbPropName,
            string pbCurvePropName,
            SerializedProperty overridePropName,
            params SerializedProperty[] overrides)
        {
            PbPropImpl(label, overridePropName, overrides, (rect, obj, labelContent) =>
            {
                var (valueRect, buttonRect) = SplitRect(rect, CurveButtonWidth);

                var valueProp = obj.FindProperty(pbPropName);
                var curveProp = obj.FindProperty(pbCurvePropName);

                EditorGUI.PropertyField(valueRect, valueProp, labelContent);
                DrawCurveFieldWithButton(curveProp, buttonRect, 
                    () => SplitRect(EditorGUILayout.GetControlRect(), OverrideWidth).restRect);

                return valueProp.hasMultipleDifferentValues || curveProp.hasMultipleDifferentValues;
            });
        }

        protected override void PbPermissionProp(string label,
            string pbPropName,
            string pbFilterPropName,
            SerializedProperty overridePropName,
            params SerializedProperty[] overrides)
        {
            PbPropImpl(label, overridePropName, overrides, (rect, obj, labelContent) =>
            {
                var valueProp = obj.FindProperty(pbPropName);

                EditorGUI.PropertyField(rect, valueProp, labelContent);
                if (valueProp.enumValueIndex == 2)
                {
                    var filterProp = obj.FindProperty(pbFilterPropName);
                    var allowSelf = filterProp.FindPropertyRelative("allowSelf");
                    var allowOthers = filterProp.FindPropertyRelative("allowOthers");
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(allowSelf);
                    EditorGUILayout.PropertyField(allowOthers);
                    EditorGUI.indentLevel--;
                    return valueProp.hasMultipleDifferentValues || filterProp.hasMultipleDifferentValues;
                }
                else
                {
                    return valueProp.hasMultipleDifferentValues;
                }
            });
        }

        protected override void Pb3DCurveProp(string label,
            string pbPropName,
            string pbXCurveLabel, string pbXCurvePropName,
            string pbYCurveLabel, string pbYCurvePropName,
            string pbZCurveLabel, string pbZCurvePropName,
            SerializedProperty overridePropName,
            params SerializedProperty[] overrides)
        {
            PbPropImpl(label, overridePropName, overrides, (rect, obj, labelContent) =>
            {
                var (valueRect, buttonRect) = SplitRect(rect, CurveButtonWidth);

                var valueProp = obj.FindProperty(pbPropName);
                var xCurveProp = obj.FindProperty(pbXCurvePropName);
                var yCurveProp = obj.FindProperty(pbYCurvePropName);
                var zCurveProp = obj.FindProperty(pbZCurvePropName);

                void DrawCurve(string curveLabel, SerializedProperty curveProp)
                {
                    var (curveRect, curveButtonRect, _) = SplitRect(EditorGUILayout.GetControlRect(true),
                        CurveButtonWidth, OverrideWidth);

                    EditorGUI.LabelField(curveRect, curveLabel, " ");
                    DrawCurveFieldWithButton(curveProp, curveButtonRect, () => curveRect);
                }

                if (IsCurveWithValue(xCurveProp) || IsCurveWithValue(yCurveProp) || IsCurveWithValue(zCurveProp))
                {
                    // with curve
                    EditorGUI.PropertyField(valueRect, valueProp, labelContent);
                    DrawCurve(pbXCurveLabel, xCurveProp);
                    DrawCurve(pbYCurveLabel, yCurveProp);
                    DrawCurve(pbZCurveLabel, zCurveProp);
                }
                else
                {
                    // without curve: constant
                    EditorGUI.PropertyField(valueRect, valueProp, labelContent);
                    
                    if (GUI.Button(buttonRect, "C"))
                    {
                        var curve = new AnimationCurve();
                        curve.AddKey(new Keyframe(0.0f, 1f));
                        curve.AddKey(new Keyframe(1f, 1f));
                        xCurveProp.animationCurveValue = curve;
                        yCurveProp.animationCurveValue = curve;
                        zCurveProp.animationCurveValue = curve;
                    }
                }

                return valueProp.hasMultipleDifferentValues
                       || xCurveProp.hasMultipleDifferentValues
                       || yCurveProp.hasMultipleDifferentValues
                       || zCurveProp.hasMultipleDifferentValues;
            });
        }

        private static readonly string[] CopyOverride = { "C:Copy", "O:Override" };

        private void PbPropImpl([NotNull] string label, 
            [NotNull] SerializedProperty overrideProp, 
            [ItemNotNull] [NotNull] SerializedProperty[] overrides, 
            [NotNull] Func<Rect, SerializedObject, GUIContent, bool> renderer)
        {
            var labelContent = new GUIContent(label);
            var forceOverride = overrides.Any(x => x.boolValue);

            var (valueRect, overrideRect) =
                SplitRect(EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight), OverrideWidth);

            if (forceOverride || overrideProp.boolValue)
            {
                // Override mode

                renderer(valueRect, _mergedPhysBone, labelContent);

                if (forceOverride)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    PopupNoIndent(overrideRect, 1, CopyOverride);
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    EditorGUI.BeginProperty(overrideRect, null, overrideProp);
                    var selected = PopupNoIndent(overrideRect, 1, CopyOverride);
                    if (selected != 1)
                        overrideProp.boolValue = false;
                    EditorGUI.EndProperty();
                }
            }
            else
            {
                // Copy mode
                EditorGUI.BeginDisabledGroup(true);
                var differ = renderer(valueRect, _sourcePhysBone, labelContent);
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginProperty(overrideRect, null, overrideProp);
                var selected = PopupNoIndent(overrideRect, 0, CopyOverride);
                if (selected != 0)
                    overrideProp.boolValue = true;
                EditorGUI.EndProperty();

                if (differ)
                {
                    EditorGUILayout.HelpBox(
                        "The value is differ between two or more sources. " +
                        "You have to set same value OR override this property", 
                        MessageType.Error);
                }
            }
        }

        protected override void ColliderProp(string label, string pbProp, SerializedProperty overrideProp)
        {
            var labelContent = new GUIContent(label);

            Rect valueRect, overrideRect;

            switch ((CollidersSettings)overrideProp.enumValueIndex)
            {
                case CollidersSettings.Copy:
                {
                    Debug.Assert(_sourcePhysBone != null, nameof(_sourcePhysBone) + " != null");
                    var colliders = _sourcePhysBone.FindProperty(pbProp);

                    var height = EditorGUI.GetPropertyHeight(colliders, null, true);

                    (valueRect, overrideRect) = SplitRect(EditorGUILayout.GetControlRect(true, height), OverrideWidth);

                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUI.PropertyField(valueRect, colliders, labelContent, true);
                    EditorGUI.EndDisabledGroup();

                    if (colliders.hasMultipleDifferentValues)
                    {
                        EditorGUILayout.HelpBox(
                            "The value is differ between two or more sources. " +
                            "You have to set same value OR override this property",
                            MessageType.Error);
                    }
                }
                    break;
                case CollidersSettings.Merge:
                {
                    (valueRect, overrideRect) =
                        SplitRect(EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight), OverrideWidth);

                    var colliders = _componentsSetEditorUtil.Values.SelectMany(x => x.colliders).Distinct().ToList();
                    var mergedProp = _mergedPhysBone.FindProperty(pbProp);
                    EditorGUI.BeginDisabledGroup(true);
                    mergedProp.isExpanded = EditorGUI.Foldout(valueRect, mergedProp.isExpanded, labelContent);
                    if (mergedProp.isExpanded)
                    {
                        EditorGUILayout.IntField("Size", colliders.Count);
                        for (var i = 0; i < colliders.Count; i++)
                            EditorGUILayout.ObjectField($"Element {i}", colliders[i], typeof(VRCPhysBoneColliderBase),
                                true);
                    }

                    EditorGUI.EndDisabledGroup();
                }
                    break;
                case CollidersSettings.Override:
                {
                    var colliders = _mergedPhysBone.FindProperty(pbProp);

                    var height = EditorGUI.GetPropertyHeight(colliders, null, true);

                    (valueRect, overrideRect) = SplitRect(EditorGUILayout.GetControlRect(true, height), OverrideWidth);

                    EditorGUI.PropertyField(valueRect, colliders, labelContent, true);
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            EditorGUI.BeginProperty(overrideRect, null, overrideProp);
            var selected = PopupNoIndent(overrideRect, overrideProp.enumValueIndex, overrideProp.enumDisplayNames);
            if (selected != 0) overrideProp.enumValueIndex = selected;
            EditorGUI.EndProperty();
        }

        private static int PopupNoIndent(Rect position, int selectedIndex, string[] displayedOptions)
        {
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            var result = EditorGUI.Popup(position, selectedIndex, displayedOptions);
            EditorGUI.indentLevel = indent;
            return result;
        }
    }
}
