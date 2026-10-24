#if AAO_VRCSDK3_AVATARS

using System;
using NUnit.Framework;
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDKBase;

namespace Anatawa12.AvatarOptimizer.Test
{
    public class EnumValues
    {
        [Test]
        public void PlayableLayerControl_BlendableLayer() => TestEnum(new[]
        {
            VRC_PlayableLayerControl.BlendableLayer.Action,
            VRC_PlayableLayerControl.BlendableLayer.FX,
            VRC_PlayableLayerControl.BlendableLayer.Gesture,
            VRC_PlayableLayerControl.BlendableLayer.Additive,
        });

        [Test]
        public void AnimatorLayerControl_BlendableLayer() => TestEnum(new[]
        {
            VRC_AnimatorLayerControl.BlendableLayer.Action,
            VRC_AnimatorLayerControl.BlendableLayer.FX,
            VRC_AnimatorLayerControl.BlendableLayer.Gesture,
            VRC_AnimatorLayerControl.BlendableLayer.Additive,
        });

        [Test]
        public void AvatarDescriptor_LipSyncStyle() => TestEnum(new[]
        {
            VRC_AvatarDescriptor.LipSyncStyle.Default,
            VRC_AvatarDescriptor.LipSyncStyle.JawFlapBone,
            VRC_AvatarDescriptor.LipSyncStyle.JawFlapBlendShape,
            VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape,
            VRC_AvatarDescriptor.LipSyncStyle.VisemeParameterOnly,
#if AAO_VRCSDK3_AVATARS_NATIVE_FACE_TRACKING
            VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShapeFaceTracking,
#endif
        });

        [Test]
        public void VRCAvatarDescriptor_ColliderConfig_State() => TestEnum(new[]
        {
            VRCAvatarDescriptor.ColliderConfig.State.Automatic,
            VRCAvatarDescriptor.ColliderConfig.State.Custom,
            VRCAvatarDescriptor.ColliderConfig.State.Disabled,
        });

        [Test]
        public void VRCAvatarDescriptor_EyelidType() => TestEnum(new[]
        {
            VRCAvatarDescriptor.EyelidType.None,
            VRCAvatarDescriptor.EyelidType.Bones,
            VRCAvatarDescriptor.EyelidType.Blendshapes,
#if AAO_VRCSDK3_AVATARS_NATIVE_FACE_TRACKING
            VRCAvatarDescriptor.EyelidType.FaceTracking,
#endif
        });

#if AAO_VRCSDK3_AVATARS_NATIVE_FACE_TRACKING
        [Test]
        public void VRCAvatarDescriptor_FaceTrackingKey() => TestEnum(new[]
        {
            VRCAvatarDescriptor.FaceTrackingKey.EyeLookOutRight,
            VRCAvatarDescriptor.FaceTrackingKey.EyeLookInRight,
            VRCAvatarDescriptor.FaceTrackingKey.EyeLookUpRight,
            VRCAvatarDescriptor.FaceTrackingKey.EyeLookDownRight,
            VRCAvatarDescriptor.FaceTrackingKey.EyeLookOutLeft,
            VRCAvatarDescriptor.FaceTrackingKey.EyeLookInLeft,
            VRCAvatarDescriptor.FaceTrackingKey.EyeLookUpLeft,
            VRCAvatarDescriptor.FaceTrackingKey.EyeLookDownLeft,
            VRCAvatarDescriptor.FaceTrackingKey.EyeClosedRight,
            VRCAvatarDescriptor.FaceTrackingKey.EyeClosedLeft,
            VRCAvatarDescriptor.FaceTrackingKey.EyeDilationRight,
            VRCAvatarDescriptor.FaceTrackingKey.EyeDilationLeft,
            VRCAvatarDescriptor.FaceTrackingKey.EyeConstrictRight,
            VRCAvatarDescriptor.FaceTrackingKey.EyeConstrictLeft,
            VRCAvatarDescriptor.FaceTrackingKey.EyeSquintRight,
            VRCAvatarDescriptor.FaceTrackingKey.EyeSquintLeft,
            VRCAvatarDescriptor.FaceTrackingKey.EyeWideRight,
            VRCAvatarDescriptor.FaceTrackingKey.EyeWideLeft,
            VRCAvatarDescriptor.FaceTrackingKey.BrowPinchRight,
            VRCAvatarDescriptor.FaceTrackingKey.BrowPinchLeft,
            VRCAvatarDescriptor.FaceTrackingKey.BrowLowererRight,
            VRCAvatarDescriptor.FaceTrackingKey.BrowLowererLeft,
            VRCAvatarDescriptor.FaceTrackingKey.BrowInnerUpRight,
            VRCAvatarDescriptor.FaceTrackingKey.BrowInnerUpLeft,
            VRCAvatarDescriptor.FaceTrackingKey.BrowOuterUpRight,
            VRCAvatarDescriptor.FaceTrackingKey.BrowOuterUpLeft,
            VRCAvatarDescriptor.FaceTrackingKey.NasalDilationRight,
            VRCAvatarDescriptor.FaceTrackingKey.NasalDilationLeft,
            VRCAvatarDescriptor.FaceTrackingKey.NasalConstrictRight,
            VRCAvatarDescriptor.FaceTrackingKey.NasalConstrictLeft,
            VRCAvatarDescriptor.FaceTrackingKey.CheekSquintRight,
            VRCAvatarDescriptor.FaceTrackingKey.CheekSquintLeft,
            VRCAvatarDescriptor.FaceTrackingKey.CheekPuffRight,
            VRCAvatarDescriptor.FaceTrackingKey.CheekPuffLeft,
            VRCAvatarDescriptor.FaceTrackingKey.CheekSuckRight,
            VRCAvatarDescriptor.FaceTrackingKey.CheekSuckLeft,
            VRCAvatarDescriptor.FaceTrackingKey.JawOpen,
            VRCAvatarDescriptor.FaceTrackingKey.JawRight,
            VRCAvatarDescriptor.FaceTrackingKey.JawLeft,
            VRCAvatarDescriptor.FaceTrackingKey.JawForward,
            VRCAvatarDescriptor.FaceTrackingKey.JawBackward,
            VRCAvatarDescriptor.FaceTrackingKey.JawClench,
            VRCAvatarDescriptor.FaceTrackingKey.JawMandibleRaise,
            VRCAvatarDescriptor.FaceTrackingKey.MouthClosed,
            VRCAvatarDescriptor.FaceTrackingKey.LipSuckUpperRight,
            VRCAvatarDescriptor.FaceTrackingKey.LipSuckUpperLeft,
            VRCAvatarDescriptor.FaceTrackingKey.LipSuckLowerRight,
            VRCAvatarDescriptor.FaceTrackingKey.LipSuckLowerLeft,
            VRCAvatarDescriptor.FaceTrackingKey.LipSuckCornerRight,
            VRCAvatarDescriptor.FaceTrackingKey.LipSuckCornerLeft,
            VRCAvatarDescriptor.FaceTrackingKey.LipFunnelUpperRight,
            VRCAvatarDescriptor.FaceTrackingKey.LipFunnelUpperLeft,
            VRCAvatarDescriptor.FaceTrackingKey.LipFunnelLowerRight,
            VRCAvatarDescriptor.FaceTrackingKey.LipFunnelLowerLeft,
            VRCAvatarDescriptor.FaceTrackingKey.LipPuckerUpperRight,
            VRCAvatarDescriptor.FaceTrackingKey.LipPuckerUpperLeft,
            VRCAvatarDescriptor.FaceTrackingKey.LipPuckerLowerRight,
            VRCAvatarDescriptor.FaceTrackingKey.LipPuckerLowerLeft,
            VRCAvatarDescriptor.FaceTrackingKey.MouthUpperUpRight,
            VRCAvatarDescriptor.FaceTrackingKey.MouthUpperUpLeft,
            VRCAvatarDescriptor.FaceTrackingKey.MouthUpperDeepenRight,
            VRCAvatarDescriptor.FaceTrackingKey.MouthUpperDeepenLeft,
            VRCAvatarDescriptor.FaceTrackingKey.NoseSneerRight,
            VRCAvatarDescriptor.FaceTrackingKey.NoseSneerLeft,
            VRCAvatarDescriptor.FaceTrackingKey.MouthLowerDownRight,
            VRCAvatarDescriptor.FaceTrackingKey.MouthLowerDownLeft,
            VRCAvatarDescriptor.FaceTrackingKey.MouthUpperRight,
            VRCAvatarDescriptor.FaceTrackingKey.MouthUpperLeft,
            VRCAvatarDescriptor.FaceTrackingKey.MouthLowerRight,
            VRCAvatarDescriptor.FaceTrackingKey.MouthLowerLeft,
            VRCAvatarDescriptor.FaceTrackingKey.MouthCornerPullRight,
            VRCAvatarDescriptor.FaceTrackingKey.MouthCornerPullLeft,
            VRCAvatarDescriptor.FaceTrackingKey.MouthCornerSlantRight,
            VRCAvatarDescriptor.FaceTrackingKey.MouthCornerSlantLeft,
            VRCAvatarDescriptor.FaceTrackingKey.MouthFrownRight,
            VRCAvatarDescriptor.FaceTrackingKey.MouthFrownLeft,
            VRCAvatarDescriptor.FaceTrackingKey.MouthStretchRight,
            VRCAvatarDescriptor.FaceTrackingKey.MouthStretchLeft,
            VRCAvatarDescriptor.FaceTrackingKey.MouthDimpleRight,
            VRCAvatarDescriptor.FaceTrackingKey.MouthDimpleLeft,
            VRCAvatarDescriptor.FaceTrackingKey.MouthRaiserUpper,
            VRCAvatarDescriptor.FaceTrackingKey.MouthRaiserLower,
            VRCAvatarDescriptor.FaceTrackingKey.MouthPressRight,
            VRCAvatarDescriptor.FaceTrackingKey.MouthPressLeft,
            VRCAvatarDescriptor.FaceTrackingKey.MouthTightenerRight,
            VRCAvatarDescriptor.FaceTrackingKey.MouthTightenerLeft,
            VRCAvatarDescriptor.FaceTrackingKey.TongueOut,
            VRCAvatarDescriptor.FaceTrackingKey.TongueUp,
            VRCAvatarDescriptor.FaceTrackingKey.TongueDown,
            VRCAvatarDescriptor.FaceTrackingKey.TongueRight,
            VRCAvatarDescriptor.FaceTrackingKey.TongueLeft,
            VRCAvatarDescriptor.FaceTrackingKey.TongueRoll,
            VRCAvatarDescriptor.FaceTrackingKey.TongueBendDown,
            VRCAvatarDescriptor.FaceTrackingKey.TongueCurlUp,
            VRCAvatarDescriptor.FaceTrackingKey.TongueSquish,
            VRCAvatarDescriptor.FaceTrackingKey.TongueFlat,
            VRCAvatarDescriptor.FaceTrackingKey.TongueTwistRight,
            VRCAvatarDescriptor.FaceTrackingKey.TongueTwistLeft,
            VRCAvatarDescriptor.FaceTrackingKey.SoftPalateClose,
            VRCAvatarDescriptor.FaceTrackingKey.ThroatSwallow,
            VRCAvatarDescriptor.FaceTrackingKey.NeckFlexRight,
            VRCAvatarDescriptor.FaceTrackingKey.NeckFlexLeft,
            VRCAvatarDescriptor.FaceTrackingKey.Max,
        });
#endif

        [Test]
        public void ContactReceiver_ReceiverType() => TestEnum(new[]
        {
            ContactReceiver.ReceiverType.Constant,
            ContactReceiver.ReceiverType.OnEnter,
            ContactReceiver.ReceiverType.Proximity,
        });

        [Test]
        public void VRCPhysBoneBase_Version() => TestEnum(new[]
        {
            VRCPhysBoneBase.Version.Version_1_0,
            VRCPhysBoneBase.Version.Version_1_1,
        });

        [Test]
        public void VRCPhysBoneColliderBase_ShapeType() => TestEnum(new[]
        {
            VRCPhysBoneColliderBase.ShapeType.Sphere,
            VRCPhysBoneColliderBase.ShapeType.Capsule,
            VRCPhysBoneColliderBase.ShapeType.Plane,
        });

#if AAO_VRCSDK3_AVATARS_VRC_RAYCAST_V1_1
        [Test]
        public void VRCRaycast_RaycastComponentVersion() => TestEnum(new[]
        {
            VRCRaycast.RaycastComponentVersion.Version_1_0,
            VRCRaycast.RaycastComponentVersion.Version_1_1,
        });
#endif

        private void TestEnum<T>(T[] values) where T : Enum
        {
            Assert.That(Enum.GetValues(typeof(T)), Is.EquivalentTo(values));
        }
    }
}

#endif