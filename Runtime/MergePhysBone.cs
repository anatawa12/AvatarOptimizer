#if AAO_VRCSDK3_AVATARS

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Serialization;
using VRC.Dynamics;

namespace Anatawa12.AvatarOptimizer
{
    /// <summary>
    /// This component is used to merge multiple VRCPhysBone components into one.
    /// </summary>
    /// <remarks>
    /// This component is added to component public API in 1.9.0.
    /// </remarks>
    /// <remarks>
    /// 
    /// <para>
    /// This component is a little special in the public API compared to other components in Avatar Optimizer.
    /// This is because this component is deeply related to VRCPhysBone component from VRCSDK, which has
    /// many breaking changes throughout VRCSDK versions.
    /// </para>
    /// 
    /// <para>
    /// The first thing special is that the API of this component may experience breaking changes
    /// in patch / minor version of Avatar Optimizer that adds support for newer
    /// VRCSDK 'Breaking' versions, to support / follow the changes of VRCPhysBone component from VRCSDK.
    /// Those changes are not considered as breaking changes for Avatar Optimizer public API and will remain
    /// same major version.
    /// Therefore, you should consider guarding your code with both Avatar Optimizer and VRCSDK versions when
    /// you use this component's API, or accept same-level of breaking changes as VRCSDK's 'breaking' changes.
    /// </para>
    ///
    /// <para>
    /// Historically, before v1 release of Avatar Optimizer, changed the type of allowCollision, allowGrabbing,
    /// and allowPosing properties from bool to VRCPhysBoneBase.AdvancedBool in VRCSDK 3.1.12.
    /// (At that time, VRCSDK does not have proper versioning scheme so it was done in patch release of VRCSDK,
    /// but now, after deeply talking with VRCSDK developers and proper versioning scheme is introduced,
    /// such changes should be done in 'breaking' version of VRCSDK version change.
    /// please note that VRCSDK uses branding.breaking.bump that is same as the romantic versioning.)
    /// </para>
    ///
    /// <para>
    /// In addition, we might add new properties to this component on patch release as
    /// "bug fix to not supporting new property in VRCPhysBone".
    /// Therefore, some properties might need to be checked for patch version before accessing some of properties.
    /// All such properties will be documented in their xml doc.
    /// </para>
    /// 
    /// </remarks>
    [AddComponentMenu("Avatar Optimizer/AAO Merge PhysBone")]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/merge-physbone/")]
    [PublicAPI]
    [ApiExceptionType(typeof(VRCPhysBoneBase))]
    public class MergePhysBone : AvatarTagComponent, VRC.SDKBase.INetworkID, ISerializationCallbackReceiver
    {
        [NotKeyable]
        [AAOLocalized("MergePhysBone:prop:makeParent", "MergePhysBone:tooltip:makeParent")]
        [SerializeField]
        internal bool makeParent;

        #region OverrideAndValue

        [NotKeyable, SerializeField] internal ValueWithOverride<VRCPhysBoneBase.Version> versionConfig;
        // == Transform ==
        // rootTransform
        // ignoreTransforms
        [NotKeyable, SerializeField] internal EndPointPositionConfigStruct endpointPositionConfig;
        [NotKeyable, SerializeField] internal ValueWithOverride<bool> ignoreOtherPhysBones;
        // multiChildType
        // == Forces ==
        [NotKeyable, SerializeField] internal ValueWithOverride<VRCPhysBoneBase.IntegrationType> integrationTypeConfig;
        [NotKeyable, SerializeField] [Range(0f, 1f)] internal CurveFloatConfig pullConfig;
        // spring a.k.a. Momentum
        [NotKeyable, SerializeField] [Range(0f, 1f)] internal CurveFloatConfig springConfig;
        [NotKeyable, SerializeField] [Range(0f, 1f)] internal CurveFloatConfig stiffnessConfig;
        [NotKeyable, SerializeField] [Range(-1f, 1f)] internal CurveFloatConfig gravityConfig;
        [NotKeyable, SerializeField] [Range(0f, 1f)] internal CurveFloatConfig gravityFalloffConfig;
        [NotKeyable, SerializeField] internal ValueWithOverride<VRCPhysBoneBase.ImmobileType> immobileTypeConfig;
        [NotKeyable, SerializeField] [Range(0f, 1f)] internal CurveFloatConfig immobileConfig;
        // == Limits ==
        [NotKeyable, SerializeField] internal ValueWithOverride<VRCPhysBoneBase.LimitType> limitTypeConfig;
        [NotKeyable, SerializeField] [Range(0f, 180f)] internal CurveFloatConfig maxAngleXConfig = new(45f);
        [NotKeyable, SerializeField] [Range(0f, 90f)] internal CurveFloatConfig maxAngleZConfig = new(45f);
        [NotKeyable, SerializeField] internal LimitRotationConfigStruct limitRotationConfig;
        // == Collision ==
        [NotKeyable, SerializeField] internal CurveFloatConfig radiusConfig;
        [NotKeyable, SerializeField] internal PermissionConfig allowCollisionConfig;
        [NotKeyable, SerializeField] internal CollidersConfigStruct collidersConfig;
        // == Stretch & Squish ==
        [NotKeyable, SerializeField] [Range(0f, 1f)] internal CurveFloatConfig stretchMotionConfig;
        [NotKeyable, SerializeField] internal CurveFloatConfig maxStretchConfig;
        [NotKeyable, SerializeField] [Range(0f, 1f)] internal CurveFloatConfig maxSquishConfig;
        // == Grab & Pose ==
        [NotKeyable, SerializeField] internal PermissionConfig allowGrabbingConfig;
        [NotKeyable, SerializeField] internal PermissionConfig allowPosingConfig;
        [NotKeyable, SerializeField] [Range(0f, 1f)] internal ValueWithOverride<float> grabMovementConfig;
        [NotKeyable, SerializeField] internal ValueWithOverride<bool> snapToHandConfig;
        // == Options ==
        [NotKeyable, SerializeField] internal ValueOnly<string> parameterConfig;
        [NotKeyable, SerializeField] internal ValueOnly<bool> isAnimatedConfig;
        [NotKeyable, SerializeField] internal ValueWithOverride<bool> resetWhenDisabledConfig;

        private void Reset()
        {
            // copy by default for newly added component
            endpointPositionConfig.@override = EndPointPositionConfigStruct.Override.Copy;
        }

        [Serializable]
        internal struct EndPointPositionConfigStruct
        {
            public Override @override;
            public Vector3 value;

            public enum Override
            {
                Clear,
                Copy,
                Override,
            }
        }

        [Serializable]
        internal struct CurveFloatConfig
        {
            public bool @override;
            [DrawWithContainer]
            public float value;
            public AnimationCurve? curve;

            public CurveFloatConfig(float value) : this()
            {
                this.value = value;
                curve = new AnimationCurve();
            }
        }

        [Serializable]
        internal struct LimitRotationConfigStruct
        {
            public Override @override;
            public Vector3 value;
            public AnimationCurve? curveX;
            public AnimationCurve? curveY;
            public AnimationCurve? curveZ;
            
            public enum Override
            {
                Copy,
                Override,
                // Change bone angle to match the curve
                Fix,
            }
        }

        [Serializable]
        internal struct ValueWithOverride<TValue>
        {
            [SerializeField] public bool @override;
            [SerializeField] [DrawWithContainer] public TValue value;

            public ValueWithOverride(TValue value) : this()
            {
                this.value = value;
            }
        }

        [Serializable]
        internal struct PermissionConfig
        {
            public bool @override;
            public VRCPhysBoneBase.AdvancedBool value;
            public VRCPhysBoneBase.PermissionFilter filter;
        }

        [Serializable]
        internal struct CollidersConfigStruct
        {
            public Override @override;
            public List<VRCPhysBoneColliderBase>? value;
            
            public enum Override
            {
                Copy,
                Override,
                Merge,
            }
        }

        [Serializable]
        internal struct ValueOnly<TValue>
        {
            public TValue? value;
        }

        #endregion

        [AAOLocalized("MergePhysBone:prop:components")]
        [SerializeField]
        internal PrefabSafeSet.PrefabSafeSet<VRCPhysBoneBase> componentsSet;

        internal MergePhysBone()
        {
            componentsSet = new PrefabSafeSet.PrefabSafeSet<VRCPhysBoneBase>(this);
        }

        private void ValidatePSUC()
        {
            PrefabSafeSet.PrefabSafeSet.OnValidate(this, x => x.componentsSet);
        }

        private void OnValidate() => ValidatePSUC();
        void ISerializationCallbackReceiver.OnBeforeSerialize() => ValidatePSUC();

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
        }

        #region Public API

        /// <summary>
        /// Initializes the MergePhysBone with the specified default behavior version.
        ///
        /// As described in the documentation, you have to call this method after `AddComponent` to make sure
        /// the default configuration is what you want.
        /// Without calling this method, the default configuration might be changed in the future.
        /// </summary>
        /// <param name="version">
        /// <para>
        /// The default configuration version.
        /// </para>
        /// <para>
        /// Since 1.9.0, version 1 is supported.
        /// </para>
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">Unsupported configuration version</exception>
        [PublicAPI]
        public void Initialize(int version)
        {
            switch (version)
            {
                case 1:
                    // nothing to do
                    break; 
                default:
                    throw new ArgumentOutOfRangeException(nameof(version), $"unsupported version: {version}");
            }
        }

        [PublicAPI]
        public bool MakeParent
        {
            get => makeParent;
            set => makeParent = value;
        }

        [PublicAPI] public OverrideAndValueAPI VersionConfig => new(this, OverrideAndValueAPI.InternalType.Version);
        [PublicAPI] public OverrideAndValueAPI EndPointPositionConfig => new(this, OverrideAndValueAPI.InternalType.EndPointPosition);
        [PublicAPI] public OverrideAndValueAPI IgnoreOtherPhysBonesConfig => new(this, OverrideAndValueAPI.InternalType.IgnoreOtherPhysBones);
        [PublicAPI] public OverrideAndValueAPI IntegrationTypeConfig => new(this, OverrideAndValueAPI.InternalType.IntegrationType);
        [PublicAPI] public OverrideAndValueAPI PullConfig => new(this, OverrideAndValueAPI.InternalType.Pull);
        [PublicAPI] public OverrideAndValueAPI SpringConfig => new(this, OverrideAndValueAPI.InternalType.Spring);
        [PublicAPI] public OverrideAndValueAPI StiffnessConfig => new(this, OverrideAndValueAPI.InternalType.Stiffness);
        [PublicAPI] public OverrideAndValueAPI GravityConfig => new(this, OverrideAndValueAPI.InternalType.Gravity);
        [PublicAPI] public OverrideAndValueAPI GravityFalloffConfig => new(this, OverrideAndValueAPI.InternalType.GravityFalloff);
        [PublicAPI] public OverrideAndValueAPI ImmobileTypeConfig => new(this, OverrideAndValueAPI.InternalType.ImmobileType);
        [PublicAPI] public OverrideAndValueAPI ImmobileConfig => new(this, OverrideAndValueAPI.InternalType.Immobile);
        [PublicAPI] public OverrideAndValueAPI LimitTypeConfig => new(this, OverrideAndValueAPI.InternalType.LimitType);
        [PublicAPI] public OverrideAndValueAPI MaxAngleXConfig => new(this, OverrideAndValueAPI.InternalType.MaxAngleX);
        [PublicAPI] public OverrideAndValueAPI MaxAngleZConfig => new(this, OverrideAndValueAPI.InternalType.MaxAngleZ);
        [PublicAPI] public OverrideAndValueAPI LimitRotationConfig => new(this, OverrideAndValueAPI.InternalType.LimitRotation);
        [PublicAPI] public OverrideAndValueAPI RadiusConfig => new(this, OverrideAndValueAPI.InternalType.Radius);
        [PublicAPI] public OverrideAndValueAPI AllowCollisionConfig => new(this, OverrideAndValueAPI.InternalType.AllowCollision);
        [PublicAPI] public OverrideAndValueAPI CollidersConfig => new(this, OverrideAndValueAPI.InternalType.Colliders);
        [PublicAPI] public OverrideAndValueAPI StretchMotionConfig => new(this, OverrideAndValueAPI.InternalType.StretchMotion);
        [PublicAPI] public OverrideAndValueAPI MaxStretchConfig => new(this, OverrideAndValueAPI.InternalType.MaxStretch);
        [PublicAPI] public OverrideAndValueAPI MaxSquishConfig => new(this, OverrideAndValueAPI.InternalType.MaxSquish);
        [PublicAPI] public OverrideAndValueAPI AllowGrabbingConfig => new(this, OverrideAndValueAPI.InternalType.AllowGrabbing);
        [PublicAPI] public OverrideAndValueAPI AllowPosingConfig => new(this, OverrideAndValueAPI.InternalType.AllowPosing);
        [PublicAPI] public OverrideAndValueAPI GrabMovementConfig => new(this, OverrideAndValueAPI.InternalType.GrabMovement);
        [PublicAPI] public OverrideAndValueAPI SnapToHandConfig => new(this, OverrideAndValueAPI.InternalType.SnapToHand);
        [PublicAPI] public OverrideAndValueAPI ParameterConfig => new(this, OverrideAndValueAPI.InternalType.Parameter);
        [PublicAPI] public OverrideAndValueAPI IsAnimatedConfig => new(this, OverrideAndValueAPI.InternalType.IsAnimated);
        [PublicAPI] public OverrideAndValueAPI ResetWhenDisabledConfig => new(this, OverrideAndValueAPI.InternalType.ResetWhenDisabled);

        [PublicAPI]
        public API.PrefabSafeSetAccessor<VRCPhysBoneBase> PhysBones => new (componentsSet);

        /// <summary>
        /// The struct represents config for one property of VRCPhysBone.
        ///
        /// Single struct is shared between multiple property of MergePhysBone to keep future API compatibility,
        /// with cost of no compile-time checking for which property this struct can be used.
        ///
        /// This struct is declared as a ref struct to allow potential use of ref fields and other
        /// stack-only features in future versions of the API.
        ///
        /// Detailed explanation: We might add some new associated fields to some properties in the future, and
        /// we might want to add some new override status to some properties in the future. When we split structs
        /// for type of property, we might need to support for all properties with same type, or we might need to
        /// add partially-supported properties. Adding struct for each property takes too much to maintain.
        /// </summary>
        // note: this struct currently does not use ref fields or similar thing but may use in the future so this is ref struct.
        [PublicAPI]
        [ApiExceptionType(typeof(VRCPhysBoneBase.Version))]
        [ApiExceptionType(typeof(VRCPhysBoneBase.IntegrationType))]
        [ApiExceptionType(typeof(VRCPhysBoneBase.ImmobileType))]
        [ApiExceptionType(typeof(VRCPhysBoneBase.LimitType))]
        [ApiExceptionType(typeof(VRCPhysBoneBase.AdvancedBool))]
        [ApiExceptionType(typeof(VRCPhysBoneBase.PermissionFilter))]
        public ref struct OverrideAndValueAPI
        {
            private MergePhysBone _mergePb;
            private InternalType _type;

            internal enum InternalType
            {
                Invalid, // default value is Invalid
                Version,
                EndPointPosition,
                IgnoreOtherPhysBones,
                IntegrationType,
                Pull,
                Spring,
                Stiffness,
                Gravity,
                GravityFalloff,
                ImmobileType,
                Immobile,
                LimitType,
                MaxAngleX,
                MaxAngleZ,
                LimitRotation,
                Radius,
                AllowCollision,
                Colliders,
                StretchMotion,
                MaxStretch,
                MaxSquish,
                AllowGrabbing,
                AllowPosing,
                GrabMovement,
                SnapToHand,
                Parameter,
                IsAnimated,
                ResetWhenDisabled,
            }

            internal OverrideAndValueAPI(MergePhysBone mergePb, InternalType type)
            {
                _mergePb = mergePb;
                _type = type;
            }

            [PublicAPI]
            public OverrideStatus OverrideStatus 
            {
                get => _type switch
                {
                    InternalType.Version => B(_mergePb.versionConfig.@override),
                    InternalType.EndPointPosition => EP(_mergePb.endpointPositionConfig.@override),
                    InternalType.IgnoreOtherPhysBones => B(_mergePb.ignoreOtherPhysBones.@override),
                    InternalType.IntegrationType => B(_mergePb.integrationTypeConfig.@override),
                    InternalType.Pull => B(_mergePb.pullConfig.@override),
                    InternalType.Spring => B(_mergePb.springConfig.@override),
                    InternalType.Stiffness => B(_mergePb.stiffnessConfig.@override),
                    InternalType.Gravity => B(_mergePb.gravityConfig.@override),
                    InternalType.GravityFalloff => B(_mergePb.gravityFalloffConfig.@override),
                    InternalType.ImmobileType => B(_mergePb.immobileTypeConfig.@override),
                    InternalType.Immobile => B(_mergePb.immobileConfig.@override),
                    InternalType.LimitType => B(_mergePb.limitTypeConfig.@override),
                    InternalType.MaxAngleX => B(_mergePb.maxAngleXConfig.@override),
                    InternalType.MaxAngleZ => B(_mergePb.maxAngleZConfig.@override),
                    InternalType.LimitRotation => LR(_mergePb.limitRotationConfig.@override),
                    InternalType.Radius => B(_mergePb.radiusConfig.@override),
                    InternalType.AllowCollision => B(_mergePb.allowCollisionConfig.@override),
                    InternalType.Colliders => C(_mergePb.collidersConfig.@override),
                    InternalType.StretchMotion => B(_mergePb.stretchMotionConfig.@override),
                    InternalType.MaxStretch => B(_mergePb.maxStretchConfig.@override),
                    InternalType.MaxSquish => B(_mergePb.maxSquishConfig.@override),
                    InternalType.AllowGrabbing => B(_mergePb.allowGrabbingConfig.@override),
                    InternalType.AllowPosing => B(_mergePb.allowPosingConfig.@override),
                    InternalType.GrabMovement => B(_mergePb.grabMovementConfig.@override),
                    InternalType.SnapToHand => B(_mergePb.snapToHandConfig.@override),
                    InternalType.ResetWhenDisabled => B(_mergePb.resetWhenDisabledConfig.@override),
                    _ => throw new InvalidOperationException($"Override is invalid for {_type}"),
                };
                set
                {
                    OverrideStatus x;
                    switch (_type)
                    {
                        case InternalType.Version: _mergePb.versionConfig.@override = B(value); break;
                        case InternalType.EndPointPosition: _mergePb.endpointPositionConfig.@override = EP(value); break;
                        case InternalType.IgnoreOtherPhysBones: _mergePb.ignoreOtherPhysBones.@override = B(value); break;
                        case InternalType.IntegrationType: _mergePb.integrationTypeConfig.@override = B(value); break;
                        case InternalType.Pull: _mergePb.pullConfig.@override = B(value); break;
                        case InternalType.Spring: _mergePb.springConfig.@override = B(value); break;
                        case InternalType.Stiffness: _mergePb.stiffnessConfig.@override = B(value); break;
                        case InternalType.Gravity: _mergePb.gravityConfig.@override = B(value); break;
                        case InternalType.GravityFalloff: _mergePb.gravityFalloffConfig.@override = B(value); break;
                        case InternalType.ImmobileType: _mergePb.immobileTypeConfig.@override = B(value); break;
                        case InternalType.Immobile: _mergePb.immobileConfig.@override = B(value); break;
                        case InternalType.LimitType: _mergePb.limitTypeConfig.@override = B(value); break;
                        case InternalType.MaxAngleX: _mergePb.maxAngleXConfig.@override = B(value); break;
                        case InternalType.MaxAngleZ: _mergePb.maxAngleZConfig.@override = B(value); break;
                        case InternalType.LimitRotation: _mergePb.limitRotationConfig.@override = LR(value); break;
                        case InternalType.Radius: _mergePb.radiusConfig.@override = B(value); break;
                        case InternalType.AllowCollision: _mergePb.allowCollisionConfig.@override = B(value); break;
                        case InternalType.Colliders: _mergePb.collidersConfig.@override = C(value); break;
                        case InternalType.StretchMotion: _mergePb.stretchMotionConfig.@override = B(value); break;
                        case InternalType.MaxStretch: _mergePb.maxStretchConfig.@override = B(value); break;
                        case InternalType.MaxSquish: _mergePb.maxSquishConfig.@override = B(value); break;
                        case InternalType.AllowGrabbing: _mergePb.allowGrabbingConfig.@override = B(value); break;
                        case InternalType.AllowPosing: _mergePb.allowPosingConfig.@override = B(value); break;
                        case InternalType.GrabMovement: _mergePb.grabMovementConfig.@override = B(value); break;
                        case InternalType.SnapToHand: _mergePb.snapToHandConfig.@override = B(value); break;
                        case InternalType.ResetWhenDisabled: _mergePb.resetWhenDisabledConfig.@override = B(value); break;
                        default:
                            throw new InvalidOperationException($"Override is invalid for {_type}");
                    }
                }
            }

            private static OverrideStatus B(bool overrideValue) => overrideValue ? OverrideStatus.Overridden : OverrideStatus.Copied;
            private bool B(OverrideStatus overrideStatus) => overrideStatus switch
            {
                OverrideStatus.Copied => false,
                OverrideStatus.Overridden => true,
                _ => throw new InvalidOperationException($"OverrideStatus {overrideStatus} is invalid for {_type} Property"),
            };
            private static OverrideStatus EP(EndPointPositionConfigStruct.Override overrideValue) => overrideValue switch
            {
                EndPointPositionConfigStruct.Override.Clear => OverrideStatus.Cleared,
                EndPointPositionConfigStruct.Override.Copy => OverrideStatus.Copied,
                EndPointPositionConfigStruct.Override.Override => OverrideStatus.Overridden,
                _ => throw new InvalidOperationException(),
            };
            private EndPointPositionConfigStruct.Override EP(OverrideStatus overrideStatus) => overrideStatus switch
            {
                OverrideStatus.Cleared => EndPointPositionConfigStruct.Override.Clear,
                OverrideStatus.Copied => EndPointPositionConfigStruct.Override.Copy,
                OverrideStatus.Overridden => EndPointPositionConfigStruct.Override.Override,
                _ => throw new InvalidOperationException($"OverrideStatus {overrideStatus} is invalid for {_type} Property"),
            };
            private static OverrideStatus LR(LimitRotationConfigStruct.Override overrideValue) => overrideValue switch
            {
                LimitRotationConfigStruct.Override.Copy => OverrideStatus.Copied,
                LimitRotationConfigStruct.Override.Override => OverrideStatus.Overridden,
                LimitRotationConfigStruct.Override.Fix => OverrideStatus.Fixed,
                _ => throw new InvalidOperationException(),
            };
            private LimitRotationConfigStruct.Override LR(OverrideStatus overrideStatus) => overrideStatus switch
            {
                OverrideStatus.Copied => LimitRotationConfigStruct.Override.Copy,
                OverrideStatus.Overridden => LimitRotationConfigStruct.Override.Override,
                OverrideStatus.Fixed => LimitRotationConfigStruct.Override.Fix,
                _ => throw new InvalidOperationException($"OverrideStatus {overrideStatus} is invalid for {_type} Property"),
            };
            private static OverrideStatus C(CollidersConfigStruct.Override overrideValue) => overrideValue switch
            {
                CollidersConfigStruct.Override.Copy => OverrideStatus.Copied,
                CollidersConfigStruct.Override.Override => OverrideStatus.Overridden,
                CollidersConfigStruct.Override.Merge => OverrideStatus.Merged,
                _ => throw new InvalidOperationException(),
            };
            private CollidersConfigStruct.Override C(OverrideStatus overrideStatus) => overrideStatus switch
            {
                OverrideStatus.Copied => CollidersConfigStruct.Override.Copy,
                OverrideStatus.Overridden => CollidersConfigStruct.Override.Override,
                OverrideStatus.Merged => CollidersConfigStruct.Override.Merge,
                _ => throw new InvalidOperationException($"OverrideStatus {overrideStatus} is invalid for {_type} Property"),
            };

            /// <summary>
            /// Gets and Sets whether to override the property value.
            ///
            /// Setting this value to false will make the property copy from source PhysBones.
            ///
            /// For most properties, false value of this means "copy" for now, but might be changed in the future.
            /// If you want to check if this property is copied from source PhysBones, check the IsCopy property, or OverrideStatus property.
            /// </summary>
            /// <exception cref="InvalidOperationException">
            /// Throws when 'Override' is not applicable to the property.
            /// Any property that cannot copy from source PhysBones will throw this exception.
            /// </exception>
            [PublicAPI]
            public bool Override
            {
                get => OverrideStatus == OverrideStatus.Overridden;
                set => OverrideStatus = value ? OverrideStatus.Overridden : OverrideStatus.Copied;
            }

            [PublicAPI]
            public bool IsCopy => OverrideStatus == OverrideStatus.Copied;

            /// <summary>
            /// The overridden value of Version property.
            /// </summary>
            /// <exception cref="InvalidOperationException">
            /// Throws when this property is not Version property.
            /// </exception>
            [PublicAPI]
            public VRCPhysBoneBase.Version VersionOverrideValue
            {
                get => _type switch
                {
                    InternalType.Version => _mergePb.versionConfig.value,
                    _ => throw new InvalidOperationException($"VersionOverrideValue is invalid for {_type} Property"),
                };
                set
                {
                    switch (_type)
                    {
                        case InternalType.Version: _mergePb.versionConfig.value = value; break;
                        default: 
                            throw new InvalidOperationException($"VersionOverrideValue is invalid for {_type} Property");
                    }
                }
            }

            /// <summary>
            /// Boolean overridden value for properties that are boolean type.
            /// This is used for IgnoreOtherPhysBones, SnapToHand, ResetWhenDisabled properties.
            /// </summary>
            /// <exception cref="InvalidOperationException">If this property is not boolean type property.</exception>
            [PublicAPI]
            public bool BoolOverrideValue
            {
                get => _type switch
                {
                    InternalType.IgnoreOtherPhysBones => _mergePb.ignoreOtherPhysBones.value,
                    InternalType.SnapToHand => _mergePb.snapToHandConfig.value,
                    InternalType.ResetWhenDisabled => _mergePb.resetWhenDisabledConfig.value,
                    _ => throw new InvalidOperationException($"BoolOverrideValue is invalid for {_type} Property"),
                };
                set
                {
                    switch (_type)
                    {
                        case InternalType.IgnoreOtherPhysBones: _mergePb.ignoreOtherPhysBones.value = value; break;
                        case InternalType.SnapToHand: _mergePb.snapToHandConfig.value = value; break;
                        case InternalType.ResetWhenDisabled: _mergePb.resetWhenDisabledConfig.value = value; break;
                        default:
                            throw new InvalidOperationException($"BoolOverrideValue is invalid for {_type} Property");
                    }
                }
            }

            /// <summary>
            /// The overridden value of IntegrationType property.
            /// </summary>
            /// <exception cref="InvalidOperationException">Throws when this property is not IntegrationType property.</exception>
            [PublicAPI]
            public VRCPhysBoneBase.IntegrationType IntegrationTypeOverrideValue
            {
                get => _type switch
                {
                    InternalType.IntegrationType => _mergePb.integrationTypeConfig.value,
                    _ => throw new InvalidOperationException($"IntegrationType is invalid for {_type} Property"),
                };
                set
                {
                    switch (_type)
                    {
                        case InternalType.IntegrationType: _mergePb.integrationTypeConfig.value = value; break;
                        default:
                            throw new InvalidOperationException($"IntegrationType is invalid for {_type} Property");
                    }
                }
            }

            /// <summary>
            /// The overridden value of ImmobileType property.
            /// </summary>
            /// <exception cref="InvalidOperationException">Throws when this property is not ImmobileType property.</exception>
            [PublicAPI]
            public VRCPhysBoneBase.ImmobileType ImmobileTypeOverrideValue
            {
                get => _type switch
                {
                    InternalType.ImmobileType => _mergePb.immobileTypeConfig.value,
                    _ => throw new InvalidOperationException($"ImmobileType is invalid for {_type} Property"),
                };
                set
                {
                    switch (_type)
                    {
                        case InternalType.ImmobileType: _mergePb.immobileTypeConfig.value = value; break;
                        default:
                            throw new InvalidOperationException($"ImmobileType is invalid for {_type} Property");
                    }
                }
            }

            /// <summary>
            /// The overridden value of LimitType property.
            /// </summary>
            /// <exception cref="InvalidOperationException">Throws when this property is not LimitType property.</exception>
            [PublicAPI]
            public VRCPhysBoneBase.LimitType LimitTypeOverrideValue
            {
                get => _type switch
                {
                    InternalType.LimitType => _mergePb.limitTypeConfig.value,
                    _ => throw new InvalidOperationException($"LimitType is invalid for {_type} Property"),
                };
                set
                {
                    switch (_type)
                    {
                        case InternalType.LimitType: _mergePb.limitTypeConfig.value = value; break;
                        default:
                            throw new InvalidOperationException($"LimitType is invalid for {_type} Property");
                    }
                }
            }

            /// <summary>
            /// The overridden value of AllowCollision, AllowGrabbing, or AllowPosing property.
            /// </summary>
            /// <exception cref="InvalidOperationException">Throws when this property is not AllowCollision, AllowGrabbing, or AllowPosing property.</exception>
            [PublicAPI]
            public VRCPhysBoneBase.AdvancedBool PermissionOverrideValue
            {
                get => _type switch
                {
                    InternalType.AllowCollision => _mergePb.allowCollisionConfig.value,
                    InternalType.AllowGrabbing => _mergePb.allowGrabbingConfig.value,
                    InternalType.AllowPosing => _mergePb.allowPosingConfig.value,
                    _ => throw new InvalidOperationException($"PermissionOverrideValue is invalid for {_type} Property"),
                };
                set
                {
                    switch (_type)
                    {
                        case InternalType.AllowCollision: _mergePb.allowCollisionConfig.value = value; break;
                        case InternalType.AllowGrabbing: _mergePb.allowGrabbingConfig.value = value; break;
                        case InternalType.AllowPosing: _mergePb.allowPosingConfig.value = value; break;
                        default:
                            throw new InvalidOperationException($"PermissionOverrideValue is invalid for {_type} Property");
                    }
                }
            }

            /// <summary>
            /// The overridden value of PermissionFilter for AllowCollision, AllowGrabbing, or AllowPosing property.
            /// </summary>
            /// <exception cref="InvalidOperationException">Throws when this property is not AllowCollision, AllowGrabbing, or AllowPosing property.</exception>
            [PublicAPI]
            public VRCPhysBoneBase.PermissionFilter PermissionFilterOverrideValue
            {
                get => _type switch
                {
                    InternalType.AllowCollision => _mergePb.allowCollisionConfig.filter,
                    InternalType.AllowGrabbing => _mergePb.allowGrabbingConfig.filter,
                    InternalType.AllowPosing => _mergePb.allowPosingConfig.filter,
                    _ => throw new InvalidOperationException($"PermissionFilterOverrideValue is invalid for {_type} Property"),
                };
                set
                {
                    switch (_type)
                    {
                        case InternalType.AllowCollision: _mergePb.allowCollisionConfig.filter = value; break;
                        case InternalType.AllowGrabbing: _mergePb.allowGrabbingConfig.filter = value; break;
                        case InternalType.AllowPosing: _mergePb.allowPosingConfig.filter = value; break;
                        default:
                            throw new InvalidOperationException($"PermissionFilterOverrideValue is invalid for {_type} Property");
                    }
                }
            }
            

            /// <summary>
            /// The overridden value of float type properties.
            /// </summary>
            /// <exception cref="InvalidOperationException">Throws when this property is not float type property.</exception>
            [PublicAPI]
            public float FloatOverrideValue
            {
                get => _type switch
                {
                    InternalType.Pull => _mergePb.pullConfig.value,
                    InternalType.Spring => _mergePb.springConfig.value,
                    InternalType.Stiffness => _mergePb.stiffnessConfig.value,
                    InternalType.Gravity => _mergePb.gravityConfig.value,
                    InternalType.GravityFalloff => _mergePb.gravityFalloffConfig.value,
                    InternalType.Immobile => _mergePb.immobileConfig.value,
                    InternalType.MaxAngleX => _mergePb.maxAngleXConfig.value,
                    InternalType.MaxAngleZ => _mergePb.maxAngleZConfig.value,
                    InternalType.Radius => _mergePb.radiusConfig.value,
                    InternalType.StretchMotion => _mergePb.stretchMotionConfig.value,
                    InternalType.MaxStretch => _mergePb.maxStretchConfig.value,
                    InternalType.MaxSquish => _mergePb.maxSquishConfig.value,
                    InternalType.GrabMovement => _mergePb.grabMovementConfig.value,
                    _ => throw new InvalidOperationException($"FloatOverrideValue is invalid for {_type} Property"),
                };
                set
                {
                    switch (_type)
                    {
                        case InternalType.Pull: _mergePb.pullConfig.value = value; break;
                        case InternalType.Spring: _mergePb.springConfig.value = value; break;
                        case InternalType.Stiffness: _mergePb.stiffnessConfig.value = value; break;
                        case InternalType.Gravity: _mergePb.gravityConfig.value = value; break;
                        case InternalType.GravityFalloff: _mergePb.gravityFalloffConfig.value = value; break;
                        case InternalType.Immobile: _mergePb.immobileConfig.value = value; break;
                        case InternalType.MaxAngleX: _mergePb.maxAngleXConfig.value = value; break;
                        case InternalType.MaxAngleZ: _mergePb.maxAngleZConfig.value = value; break;
                        case InternalType.Radius: _mergePb.radiusConfig.value = value; break;
                        case InternalType.StretchMotion: _mergePb.stretchMotionConfig.value = value; break;
                        case InternalType.MaxStretch: _mergePb.maxStretchConfig.value = value; break;
                        case InternalType.MaxSquish: _mergePb.maxSquishConfig.value = value; break;
                        case InternalType.GrabMovement: _mergePb.grabMovementConfig.value = value; break;
                        default:
                            throw new InvalidOperationException($"FloatOverrideValue is invalid for {_type} Property");
                    }
                }
            }

            /// <summary>
            /// The overridden value of float curve type properties.
            ///
            /// This is only valid for properties with curve and single float.
            /// This cannot be used for 3d vector properties with 3 curves.
            /// </summary>
            /// <exception cref="InvalidOperationException">Throws when this property is not float curve type property.</exception>
            [PublicAPI]
            public AnimationCurve FloatCurveOverrideValue
            {
                get => _type switch
                {
                    InternalType.Pull => _mergePb.pullConfig.curve,
                    InternalType.Spring => _mergePb.springConfig.curve,
                    InternalType.Stiffness => _mergePb.stiffnessConfig.curve,
                    InternalType.Gravity => _mergePb.gravityConfig.curve,
                    InternalType.GravityFalloff => _mergePb.gravityFalloffConfig.curve,
                    InternalType.Immobile => _mergePb.immobileConfig.curve,
                    InternalType.MaxAngleX => _mergePb.maxAngleXConfig.curve,
                    InternalType.MaxAngleZ => _mergePb.maxAngleZConfig.curve,
                    InternalType.Radius => _mergePb.radiusConfig.curve,
                    InternalType.MaxStretch => _mergePb.maxStretchConfig.curve,
                    _ => throw new InvalidOperationException($"FloatCurveOverrideValue is invalid for {_type} Property"),
                } ?? new AnimationCurve();
                set
                {
                    switch (_type)
                    {
                        case InternalType.Pull: _mergePb.pullConfig.curve = value; break;
                        case InternalType.Spring: _mergePb.springConfig.curve = value; break;
                        case InternalType.Stiffness: _mergePb.stiffnessConfig.curve = value; break;
                        case InternalType.Gravity: _mergePb.gravityConfig.curve = value; break;
                        case InternalType.GravityFalloff: _mergePb.gravityFalloffConfig.curve = value; break;
                        case InternalType.Immobile: _mergePb.immobileConfig.curve = value; break;
                        case InternalType.MaxAngleX: _mergePb.maxAngleXConfig.curve = value; break;
                        case InternalType.MaxAngleZ: _mergePb.maxAngleZConfig.curve = value; break;
                        case InternalType.Radius: _mergePb.radiusConfig.curve = value; break;
                        case InternalType.MaxStretch: _mergePb.maxStretchConfig.curve = value; break;
                        default:
                            throw new InvalidOperationException($"FloatCurveOverrideValue is invalid for {_type} Property");
                    }
                }
            }

            /// <summary>
            /// The overridden value of EndPointPosition or LimitRotation property.
            /// </summary>
            /// <exception cref="InvalidOperationException">
            /// Throws when this property is not EndPointPosition or LimitRotation property.
            /// </exception>
            [PublicAPI]
            public Vector3 PositionOverrideValue
            {
                get => _type switch
                {
                    InternalType.EndPointPosition => _mergePb.endpointPositionConfig.value,
                    InternalType.LimitRotation => _mergePb.limitRotationConfig.value,
                    _ => throw new InvalidOperationException($"PositionOverrideValue is invalid for {_type} Property"),
                };
                set
                {
                    switch (_type)
                    {
                        case InternalType.EndPointPosition: _mergePb.endpointPositionConfig.value = value; break;
                        case InternalType.LimitRotation: _mergePb.limitRotationConfig.value = value; break;
                        default:
                            throw new InvalidOperationException($"PositionOverrideValue is invalid for {_type} Property");
                    }
                }
            }

            /// <summary>
            /// The overridden curve for X axis of LimitRotation property.
            /// </summary>
            /// <exception cref="InvalidOperationException">
            /// Throws when this property is not LimitRotation property.
            /// </exception>
            [PublicAPI]
            public AnimationCurve LimitRotationCurveXOverrideValue
            {
                get => _type switch
                {
                    InternalType.LimitRotation => _mergePb.limitRotationConfig.curveX,
                    _ => throw new InvalidOperationException(
                        $"LimitRotationCurveXOverrideValue is invalid for {_type} Property"),
                } ?? new AnimationCurve();
                set
                {
                    switch (_type)
                    {
                        case InternalType.LimitRotation: _mergePb.limitRotationConfig.curveX = value; break;
                        default:
                            throw new InvalidOperationException(
                                $"LimitRotationCurveXOverrideValue is invalid for {_type} Property");
                    }
                }
            }

            /// <summary>
            /// The overridden curve for Y axis of LimitRotation property.
            /// </summary>
            /// <exception cref="InvalidOperationException">
            /// Throws when this property is not LimitRotation property.
            /// </exception>
            [PublicAPI]
            public AnimationCurve LimitRotationCurveYOverrideValue
            {
                get => _type switch
                {
                    InternalType.LimitRotation => _mergePb.limitRotationConfig.curveY,
                    _ => throw new InvalidOperationException(
                        $"LimitRotationCurveYOverrideValue is invalid for {_type} Property"),
                } ?? new AnimationCurve();
                set
                {
                    switch (_type)
                    {
                        case InternalType.LimitRotation: _mergePb.limitRotationConfig.curveY = value; break;
                        default:
                            throw new InvalidOperationException(
                                $"LimitRotationCurveYOverrideValue is invalid for {_type} Property");
                    }
                }
            }

            /// <summary>
            /// The overridden curve for Z axis of LimitRotation property.
            /// </summary>
            /// <exception cref="InvalidOperationException">
            /// Throws when this property is not LimitRotation property.
            /// </exception>
            [PublicAPI]
            public AnimationCurve LimitRotationCurveZOverrideValue
            {
                get => _type switch
                {
                    InternalType.LimitRotation => _mergePb.limitRotationConfig.curveZ,
                    _ => throw new InvalidOperationException(
                        $"LimitRotationCurveZOverrideValue is invalid for {_type} Property"),
                } ?? new AnimationCurve();
                set
                {
                    switch (_type)
                    {
                        case InternalType.LimitRotation: _mergePb.limitRotationConfig.curveZ = value; break;
                        default:
                            throw new InvalidOperationException(
                                $"LimitRotationCurveZOverrideValue is invalid for {_type} Property");
                    }
                }
            }
        }

        [PublicAPI]
        public enum OverrideStatus
        {
            Copied,
            Overridden,
            /// <summary>
            /// Clear by adding new objects as a alternative to Copy or Override.
            /// This is currently used in EndPointPosition property.
            /// This is valid for any combination of multiple source PhysBones without making behavior capability lost.
            /// This was the default option in traditional version of MergePhysBone.
            /// </summary>
            Cleared,
            /// <summary>
            /// Merge list by just connecting both lists.
            /// This is currently used in Colliders property.
            /// </summary>
            Merged,
            /// <summary>
            /// Fixes 'roll' of each bone according to the curve.
            /// This is currently used in LimitRotation property.
            /// </summary>
            Fixed,
        }
        #endregion
    }
}

#endif
