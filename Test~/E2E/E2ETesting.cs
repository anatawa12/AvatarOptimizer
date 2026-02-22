using System;
using System.Diagnostics;
using System.Linq;
using nadena.dev.ndmf;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using VRC.Dynamics;
using Debug = UnityEngine.Debug;
#if AAO_VRCSDK3_AVATARS
using VRC.SDK3.Dynamics.PhysBone.Components;
#endif

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


        [Test]
        public void Pr1631BasicWithIntermediate()
        {
            // create avatar with following tree
            // Root
            //  +- a
            //  |  `- b
            //  |     `- c
            //  |        `- Renderer0
            //  `- d
            //     `- e
            //        `- f
            //           `- Renderer1
            // and animations that toggles:
            // - a and d
            // - b and e
            // - c and f
            // in each different layers

            var avatar = TestUtils.NewAvatar();
            avatar.AddComponent<TraceAndOptimize>();
            var a = Utils.NewGameObject("a", avatar.transform);
            var b = Utils.NewGameObject("b", a.transform);
            var c = Utils.NewGameObject("c", b.transform);
            var d = Utils.NewGameObject("d", avatar.transform);
            var e = Utils.NewGameObject("e", d.transform);
            var f = Utils.NewGameObject("f", e.transform);
            var renderer0GameObject = Utils.NewGameObject("Renderer0", c.transform);
            var renderer1GameObject = Utils.NewGameObject("Renderer1", f.transform);
            var renderer0 = renderer0GameObject.AddComponent<SkinnedMeshRenderer>();
            var renderer1 = renderer1GameObject.AddComponent<SkinnedMeshRenderer>();
            var rootBone = Utils.NewGameObject("RootBone", avatar.transform);
            renderer0.rootBone = rootBone.transform;
            renderer1.rootBone = rootBone.transform;
            renderer0.probeAnchor = rootBone.transform;
            renderer1.probeAnchor = rootBone.transform;
            renderer0.sharedMesh = TestUtils.NewCubeMeshWithBone();
            renderer1.sharedMesh = TestUtils.NewCubeMeshWithBone();
            renderer0.bones = new[] { rootBone.transform };
            renderer1.bones = new[] { rootBone.transform };

            TestUtils.SetFxLayer(avatar, new AnimatorControllerBuilder("")
                .AddLayer("LayerA", sm =>
                {
                    sm.NewClipState("StateAD on", clip => clip
                        .AddPropertyBinding("a", typeof(GameObject), "m_IsActive",
                            new Keyframe(0, 0), new Keyframe(1, 1))
                        .AddPropertyBinding("d", typeof(GameObject), "m_IsActive",
                            new Keyframe(0, 0), new Keyframe(1, 1)));
                    sm.NewClipState("StateAD off", clip => clip
                        .AddPropertyBinding("a", typeof(GameObject), "m_IsActive",
                            new Keyframe(0, 1), new Keyframe(1, 0))
                        .AddPropertyBinding("d", typeof(GameObject), "m_IsActive",
                            new Keyframe(0, 1), new Keyframe(1, 0)));
                })
                .AddLayer("LayerB", sm =>
                {
                    sm.NewClipState("StateBE on", clip => clip
                        .AddPropertyBinding("a/b", typeof(GameObject), "m_IsActive",
                            new Keyframe(0, 0), new Keyframe(1, 1))
                        .AddPropertyBinding("d/e", typeof(GameObject), "m_IsActive",
                            new Keyframe(0, 0), new Keyframe(1, 1)));
                })
                .AddLayer("LayerC", sm =>
                {
                    sm.NewClipState("StateC", clip => clip
                        .AddPropertyBinding("a/b/c", typeof(GameObject), "m_IsActive",
                            new Keyframe(0, 0), new Keyframe(1, 1))
                        .AddPropertyBinding("d/e/f", typeof(GameObject), "m_IsActive",
                            new Keyframe(0, 0), new Keyframe(1, 1)));
                })
                .Build());

            // Run NDMF.
            // The issue is exception is thrown here.
            AvatarProcessor.ProcessAvatar(avatar);

            // assert that meshes are merged as expected, to verify test runs correctly
            // $$AAO_AUTO_MERGE_SMR_INTERMEDIATE_0 should be at root and $$AAO_AUTO_MERGE_SKINNED_MESH_1 in child
            var intermediate = avatar.transform.Find("$$AAO_AUTO_MERGE_SMR_INTERMEDIATE_0");
            Assert.That(intermediate, Is.Not.Null, "Intermediate merged GameObject should exist");
            var mergedMesh = intermediate.Find("$$AAO_AUTO_MERGE_SKINNED_MESH_1");
            Assert.That(mergedMesh, Is.Not.Null, "Merged SkinnedMeshRenderer should exist");
            Assert.That(mergedMesh.GetComponent<SkinnedMeshRenderer>(), Is.Not.Null, "Merged SkinnedMeshRenderer component should exist");
            Assert.That(a == null, "GameObject 'a' should be removed");
            Assert.That(b == null, "GameObject 'b' should be removed");
            Assert.That(c == null, "GameObject 'c' should be removed");
            Assert.That(d == null, "GameObject 'd' should be removed");
            Assert.That(e == null, "GameObject 'e' should be removed");
            Assert.That(f == null, "GameObject 'f' should be removed");
            Assert.That(renderer0GameObject == null, "GameObject 'renderer0' should be removed");
            Assert.That(renderer1GameObject == null, "GameObject 'renderer1' should be removed");
        }

        [Test]
        public void Issue1630MergeSmrMergeSameNameBlendShape()
        {
            // mesh A and B has blendShape named "BlendShape" and merged to single mesh with manually configured MergeSMR
            // Animation targeting A.BlendShape and B.BlendShape should be remapped correctly without error
            var avatar = TestUtils.NewAvatar();
            var meshA = TestUtils.NewCubeMesh();
            meshA.AddBlendShapeFrame("BlendShape", 100f, TestUtils.NewCubeBlendShapeFrame((0, Vector3.up)), null, null);
            var meshB = TestUtils.NewCubeMesh();
            meshB.AddBlendShapeFrame("BlendShape", 100f, TestUtils.NewCubeBlendShapeFrame((0, Vector3.up)), null, null);
            var goA = Utils.NewGameObject("MeshA", avatar.transform);
            var goB = Utils.NewGameObject("MeshB", avatar.transform);
            var smrA = goA.AddComponent<SkinnedMeshRenderer>();
            var smrB = goB.AddComponent<SkinnedMeshRenderer>();
            smrA.sharedMesh = meshA;
            smrB.sharedMesh = meshB;
            var mergeSMRGO = Utils.NewGameObject("MergeSMR", avatar.transform);
            var mergeSMRRenderer = mergeSMRGO.AddComponent<SkinnedMeshRenderer>();
            var mergeSMR = mergeSMRGO.AddComponent<MergeSkinnedMesh>();
            mergeSMR.blendShapeMode = MergeSkinnedMesh.BlendShapeMode.MergeSameName;
            mergeSMR.renderersSet.SetValueNonPrefab(new[] { smrA, smrB });
            mergeSMRRenderer.probeAnchor = mergeSMR.transform;
            mergeSMRRenderer.rootBone = mergeSMR.transform;

            TestUtils.SetFxLayer(avatar, new AnimatorControllerBuilder("")
                .AddLayer("Base Layer", sm =>
                {
                    sm.NewClipState("AnimateBlendShapes", clip => clip
                        .AddPropertyBinding("MeshA", typeof(SkinnedMeshRenderer), "blendShape.BlendShape",
                            new Keyframe(0, 0), new Keyframe(1, 100))
                        .AddPropertyBinding("MeshB", typeof(SkinnedMeshRenderer), "blendShape.BlendShape",
                            new Keyframe(0, 0), new Keyframe(1, 100)));
                })
                .Build());

            LogTestUtility.Test(_ =>
            {
                AvatarProcessor.ProcessAvatar(avatar);
            });

            // if no error is reported, test is passed
        }

#if AAO_VRCSDK3_AVATARS
        [Test]
        public void Issue1645_AutoMergePhysBone_Enables_Disabled_PhysBone()
        {
            var avatar = TestUtils.NewAvatar();
            var pb1 = Utils.NewGameObject("PB1", avatar.transform);
            var pbTarget1 = Utils.NewGameObject("PBTarget", pb1.transform);
            var pbComponent1 = pb1.AddComponent<VRCPhysBone>();
            pbComponent1.endpointPosition = Vector3.up;
            pbComponent1.enabled = false;
            pbComponent1.allowGrabbing = VRCPhysBoneBase.AdvancedBool.False;
            pbTarget1.AddComponent<MeshRenderer>();
            pbTarget1.AddComponent<MeshFilter>().sharedMesh = TestUtils.NewCubeMesh();
            var pb2 = Utils.NewGameObject("PB2", avatar.transform);
            var pbTarget2 = Utils.NewGameObject("PBTarget2", pb2.transform);
            var pbComponent2 = pb2.AddComponent<VRCPhysBone>();
            pbComponent2.endpointPosition = Vector3.up;
            pbComponent2.enabled = false;
            pbComponent2.allowGrabbing = VRCPhysBoneBase.AdvancedBool.False;
            pbTarget2.AddComponent<MeshRenderer>();
            pbTarget2.AddComponent<MeshFilter>().sharedMesh = TestUtils.NewCubeMesh();
            TestUtils.SetFxLayer(avatar, new AnimatorControllerBuilder("").Build());
            avatar.AddComponent<TraceAndOptimize>();

            LogTestUtility.Test(_ =>
            {
                AvatarProcessor.ProcessAvatar(avatar);
            });

            Assert.That(avatar.GetComponentsInChildren<VRCPhysBone>(), Is.Empty, "No PhysBone should remain after processing");
        }
#endif

        [Test]
        public void Issue1648_MergeMaterialSlots_Doesnt_Map_Slot_Animations()
        {
            var avatar = TestUtils.NewAvatar();
            avatar.AddComponent<TraceAndOptimize>();
            var mesh = TestUtils.NewCubeMesh(subMeshCount: 3);
            var go = Utils.NewGameObject("Mesh", avatar.transform);
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh;
            var material1 = new Material(Shader.Find("Standard"));
            material1.name = "Material1";
            var material2 = new Material(Shader.Find("Standard"));
            material2.name = "Material2";
            var material3 = new Material(Shader.Find("Standard"));
            material3.name = "Material3";
            smr.sharedMaterials = new[] { material1, material1, material2 };

            TestUtils.SetFxLayer(avatar, new AnimatorControllerBuilder("")
                .AddLayer("Base Layer", sm =>
                {
                    sm.NewClipState("AnimateMaterialSlots", clip => clip
                        .AddObjectReferenceBinding("Mesh", typeof(SkinnedMeshRenderer), "m_Materials.Array.data[2]",
                            new ObjectReferenceKeyframe { time = 0, value = material2 },
                            new ObjectReferenceKeyframe { time = 1, value = material3 }));
                })
                .Build());

            LogTestUtility.Test(_ =>
            {
                AvatarProcessor.ProcessAvatar(avatar);
            });

            // assert merged state

            Assert.That(smr.sharedMaterials, Has.Length.EqualTo(2), "Material slots should be merged to 2");
            Assert.That(smr.sharedMaterials[0].name, Does.Contain(material1.name), "Material1 should be used as the merged material for slot 0 and 1");
            Assert.That(smr.sharedMaterials[1].name, Does.Contain(material2.name), "Material1 should be used as the merged material for slot 2");

            var parsed =
                new AnimatorParsersV2.AnimatorParser(mmdWorldCompatibility: true).GatherAnimationModifications(
                    new BuildContext(avatar, null));

            Assert.That(parsed.ObjectNodes.Keys, Has.Exactly(1).Matches<(ComponentOrGameObject target, string prop)>(x => 
                x.target.Value == smr && x.prop == "m_Materials.Array.data[1]"), "Material slot animation should be remapped to the correct slot");
        }

        [Test]
        public void Issue1648_MergeMaterialSlots_Incorrectly_Merge_Animated_Slots_NoShuffle()
        {
            var avatar = TestUtils.NewAvatar();
            var traceAndOptimize = avatar.AddComponent<TraceAndOptimize>();
            traceAndOptimize.allowShuffleMaterialSlots = false;
            var mesh = TestUtils.NewCubeMesh(subMeshCount: 3);
            var go = Utils.NewGameObject("Mesh", avatar.transform);
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh;
            var material1 = new Material(Shader.Find("Standard"));
            material1.name = "Material1";
            var material2 = new Material(Shader.Find("Standard"));
            material2.name = "Material2";
            var material3 = new Material(Shader.Find("Standard"));
            material3.name = "Material3";
            smr.sharedMaterials = new[] { material1, material1, material2 };

            TestUtils.SetFxLayer(avatar, new AnimatorControllerBuilder("")
                .AddLayer("Base Layer", sm =>
                {
                    sm.NewClipState("AnimateMaterialSlots", clip => clip
                        .AddObjectReferenceBinding("Mesh", typeof(SkinnedMeshRenderer), "m_Materials.Array.data[1]",
                            new ObjectReferenceKeyframe { time = 0, value = material2 },
                            new ObjectReferenceKeyframe { time = 1, value = material3 }));
                })
                .Build());

            LogTestUtility.Test(_ =>
            {
                AvatarProcessor.ProcessAvatar(avatar);
            });

            // assert merged state

            Assert.That(smr.sharedMaterials, Has.Length.EqualTo(3), "Material slots should NOT be merged.");
        }

        [Test]
        public void Issue1648_MergeMaterialSlots_Incorrectly_Merge_Animated_Slots_Shuffle()
        {
            var avatar = TestUtils.NewAvatar();
            var traceAndOptimize = avatar.AddComponent<TraceAndOptimize>();
            traceAndOptimize.allowShuffleMaterialSlots = true;
            var mesh = TestUtils.NewCubeMesh(subMeshCount: 3);
            var go = Utils.NewGameObject("Mesh", avatar.transform);
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh;
            var material1 = new Material(Shader.Find("Standard"));
            material1.name = "Material1";
            var material2 = new Material(Shader.Find("Standard"));
            material2.name = "Material2";
            var material3 = new Material(Shader.Find("Standard"));
            material3.name = "Material3";
            smr.sharedMaterials = new[] { material1, material1, material2 };

            TestUtils.SetFxLayer(avatar, new AnimatorControllerBuilder("")
                .AddLayer("Base Layer", sm =>
                {
                    sm.NewClipState("AnimateMaterialSlots", clip => clip
                        .AddObjectReferenceBinding("Mesh", typeof(SkinnedMeshRenderer), "m_Materials.Array.data[1]",
                            new ObjectReferenceKeyframe { time = 0, value = material2 },
                            new ObjectReferenceKeyframe { time = 1, value = material3 }));
                })
                .Build());

            LogTestUtility.Test(_ =>
            {
                AvatarProcessor.ProcessAvatar(avatar);
            });

            // assert merged state

            Assert.That(smr.sharedMaterials, Has.Length.EqualTo(3), "Material slots should NOT be merged.");
        }

        [Test]
        public void Issue1655_AutoMergePB_Should_Not_Merge_PhysBones_Targeting_Same_Target()
        {
            var avatar = TestUtils.NewAvatar();
            var pbTarget = Utils.NewGameObject("PBTarget", avatar.transform);
            var pb1 = Utils.NewGameObject("PB1", avatar.transform);
            var pbComponent1 = pb1.AddComponent<VRCPhysBone>();
            pbComponent1.endpointPosition = Vector3.up;
            pbComponent1.rootTransform = pbTarget.transform;
            pbComponent1.allowGrabbing = VRCPhysBoneBase.AdvancedBool.False;
            var pb2 = Utils.NewGameObject("PB2", avatar.transform);
            var pbComponent2 = pb2.AddComponent<VRCPhysBone>();
            pbComponent2.endpointPosition = Vector3.up;
            pbComponent2.rootTransform = pbTarget.transform;
            pbComponent2.allowGrabbing = VRCPhysBoneBase.AdvancedBool.False;
            TestUtils.SetFxLayer(avatar, new AnimatorControllerBuilder("").Build());
            // we nned to keep pbTarget
            var meshFilter = pbTarget.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = TestUtils.NewCubeMesh();
            pbTarget.AddComponent<MeshRenderer>();

            avatar.AddComponent<TraceAndOptimize>();

            LogTestUtility.Test(_ =>
            {
                AvatarProcessor.ProcessAvatar(avatar);
            });

            Assert.That(avatar.GetComponentsInChildren<VRCPhysBone>(), Has.Length.EqualTo(2), "PhysBones should NOT be merged.");
            Assert.That(pb1 != null, "PB1 should not be merged");
            Assert.That(pb2 != null, "PB2 should not be merged");
        }

#if AAO_VRCSDK3_AVATARS
        [Test]
        public void PR1675_AutoMergePB_Targeting_External_Bone()
        {
            var outOfAvatar = new GameObject("OutOfAvatarRoot");
            var outAvatarPbTarget1 = Utils.NewGameObject("OutAvatarPBTarget1", outOfAvatar.transform);
            var outAvatarPbTarget2 = Utils.NewGameObject("OutAvatarPBTarget2", outOfAvatar.transform);
            var outAvatarPbTarget3 = Utils.NewGameObject("OutAvatarPBTarget3", outOfAvatar.transform);

            var avatar = TestUtils.NewAvatar();
            var pb1 = Utils.NewGameObject("PB1", avatar.transform);
            var pbComponent1 = pb1.AddComponent<VRCPhysBone>();
            pbComponent1.rootTransform = outAvatarPbTarget1.transform;
            pbComponent1.endpointPosition = Vector3.up;
            pbComponent1.allowGrabbing = VRCPhysBoneBase.AdvancedBool.False;
            var pb2 = Utils.NewGameObject("PB2", avatar.transform);
            var pbComponent2 = pb2.AddComponent<VRCPhysBone>();
            pbComponent2.endpointPosition = Vector3.up;
            pbComponent2.rootTransform = outAvatarPbTarget2.transform;
            pbComponent2.allowGrabbing = VRCPhysBoneBase.AdvancedBool.False;
            TestUtils.SetFxLayer(avatar, new AnimatorControllerBuilder("").Build());
            avatar.AddComponent<TraceAndOptimize>();

            LogTestUtility.Test(_ =>
            {
                AvatarProcessor.ProcessAvatar(avatar);
            });
        }
        
        [Test]
        public void PR1675_ManualMergePB_Targeting_External_Bone()
        {
            var outOfAvatar = new GameObject("OutOfAvatarRoot");
            var outAvatarPbTarget1 = Utils.NewGameObject("OutAvatarPBTarget1", outOfAvatar.transform);
            var outAvatarPbTarget2 = Utils.NewGameObject("OutAvatarPBTarget2", outOfAvatar.transform);
            var outAvatarPbTarget3 = Utils.NewGameObject("OutAvatarPBTarget3", outOfAvatar.transform);

            var avatar = TestUtils.NewAvatar();
            var pb1 = Utils.NewGameObject("PB1", avatar.transform);
            var pbComponent1 = pb1.AddComponent<VRCPhysBone>();
            pbComponent1.rootTransform = outAvatarPbTarget1.transform;
            pbComponent1.endpointPosition = Vector3.up;
            pbComponent1.allowGrabbing = VRCPhysBoneBase.AdvancedBool.False;
            var pb2 = Utils.NewGameObject("PB2", avatar.transform);
            var pbComponent2 = pb2.AddComponent<VRCPhysBone>();
            pbComponent2.endpointPosition = Vector3.up;
            pbComponent2.rootTransform = outAvatarPbTarget2.transform;
            pbComponent2.allowGrabbing = VRCPhysBoneBase.AdvancedBool.False;

            var mergePB = Utils.NewGameObject("MergePB", avatar.transform);
            var mergePBComponent = mergePB.AddComponent<MergePhysBone>();
            mergePBComponent.componentsSet.AddRange(new[] { pbComponent1, pbComponent2 });

            TestUtils.SetFxLayer(avatar, new AnimatorControllerBuilder("").Build());
            avatar.AddComponent<TraceAndOptimize>();

            LogTestUtility.Test(c =>
            {
                AvatarProcessor.ProcessAvatar(avatar);
                c.ExpectError(ErrorSeverity.Error, "MergePhysBone:error:physbone-outside-of-avatar-root");
            });
        }

        // similar to 1645 but disabled with IsActive of parent GameObject, which is more common case and was not fixed by 1645 fix.
        [Test]
        public void Issue1682_AutoMergePB_Activates_PB_Disabled_With_GameObject_IsActivate()
        {
            
            var avatar = TestUtils.NewAvatar();
            var pb1 = Utils.NewGameObject("PB1", avatar.transform);
            var pbTarget1 = Utils.NewGameObject("PBTarget", pb1.transform);
            var pbComponent1 = pb1.AddComponent<VRCPhysBone>();
            pbComponent1.endpointPosition = Vector3.up;
            pbComponent1.allowGrabbing = VRCPhysBoneBase.AdvancedBool.False;
            pb1.SetActive(false);
            pbTarget1.AddComponent<MeshRenderer>();
            pbTarget1.AddComponent<MeshFilter>().sharedMesh = TestUtils.NewCubeMesh();
            var pb2 = Utils.NewGameObject("PB2", avatar.transform);
            var pbTarget2 = Utils.NewGameObject("PBTarget2", pb2.transform);
            var pbComponent2 = pb2.AddComponent<VRCPhysBone>();
            pbComponent2.endpointPosition = Vector3.up;
            pbComponent2.allowGrabbing = VRCPhysBoneBase.AdvancedBool.False;
            pb2.SetActive(false);
            pbTarget2.AddComponent<MeshRenderer>();
            pbTarget2.AddComponent<MeshFilter>().sharedMesh = TestUtils.NewCubeMesh();
            TestUtils.SetFxLayer(avatar, new AnimatorControllerBuilder("").Build());
            avatar.AddComponent<TraceAndOptimize>();

            LogTestUtility.Test(_ =>
            {
                AvatarProcessor.ProcessAvatar(avatar);
            });

            Assert.That(avatar.GetComponentsInChildren<VRCPhysBone>(), Is.Empty, "No PhysBone should remain after processing");
        }
#endif

        #endregion
    }
}