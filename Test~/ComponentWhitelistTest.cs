using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.APIInternal;
using NUnit.Framework;
using UnityEngine;
using VRC.SDKBase.Validation;
using BaseAvatarValidation = VRC.SDKBase.Validation.AvatarValidation;

namespace Anatawa12.AvatarOptimizer.Test
{
    public class ComponentWhitelistTest
    {
        public static IEnumerable<string> WhitelistedTypes =>
            BaseAvatarValidation.ComponentTypeWhiteListCommon
                .Concat(BaseAvatarValidation.ComponentTypeWhiteListSdk3);

        [Test]
        public void TestNameList()
        {
            Assert.That(WhitelistedTypes, Is.EquivalentTo(KnownWhitelist));
        }

        [TestCaseSource(nameof(KnownTypes))]
        public void TestEachType(Type type)
        {
            Assert.That(ComponentInfoRegistry.TryGetInformation(type, out _), Is.True);
        }

        public static IEnumerable<Type> KnownTypes()
        {
            return ValidationUtils.WhitelistedTypes("avatar-sdk3", WhitelistedTypes)
                .Where(x => typeof(Component).IsAssignableFrom(x));
        }

        private static string[] KnownWhitelist =
        {
            // common whitelist
            "DynamicBone",
            "DynamicBoneCollider",
            "RootMotion.FinalIK.IKExecutionOrder",
            "RootMotion.FinalIK.VRIK",
            "RootMotion.FinalIK.FullBodyBipedIK",
            "RootMotion.FinalIK.LimbIK",
            "RootMotion.FinalIK.AimIK",
            "RootMotion.FinalIK.BipedIK",
            "RootMotion.FinalIK.GrounderIK",
            "RootMotion.FinalIK.GrounderFBBIK",
            "RootMotion.FinalIK.GrounderVRIK",
            "RootMotion.FinalIK.GrounderQuadruped",
            "RootMotion.FinalIK.TwistRelaxer",
            "RootMotion.FinalIK.ShoulderRotator",
            "RootMotion.FinalIK.FBBIKArmBending",
            "RootMotion.FinalIK.FBBIKHeadEffector",
            "RootMotion.FinalIK.FABRIK",
            "RootMotion.FinalIK.FABRIKChain",
            "RootMotion.FinalIK.FABRIKRoot",
            "RootMotion.FinalIK.CCDIK",
            "RootMotion.FinalIK.RotationLimit",
            "RootMotion.FinalIK.RotationLimitHinge",
            "RootMotion.FinalIK.RotationLimitPolygonal",
            "RootMotion.FinalIK.RotationLimitSpline",
            "UnityEngine.Cloth",
            "UnityEngine.Light",
            "UnityEngine.BoxCollider",
            "UnityEngine.SphereCollider",
            "UnityEngine.CapsuleCollider",
            "UnityEngine.Rigidbody",
            "UnityEngine.Joint",
            "UnityEngine.Animations.AimConstraint",
            "UnityEngine.Animations.LookAtConstraint",
            "UnityEngine.Animations.ParentConstraint",
            "UnityEngine.Animations.PositionConstraint",
            "UnityEngine.Animations.RotationConstraint",
            "UnityEngine.Animations.ScaleConstraint",
            "UnityEngine.Camera",
            "UnityEngine.AudioSource",
            "ONSPAudioSource",
            "VRC.Core.PipelineSaver",
            "VRC.Core.PipelineManager",
            "UnityEngine.Transform",
            "UnityEngine.Animator",
            "UnityEngine.SkinnedMeshRenderer",
            "LimbIK", // VRCSDK's LimbIK
            "LoadingAvatarTextureAnimation",
            "UnityEngine.MeshFilter",
            "UnityEngine.MeshRenderer",
            "UnityEngine.Animation",
            "UnityEngine.ParticleSystem",
            "UnityEngine.ParticleSystemRenderer",
            "UnityEngine.TrailRenderer",
            "UnityEngine.FlareLayer",
            "UnityEngine.GUILayer",
            "UnityEngine.LineRenderer",
            "RealisticEyeMovements.EyeAndHeadAnimator",
            "RealisticEyeMovements.LookTargetController",

            // SDK3 whitelist
            "VRC.SDK3.Avatars.Components.VRCSpatialAudioSource",
            "VRC.SDK3.VRCTestMarker",
            "VRC.SDK3.Avatars.Components.VRCAvatarDescriptor",
            "VRC.SDK3.Avatars.Components.VRCStation",
            "VRC.SDK3.Avatars.Components.VRCImpostorSettings",
            "VRC.SDK3.Avatars.Components.VRCImpostorEnvironment",
            "VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone",
            "VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBoneCollider",
            "VRC.SDK3.Dynamics.Contact.Components.VRCContactSender",
            "VRC.SDK3.Dynamics.Contact.Components.VRCContactReceiver",
        };
    }
}