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
    [AddComponentMenu("Avatar Optimizer/AAO Merge PhysBone")]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/merge-physbone/")]
    // INetworkID is implemented to make it possible to assign networkID to this components GameObject
    // Note: when MergePhysBone become a public API, we should consider removing INetworkID implementation
    internal class MergePhysBone : AvatarTagComponent, VRC.SDKBase.INetworkID, ISerializationCallbackReceiver
    {
        [NotKeyable]
        [AAOLocalized("MergePhysBone:prop:makeParent", "MergePhysBone:tooltip:makeParent")]
        public bool makeParent;

        #region OverrideAndValue

        [NotKeyable] public VersionConfig versionConfig;

        #region == Transform ==
        // rootTransform
        // ignoreTransforms
        [NotKeyable] public EndPointPositionConfig endpointPositionConfig;
        [NotKeyable] public BoolConfig ignoreOtherPhysBones;
        // multiChildType
        #endregion

        #region == Forces ==
        [NotKeyable] public IntegrationTypeConfig integrationTypeConfig;
        [NotKeyable] public Curve0To1Config pullConfig;
        // spring a.k.a. Momentum
        [NotKeyable] public Curve0To1Config springConfig;
        [NotKeyable] public Curve0To1Config stiffnessConfig;
        [NotKeyable] public CurveM1To1Config gravityConfig;
        [NotKeyable] public Curve0To1Config gravityFalloffConfig;
        [NotKeyable] public ImmobileTypeConfig immobileTypeConfig;
        [NotKeyable] public Curve0To1Config immobileConfig;
        #endregion
        #region == Limits ==
        [NotKeyable] public LimitTypeConfig limitTypeConfig;
        [NotKeyable] public Curve0To180Config maxAngleXConfig = new Curve0To180Config(45f);
        [NotKeyable] public Curve0To90Config maxAngleZConfig = new Curve0To90Config(45f);
        [NotKeyable] public CurveVector3Config limitRotationConfig;
        #endregion
        #region == Collision ==
        [NotKeyable] public CurveNoLimitConfig radiusConfig;
        [NotKeyable] public PermissionConfig allowCollisionConfig;
        [NotKeyable] public CollidersConfig collidersConfig;
        #endregion
        #region == Stretch & Squish ==
        [NotKeyable] public Curve0To1Config stretchMotionConfig;
        [NotKeyable] public CurveNoLimitConfig maxStretchConfig;
        [NotKeyable] public Curve0To1Config maxSquishConfig;
        #endregion
        #region == Grab & Pose ==
        [NotKeyable] public PermissionConfig allowGrabbingConfig;
        [NotKeyable] public PermissionConfig allowPosingConfig;
        [NotKeyable] public Float0To1Config grabMovementConfig;
        [NotKeyable] public BoolConfig snapToHandConfig;
        #endregion
        #region == Options ==
        [NotKeyable] public ParameterConfig parameterConfig;
        [NotKeyable] public IsAnimatedConfig isAnimatedConfig;
        [NotKeyable] public BoolConfig resetWhenDisabledConfig;
        #endregion

        private void Reset()
        {
            // copy by default for newly added component
            endpointPositionConfig.@override = EndPointPositionConfig.Override.Copy;
        }

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
                curve = new AnimationCurve();
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
                curve = new AnimationCurve();
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
            public CurveOverride @override;
            public Vector3 value;
            public AnimationCurve curveX;
            public AnimationCurve curveY;
            public AnimationCurve curveZ;
            
            public enum CurveOverride
            {
                Copy,
                Override,
                // Change bone angle to match the curve
                Fix,
            }
        }

        [Serializable]
        public struct Float0To1Config
        {
            public bool @override;
            [Range(0f, 1f)]
            public float value;
        }

        [Serializable]
        public struct NewBoolConfig
        {
            public bool @override;
            public bool errorConflictedSettings;
            public bool value;
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

        [AAOLocalized("MergePhysBone:prop:components")]
        public PrefabSafeSet.PrefabSafeSet<VRCPhysBoneBase> componentsSet;

        public MergePhysBone()
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
    }

    internal enum CollidersSettings
    {
        Copy,
        Merge,
        Override,
    }
}

#endif
