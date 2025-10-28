#if AAO_VRCSDK3_AVATARS

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using VRC.Dynamics;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer
{
    internal abstract partial class MergePhysBoneEditorModificationUtils
    {
        // ReSharper disable MemberCanBePrivate.Global
        private readonly SerializedObject _serializedObject;
        private SerializedObject? _sourcePhysBone;
        protected readonly SerializedProperty MakeParent;
        private readonly List<PropBase> _props = new List<PropBase>();
            
        protected readonly ValueConfigProp Version;

        #region == Transform ==
        // rootTransform
        // ignoreTransforms
        protected readonly EndpointPositionConfigProp EndpointPosition;
        protected readonly ValueConfigProp IgnoreOtherPhysBones;
        // multiChildType
        #endregion

        #region == Forces ==
        protected readonly ValueConfigProp IntegrationType;
        protected readonly CurveConfigProp Pull;
        // spring a.k.a. Momentum
        protected readonly CurveConfigProp Spring;
        protected readonly CurveConfigProp Stiffness;
        protected readonly CurveConfigProp Gravity;
        protected readonly CurveConfigProp GravityFalloff;
        protected readonly ValueConfigProp ImmobileType;
        protected readonly CurveConfigProp Immobile;
        #endregion
        #region == Limits ==
        protected readonly ValueConfigProp LimitType;
        protected readonly CurveConfigProp MaxAngleX;
        protected readonly CurveConfigProp MaxAngleZ;
        protected readonly CurveVector3ConfigProp LimitRotation;
        #endregion
        #region == Collision ==
        protected readonly CurveConfigProp Radius;
        protected readonly PermissionConfigProp AllowCollision;
        protected readonly CollidersConfigProp Colliders;
        #endregion
        #region == Stretch & Squish ==
        protected readonly CurveConfigProp StretchMotion;
        protected readonly CurveConfigProp MaxStretch;
        protected readonly CurveConfigProp MaxSquish;
        #endregion
        #region == Grab & Pose ==
        protected readonly PermissionConfigProp AllowGrabbing;
        protected readonly PermissionConfigProp AllowPosing;
        protected readonly ValueConfigProp GrabMovement;
        protected readonly ValueConfigProp SnapToHand;
        #endregion
        #region == Options ==
        protected readonly NoOverrideValueConfigProp Parameter;
        protected readonly NoOverrideValueConfigProp IsAnimated;
        protected readonly ValueConfigProp ResetWhenDisabled;
        #endregion

        protected readonly PrefabSafeSet.PSSEditorUtil<VRCPhysBoneBase> ComponentsSetEditorUtil;
        // ReSharper restore MemberCanBePrivate.Global

        public MergePhysBoneEditorModificationUtils(SerializedObject serializedObject)
        {
            this._serializedObject = serializedObject;
            MakeParent = serializedObject.FindProperty(nameof(MergePhysBone.makeParent));

            Version = ValueProp(nameof(MergePhysBone.versionConfig), nameof(VRCPhysBoneBase.version));
            // == Transform ==
            EndpointPosition = EndpointPositionProp(nameof(MergePhysBone.endpointPositionConfig), nameof(VRCPhysBoneBase.endpointPosition));
            IgnoreOtherPhysBones = ValueProp(nameof(MergePhysBone.ignoreOtherPhysBones), nameof(VRCPhysBoneBase.ignoreOtherPhysBones));
            //  == Forces ==
            IntegrationType = ValueProp(nameof(MergePhysBone.integrationTypeConfig), nameof(VRCPhysBoneBase.integrationType));
            Pull = CurveProp(nameof(MergePhysBone.pullConfig), nameof(VRCPhysBoneBase.pull), nameof(VRCPhysBoneBase.pullCurve));
            Spring = CurveProp(nameof(MergePhysBone.springConfig), nameof(VRCPhysBoneBase.spring), nameof(VRCPhysBoneBase.springCurve));
            Stiffness = CurveProp(nameof(MergePhysBone.stiffnessConfig), nameof(VRCPhysBoneBase.stiffness), nameof(VRCPhysBoneBase.stiffnessCurve));
            Gravity = CurveProp(nameof(MergePhysBone.gravityConfig), nameof(VRCPhysBoneBase.gravity), nameof(VRCPhysBoneBase.gravityCurve));
            GravityFalloff = CurveProp(nameof(MergePhysBone.gravityFalloffConfig), nameof(VRCPhysBoneBase.gravityFalloff), nameof(VRCPhysBoneBase.gravityFalloffCurve));
            ImmobileType = ValueProp(nameof(MergePhysBone.immobileTypeConfig), nameof(VRCPhysBoneBase.immobileType));
            Immobile = CurveProp(nameof(MergePhysBone.immobileConfig), nameof(VRCPhysBoneBase.immobile), nameof(VRCPhysBoneBase.immobileCurve));
            // == Limits ==
            LimitType = ValueProp(nameof(MergePhysBone.limitTypeConfig), nameof(VRCPhysBoneBase.limitType));
            MaxAngleX = CurveProp(nameof(MergePhysBone.maxAngleXConfig), nameof(VRCPhysBoneBase.maxAngleX), nameof(VRCPhysBoneBase.maxAngleXCurve));
            MaxAngleZ = CurveProp(nameof(MergePhysBone.maxAngleZConfig), nameof(VRCPhysBoneBase.maxAngleZ), nameof(VRCPhysBoneBase.maxAngleZCurve));
            LimitRotation = CurveVector3Prop(nameof(MergePhysBone.limitRotationConfig), nameof(VRCPhysBoneBase.limitRotation), 
                nameof(VRCPhysBoneBase.limitRotationXCurve), nameof(VRCPhysBoneBase.limitRotationYCurve), nameof(VRCPhysBoneBase.limitRotationZCurve));
            // == Collision ==
            Radius = CurveProp(nameof(MergePhysBone.radiusConfig), nameof(VRCPhysBoneBase.radius), nameof(VRCPhysBoneBase.radiusCurve));
            AllowCollision = PermissionProp(nameof(MergePhysBone.allowCollisionConfig), nameof(VRCPhysBoneBase.allowCollision), nameof(VRCPhysBoneBase.collisionFilter));
            Colliders = CollidersProp(nameof(MergePhysBone.collidersConfig), nameof(VRCPhysBoneBase.colliders));
            // == Stretch & Squish ==
            StretchMotion = CurveProp(nameof(MergePhysBone.stretchMotionConfig), nameof(VRCPhysBoneBase.stretchMotion), nameof(VRCPhysBoneBase.stretchMotionCurve));
            MaxStretch = CurveProp(nameof(MergePhysBone.maxStretchConfig), nameof(VRCPhysBoneBase.maxStretch), nameof(VRCPhysBoneBase.maxStretchCurve));
            MaxSquish = CurveProp(nameof(MergePhysBone.maxSquishConfig), nameof(VRCPhysBoneBase.maxSquish), nameof(VRCPhysBoneBase.maxSquishCurve));
            // == Grab & Pose ==
            AllowGrabbing = PermissionProp(nameof(MergePhysBone.allowGrabbingConfig), nameof(VRCPhysBoneBase.allowGrabbing), nameof(VRCPhysBoneBase.grabFilter));
            AllowPosing = PermissionProp(nameof(MergePhysBone.allowPosingConfig), nameof(VRCPhysBoneBase.allowPosing), nameof(VRCPhysBoneBase.poseFilter));
            GrabMovement = ValueProp(nameof(MergePhysBone.grabMovementConfig), nameof(VRCPhysBoneBase.grabMovement));
            SnapToHand = ValueProp(nameof(MergePhysBone.snapToHandConfig), nameof(VRCPhysBoneBase.snapToHand));
            // == Options ==
            Parameter = NoOverrideProp(nameof(MergePhysBone.parameterConfig), nameof(VRCPhysBoneBase.parameter));
            IsAnimated = NoOverrideProp(nameof(MergePhysBone.isAnimatedConfig), nameof(VRCPhysBoneBase.isAnimated));
            ResetWhenDisabled = ValueProp(nameof(MergePhysBone.resetWhenDisabledConfig), nameof(VRCPhysBoneBase.resetWhenDisabled));

            var componentsSetProp = serializedObject.FindProperty(nameof(MergePhysBone.componentsSet));
            ComponentsSetEditorUtil = PrefabSafeSet.PSSEditorUtil<VRCPhysBoneBase>.Create(
                componentsSetProp, x => (VRCPhysBoneBase)x.objectReferenceValue,
                (x, v) => x.objectReferenceValue = v);
        }

        private T AddProp<T>(T prop) where T : PropBase
        {
            _props.Add(prop);
            return prop;
        }
        private ValueConfigProp ValueProp(string configName, string pbName) =>
            AddProp(new ValueConfigProp(_serializedObject.FindProperty(configName), pbName));
        private CurveConfigProp CurveProp(string configName, string pbValueName, string pbCurveName) =>
            AddProp(new CurveConfigProp(_serializedObject.FindProperty(configName), pbValueName, pbCurveName));
        private CurveVector3ConfigProp CurveVector3Prop(string configName, string pbValueName, 
            string pbCurveXName, string pbCurveYName, string pbCurveZName) =>
            AddProp(new CurveVector3ConfigProp(_serializedObject.FindProperty(configName), pbValueName,
                pbCurveXName, pbCurveYName, pbCurveZName));
        private PermissionConfigProp PermissionProp(string configName, string pbValueName, string pbFilterName) =>
            AddProp(new PermissionConfigProp(_serializedObject.FindProperty(configName), pbValueName, pbFilterName));
        private CollidersConfigProp CollidersProp(string configName, string pbValueName) =>
            AddProp(new CollidersConfigProp(_serializedObject.FindProperty(configName), pbValueName));
        private EndpointPositionConfigProp EndpointPositionProp(string configName, string pbValueName) =>
            AddProp(new EndpointPositionConfigProp(_serializedObject.FindProperty(configName), pbValueName));
        private NoOverrideValueConfigProp NoOverrideProp(string configName, string pbName) =>
            AddProp(new NoOverrideValueConfigProp(_serializedObject.FindProperty(configName), pbName));

        public void DoProcess()
        {
            var sourcePysBone = ComponentsSetEditorUtil.Values.FirstOrDefault();
            _sourcePhysBone = sourcePysBone == null
                ? null
                : new SerializedObject(ComponentsSetEditorUtil.Values.Cast<Object>().ToArray());

            if (_sourcePhysBone == null)
            {
                NoSource();
            }
            else
            {
                foreach (var propBase in _props)
                    propBase.UpdateSource(_sourcePhysBone);

                BeginPbConfig();

                PbVersionProp("Version", Version);

                var version = (VRCPhysBoneBase.Version)Version.GetValueProperty(Version.IsOverride).enumValueIndex;

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
                    PbProp("Integration Type", IntegrationType);
                    var forceOverride = IntegrationType.IsOverride;
                    var isSimplified = IntegrationType.GetValueProperty(IntegrationType.IsOverride).enumValueIndex == 0;
                    PbCurveProp("Pull", Pull);
                    if (isSimplified)
                    {
                        PbCurveProp("Spring", Spring, forceOverride);
                    }
                    else
                    {
                        PbCurveProp("Momentum", Spring, forceOverride);
                        PbCurveProp("Stiffness", Stiffness, forceOverride);
                    }

                    PbCurveProp("Gravity", Gravity);
                    PbCurveProp("Gravity Falloff", GravityFalloff);
                    PbProp("Immobile Type", ImmobileType);
                    PbCurveProp("Immobile", Immobile);
                }

                // == Limits ==
                if (NextSection("Limits", "limits"))
                {
                    PbProp("Limit Type", LimitType);
                    var limitType = (VRCPhysBoneBase.LimitType)LimitType.GetValueProperty(LimitType.IsOverride).enumValueIndex;
                    var forceOverride = LimitType.IsOverride;

                    switch (limitType)
                    {
                        case VRCPhysBoneBase.LimitType.None:
                            break;
                        case VRCPhysBoneBase.LimitType.Angle:
                        case VRCPhysBoneBase.LimitType.Hinge:
                            PbCurveProp("Max Angle", MaxAngleX, forceOverride);
                            Pb3DCurveProp("Rotation", "Pitch", "Roll", "Yaw", LimitRotation, forceOverride);
                            break;
                        case VRCPhysBoneBase.LimitType.Polar:
                            PbCurveProp("Max Angle X", MaxAngleX, forceOverride);
                            PbCurveProp("Max Angle Z", MaxAngleZ, forceOverride);
                            Pb3DCurveProp("Rotation", "Pitch", "Roll", "Yaw", LimitRotation, forceOverride);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                // == Collision ==
                if (NextSection("Collision", "collision"))
                {
                    PbCurveProp("Radius", Radius);
                    PbPermissionProp("Allow Collision", AllowCollision);
                    CollidersProp("Colliders", Colliders);
                }

                // == Stretch & Squish ==
                if (NextSection("Stretch & Squish", "stretch--squish"))
                {
                    if (CheckMinVersion(VRCPhysBoneBase.Version.Version_1_1))
                        PbCurveProp("Stretch Motion", StretchMotion);
                    PbCurveProp("Max Stretch", MaxStretch);
                    PbCurveProp("Max Squish", MaxSquish);
                }

                // == Grab & Pose ==
                if (NextSection("Grab & Pose", "grab--pose"))
                {
                    PbPermissionProp("Allow Grabbing", AllowGrabbing);
                    PbPermissionProp("Allow Posing", AllowPosing);
                    PbProp("Grab Movement", GrabMovement);
                    PbProp("Snap To Hand", SnapToHand);
                }

                // == Options ==
                if (NextSection("Options", "options"))
                {
                    OptionParameter();
                    OptionIsAnimated();
                    PbProp("Reset When Disabled", ResetWhenDisabled);
                }

                EndSection();

                EndPbConfig();
            }
        }

        protected SerializedProperty GetSourceProperty(string name) => _sourcePhysBone!.FindProperty(name);

        protected IEnumerable<VRCPhysBoneBase> SourcePhysBones => _sourcePhysBone!.targetObjects.Cast<VRCPhysBoneBase>();

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

        protected abstract void PbVersionProp(string label, ValueConfigProp prop, bool forceOverride = false);

        protected abstract void PbProp(string label, ValueConfigProp prop, bool forceOverride = false);

        protected abstract void PbCurveProp(string label, CurveConfigProp prop, bool forceOverride = false);

        protected abstract void PbPermissionProp(string label, PermissionConfigProp prop, bool forceOverride = false);

        protected abstract void Pb3DCurveProp(string label,
            string pbXCurveLabel,
            string pbYCurveLabel,
            string pbZCurveLabel,
            CurveVector3ConfigProp prop,
            bool forceOverride = false);

        protected abstract void CollidersProp(string label, CollidersConfigProp prop);
        
        protected abstract class PropBase
        {
            public readonly SerializedProperty RootProperty;

            public PropBase(SerializedProperty rootProperty)
            {
                RootProperty = rootProperty ?? throw new ArgumentNullException(nameof(rootProperty));
            }

            internal abstract void UpdateSource(SerializedObject sourcePb);
        }

        protected abstract class OverridePropBase: PropBase
        {
            public readonly SerializedProperty IsOverrideProperty;

            public bool IsOverride => IsOverrideProperty.boolValue;

            public OverridePropBase(SerializedProperty rootProperty) : base(rootProperty)
            {
                IsOverrideProperty = rootProperty.FindPropertyRelative("override");
            }
        }

        // Very Special Case
        protected class CollidersConfigProp : PropBase
        {
            public readonly SerializedProperty OverrideProperty;
            public readonly SerializedProperty ValueProperty;
            public SerializedProperty? PhysBoneValue { get; private set; } 
            public readonly string PhysBoneValueName;

            public CollidersConfigProp(
                SerializedProperty rootProperty, 
                string physBoneValueName) : base(rootProperty)
            {
                OverrideProperty = rootProperty.FindPropertyRelative("override");
                ValueProperty = rootProperty.FindPropertyRelative("value");
                PhysBoneValueName = physBoneValueName ?? throw new ArgumentNullException(nameof(physBoneValueName));
            }

            internal override void UpdateSource(SerializedObject sourcePb)
            {
                PhysBoneValue = sourcePb.FindProperty(PhysBoneValueName);
            }
        }

        // Very Special Case
        protected class EndpointPositionConfigProp : PropBase
        {
            public readonly SerializedProperty OverrideProperty;
            public readonly SerializedProperty ValueProperty;
            public SerializedProperty? PhysBoneValue { get; private set; } 
            public readonly string PhysBoneValueName;

            public EndpointPositionConfigProp(
                SerializedProperty rootProperty, 
                string physBoneValueName) : base(rootProperty)
            {
                OverrideProperty = rootProperty.FindPropertyRelative("override");
                ValueProperty = rootProperty.FindPropertyRelative("value");
                PhysBoneValueName = physBoneValueName ?? throw new ArgumentNullException(nameof(physBoneValueName));
            }

            internal override void UpdateSource(SerializedObject sourcePb)
            {
                PhysBoneValue = sourcePb.FindProperty(PhysBoneValueName);
            }
        }

        // Very Special Case
        protected partial class CurveVector3ConfigProp : PropBase
        {
            public readonly SerializedProperty OverrideProperty;
            public readonly SerializedProperty OverrideValue;
            public SerializedProperty? SourceValue { get; private set; }
            public readonly string PhysBoneValueName;
            public readonly SerializedProperty OverrideCurveX;
            public SerializedProperty? SourceCurveX { get; private set; }
            public readonly string PhysBoneCurveXName;
            public readonly SerializedProperty OverrideCurveY;
            public SerializedProperty? SourceCurveY { get; private set; }
            public readonly string PhysBoneCurveYName;
            public readonly SerializedProperty OverrideCurveZ;
            public SerializedProperty? SourceCurveZ { get; private set; }
            public readonly string PhysBoneCurveZName;

            public CurveVector3ConfigProp(
                SerializedProperty rootProperty
                , string physBoneValueName
                , string physBoneCurveXName
                , string physBoneCurveYName
                , string physBoneCurveZName
                ) : base(rootProperty)
            {
                OverrideProperty = rootProperty.FindPropertyRelative("override");
                OverrideValue = rootProperty.FindPropertyRelative("value");
                PhysBoneValueName = physBoneValueName;
                OverrideCurveX = rootProperty.FindPropertyRelative("curveX");
                PhysBoneCurveXName = physBoneCurveXName;
                OverrideCurveY = rootProperty.FindPropertyRelative("curveY");
                PhysBoneCurveYName = physBoneCurveYName;
                OverrideCurveZ = rootProperty.FindPropertyRelative("curveZ");
                PhysBoneCurveZName = physBoneCurveZName;
            }

            internal override void UpdateSource(SerializedObject sourcePb)
            {
                SourceValue = sourcePb.FindProperty(PhysBoneValueName);
                SourceCurveX = sourcePb.FindProperty(PhysBoneCurveXName);
                SourceCurveY = sourcePb.FindProperty(PhysBoneCurveYName);
                SourceCurveZ = sourcePb.FindProperty(PhysBoneCurveZName);
            }

            public MergePhysBone.CurveVector3Config.CurveOverride GetOverride(bool forceOverride) =>
                forceOverride
                    ? MergePhysBone.CurveVector3Config.CurveOverride.Override
                    : (MergePhysBone.CurveVector3Config.CurveOverride)OverrideProperty.enumValueIndex;

            public SerializedProperty GetValueProperty(bool @override) => @override ? OverrideValue : SourceValue!;
            public SerializedProperty GetCurveXProperty(bool @override) => @override ? OverrideCurveX : SourceCurveX!;
            public SerializedProperty GetCurveYProperty(bool @override) => @override ? OverrideCurveY : SourceCurveY!;
            public SerializedProperty GetCurveZProperty(bool @override) => @override ? OverrideCurveZ : SourceCurveZ!;
        }
    }
}

#endif
