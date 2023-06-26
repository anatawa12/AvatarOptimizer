using System;
using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.ErrorReporting;
using CustomLocalization4EditorExtension;
using JetBrains.Annotations;
using UnityEngine;
using VRC.Dynamics;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Optimizer/Merge PhysBone")]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    internal class MergePhysBone : AvatarTagComponent, IStaticValidated
    {
        [CL4EELocalized("MergePhysBone:prop:makeParent", "MergePhysBone:tooltip:makeParent")]
        public bool makeParent;

        #region OverrideAndValue

        public VersionConfig versionConfig;

        #region == Transform ==
        // rootTransform
        // ignoreTransforms
        public EndPointPositionConfig endpointPositionConfig;
        // multiChildType
        #endregion

        #region == Forces ==
        public IntegrationTypeConfig integrationTypeConfig;
        public Curve0To1Config pullConfig;
        // spring a.k.a. Momentum
        public Curve0To1Config springConfig;
        public Curve0To1Config stiffnessConfig;
        public CurveM1To1Config gravityConfig;
        public Curve0To1Config gravityFalloffConfig;
        public ImmobileTypeConfig immobileTypeConfig;
        public Curve0To1Config immobileConfig;
        #endregion
        #region == Limits ==
        public LimitTypeConfig limitTypeConfig;
        public Curve0To180Config maxAngleXConfig = new Curve0To180Config(45f);
        public Curve0To90Config maxAngleZConfig = new Curve0To90Config(45f);
        public CurveVector3Config limitRotationConfig;
        #endregion
        #region == Collision ==
        public CurveNoLimitConfig radiusConfig;
        public PermissionConfig allowCollisionConfig;
        public CollidersConfig collidersConfig;
        #endregion
        #region == Stretch & Squish ==
        public Curve0To1Config stretchMotionConfig;
        public CurveNoLimitConfig maxStretchConfig;
        public Curve0To1Config maxSquishConfig;
        #endregion
        #region == Grab & Pose ==
        public PermissionConfig allowGrabbingConfig;
        public PermissionConfig allowPosingConfig;
        public Float0To1Config grabMovementConfig;
        public BoolConfig snapToHandConfig;
        #endregion
        #region == Options ==
        public ParameterConfig parameterConfig;
        public IsAnimatedConfig isAnimatedConfig;
        public BoolConfig resetWhenDisabledConfig;
        #endregion

        [Serializable]
        public struct EndPointPositionConfig
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
        public struct Curve0To1Config
        {
            public bool @override;
            [Range(0f, 1f)]
            public float value;
            public AnimationCurve curve;
        }

        [Serializable]
        public struct CurveM1To1Config
        {
            public bool @override;
            [Range(-1f, 1f)]
            public float value;
            public AnimationCurve curve;
        }

        [Serializable]
        public struct Curve0To180Config
        {
            public bool @override;
            [Range(0f, 180f)]
            public float value;
            public AnimationCurve curve;
            
            public Curve0To180Config(float value) : this()
            {
                this.value = value;
            }
        }

        [Serializable]
        public struct Curve0To90Config
        {
            public bool @override;
            [Range(0f, 90f)]
            public float value;
            public AnimationCurve curve;

            public Curve0To90Config(float value) : this()
            {
                this.value = value;
            }
        }

        [Serializable]
        public struct CurveNoLimitConfig
        {
            public bool @override;
            public float value;
            public AnimationCurve curve;
        }

        [Serializable]
        public struct CurveVector3Config
        {
            public bool @override;
            public Vector3 value;
            public AnimationCurve curveX;
            public AnimationCurve curveY;
            public AnimationCurve curveZ;
        }

        [Serializable]
        public struct Float0To1Config
        {
            public bool @override;
            [Range(0f, 1f)]
            public float value;
        }

        [Serializable]
        public struct BoolConfig
        {
            public bool @override;
            public bool value;
        }

        [Serializable]
        public struct VersionConfig
        {
            public bool @override;
            public VRCPhysBoneBase.Version value;
        }

        [Serializable]
        public struct IntegrationTypeConfig
        {
            public bool @override;
            public VRCPhysBoneBase.IntegrationType value;
        }

        [Serializable]
        public struct ImmobileTypeConfig
        {
            public bool @override;
            public VRCPhysBoneBase.ImmobileType value;
        }

        [Serializable]
        public struct LimitTypeConfig
        {
            public bool @override;
            public VRCPhysBoneBase.LimitType value;
        }

        [Serializable]
        public struct PermissionConfig
        {
            public bool @override;
            public VRCPhysBoneBase.AdvancedBool value;
            public VRCPhysBoneBase.PermissionFilter filter;
        }

        [Serializable]
        public struct CollidersConfig
        {
            public CollidersOverride @override;
            [CanBeNull] public List<VRCPhysBoneColliderBase> value;
            
            public enum CollidersOverride
            {
                Copy,
                Override,
                Merge,
            }
        }

        [Serializable]
        public struct ParameterConfig
        {
            //public bool @override; // always
            public string value;
        }

        [Serializable]
        public struct IsAnimatedConfig
        {
            //public bool @override; // always
            public bool value;
        }

        #endregion

        [CL4EELocalized("MergePhysBone:prop:components")]
        public PrefabSafeSet.VRCPhysBoneBaseSet componentsSet;

        public MergePhysBone()
        {
            componentsSet = new PrefabSafeSet.VRCPhysBoneBaseSet(this);
        }
    }

    internal enum CollidersSettings
    {
        Copy,
        Merge,
        Override,
    }
}
