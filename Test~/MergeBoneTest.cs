using Anatawa12.AvatarOptimizer.Processors;
using NUnit.Framework;
using UnityEngine;

#if AAO_VRCSDK3_AVATARS
using VRC.SDK3.Dynamics.PhysBone.Components;
#endif

namespace Anatawa12.AvatarOptimizer.Test
{
    public class MergeBoneTest
    {
        [Test]
        public void ExtremeSmall()
        {
            var epsilonVector3 = new Vector3(float.Epsilon, float.Epsilon, float.Epsilon);
            var root = TestUtils.NewAvatar();
            var merged = Utils.NewGameObject("merged", root.transform);
            merged.transform.localScale = epsilonVector3;

            var identity = Utils.NewGameObject("identity", merged.transform);
            var moved = Utils.NewGameObject("moved", merged.transform);
            moved.transform.localPosition = Vector3.one;

            var transInfo = MergeBoneProcessor.MergeBoneTransParentInfo.Compute(merged.transform, root.transform);

            var identityAfter = transInfo.ComputeInfoFor(identity.transform);
            Assert.That(identityAfter.scale, Is.EqualTo(epsilonVector3));
            Assert.That(identityAfter.position, Is.EqualTo(Vector3.zero));
            Assert.That(identityAfter.rotation, Is.EqualTo(Quaternion.identity));
            
            var movedAfter = transInfo.ComputeInfoFor(moved.transform);
            Assert.That(movedAfter.scale, Is.EqualTo(epsilonVector3));
            Assert.That(movedAfter.position, Is.EqualTo(epsilonVector3));
            Assert.That(movedAfter.rotation, Is.EqualTo(Quaternion.identity));
        }
        
#if AAO_VRCSDK3_AVATARS

        [Test]
        public void IgnoreTransformOfPhysBone()
        {
            var root = TestUtils.NewAvatar();
            var pbRoot = Utils.NewGameObject("merged", root.transform);
            var child = Utils.NewGameObject("child", pbRoot.transform);
            var mergedIgnored = Utils.NewGameObject("mergedIgnored", child.transform);
            var mergedChild = Utils.NewGameObject("mergedChild", mergedIgnored.transform);

            var nonMergedIgnored = Utils.NewGameObject("nonMergedIgnored", child.transform);

            mergedIgnored.AddComponent<MergeBone>();

            var physBone = pbRoot.AddComponent<VRCPhysBone>();

            physBone.ignoreTransforms.Add(mergedIgnored.transform);
            physBone.ignoreTransforms.Add(nonMergedIgnored.transform);

            MergeBoneProcessor.MapIgnoreTransforms(physBone);

            Assert.That(physBone.ignoreTransforms,
                Is.EquivalentTo(new[] { nonMergedIgnored.transform, mergedChild.transform }));
        }
        
        [Test]
        public void NullElementsInIgnoreTransformInPhysBone()
        {
            var root = TestUtils.NewAvatar();
            var destoryed = Utils.NewGameObject("destoryed", root.transform);
            var pbRoot = Utils.NewGameObject("merged", root.transform);
            var child = Utils.NewGameObject("child", pbRoot.transform);
            var mergedIgnored = Utils.NewGameObject("mergedIgnored", child.transform);
            var mergedChild = Utils.NewGameObject("mergedChild", mergedIgnored.transform);

            var nonMergedIgnored = Utils.NewGameObject("nonMergedIgnored", child.transform);

            mergedIgnored.AddComponent<MergeBone>();

            var physBone = pbRoot.AddComponent<VRCPhysBone>();

            physBone.ignoreTransforms.Add(mergedIgnored.transform);
            physBone.ignoreTransforms.Add(nonMergedIgnored.transform);
            physBone.ignoreTransforms.Add(destoryed.transform);
            physBone.ignoreTransforms.Add(null);
            Object.DestroyImmediate(destoryed);

            MergeBoneProcessor.MapIgnoreTransforms(physBone);
        }
        
        [Test]
        public void NullIgnoreTransformListInPhysBone()
        {
            var root = TestUtils.NewAvatar();
            var pbRoot = Utils.NewGameObject("merged", root.transform);
            var physBone = pbRoot.AddComponent<VRCPhysBone>();
            physBone.ignoreTransforms = null;
            MergeBoneProcessor.MapIgnoreTransforms(physBone);
        }

#endif

    }
}