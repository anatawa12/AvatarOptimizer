using System.Linq;
using nadena.dev.ndmf;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

namespace Anatawa12.AvatarOptimizer.Test.E2E
{
    // We should order test cases by issue / pr number.
    // If the test has issue, use issue number and add comment with link to the issue.
    // If the test does not have issue, use PR number that fixes the error and add comment with link to the PR.
    public class E2ETesting
    {
        #region Issue or Regression Testing

        [Test]
        public void PR1522_GCComponentDoesNotRemoveDisabledComponents()
        {
            // Create an avatar
            var avatar = TestUtils.NewAvatar();
            TestUtils.SetFxLayer(avatar, new AnimatorController());
            var rendererGO = new GameObject("Renderer");
            rendererGO.transform.SetParent(avatar.transform, false);
            var smr = rendererGO.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = TestUtils.NewCubeMesh();
            smr.enabled = false;
            avatar.AddComponent<TraceAndOptimize>();

            // Run NDMF
            AvatarProcessor.ProcessAvatar(avatar);

            // Run Checks
            Assert.That(smr == null, "SkinnedMeshRenderer should be removed by GCComponent");
            Assert.That(rendererGO == null, "SkinnedMeshRenderer should be removed by GCComponent");
        }

        [Test]
        public void Issue1559_KeyNotFoundWhenGeneratedObjectIsMovedByMergeBone()
        {
            // Create an avatar
            var avatar = TestUtils.NewAvatar();
            var bone = new GameObject("BoneToMerge");
            bone.transform.SetParent(avatar.transform, false);
            var parent = new GameObject("Parent"); // The bone will be merged by MergeBone
            parent.transform.SetParent(avatar.transform, false);
            var child = new GameObject("Child");
            child.transform.SetParent(parent.transform, false);
            var childSmr = child.AddComponent<SkinnedMeshRenderer>();
            childSmr.sharedMesh = TestUtils.NewCubeMeshWithBone();
            childSmr.bones = new Transform[] { bone.transform };
            childSmr.rootBone = bone.transform;
            childSmr.lightProbeUsage = LightProbeUsage.Off;
            childSmr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            childSmr.sharedMaterials = new[] { new Material(Shader.Find("Standard")) };
            var grandChild = new GameObject("GrandChild");
            grandChild.transform.SetParent(child.transform, false);
            var grandChildSmr = grandChild.AddComponent<SkinnedMeshRenderer>();
            grandChildSmr.sharedMesh = TestUtils.NewCubeMeshWithBone();
            grandChildSmr.bones = new Transform[] { bone.transform };
            grandChildSmr.rootBone = bone.transform;
            grandChildSmr.lightProbeUsage = LightProbeUsage.Off;
            grandChildSmr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            grandChildSmr.sharedMaterials = new[] { new Material(Shader.Find("Standard")) };
            // prevent 2nd sub-issue below
            var child2 = new GameObject("Child2");
            child2.transform.SetParent(parent.transform, false);
            child2.AddComponent<SkinnedMeshRenderer>().sharedMesh = TestUtils.NewCubeMesh();
            TestUtils.SetFxLayer(avatar, new AnimatorControllerBuilder("FX")
                .AddLayer("Base Layer", layer => layer
                    .NewClipState("AnimateChild", clip => clip
                        .AddPropertyBinding("Parent/Child", typeof(GameObject), "m_IsActive",
                            AnimationCurve.Linear(0, 0, 1, 1))))
                .AddLayer("1st Layer", 1, layer => layer
                    .NewClipState("AnimateChild", clip => clip
                        .AddPropertyBinding("Parent/Child2", typeof(GameObject), "m_IsActive",
                            AnimationCurve.Linear(0, 0, 1, 1))))
                .Build());
            avatar.AddComponent<TraceAndOptimize>();
            
            // Run NDMF.
            // The issue is exception is thrown here.
            AvatarProcessor.ProcessAvatar(avatar);

            // print hierarchy
            Debug.Log("Avatar Hierarchy After Process:");
            foreach (var tr in avatar.GetComponentsInChildren<Transform>(true))
                Debug.Log(string.Join("/", tr.ParentEnumerable(tr, includeMe: true).Reverse().Select(t => t.name)));
        }

        [Test]
        public void Issue1559_ParentIsRemovedWhenAutomaticallyMerged()
        {
            // Create an avatar
            var avatar = TestUtils.NewAvatar();
            var bone = new GameObject("BoneToMerge");
            bone.transform.SetParent(avatar.transform, false);
            var parent = new GameObject("Parent"); // The bone will be merged by MergeBone
            parent.transform.SetParent(avatar.transform, false);
            var child = new GameObject("Child");
            child.transform.SetParent(parent.transform, false);
            var childSmr = child.AddComponent<SkinnedMeshRenderer>();
            childSmr.sharedMesh = TestUtils.NewCubeMeshWithBone();
            childSmr.bones = new Transform[] { bone.transform };
            childSmr.rootBone = bone.transform;
            childSmr.lightProbeUsage = LightProbeUsage.Off;
            childSmr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            childSmr.sharedMaterials = new[] { new Material(Shader.Find("Standard")) };
            var grandChild = new GameObject("GrandChild");
            grandChild.transform.SetParent(child.transform, false);
            var grandChildSmr = grandChild.AddComponent<SkinnedMeshRenderer>();
            grandChildSmr.sharedMesh = TestUtils.NewCubeMeshWithBone();
            grandChildSmr.bones = new Transform[] { bone.transform };
            grandChildSmr.rootBone = bone.transform;
            grandChildSmr.lightProbeUsage = LightProbeUsage.Off;
            grandChildSmr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            grandChildSmr.sharedMaterials = new[] { new Material(Shader.Find("Standard")) };
            // prevent 2nd sub-issue below
            TestUtils.SetFxLayer(avatar, new AnimatorControllerBuilder("FX")
                .AddLayer("Base Layer", layer => layer
                    .NewClipState("AnimateChild", clip => clip
                        .AddPropertyBinding("Parent/Child", typeof(GameObject), "m_IsActive",
                            AnimationCurve.Linear(0, 0, 1, 1))))
                .Build());
            avatar.AddComponent<TraceAndOptimize>();
            
            // Run NDMF.
            // The issue is exception is thrown here.
            AvatarProcessor.ProcessAvatar(avatar);

            Assert.That(avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true),
                Has.Exactly(1).Not.Null);
        }

        #endregion
    }
}