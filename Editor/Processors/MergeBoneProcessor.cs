using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using nadena.dev.ndmf;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

#if AAO_VRCSDK3_AVATARS
using VRC.Dynamics;
#endif

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class MergeBoneProcessor : Pass<MergeBoneProcessor>
    {
        public static void Validate(MergeBone mergeBone, GameObject root)
        {
            // TODO: use AvatarRoot API
            if (mergeBone.transform == root.transform)
            {
                BuildLog.LogError("MergeBone:validation:onAvatarRoot");
            }

            if (mergeBone.GetComponents<Component>().Except(new Component[] { mergeBone, mergeBone.transform })
                .Any())
                BuildLog.LogWarning("MergeBone:validation:thereAreComponent");

            if (AnyNotMergedBone(mergeBone.transform))
            {
                // if the bone has non-merged bones, uneven scaling is not supported.
                if (!Utils.ScaledEvenly(mergeBone.transform.localScale))
                    BuildLog.LogWarning("MergeBone:validation:unevenScaling");
            }

            bool AnyNotMergedBone(Transform bone)
            {
                if (bone.CompareTag("EditorOnly")) return false;
                if (!bone.TryGetComponent<MergeBone>(out _)) return true;
                foreach (var transform in bone.DirectChildrenEnumerable())
                    if (AnyNotMergedBone(transform))
                        return true;
                return false;
            }
        }

        protected override void Execute(BuildContext context)
        {
            // merge from -> merge into
            Profiler.BeginSample("Create Merge Mapping");
            var mergeMapping = new Dictionary<Transform, Transform>();
            foreach (var component in context.GetComponents<MergeBone>())
            {
                // Error by validator
                if (component.transform == context.AvatarRootTransform) continue;
                var transform = component.transform;
                mergeMapping[transform] = transform.parent;
            }

            // normalize map
            mergeMapping.FlattenMapping();

            Profiler.EndSample();

            if (mergeMapping.Count == 0) return;

#if AAO_VRCSDK3_AVATARS
            foreach (var physBone in context.GetComponents<VRCPhysBoneBase>())
            {
                Profiler.BeginSample("MapIgnoreTransforms");
                using (ErrorReport.WithContextObject(physBone))
                    MapIgnoreTransforms(physBone);
                Profiler.EndSample();
            }
#endif
            foreach (var renderer in context.GetComponents<SkinnedMeshRenderer>())
            {
                using (ErrorReport.WithContextObject(renderer))
                {
                    Profiler.BeginSample("DoBoneMap");
                    var meshInfo2 = context.GetMeshInfoFor(renderer);
                    if (meshInfo2.Bones.Any(x => x.Transform != null && mergeMapping.ContainsKey(x.Transform)))
                        DoBoneMap2(meshInfo2, mergeMapping, context);
                    Profiler.EndSample();
                }
            }

            Profiler.BeginSample("Flatten Bone Tree");

            var counter = 0;
            foreach (var pair in mergeMapping)
            {
                var mapping = pair.Key;
                var mapped = pair.Value;
                var avoidNameConflict = mapping.GetComponent<MergeBone>().avoidNameConflict;
                // if intermediate objects are inactive, moved bone should be initially inactive
                // animations are not performed correctly but if bones activity is animated, automatic 
                // merge bone doesn't merge such bone so ignore that for manual merge bone.
                var parentInfo = MergeBoneTransParentInfo.Compute(mapping, mapped);
                foreach (var child in mapping.DirectChildrenEnumerable().ToArray())
                {
                    if (mergeMapping.ContainsKey(child)) continue;

                    var (position, rotation, scale) = parentInfo.ComputeInfoFor(child);

                    context.Extension<GCComponentInfoContext>().SetParent(child, mapped);
                    child.localPosition = position;
                    child.localRotation = rotation;
                    child.localScale = scale;
                    if (!parentInfo.ActiveSelf) child.gameObject.SetActive(false);
                    if (avoidNameConflict)
                        child.name = parentInfo.NamePrefix + "$" + child.name + "$" + counter++;
                }
            }

            Profiler.EndSample();

            Profiler.BeginSample("Destroy Unnecessary Objects");
            foreach (var pair in mergeMapping.Keys)
                if (pair)
                    DestroyTracker.DestroyImmediate(pair.gameObject);
            Profiler.EndSample();
        }

#if AAO_VRCSDK3_AVATARS
        internal static void MapIgnoreTransforms(VRCPhysBoneBase physBone)
        {
            if (physBone.ignoreTransforms == null) return;
            var ignoreTransforms = new HashSet<Transform>();

            var processQueue = new Queue<Transform>(physBone.ignoreTransforms);
            while (processQueue.Count != 0)
            {
                var transform = processQueue.Dequeue();
                if (transform == null) continue;
                if (!transform.gameObject.GetComponent<MergeBone>())
                {
                    ignoreTransforms.Add(transform);
                }
                else
                {
                    foreach (var child in transform.DirectChildrenEnumerable())
                        processQueue.Enqueue(child);
                }
            }

            physBone.ignoreTransforms = ignoreTransforms.ToList();
        }
#endif

        private void DoBoneMap2(MeshInfo2 meshInfo2, Dictionary<Transform, Transform> mergeMapping, BuildContext context)
        {
            var primaryBones = new ConcurrentDictionary<Transform, Bone>();
            var boneReplaced = false;

            Profiler.BeginSample("Map Bone");

            // first, simply update bone weights by updating BindPose
            foreach (var bone in meshInfo2.Bones)
            {
                if (bone.Transform == null) continue;
                if (mergeMapping.TryGetValue(bone.Transform, out var mapped))
                {
                    bone.Bindpose = mapped.worldToLocalMatrix * bone.Transform.localToWorldMatrix * bone.Bindpose;
                    bone.Transform = mapped;
                    context.Extension<GCComponentInfoContext>().GetInfo(meshInfo2.SourceRenderer)
                        .AddDependency(mapped, GCComponentInfo.DependencyType.Bone);
                    boneReplaced = true;
                }
                else
                {
                    // we assume fist bone we find is the most natural bone.
                    if (ValidBindPose(bone.Bindpose))
                        primaryBones.TryAdd(bone.Transform, bone);
                }
            }

            Profiler.EndSample();

            if (!boneReplaced) return;

            Profiler.BeginSample("Optimize Bindpose Phase 1");

            // Optimization 1: if vertex is affected by only one bone, we can merge to one weight
            Parallel.ForEach(meshInfo2.Vertices, vertex =>
            {
                var singleBoneTransform = vertex.BoneWeights.Select(x => x.bone.Transform)
                    .DistinctSingleOrDefaultIfNoneOrMultiple<Transform?>(ReferenceEqualityComparer.Instance);
                if (singleBoneTransform is null) return;
                var finalBone = primaryBones.GetOrAdd(singleBoneTransform, vertex.BoneWeights[0].bone);

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
                vertex.Normal = transBindPose.MultiplyPoint3x3(vertex.Normal);
                var tangentVec3 = transBindPose.MultiplyPoint3x3(vertex.Tangent);
                vertex.Tangent = new Vector4(tangentVec3.x, tangentVec3.y, tangentVec3.z, vertex.Tangent.w);

                var buffer = vertex.BlendShapeBuffer;
                var bufferVertexIndex = vertex.BlendShapeBufferVertexIndex;

                static void ApplyMatrixToArray(Matrix4x4 matrix, NativeArray<Vector3>[] arrayArray, int index)
                {
                    foreach (var array1 in arrayArray)
                    {
                        // Why NativeArray<Vector3>.[array] is not readonly accessor?
                        var array = array1;
                        array[index] = matrix.MultiplyPoint3x3(array[index]);
                    }
                }

                ApplyMatrixToArray(transBindPose, buffer.DeltaVertices, bufferVertexIndex);
                ApplyMatrixToArray(transBindPose, buffer.DeltaNormals, bufferVertexIndex);
                ApplyMatrixToArray(transBindPose, buffer.DeltaTangents, bufferVertexIndex);

                var weightSum = vertex.BoneWeights.Select(x => x.weight).Sum();
                // I want weightSum to be 1.0 but it may not.
                vertex.BoneWeights.Clear();
                vertex.BoneWeights.Add((finalBone, weightSum));
            });

            Profiler.EndSample();

            Profiler.BeginSample("Optimize Bindpose Phase 2");
            // Optimization2: If there are same (BindPose, Transform) pair, merge
            // This is optimization for RestPose bone merging
            var boneMapping = new Dictionary<Bone, Bone>();
            foreach (var grouping in meshInfo2.Bones.GroupBy(x => new BoneUniqKey(x)))
            {
                if (grouping.Key.Transform == null) continue;
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

            Profiler.EndSample();
        }

        private bool ValidBindPose(Matrix4x4 matrix)
        {
            const float SMALL = 0.001f;
            const float BIG = 10000;

            // if scaling part of bindpose is too small or too big, it can lead to invalid bind pose optimization
            var scaling = Mathf.Abs(new Matrix3x3(matrix).determinant);

            if (float.IsInfinity(scaling)) return false;
            if (float.IsNaN(scaling)) return false;
            if (scaling < SMALL) return false;
            if (scaling > BIG) return false;

            // if offset part of bindpose is too big, it may lead to invalid bind pose optimization

            var offset = matrix.offset;
            if (Mathf.Abs(offset.x) > BIG) return false;
            if (Mathf.Abs(offset.y) > BIG) return false;
            if (Mathf.Abs(offset.z) > BIG) return false;

            return true;
        }

        private readonly struct BoneUniqKey : IEquatable<BoneUniqKey>
        {
            private readonly Matrix4x4 _bindPoseInfo;
            public readonly Transform? Transform;

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

            public override bool Equals(object? obj) => obj is BoneUniqKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(_bindPoseInfo, Transform);
        }


        public struct MergeBoneTransParentInfo
        {
            public Quaternion ParentRotation;
            public Matrix4x4 ParentMatrix;
            public bool ActiveSelf;
            public string NamePrefix;

            public (Vector3 position, Quaternion rotation, Vector3 scale) ComputeInfoFor(Transform child)
            {
                if (child == null) throw new ArgumentNullException(nameof(child));

                var selfLocalRotation = child.localRotation;

                var matrix = ParentMatrix * Matrix4x4.TRS(child);

                var rotation = ParentRotation * FixRotWithParentScale(selfLocalRotation, child.parent.localScale);

                var reversedMatrix = Matrix3x3.Rotate(Quaternion.Inverse(rotation)) * matrix.To3x3();
                var scale = new Vector3(reversedMatrix.m00, reversedMatrix.m11, reversedMatrix.m22);


                return (matrix.offset, rotation, scale);
            }

            public static MergeBoneTransParentInfo Compute(Transform parent, Transform? root)
            {
                var parentRotation = Quaternion.identity;
                var parentMatrix = Matrix4x4.identity;
                var segments = new List<string>();
                var activeSelf = true;

                for (var current = parent; current != root; current = current.parent)
                {
                    parentRotation = current.localRotation * FixRotWithParentScale(parentRotation, current.localScale);
                    parentMatrix = Matrix4x4.TRS(current) * parentMatrix;
                    segments.Add(current.name);
                    activeSelf &= current.gameObject.activeSelf;
                }

                segments.Reverse();

                return new MergeBoneTransParentInfo
                {
                    ParentRotation = parentRotation,
                    ParentMatrix = parentMatrix,
                    ActiveSelf = activeSelf,
                    NamePrefix = string.Join("$", segments),
                };
            }

            private static Quaternion FixRotWithParentScale(Quaternion rotation, Vector3 parentScale)
            {
                // adjust rotation based on scale sign of parent
                return new Quaternion
                {
                    x = Mathf.Sign(parentScale.z * parentScale.y) * rotation.x,
                    y = Mathf.Sign(parentScale.z * parentScale.x) * rotation.y,
                    z = Mathf.Sign(parentScale.y * parentScale.x) * rotation.z,
                    w = rotation.w,
                };
            }
        }
    }
}
