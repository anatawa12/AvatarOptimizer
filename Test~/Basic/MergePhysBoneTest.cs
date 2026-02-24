#if AAO_VRCSDK3_AVATARS

using System;
using Anatawa12.AvatarOptimizer.PrefabSafeSet;
using Anatawa12.AvatarOptimizer.Processors;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.PhysBone.Components;
using Random = UnityEngine.Random;

namespace Anatawa12.AvatarOptimizer.Test
{
    public class MergePhysBoneTest
    {
        private MergePhysBone CreateMergePhysBone(GameObject target, params VRCPhysBone[] sources)
        {
            var mergePhysBone = target.AddComponent<MergePhysBone>();
            var serialized = new SerializedObject(mergePhysBone);
            var set = PSSEditorUtil<VRCPhysBoneBase>.Create(serialized.FindProperty("componentsSet"),
                x => (VRCPhysBoneBase)x.objectReferenceValue,
                (x, v) => x.objectReferenceValue = v);

            foreach (var vrcPhysBone in sources)
                set.GetElementOf(vrcPhysBone).EnsureAdded();
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return mergePhysBone;
        }

        [Test]
        public void CopyTest()
        {
            var root = TestUtils.NewAvatar();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child1Component = AddConfigure(child1);
            var child2 = Utils.NewGameObject("child2", root.transform);
            var child2Component = AddConfigure(child2);
            var merged = Utils.NewGameObject("merged", root.transform);
            var mergePhysBone = CreateMergePhysBone(merged, child1Component, child2Component);
            mergePhysBone.endpointPositionConfig.@override = MergePhysBone.EndPointPositionConfigStruct.Override.Copy;

            MergePhysBoneProcessor.DoMerge(mergePhysBone, null);

            var mergedPhysBone = merged.GetComponent<VRCPhysBoneBase>();
            Assert.That(mergedPhysBone.pull, Is.EqualTo(0.4f));            
            Assert.That(mergedPhysBone.pullCurve, Is.EqualTo(AnimationCurve.Constant(0, 1, 0)));
            Assert.That(mergedPhysBone.gravity, Is.EqualTo(0.5f));
            Assert.That(mergedPhysBone.allowPosing, Is.EqualTo(VRCPhysBoneBase.AdvancedBool.False));
            Assert.That(mergedPhysBone.allowGrabbing, Is.EqualTo(VRCPhysBoneBase.AdvancedBool.True));
            Assert.That(mergedPhysBone.allowCollision, Is.EqualTo(VRCPhysBoneBase.AdvancedBool.Other));
            Assert.That(mergedPhysBone.collisionFilter.allowOthers, Is.EqualTo(false));
            Assert.That(mergedPhysBone.collisionFilter.allowSelf, Is.EqualTo(true));
            Assert.That(mergedPhysBone.endpointPosition, Is.EqualTo(Vector3.up));

            VRCPhysBone AddConfigure(GameObject go)
            {
                var physBone = go.AddComponent<VRCPhysBone>();
                physBone.pull = 0.4f;
                physBone.pullCurve = AnimationCurve.Linear(0, 0, 1, 1);
                physBone.gravity = 0.5f;
                physBone.allowPosing = VRCPhysBoneBase.AdvancedBool.False;
                physBone.allowGrabbing = VRCPhysBoneBase.AdvancedBool.True;
                physBone.allowCollision = VRCPhysBoneBase.AdvancedBool.Other;
                physBone.collisionFilter.allowOthers = false;
                physBone.collisionFilter.allowSelf = true;
                physBone.endpointPosition = Vector3.up;
                return physBone;
            }
        }

        public enum CurveType
        {
            NoCurve,
            Constant,
            Linear,
            NoTangent,
            WithTangent,
            ThreeKeys,
            FourKeys,
        }

        // test for https://github.com/anatawa12/AvatarOptimizer/issues/1684
        [Test]
        // Shorter chains for basic algorithm verification, longer for pratical case check, and 0 for edge case check.
        [Combinatorial]
        public void CopyCurveActualValueTest(
            [Values(0, 1, 10, 20)] int chainLength,
            [Values] bool withEndpoints,
            [Values] CurveType curveType
        ) {
            if (chainLength == 0 && !withEndpoints)
            {
                // PB without bone chain and without endpoint is not meaningful case, and may produce NaN, so we omit this case.
                return;
            }

            var curve = curveType switch
            {
                CurveType.NoCurve => new AnimationCurve(), // animation curve with no key
                CurveType.Constant => AnimationCurve.Constant(0, 1, value: Random.Range(0f, 1f)),
                CurveType.Linear => AnimationCurve.Linear(0, Random.Range(0f, 1f), 1, Random.Range(0f, 1f)),
                CurveType.NoTangent => new AnimationCurve(new Keyframe(0, Random.Range(0f, 1f)), new Keyframe(1, Random.Range(0f, 1f))),
                CurveType.WithTangent => new AnimationCurve(new Keyframe(0, Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)), new Keyframe(1, Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f))),
                CurveType.ThreeKeys => new AnimationCurve(new Keyframe(0, Random.Range(0f, 1f)), new Keyframe(0.5f, Random.Range(0f, 1f)), new Keyframe(1, Random.Range(0f, 1f))),
                CurveType.FourKeys => new AnimationCurve(new Keyframe(0, Random.Range(0f, 1f)), new Keyframe(0.33f, Random.Range(0f, 1f)), new Keyframe(0.66f, Random.Range(0f, 1f)), new Keyframe(1, Random.Range(0f, 1f))),
                _ => throw new ArgumentOutOfRangeException(nameof(curveType), curveType, null),
            };

            var parent = TestUtils.NewAvatar();
            var chain1 = NewPBChain();
            var chain2 = NewPBChain();
            var referenceChain = NewPBChain();
            var merged = Utils.NewGameObject("merged", parent.transform);
            var mergePhysBone = CreateMergePhysBone(merged, chain1.GetComponent<VRCPhysBone>(), chain2.GetComponent<VRCPhysBone>());
            mergePhysBone.endpointPositionConfig.@override = MergePhysBone.EndPointPositionConfigStruct.Override.Copy;

            MergePhysBoneProcessor.DoMerge(mergePhysBone, null);

            var mergedPb = merged.GetComponent<VRCPhysBone>();
            var refrencePb = referenceChain.GetComponent<VRCPhysBone>();

            mergedPb.InitTransforms(force: true);
            refrencePb.InitTransforms(force: true);

            foreach (var referenceObj in referenceChain.GetComponentsInChildren<Transform>(true))
            {
                var objPath = Utils.RelativePath(root: referenceChain.transform, referenceObj);
                var merged1 = chain1.transform.Find(objPath);
                var merged2 = chain2.transform.Find(objPath);

                var refBone = refrencePb.bones.Find(x => x.transform == referenceObj);
                var m1Bone = mergedPb.bones.Find(x => x.transform == merged1);
                var m2Bone = mergedPb.bones.Find(x => x.transform == merged2);

                Assert.That(m1Bone.boneChainIndex, Is.EqualTo(m2Bone.boneChainIndex), $"boneChainIndex of {objPath} does not match between chain1 and chain2");

                var refTransformRatio = refrencePb.CalcTransformRatio(refBone.boneChainIndex);
                var refTextTransformRatio = refrencePb.CalcTransformRatio(refBone.boneChainIndex + 1);
                var refBoneRatio = refrencePb.CalcBoneRatio(refBone.boneChainIndex);

                var mergedTransformRatio = mergedPb.CalcTransformRatio(m1Bone.boneChainIndex);
                var mergedNextTransformRatio = mergedPb.CalcTransformRatio(m1Bone.boneChainIndex + 1);
                var mergedBoneRatio = mergedPb.CalcBoneRatio(m1Bone.boneChainIndex);

                Compare("radius", refrencePb.CalcRadius(refTransformRatio), refTransformRatio, mergedPb.CalcRadius(mergedTransformRatio), mergedTransformRatio);
                Compare("radius(next)", refrencePb.CalcRadius(refTextTransformRatio), refTextTransformRatio, mergedPb.CalcRadius(mergedNextTransformRatio), mergedNextTransformRatio);

                Compare("pull", refrencePb.CalcPull(refBoneRatio), refBoneRatio, mergedPb.CalcPull(mergedBoneRatio), mergedBoneRatio);
                Compare("spring", refrencePb.CalcSpring(refBoneRatio), refBoneRatio, mergedPb.CalcSpring(mergedBoneRatio), mergedBoneRatio);
                Compare("stiffness", refrencePb.CalcStiffness(refBoneRatio), refBoneRatio, mergedPb.CalcStiffness(mergedBoneRatio), mergedBoneRatio);
                Compare("immobile", refrencePb.CalcImmobile(refBoneRatio), refBoneRatio, mergedPb.CalcImmobile(mergedBoneRatio), mergedBoneRatio);
                Compare("gravity", refrencePb.CalcGravity(refBoneRatio), refBoneRatio, mergedPb.CalcGravity(mergedBoneRatio), mergedBoneRatio);
                Compare("gravityFalloff", refrencePb.CalcGravityFalloff(refBoneRatio), refBoneRatio, mergedPb.CalcGravityFalloff(mergedBoneRatio), mergedBoneRatio);
                var refMaxAngle = refrencePb.CalcMaxAngle(refBoneRatio);
                var mergedMaxAngle = mergedPb.CalcMaxAngle(mergedBoneRatio);
                Compare("maxAngle.x", refMaxAngle.x, refBoneRatio, mergedMaxAngle.x, mergedBoneRatio);
                Compare("maxAngle.y", refMaxAngle.y, refBoneRatio, mergedMaxAngle.y, mergedBoneRatio);

                Compare("stretchMotion", refrencePb.CalcStretchMotion(refBoneRatio), refBoneRatio, mergedPb.CalcStretchMotion(mergedBoneRatio), mergedBoneRatio);
                Compare("maxSquish", refrencePb.CalcMaxSquish(refBoneRatio), refBoneRatio, mergedPb.CalcMaxSquish(mergedBoneRatio), mergedBoneRatio);
                Compare("maxStretch", refrencePb.CalcMaxStretch(refBoneRatio), refBoneRatio, mergedPb.CalcMaxStretch(mergedBoneRatio), mergedBoneRatio);

                var refLimitRotation = refrencePb.CalcLimitRotation(refBoneRatio);
                var mergedLimitRotation = mergedPb.CalcLimitRotation(mergedBoneRatio);
                Compare("limitRotation.x", refLimitRotation.x, refBoneRatio, mergedLimitRotation.x, mergedBoneRatio);
                Compare("limitRotation.y", refLimitRotation.y, refBoneRatio, mergedLimitRotation.y, mergedBoneRatio);
                Compare("limitRotation.z", refLimitRotation.z, refBoneRatio, mergedLimitRotation.z, mergedBoneRatio);
            }

            GameObject NewPBChain()
            {
                var root = Utils.NewGameObject("root", parent.transform);
                var current = root;
                for (int i = 0; i < chainLength; i++)
                {
                    var child = Utils.NewGameObject("child", current.transform);
                    current = child;
                }

                var pb = root.AddComponent<VRCPhysBone>();
                pb.integrationType = VRCPhysBoneBase.IntegrationType.Advanced;
                pb.radiusCurve = curve;
                pb.radius = 1;
                pb.pullCurve = curve;
                pb.pull = 1;
                pb.springCurve = curve;
                pb.spring = 1;
                pb.stiffnessCurve = curve;
                pb.stiffness = 1;
                pb.immobileCurve = curve;
                pb.immobile = 1;
                pb.gravityCurve = curve;
                pb.gravity = 1;
                pb.limitType = VRCPhysBoneBase.LimitType.Polar;
                pb.maxAngleXCurve = curve;
                pb.maxAngleX = 90;
                pb.maxAngleZCurve = curve;
                pb.maxAngleZ = 90;
                pb.stretchMotionCurve = curve;
                pb.stretchMotion = 1;
                pb.maxSquishCurve = curve;
                pb.maxSquish = 1;
                pb.maxStretchCurve = curve;
                pb.maxStretch = 1;
                pb.limitRotationXCurve = curve;
                pb.limitRotationYCurve = curve;
                pb.limitRotationZCurve = curve;
                pb.limitRotation = new Vector3(1, 1, 1);
                if (withEndpoints) pb.endpointPosition = Vector3.up;
                return root;
            }

            void Compare(string prop, float reference, float refRatio, float merged, float mergedRatio)
            {
                // Our computation may encounter something like 1/11 and unity computation is not precise so we allow some error.
                // 840 ulp is maximum error for 0.0005% error range for normal float values.
                // If you found this test is flaky, please check the error message and consider to increase this error range.
                // The reason why we use ULPs instead of percentage is that tiny values can produce large percentage error especially for subnormal values, and we want to allow some error even for tiny values.
                Assert.That(merged, Is.EqualTo(reference).Within(840).Ulps, $"{prop} of chain1 does not match reference (refRatio: {refRatio}, mergedRatio: {mergedRatio})");
            }
        }

        [Test]
        public void OverrideTest()
        {
            var root = TestUtils.NewAvatar();
            var child1 = Utils.NewGameObject("child1", root.transform);
            var child1Component = AddConfigureRandom(child1);
            var child2 = Utils.NewGameObject("child2", root.transform);
            var child2Component = AddConfigureRandom(child2);
            var merged = Utils.NewGameObject("merged", root.transform);
            var mergePhysBone = CreateMergePhysBone(merged, child1Component, child2Component);
            mergePhysBone.pullConfig.@override = true;
            mergePhysBone.pullConfig.value = 0.4f;
            mergePhysBone.pullConfig.curve = AnimationCurve.Linear(0, 0, 1, 1);
            mergePhysBone.gravityConfig.@override = true;
            mergePhysBone.gravityConfig.value = 0.5f;
            mergePhysBone.allowPosingConfig.@override = true;
            mergePhysBone.allowPosingConfig.value = VRCPhysBoneBase.AdvancedBool.False;
            mergePhysBone.allowGrabbingConfig.@override = true;
            mergePhysBone.allowGrabbingConfig.value = VRCPhysBoneBase.AdvancedBool.True;
            mergePhysBone.allowCollisionConfig.@override = true;
            mergePhysBone.allowCollisionConfig.value = VRCPhysBoneBase.AdvancedBool.Other;
            mergePhysBone.allowCollisionConfig.filter.allowOthers = false;
            mergePhysBone.allowCollisionConfig.filter.allowSelf = true;

            MergePhysBoneProcessor.DoMerge(mergePhysBone, null);

            var mergedPhysBone = merged.GetComponent<VRCPhysBoneBase>();
            Assert.That(mergedPhysBone.pull, Is.EqualTo(0.4f));            
            Assert.That(mergedPhysBone.pullCurve, Is.EqualTo(AnimationCurve.Linear(0, 0, 1, 1)));
            Assert.That(mergedPhysBone.gravity, Is.EqualTo(0.5f));
            Assert.That(mergedPhysBone.allowPosing, Is.EqualTo(VRCPhysBoneBase.AdvancedBool.False));
            Assert.That(mergedPhysBone.allowGrabbing, Is.EqualTo(VRCPhysBoneBase.AdvancedBool.True));
            Assert.That(mergedPhysBone.allowCollision, Is.EqualTo(VRCPhysBoneBase.AdvancedBool.Other));
            Assert.That(mergedPhysBone.collisionFilter.allowOthers, Is.EqualTo(false));
            Assert.That(mergedPhysBone.collisionFilter.allowSelf, Is.EqualTo(true));

            VRCPhysBone AddConfigureRandom(GameObject go)
            {
                var physBone = go.AddComponent<VRCPhysBone>();
                physBone.pull = Random.Range(0f, 1f);
                physBone.pullCurve = AnimationCurve.Linear(0, Random.Range(0f, 1f), 1, Random.Range(0f, 1f));
                physBone.gravity = Random.Range(0f, 1f);
                physBone.allowPosing = (VRCPhysBoneBase.AdvancedBool)Random.Range(0, 2);
                physBone.allowGrabbing = (VRCPhysBoneBase.AdvancedBool)Random.Range(0, 2);;
                physBone.allowCollision = (VRCPhysBoneBase.AdvancedBool)Random.Range(0, 2);;
                physBone.poseFilter.allowOthers = Random.Range(0, 1) == 0;
                physBone.poseFilter.allowSelf = Random.Range(0, 1) == 0;
                physBone.grabFilter.allowOthers = Random.Range(0, 1) == 0;
                physBone.grabFilter.allowSelf = Random.Range(0, 1) == 0;
                physBone.collisionFilter.allowOthers = Random.Range(0, 1) == 0;
                physBone.collisionFilter.allowSelf = Random.Range(0, 1) == 0;
                return physBone;
            }
        }
    }
}

#endif