using System;
using nadena.dev.ndmf;
using NUnit.Framework;
using UnityEngine;
#if AAO_VRCSDK3_AVATARS
using VRC.SDK3.Avatars.Components;
#endif

namespace Anatawa12.AvatarOptimizer.Test.AnimatorParserTest
{
    public class RegressionTests
    {
#if AAO_VRCSDK3_AVATARS
        // Regression test for AAO animator parser ordering: on a generic avatar,
        // Base layer should supersede FX layer when both animate the same property.
        //
        // In the original case, Gesture layer overrides FX but this checks with generic avatar so we use base instead.
        [Test]
        public void BaseLayerOverridesFxLayer()
        {
            var avatar = TestUtils.NewAvatar();
            var smrGO = new GameObject("Renderer", typeof(SkinnedMeshRenderer));
            smrGO.transform.parent = avatar.transform;
            var smr = smrGO.GetComponent<SkinnedMeshRenderer>();

            // FX layer animates blendshapes to 100, Base animates to 0 and
            // check if 0 or 100 at the end.

            var fxLayer = new AnimatorControllerBuilder("FX")
                .AddLayer("Base", a => a
                    .NewClipState("OverriddenByBase", clip => clip
                        .AddPropertyBinding("Renderer", typeof(SkinnedMeshRenderer), "blendShape.animatedByBase", AnimationCurve.Constant(0, 1, 100))
                    ))
                .Build();

            var baseLayer = new AnimatorControllerBuilder("base")
                .AddLayer("Base", a => a
                    .NewClipState("OverriddenByBase", clip => clip
                        .AddPropertyBinding("Renderer", typeof(SkinnedMeshRenderer), "blendShape.animatedByBase", AnimationCurve.Constant(0, 1, 0))
                    ))
                .Build();

            TestUtils.InitializeAvatarDescriptor(avatar);
            TestUtils.SetPlayableLayer(avatar, VRCAvatarDescriptor.AnimLayerType.FX, fxLayer);
            TestUtils.SetPlayableLayer(avatar, VRCAvatarDescriptor.AnimLayerType.Base, baseLayer);

            var context = new BuildContext(avatar, null, null);
            var modifications = new AnimatorParsersV2.AnimatorParser(mmdWorldCompatibility: false)
                .GatherAnimationModifications(context);

            var value = modifications.FloatNodes[(smr, "blendShape.animatedByBase")].Value;
            Assert.That(value.IsConstant, Is.True);
            Assert.That(value.ConstantValue, Is.EqualTo(100));
        }
#endif
    }
}