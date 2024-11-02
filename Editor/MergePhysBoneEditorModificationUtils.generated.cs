// <generated />
#nullable enable

#if AAO_VRCSDK3_AVATARS

using System.Collections.Generic;
using UnityEditor;
using JetBrains.Annotations;

namespace Anatawa12.AvatarOptimizer
{
    partial class MergePhysBoneEditorModificationUtils
    {
        protected partial class CurveConfigProp : OverridePropBase
        {
            public readonly SerializedProperty OverrideValue;
            public SerializedProperty? SourceValue { get; private set; }
            public readonly string PhysBoneValueName;
            public readonly SerializedProperty OverrideCurve;
            public SerializedProperty? SourceCurve { get; private set; }
            public readonly string PhysBoneCurveName;

            public CurveConfigProp(
                SerializedProperty rootProperty
                , string physBoneValueName
                , string physBoneCurveName
                ) : base(rootProperty)
            {
                OverrideValue = rootProperty.FindPropertyRelative("value");
                PhysBoneValueName = physBoneValueName;
                OverrideCurve = rootProperty.FindPropertyRelative("curve");
                PhysBoneCurveName = physBoneCurveName;
            }

            internal override void UpdateSource(SerializedObject sourcePb)
            {
                SourceValue = sourcePb.FindProperty(PhysBoneValueName);
                SourceCurve = sourcePb.FindProperty(PhysBoneCurveName);
            }
            public SerializedProperty GetValueProperty(bool @override) => @override ? OverrideValue : SourceValue!;
            public SerializedProperty GetCurveProperty(bool @override) => @override ? OverrideCurve : SourceCurve!;
        }
        protected partial class PermissionConfigProp : OverridePropBase
        {
            public readonly SerializedProperty OverrideValue;
            public SerializedProperty? SourceValue { get; private set; }
            public readonly string PhysBoneValueName;
            public readonly SerializedProperty OverrideFilter;
            public SerializedProperty? SourceFilter { get; private set; }
            public readonly string PhysBoneFilterName;

            public PermissionConfigProp(
                SerializedProperty rootProperty
                , string physBoneValueName
                , string physBoneFilterName
                ) : base(rootProperty)
            {
                OverrideValue = rootProperty.FindPropertyRelative("value");
                PhysBoneValueName = physBoneValueName;
                OverrideFilter = rootProperty.FindPropertyRelative("filter");
                PhysBoneFilterName = physBoneFilterName;
            }

            internal override void UpdateSource(SerializedObject sourcePb)
            {
                SourceValue = sourcePb.FindProperty(PhysBoneValueName);
                SourceFilter = sourcePb.FindProperty(PhysBoneFilterName);
            }
            public SerializedProperty GetValueProperty(bool @override) => @override ? OverrideValue : SourceValue!;
            public SerializedProperty GetFilterProperty(bool @override) => @override ? OverrideFilter : SourceFilter!;
        }
        protected partial class ValueConfigProp : OverridePropBase
        {
            public readonly SerializedProperty OverrideValue;
            public SerializedProperty? SourceValue { get; private set; }
            public readonly string PhysBoneValueName;

            public ValueConfigProp(
                SerializedProperty rootProperty
                , string physBoneValueName
                ) : base(rootProperty)
            {
                OverrideValue = rootProperty.FindPropertyRelative("value");
                PhysBoneValueName = physBoneValueName;
            }

            internal override void UpdateSource(SerializedObject sourcePb)
            {
                SourceValue = sourcePb.FindProperty(PhysBoneValueName);
            }
            public SerializedProperty GetValueProperty(bool @override) => @override ? OverrideValue : SourceValue!;
        }
        protected partial class NoOverrideValueConfigProp : PropBase
        {
            public readonly SerializedProperty OverrideValue;
            public SerializedProperty? SourceValue { get; private set; }
            public readonly string PhysBoneValueName;

            public NoOverrideValueConfigProp(
                SerializedProperty rootProperty
                , string physBoneValueName
                ) : base(rootProperty)
            {
                OverrideValue = rootProperty.FindPropertyRelative("value");
                PhysBoneValueName = physBoneValueName;
            }

            internal override void UpdateSource(SerializedObject sourcePb)
            {
                SourceValue = sourcePb.FindProperty(PhysBoneValueName);
            }
            public SerializedProperty GetValueProperty(bool @override) => @override ? OverrideValue : SourceValue!;
        }
    }
}

#endif
