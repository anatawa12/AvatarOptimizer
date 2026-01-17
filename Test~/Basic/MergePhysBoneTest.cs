#if AAO_VRCSDK3_AVATARS

using Anatawa12.AvatarOptimizer.PrefabSafeSet;
using Anatawa12.AvatarOptimizer.Processors;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.PhysBone.Components;

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
            Assert.That(mergedPhysBone.pullCurve, Is.EqualTo(AnimationCurve.Linear(0.5f, 0, 1, 1)));
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