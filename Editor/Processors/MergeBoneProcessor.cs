using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.ErrorReporting;
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
                    DoBoneMap(session, renderer, mergeMapping);
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

        private void DoBoneMap(OptimizerSession session, SkinnedMeshRenderer renderer, 
            Dictionary<Transform, Transform> mergeMapping)
        {
            var mesh = session.MayInstantiate(renderer.sharedMesh);

            var oldBones = renderer.bones;
            var oldBindposes = mesh.bindposes;
            var boneMapping = new int[oldBones.Length];

            var newBones = oldBones.Where(x => !(x && mergeMapping.ContainsKey(x))).ToArray();
            var newBindposes = new Matrix4x4[newBones.Length];

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
