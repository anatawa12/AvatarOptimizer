using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.ErrorReporting;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class MergeBoneProcessor
    {
        public void Process(OptimizerSession session)
        {
            // merge from -> merge into
            var mergeMapping = new Dictionary<Transform, Transform>();
            foreach (var component in session.GetComponents<MergeBone>())
            {
                var transform = component.transform;
                mergeMapping[transform] = transform.parent;
            }

            // normalize map
            mergeMapping.FlattenMapping();

            BuildReport.ReportingObjects(session.GetComponents<SkinnedMeshRenderer>(), renderer =>
            {
                if (renderer.bones.Where(x => x).Any(mergeMapping.ContainsKey))
                    DoBoneMap2(session.MeshInfo2Holder.GetMeshInfoFor(renderer), mergeMapping);
            });

            foreach (var pair in mergeMapping)
            {
                var mapping = pair.Key;
                var mapped = pair.Value;
                foreach (var child in mapping.DirectChildrenEnumerable())
                    child.parent = mapped;
                mapping.parent = null;
            }

            foreach (var pair in mergeMapping.Keys)
                Object.DestroyImmediate(pair.gameObject);
        }

        private void DoBoneMap2(MeshInfo2 meshInfo2, Dictionary<Transform, Transform> mergeMapping)
        {
            var primaryBones = new Dictionary<Transform, Bone>();
            var boneReplaced = false;

            // first, simply update bone weights by updating BindPose
            foreach (var bone in meshInfo2.Bones)
            {
                if (!bone.Transform) continue;
                if (mergeMapping.TryGetValue(bone.Transform, out var mapped))
                {
                    bone.Bindpose = mapped.worldToLocalMatrix * bone.Transform.localToWorldMatrix * bone.Bindpose;
                    bone.Transform = mapped;
                    boneReplaced = true;
                }
                else
                {
                    // we assume fist bone we find is the most natural bone.
                    if (!primaryBones.ContainsKey(bone.Transform))
                        primaryBones.Add(bone.Transform, bone);
                }
            }

            if (!boneReplaced) return;

            // Optimization 1: if vertex is affected by only one bone, we can merge to one weight
            foreach (var vertex in meshInfo2.Vertices)
            {
                var singleBoneTransform = vertex.BoneWeights.Select(x => x.bone.Transform)
                    .DistinctSingleOrDefaultIfNoneOrMultiple();
                if (singleBoneTransform == null) continue;
                if (!primaryBones.TryGetValue(singleBoneTransform, out var finalBone))
                    primaryBones.Add(singleBoneTransform, finalBone = vertex.BoneWeights[0].bone);

                // about bindposes and bones
                //    (∑ localToWorldMatrix * bindPose * weight) * point
                //  = localToWorldMatrix * (∑ bindPose * weight) * point
                //  = localToWorldMatrix * newBindPose *  newBindPose^-1 * (∑ bindPose * weight) * point
                //  = localToWorldMatrix * newBindPose * (newBindPose^-1 * (∑ bindPose * weight) * point)
                //  = localToWorldMatrix * newBindPose * (newBindPose^-1 *   mergedOldBindPose   * point)
                //  = localToWorldMatrix * newBindPose * (              transBindPose            * point)
                //  = localToWorldMatrix * newBindPose *  transBindPose * point
                //  = localToWorldMatrix * newBindPose *  transBindPose * (original + (∑blendShape * weight))
                //  = localToWorldMatrix * newBindPose * (transBindPose * original + ∑transBindPose * blendShape * weight)

                var mergedOldBindPose = Matrix4x4.zero;
                foreach (var (bone, weight) in vertex.BoneWeights)
                    mergedOldBindPose += bone.Bindpose * weight;
                var transBindPose = finalBone.Bindpose.inverse * mergedOldBindPose;

                vertex.Position = transBindPose.MultiplyPoint3x4(vertex.Position);
                foreach (var frames in vertex.BlendShapes.Values)
                {
                    for (var i = 0; i < frames.Count; i++)
                    {
                        var frame = frames[i];
                        frames[i] = new Vertex.BlendShapeFrame(
                            weight: frame.Weight,
                            position: transBindPose.MultiplyPoint3x4(frame.Position),
                            normal: transBindPose.MultiplyPoint3x3(frame.Normal),
                            tangent: transBindPose.MultiplyPoint3x3(frame.Tangent)
                        );
                    }
                }

                var weightSum = vertex.BoneWeights.Select(x => x.weight).Sum();
                // I want weightSum to be 1.0 but it may not.
                vertex.BoneWeights.Clear();
                vertex.BoneWeights.Add((finalBone, weightSum));
            }

            // Optimization2: If there are same (BindPose, Transform) pair, merge
            // This is optimization for RestPose bone merging
            var boneMapping = new Dictionary<Bone, Bone>();
            foreach (var grouping in meshInfo2.Bones.GroupBy(x => new BoneUniqKey(x)))
            {
                if (!grouping.Key.Transform) continue;
                primaryBones.TryGetValue(grouping.Key.Transform, out var primaryBone);
                var group = grouping.ToArray();
                if (group.All(x => x != primaryBone))
                    primaryBone = group[0];
                foreach (var bone in group)
                    if (bone != primaryBone)
                        boneMapping[bone] = primaryBone;
            }

            foreach (var vertex in meshInfo2.Vertices)
            {
                vertex.BoneWeights = vertex.BoneWeights
                    .Select(p => boneMapping.TryGetValue(p.bone, out var bone) ? (bone, p.weight) : p)
                    .GroupBy(p => p.bone)
                    .Select(g => (g.Key, g.Sum(x => x.weight)))
                    .ToList();
            }
        }

        private readonly struct BoneUniqKey : IEquatable<BoneUniqKey>
        {
            private readonly Matrix4x4 _bindPoseInfo;
            public readonly Transform Transform;

            public BoneUniqKey(Bone bone)
            {
                _bindPoseInfo = bone.Bindpose * 100000;
                _bindPoseInfo.m00 = Mathf.Round(_bindPoseInfo.m00);
                _bindPoseInfo.m01 = Mathf.Round(_bindPoseInfo.m01);
                _bindPoseInfo.m02 = Mathf.Round(_bindPoseInfo.m02);
                _bindPoseInfo.m03 = Mathf.Round(_bindPoseInfo.m03);
                _bindPoseInfo.m10 = Mathf.Round(_bindPoseInfo.m10);
                _bindPoseInfo.m11 = Mathf.Round(_bindPoseInfo.m11);
                _bindPoseInfo.m12 = Mathf.Round(_bindPoseInfo.m12);
                _bindPoseInfo.m13 = Mathf.Round(_bindPoseInfo.m13);
                _bindPoseInfo.m20 = Mathf.Round(_bindPoseInfo.m20);
                _bindPoseInfo.m21 = Mathf.Round(_bindPoseInfo.m21);
                _bindPoseInfo.m22 = Mathf.Round(_bindPoseInfo.m22);
                _bindPoseInfo.m23 = Mathf.Round(_bindPoseInfo.m23);
                _bindPoseInfo.m30 = Mathf.Round(_bindPoseInfo.m30);
                _bindPoseInfo.m31 = Mathf.Round(_bindPoseInfo.m31);
                _bindPoseInfo.m32 = Mathf.Round(_bindPoseInfo.m32);
                Transform = bone.Transform;
            }

            public bool Equals(BoneUniqKey other) =>
                Equals(Transform, other.Transform) && _bindPoseInfo == other._bindPoseInfo;

            public override bool Equals(object obj) => obj is BoneUniqKey other && Equals(other);

            public override int GetHashCode() =>
                unchecked(_bindPoseInfo.GetHashCode() * 397) ^ (Transform != null ? Transform.GetHashCode() : 0);
        }
    }
}
