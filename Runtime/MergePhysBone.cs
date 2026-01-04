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

        [NotKeyable, SerializeField] internal ValueWithOverride<VRCPhysBoneBase.Version> versionConfig;
        // == Transform ==
        // rootTransform
        // ignoreTransforms
        [NotKeyable, SerializeField] internal EndPointPositionConfig endpointPositionConfig;
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
        [NotKeyable, SerializeField] internal LimitRotationConfig limitRotationConfig;
        // == Collision ==
        [NotKeyable, SerializeField] internal CurveFloatConfig radiusConfig;
        [NotKeyable, SerializeField] internal PermissionConfig allowCollisionConfig;
        [NotKeyable, SerializeField] internal CollidersConfig collidersConfig;
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
            endpointPositionConfig.@override = EndPointPositionConfig.Override.Copy;
        }

        [Serializable]
        internal struct EndPointPositionConfig
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
            public AnimationCurve curve;

            public CurveFloatConfig(float value) : this()
            {
                this.value = value;
                curve = new AnimationCurve();
            }
        }

        [Serializable]
        internal struct LimitRotationConfig
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
        internal struct CollidersConfig
        {
            public CollidersOverride @override;
            public List<VRCPhysBoneColliderBase>? value;
            
            public enum CollidersOverride
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
