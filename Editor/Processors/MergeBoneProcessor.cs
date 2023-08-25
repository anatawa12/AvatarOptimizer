using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.ErrorReporting;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
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
                    DoBoneMap2(session, renderer, mergeMapping);
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

        private void DoBoneMap2(OptimizerSession session, SkinnedMeshRenderer renderer,
            Dictionary<Transform, Transform> mergeMapping)
        {
            var meshInfo2 = new MeshInfo2(renderer);
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

            meshInfo2.WriteToSkinnedMeshRenderer(renderer, session);
        }

        private readonly struct BoneUniqKey : IEquatable<BoneUniqKey>
        {
            private readonly string _bindPoseInfo;
            public readonly Transform Transform;

            public BoneUniqKey(Bone bone) =>
                (_bindPoseInfo, Transform) = (bone.Bindpose.ToString(), bone.Transform);

            public bool Equals(BoneUniqKey other) =>
                Equals(Transform, other.Transform) && _bindPoseInfo == other._bindPoseInfo;

            public override bool Equals(object obj) => obj is BoneUniqKey other && Equals(other);

            public override int GetHashCode() =>
                unchecked((_bindPoseInfo != null ? _bindPoseInfo.GetHashCode() : 0) * 397) ^
                (Transform != null ? Transform.GetHashCode() : 0);
        }

        private void DoBoneMap(OptimizerSession session, SkinnedMeshRenderer renderer, 
            Dictionary<Transform, Transform> mergeMapping)
        {
            var mesh = session.MayInstantiate(renderer.sharedMesh);

            var oldBones = renderer.bones;
            var oldBindposes = mesh.bindposes;
            var boneMapping = new int[oldBones.Length];

            var newBones = oldBones.Where(x => !(x && mergeMapping.ContainsKey(x))).ToArray();
            var newBindposes = new UnityEngine.Matrix4x4[newBones.Length];

            for (int i = 0, j = 0; i < oldBones.Length; i++)
            {
                if (oldBones[i] && mergeMapping.TryGetValue(oldBones[i], out var mapped))
                {
                    var newIndex = Array.IndexOf(newBones, mapped);
                    if (newIndex == -1)
                        throw new InvalidOperationException("Some Bone Mapping is invalid");
                    boneMapping[i] = newIndex;
                }
                else
                {
                    Assert.AreEqual(oldBones[i], newBones[j]);
                    boneMapping[i] = j;
                    newBindposes[j] = oldBindposes[i];
                    j++;
                }
            }

            var oldAllWeights = mesh.GetAllBoneWeights();
            var oldBonesPerVertex = mesh.GetBonesPerVertex();

            var newBonesPerVertex = new NativeArray<byte>(oldBonesPerVertex.Length, Allocator.Temp);
            var newAllWeights = new NativeArray<BoneWeight1>(oldAllWeights.Length, Allocator.Temp);

            var buffer = new BoneWeight1[255];
            var usedBones = new BitArray(newBones.Length);

            var oldWeightIndex = 0;
            var newWeightIndex = 0;
            for (var vertexIndex = 0; vertexIndex < oldBonesPerVertex.Length; vertexIndex++)
            {
                int oldWeightCount = oldBonesPerVertex[vertexIndex];
                var oldWeights = buffer.AsSpan().Slice(0, oldWeightCount);
                oldAllWeights.AsReadOnlySpan().Slice(oldWeightIndex, oldWeightCount).CopyTo(oldWeights);
                oldWeightIndex += oldWeightCount;

                usedBones.SetAll(false);
                var duplication = false;
                // map bone index
                for (var i = 0; i < oldWeights.Length; i++)
                {
                    oldWeights[i].boneIndex = boneMapping[oldWeights[i].boneIndex];
                    if (usedBones[oldWeights[i].boneIndex]) duplication = true;
                    usedBones[oldWeights[i].boneIndex] = true;
                }

                var newWeights = duplication ? RemoveBoneDuplication(oldWeights, buffer) : oldWeights;

                // copy weights to buffer
                newWeights.CopyTo(newAllWeights.AsSpan().Slice(newWeightIndex));
                newWeightIndex += newWeights.Length;
                newBonesPerVertex[vertexIndex] = (byte)newWeights.Length;
            }

            newAllWeights = Utils.SliceNativeArray(newAllWeights, newWeightIndex, Allocator.Temp);

            // set to mesh
            mesh.bindposes = newBindposes;
            mesh.SetBoneWeights(newBonesPerVertex, newAllWeights);
            renderer.sharedMesh = mesh;
            renderer.bones = newBones;
        }

        
        private Span<BoneWeight1> RemoveBoneDuplication(Span<BoneWeight1> oldWeights, BoneWeight1[] buffer)
        {
            // because this version of Span doesn't support sorting, use buffer with index&length.
            Array.Sort(buffer, 0, oldWeights.Length, new BoneIndexComparator());

            // merge
            int srcI = 1, destI = 0;
            for (; srcI < oldWeights.Length; srcI++)
            {
                if (oldWeights[destI].boneIndex == oldWeights[srcI].boneIndex)
                    oldWeights[destI].weight += oldWeights[srcI].weight;
                else
                    oldWeights[++destI] = oldWeights[srcI];
            }

            destI++;

            var newWeights = oldWeights.Slice(0, destI);

            // resort
            Array.Sort(buffer, 0, newWeights.Length, new WeightDescendingComparator());

            return newWeights;
        }

        private struct BoneIndexComparator : IComparer<BoneWeight1>
        {
            public int Compare(BoneWeight1 x, BoneWeight1 y) => x.boneIndex.CompareTo(y.boneIndex);
        }

        private struct WeightDescendingComparator : IComparer<BoneWeight1>
        {
            public int Compare(BoneWeight1 x, BoneWeight1 y) => -x.weight.CompareTo(y.weight);
        }
    }
}
