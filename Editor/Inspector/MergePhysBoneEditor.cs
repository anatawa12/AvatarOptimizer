#if AAO_VRCSDK3_AVATARS

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(MergePhysBone))]
    internal class MergePhysBoneEditor : AvatarTagComponentEditorBase
    {
        private MergePhysBoneEditorRenderer _renderer = null!; // initialized in OnEnable
        private SerializedProperty _makeParent = null!; // initialized in OnEnable
        private SerializedProperty _componentsSetProp = null!; // initialized in OnEnable

        private void OnEnable()
        {
            _renderer = new MergePhysBoneEditorRenderer(serializedObject);
            _makeParent = serializedObject.FindProperty("makeParent");
            _componentsSetProp = serializedObject.FindProperty(nameof(MergePhysBone.componentsSet));
        }

        protected override void OnInspectorGUIInner()
        {
            EditorGUILayout.PropertyField(_makeParent);
            if (_makeParent.boolValue && ((Component)target).transform.childCount != 0)
                EditorGUILayout.HelpBox(AAOL10N.Tr("MergePhysBone:error:makeParentWithChildren"), MessageType.Error);

            EditorGUILayout.PropertyField(_componentsSetProp);

            // on DragPerform, in DoProcess, new HelpBox invocation throws ExitGUIException
            // so I ApplyModifiedProperties here.
            serializedObject.ApplyModifiedProperties();

            // draw custom editor
            _renderer.DoProcess();

            serializedObject.ApplyModifiedProperties();
        }
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
            EditorGUILayout.HelpBox(AAOL10N.Tr("MergePhysBone:error:unsupportedPbVersion"), MessageType.Error);
        }

        protected override void NoSource() {
            EditorGUILayout.HelpBox(AAOL10N.Tr("MergePhysBone:error:noSources"), MessageType.Error);
        }

        protected override void TransformSection() {
            EditorGUILayout.LabelField("Root Transform", "Auto Generated");
            if (!MakeParent.boolValue)
            {
                var differ = SourcePhysBones
                    .Select(x => x.GetTarget().parent)
                    .ZipWithNext()
                    .Any(x => x.Item1 != x.Item2);
                if (differ)
                    EditorGUILayout.HelpBox(AAOL10N.Tr("MergePhysBone:error:parentDiffer"), MessageType.Error);
            }
            EditorGUILayout.LabelField("Ignore Transforms", "Automatically Merged");
            EndpointPositionProp("Endpoint Position", EndpointPosition);
#if AAO_VRCSDK3_AVATARS_IGNORE_OTHER_PHYSBONE
            PbProp("Ignore Other Phys Bones", IgnoreOtherPhysBones);
#endif
            EditorGUILayout.LabelField("Multi Child Type", "Must be Ignore");
            var multiChildType = GetSourceProperty("multiChildType");
            if (multiChildType.enumValueIndex != 0 || multiChildType.hasMultipleDifferentValues)
                EditorGUILayout.HelpBox(AAOL10N.Tr("MergePhysBone:error:multiChildType"), MessageType.Error);
        }
        protected override void OptionParameter() {
            EditorGUILayout.PropertyField(Parameter.OverrideValue, new GUIContent("Parameter"));
            EditorGUILayout.HelpBox("See VRCPhysBone editor's text OR docs for more info about Parameter.",
                MessageType.Info);
        }
        protected override void OptionIsAnimated() {
            EditorGUILayout.PropertyField(IsAnimated.OverrideValue, new GUIContent("Is Animated"));
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
            ValueConfigProp prop, bool forceOverride = false)
        {
            var labelContent = new GUIContent(label);

            var (valueRect, buttonRect, overrideRect) =
                SplitRect(EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight), CurveButtonWidth,
                    OverrideWidth);

            if (forceOverride || prop.IsOverride)
            {
                // Override mode

                renderer(prop.OverrideValue);

                if (forceOverride)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    PopupNoIndent(overrideRect, 1, CopyOverride);
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    EditorGUI.BeginProperty(overrideRect, null, prop.IsOverrideProperty);
                    var selected = PopupNoIndent(overrideRect, 1, CopyOverride);
                    if (selected != 1)
                        prop.IsOverrideProperty.boolValue = false;
                    EditorGUI.EndProperty();
                }
            }
            else
            {
                // Copy mode
                EditorGUI.BeginDisabledGroup(true);
                var differ = renderer(prop.SourceValue!);
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginProperty(overrideRect, null, prop.IsOverrideProperty);
                var selected = PopupNoIndent(overrideRect, 0, CopyOverride);
                if (selected != 0)
                    prop.IsOverrideProperty.boolValue = true;
                EditorGUI.EndProperty();

                if (differ)
                {
                    EditorGUILayout.HelpBox(AAOL10N.Tr("MergePhysBone:error:differValueSingle"), MessageType.Error);
                }
            }
            
            const string docURL = "https://docs.vrchat.com/docs/physbones#versions";

            if (GUI.Button(buttonRect, "?"))
            {
                Application.OpenURL(docURL);
            }

            bool renderer(SerializedProperty property)
            {
                var prevValue = property.enumValueIndex;
                EditorGUI.PropertyField(valueRect, property, labelContent);
                var newValue = property.enumValueIndex;
                if (prevValue != newValue)
                {
                    switch (EditorUtility.DisplayDialogComplex(
                                AAOL10N.Tr("MergePhysBone:dialog:versionInfo:title"), 
                                AAOL10N.Tr("MergePhysBone:dialog:versionInfo:message"),
                                AAOL10N.Tr("MergePhysBone:dialog:versionInfo:openDoc"), 
                                AAOL10N.Tr("MergePhysBone:dialog:versionInfo:revert"),
                                AAOL10N.Tr("MergePhysBone:dialog:versionInfo:continue")))
                    {
                        case 0:
                            Application.OpenURL(docURL);
                            break;
                        case 1:
                            property.enumValueIndex = prevValue;
                            break;
                        case 2:
                            property.enumValueIndex = newValue;
                            break;
                    }
                }
                return property.hasMultipleDifferentValues;
            }
        }


        protected override void PbProp(string label, ValueConfigProp prop, bool forceOverride = false)
        {
            PbPropImpl(label, prop, forceOverride, (valueRect, merged, labelContent) =>
            {
                var property = prop.GetValueProperty(merged);
                EditorGUI.PropertyField(valueRect, property, labelContent);
                return property.hasMultipleDifferentValues;
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

        protected override void PbCurveProp(string label, CurveConfigProp prop, bool forceOverride = false)
        {
            PbPropImpl(label, prop, forceOverride, (rect, merged, labelContent) =>
            {
                var (valueRect, buttonRect) = SplitRect(rect, CurveButtonWidth);

                var valueProp = prop.GetValueProperty(merged);
                var curveProp = prop.GetCurveProperty(merged);

                EditorGUI.PropertyField(valueRect, valueProp, labelContent);
                DrawCurveFieldWithButton(curveProp, buttonRect, 
                    () => SplitRect(EditorGUILayout.GetControlRect(), OverrideWidth).restRect);

                return valueProp.hasMultipleDifferentValues || curveProp.hasMultipleDifferentValues;
            });
        }

        protected override void PbPermissionProp(string label, PermissionConfigProp prop, bool forceOverride = false)
        {
            PbPropImpl(label, prop, forceOverride, (rect, merged, labelContent) =>
            {
                var valueProp = prop.GetValueProperty(merged);

                EditorGUI.PropertyField(rect, valueProp, labelContent);
                if (valueProp.enumValueIndex == 2)
                {
                    var filterProp = prop.GetFilterProperty(merged);
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
            string pbXCurveLabel, string pbYCurveLabel, string pbZCurveLabel, 
            CurveVector3ConfigProp prop, bool forceOverride = false)
        {
            var (rect, overrideRect) = SplitRect(EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight), OverrideWidth);

            switch (prop.GetOverride(forceOverride))
            {
                case MergePhysBone.LimitRotationConfig.CurveOverride.Copy:
                {
                    var valueProp = prop.SourceValue!;
                    var xCurveProp = prop.SourceCurveX!;
                    var yCurveProp = prop.SourceCurveY!;
                    var zCurveProp = prop.SourceCurveZ!;

                    EditorGUI.BeginDisabledGroup(true);
                    DrawProperties(rect, new GUIContent(label), valueProp, xCurveProp, yCurveProp, zCurveProp);
                    EditorGUI.EndDisabledGroup();

                    if (valueProp.hasMultipleDifferentValues
                        || xCurveProp.hasMultipleDifferentValues
                        || yCurveProp.hasMultipleDifferentValues
                        || zCurveProp.hasMultipleDifferentValues)
                    {
                        EditorGUILayout.HelpBox(AAOL10N.Tr("MergePhysBone:error:differValueSingle"), MessageType.Error);
                    }
                }
                    break;
                case MergePhysBone.LimitRotationConfig.CurveOverride.Override:
                {
                    var valueProp = prop.OverrideValue;
                    var xCurveProp = prop.OverrideCurveX;
                    var yCurveProp = prop.OverrideCurveY;
                    var zCurveProp = prop.OverrideCurveZ;

                    DrawProperties(rect, new GUIContent(label), valueProp, xCurveProp, yCurveProp, zCurveProp);
                }
                    break;
                case MergePhysBone.LimitRotationConfig.CurveOverride.Fix:
                {
                    EditorGUI.LabelField(rect, label, AAOL10N.Tr("MergePhysBone:message:fix-yaw-pitch"));
                    
                    if (SourcePhysBones.Any())
                    {
                        foreach (var physBone in SourcePhysBones)
                            physBone.InitTransforms(force: false);

                        // skew scaling is disallowed
                        if (MergePhysBoneValidator.SkewBones(SourcePhysBones) is { Count: > 0 })
                            EditorGUILayout.HelpBox(AAOL10N.Tr("MergePhysBone:error:LimitRotationFix:SkewScaling"), MessageType.Error);

                        // error if there is different limit / rotation
                        if (MergePhysBoneValidator.HasDifferentYawPitch(SourcePhysBones))
                            EditorGUILayout.HelpBox(AAOL10N.Tr("MergePhysBone:error:LimitRotationFix:DifferRotation"), MessageType.Error);

                        // endpoint position must be zero
                        switch ((MergePhysBone.EndPointPositionConfig.Override)EndpointPosition.OverrideProperty.enumValueIndex)
                        {
                            case MergePhysBone.EndPointPositionConfig.Override.Copy when EndpointPosition.PhysBoneValue!.vector3Value != Vector3.zero:
                            case MergePhysBone.EndPointPositionConfig.Override.Override when EndpointPosition.ValueProperty.vector3Value != Vector3.zero:
                                EditorGUILayout.HelpBox(AAOL10N.Tr("MergePhysBone:error:LimitRotationFix:NonZeroEndpointPosition"), MessageType.Error);
                                break;
                        }
                    }
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            EditorGUI.BeginProperty(overrideRect, null, prop.OverrideProperty);
            var selected = PopupNoIndent(overrideRect, prop.OverrideProperty.enumValueIndex, prop.OverrideProperty.enumDisplayNames);
            if (selected != prop.OverrideProperty.enumValueIndex)
                prop.OverrideProperty.enumValueIndex = selected;
            EditorGUI.EndProperty();

            void DrawProperties(Rect rect, GUIContent labelContent, SerializedProperty valueProp, SerializedProperty xCurveProp, SerializedProperty yCurveProp, SerializedProperty zCurveProp)
            {
                var (valueRect, buttonRect) = SplitRect(rect, CurveButtonWidth);

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
            }
        }

        private static readonly string[] CopyOverride = { "C:Copy", "O:Override" };

        private void PbPropImpl(string label, 
            OverridePropBase prop, 
            bool forceOverride, 
            Func<Rect, bool, GUIContent, bool> renderer)
        {
            var labelContent = new GUIContent(label);

            var (valueRect, overrideRect) =
                SplitRect(EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight), OverrideWidth);

            if (forceOverride || prop.IsOverride)
            {
                // Override mode

                renderer(valueRect, true, labelContent);

                if (forceOverride)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    PopupNoIndent(overrideRect, 1, CopyOverride);
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    EditorGUI.BeginProperty(overrideRect, null, prop.IsOverrideProperty);
                    var selected = PopupNoIndent(overrideRect, 1, CopyOverride);
                    if (selected != 1)
                        prop.IsOverrideProperty.boolValue = false;
                    EditorGUI.EndProperty();
                }
            }
            else
            {
                // Copy mode
                EditorGUI.BeginDisabledGroup(true);
                var differ = renderer(valueRect, false, labelContent);
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginProperty(overrideRect, null, prop.IsOverrideProperty);
                var selected = PopupNoIndent(overrideRect, 0, CopyOverride);
                if (selected != 0)
                    prop.IsOverrideProperty.boolValue = true;
                EditorGUI.EndProperty();

                if (differ)
                {
                    EditorGUILayout.HelpBox(AAOL10N.Tr("MergePhysBone:error:differValueSingle"), MessageType.Error);
                }
            }
        }

        protected override void CollidersProp(string label, CollidersConfigProp prop)
        {
            var labelContent = new GUIContent(label);

            Rect valueRect, overrideRect;

            switch ((MergePhysBone.CollidersConfig.CollidersOverride)prop.OverrideProperty.enumValueIndex)
            {
                case MergePhysBone.CollidersConfig.CollidersOverride.Copy:
                {
                    var colliders = prop.PhysBoneValue!;

                    var height = EditorGUI.GetPropertyHeight(colliders, null, true);

                    (valueRect, overrideRect) = SplitRect(EditorGUILayout.GetControlRect(true, height), OverrideWidth);

                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUI.PropertyField(valueRect, colliders, labelContent, true);
                    EditorGUI.EndDisabledGroup();

                    if (colliders.hasMultipleDifferentValues)
                    {
                        EditorGUILayout.HelpBox(AAOL10N.Tr("MergePhysBone:error:differValueSingle"), MessageType.Error);
                    }
                }
                    break;
                case MergePhysBone.CollidersConfig.CollidersOverride.Merge:
                {
                    (valueRect, overrideRect) =
                        SplitRect(EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight), OverrideWidth);

                    var colliders = ComponentsSetEditorUtil.Values.SelectMany(x => x.colliders).Distinct().ToList();
                    var mergedProp = prop.ValueProperty;
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
                case MergePhysBone.CollidersConfig.CollidersOverride.Override:
                {
                    var colliders = prop.ValueProperty;

                    var height = EditorGUI.GetPropertyHeight(colliders, null, true);

                    (valueRect, overrideRect) = SplitRect(EditorGUILayout.GetControlRect(true, height), OverrideWidth);

                    EditorGUI.PropertyField(valueRect, colliders, labelContent, true);
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            EditorGUI.BeginProperty(overrideRect, null, prop.OverrideProperty);
            var selected = PopupNoIndent(overrideRect, prop.OverrideProperty.enumValueIndex, prop.OverrideProperty.enumDisplayNames);
            if (selected != prop.OverrideProperty.enumValueIndex)
                prop.OverrideProperty.enumValueIndex = selected;
            EditorGUI.EndProperty();
        }

        private void EndpointPositionProp(string label, EndpointPositionConfigProp prop)
        {
            var labelContent = new GUIContent(label);

            Rect valueRect, overrideRect;

            switch ((MergePhysBone.EndPointPositionConfig.Override)prop.OverrideProperty.enumValueIndex)
            {
                case MergePhysBone.EndPointPositionConfig.Override.Clear:
                {
                    (valueRect, overrideRect) =
                        SplitRect(EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight), OverrideWidth);

                    EditorGUI.LabelField(valueRect, labelContent, new GUIContent("Cleared to zero"));
                }
                    break;
                case MergePhysBone.EndPointPositionConfig.Override.Copy:
                {
                    var valueProperty = prop.PhysBoneValue!;

                    var height = EditorGUI.GetPropertyHeight(valueProperty, null, true);

                    (valueRect, overrideRect) = SplitRect(EditorGUILayout.GetControlRect(true, height), OverrideWidth);

                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUI.PropertyField(valueRect, valueProperty, labelContent, true);
                    EditorGUI.EndDisabledGroup();

                    if (valueProperty.hasMultipleDifferentValues)
                    {
                        EditorGUILayout.HelpBox(AAOL10N.Tr("MergePhysBone:error:differValueSingle"), MessageType.Error);
                    }
                }
                    break;
                case MergePhysBone.EndPointPositionConfig.Override.Override:
                {
                    var valueProperty = prop.ValueProperty;

                    var height = EditorGUI.GetPropertyHeight(valueProperty, null, true);

                    (valueRect, overrideRect) = SplitRect(EditorGUILayout.GetControlRect(true, height), OverrideWidth);

                    EditorGUI.PropertyField(valueRect, valueProperty, labelContent, true);
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            EditorGUI.BeginProperty(overrideRect, null, prop.OverrideProperty);
            var selected = PopupNoIndent(overrideRect, prop.OverrideProperty.enumValueIndex, prop.OverrideProperty.enumDisplayNames);
            if (selected != prop.OverrideProperty.enumValueIndex)
                prop.OverrideProperty.enumValueIndex = selected;
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

    sealed class MergePhysBoneValidator : MergePhysBoneEditorModificationUtils
    {
        private readonly List<string> _differProps = new List<string>();
        private readonly MergePhysBone _mergePhysBone;
        private bool _usingCopyCurve;

        internal static void Validate(MergePhysBone mergePhysBone)
        {
            if (mergePhysBone.makeParent && mergePhysBone.transform.childCount != 0)
                BuildLog.LogError("MergePhysBone:error:makeParentWithChildren", mergePhysBone);

            new MergePhysBoneValidator(mergePhysBone).DoProcess();
        }

        public MergePhysBoneValidator(MergePhysBone mergePhysBone) : base(new SerializedObject(mergePhysBone)) =>
            _mergePhysBone = mergePhysBone;

        private static void Void()
        {
        }

        protected override void BeginPbConfig()
        {
            if (SourcePhysBones.Count() <= 1)
                BuildLog.LogError("MergePhysBone:error:oneSource");

            foreach (var vrcPhysBoneBase in SourcePhysBones)
                vrcPhysBoneBase.InitTransforms(true);
        }

        protected override bool BeginSection(string name, string docTag) => true;
        protected override void EndSection() => Void();
        protected override void EndPbConfig() {
            if (_differProps.Count != 0)
            {
                BuildLog.LogError("MergePhysBone:error:differValues", string.Join(", ", _differProps));
            }
            
            if (_usingCopyCurve)
            {
                var maxLength = SourcePhysBones.Max(x => x.BoneChainLength());
                if (SourcePhysBones.Any(x => x.BoneChainLength() != maxLength))
                    BuildLog.LogWarning("MergePhysBone:warning:differChainLength",
                        string.Join(", ", _differProps));
            }
        }

        protected override void NoSource() => BuildLog.LogError("MergePhysBone:error:noSources");

        protected override void TransformSection()
        {
            if (!_mergePhysBone.makeParent)
            {
                var differ = SourcePhysBones
                    .Select(x => x.GetTarget().parent)
                    .ZipWithNext()
                    .Any(x => x.Item1 != x.Item2);
                if (differ)
                    BuildLog.LogError("MergePhysBone:error:parentDiffer");
            }

            if (EndpointPosition.OverrideProperty.enumValueIndex ==
                (int)MergePhysBone.EndPointPositionConfig.Override.Copy)
            {
                if (EndpointPosition.PhysBoneValue!.hasMultipleDifferentValues)
                    _differProps.Add("Endpoint Position");
            }

            // we don't produce errors for IgnoreOtherPhysBones since we merge the value

            var multiChildType = GetSourceProperty(nameof(VRCPhysBoneBase.multiChildType));
            if (multiChildType.enumValueIndex != 0 || multiChildType.hasMultipleDifferentValues)
                BuildLog.LogError("MergePhysBone:error:multiChildType");
        }

        protected override void OptionParameter() => Void();
        protected override void OptionIsAnimated() => Void();

        protected override void UnsupportedPbVersion() => BuildLog.LogError("MergePhysBone:error:unsupportedPbVersion");

        protected override void PbVersionProp(string label, ValueConfigProp prop, bool forceOverride = false)
            => PbProp(label, prop, forceOverride);

        protected override void PbProp(string label, ValueConfigProp prop, bool forceOverride = false)
        {
            if (forceOverride || prop.IsOverride) return;

            if (prop.GetValueProperty(false).hasMultipleDifferentValues)
                _differProps.Add(label);
        }

        protected override void PbCurveProp(string label, CurveConfigProp prop, bool forceOverride = false)
        {
            if (forceOverride || prop.IsOverride) return;

            if (prop.GetValueProperty(false).hasMultipleDifferentValues
                || prop.GetCurveProperty(false).hasMultipleDifferentValues)
                _differProps.Add(label);

            _usingCopyCurve |= prop.GetCurveProperty(false).animationCurveValue.length > 0;
        }

        protected override void Pb3DCurveProp(string label,
            string pbXCurveLabel, string pbYCurveLabel, string pbZCurveLabel,
            CurveVector3ConfigProp prop, bool forceOverride = false)
        {
            switch (prop.GetOverride(forceOverride))
            {
                case MergePhysBone.LimitRotationConfig.CurveOverride.Copy:
                    if (prop.SourceValue!.hasMultipleDifferentValues
                        || prop.SourceCurveX!.hasMultipleDifferentValues
                        || prop.SourceCurveY!.hasMultipleDifferentValues
                        || prop.SourceCurveZ!.hasMultipleDifferentValues)
                        _differProps.Add(label);

                    _usingCopyCurve |= prop.SourceCurveX!.animationCurveValue.length > 0;
                    _usingCopyCurve |= prop.SourceCurveY!.animationCurveValue.length > 0;
                    _usingCopyCurve |= prop.SourceCurveZ!.animationCurveValue.length > 0;
                    break;
                case MergePhysBone.LimitRotationConfig.CurveOverride.Override:
                    break;
                case MergePhysBone.LimitRotationConfig.CurveOverride.Fix:
                    if (SourcePhysBones.Any())
                    {
                        // skew scaling is disallowed
                        if (SkewBones(SourcePhysBones) is { Count: > 0 } skewBones)
                            BuildLog.LogError("MergePhysBone:error:LimitRotationFix:SkewScaling", skewBones);

                        // error if there is different limit / rotation
                        if (HasDifferentYawPitch(SourcePhysBones))
                            BuildLog.LogError("MergePhysBone:error:LimitRotationFix:DifferRotation");

                        // endpoint position must be zero
                        switch ((MergePhysBone.EndPointPositionConfig.Override)EndpointPosition.OverrideProperty.enumValueIndex)
                        {
                            case MergePhysBone.EndPointPositionConfig.Override.Copy when EndpointPosition.PhysBoneValue!.vector3Value != Vector3.zero:
                            case MergePhysBone.EndPointPositionConfig.Override.Override when EndpointPosition.ValueProperty.vector3Value != Vector3.zero:
                                BuildLog.LogError("MergePhysBone:error:LimitRotationFix:NonZeroEndpointPosition");
                                break;
                        }
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static List<Transform> SkewBones(IEnumerable<VRCPhysBoneBase> sourcePhysBones)
        {
            // skew scaling is disallowed
            return sourcePhysBones
                .SelectMany(x => x.GetAffectedTransforms())
                .Where(x => !Utils.ScaledEvenly(x.localScale))
                .ToList();
        }

        public static bool HasDifferentYawPitch(IEnumerable<VRCPhysBoneBase> sourcePhysBones)
        {
            var longestPhysBone = sourcePhysBones.MaxBy(x => x.BoneChainLength());

            var fixedRotations = Enumerable.Range(0, longestPhysBone.BoneChainLength())
                .Select(index =>
                {
                    var time = (float)index / longestPhysBone.BoneChainLength() - 1;

                    var rotation = longestPhysBone.CalcLimitRotation(time);

                    return Processors.MergePhysBoneProcessor.ConvertRotation(rotation)
                        with
                        {
                            y = 0
                        };
                })
                .ToList();

            var differRotation = sourcePhysBones
                .Any(physBone =>
                {
                    return Enumerable.Range(0, physBone.BoneChainLength()).Any(index =>
                    {
                        var time = (float)index / physBone.BoneChainLength() - 1;
                        var rotation = longestPhysBone.CalcLimitRotation(time);
                        var fixedRot = Processors.MergePhysBoneProcessor.ConvertRotation(rotation)with
                        {
                            y = 0
                        };

                        return fixedRot != fixedRotations[index];
                    });
                });

            return differRotation;
        }

        protected override void PbPermissionProp(string label, PermissionConfigProp prop, bool forceOverride = false)
        {
            if (forceOverride || prop.IsOverride) return;

            if (prop.SourceValue!.enumValueIndex == 2)
            {
                if (prop.SourceValue.hasMultipleDifferentValues
                    || prop.SourceFilter!.hasMultipleDifferentValues)
                    _differProps.Add(label);
            }
            else
            {
                if (prop.SourceValue.hasMultipleDifferentValues)
                    _differProps.Add(label);
            }
        }

        protected override void CollidersProp(string label, CollidersConfigProp prop)
        {
            // 0: copy
            if (prop.OverrideProperty.enumValueIndex == 0)
            {
                if (prop.PhysBoneValue!.hasMultipleDifferentValues)
                    _differProps.Add(label);
            }
        }
    }
}

#endif
