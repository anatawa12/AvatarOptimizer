using System;
using NUnit.Framework;
using VRC.SDK3.Avatars.Components;
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
        });

        private void TestEnum<T>(T[] values) where T : Enum
        {
            Assert.That(Enum.GetValues(typeof(T)), Is.EquivalentTo(values));
        }
    }
}
