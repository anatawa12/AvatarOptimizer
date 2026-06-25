using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

            // To prevent z-fighting from being introduced by Avatar Optimizer, we normalize bindposes to nearest
            // known bindpose if there is.
            // This may remove z-fighting that previously occurred, but I hope that is unlikely and unexpected,
            // so I accept removing z-fighting is acceptable.
            // Please let us know if this logic introduces problems for you.
            var primaryBindposes = new BoneInfoMap<Matrix4x4>();
            foreach (var renderer in context.GetComponents<SkinnedMeshRenderer>())
            {
                using (ErrorReport.WithContextObject(renderer))
                {
                    Profiler.BeginSample("CollectPrimaryBindposes");
                    foreach (var bone in context.GetMeshInfoFor(renderer).Bones)
                    {
                        // we assume the first bone we find is the most natural bone.
                        if (bone.Transform != null && !mergeMapping.ContainsKey(bone.Transform) && ValidBindPose(bone.Bindpose))
                            primaryBindposes.TryAdd(bone, bone.Bindpose);
                    }
                    Profiler.EndSample();
                }
            }

            foreach (var renderer in context.GetComponents<SkinnedMeshRenderer>())
            {
                using (ErrorReport.WithContextObject(renderer))
                {
                    Profiler.BeginSample("DoBoneMap");
                    var meshInfo2 = context.GetMeshInfoFor(renderer);
                    if (meshInfo2.Bones.Any(x => x.Transform != null && mergeMapping.ContainsKey(x.Transform)))
                        DoBoneMap2(meshInfo2, mergeMapping, context, primaryBindposes);
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

        private void DoBoneMap2(MeshInfo2 meshInfo2, Dictionary<Transform, Transform> mergeMapping,
            BuildContext context, BoneInfoMap<Matrix4x4> primaryBindposes)
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
                    bone.Bindpose = RelativeTransform(mapped, bone.Transform) * bone.Bindpose;
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
                // Normalize to our known 'best' matrix
                if (primaryBindposes.TryGetValue(bone, out var primaryBindpose))
                    bone.Bindpose = primaryBindpose;
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
                // However, due to float precision, the sum can be close to 1 without being exactly 1, so snap it to 1.
                if (Mathf.Approximately(weightSum, 1)) weightSum = 1;
                vertex.BoneWeights.Clear();
                vertex.BoneWeights.Add((finalBone, weightSum));
            });

            Profiler.EndSample();

            Profiler.BeginSample("Optimize Bindpose Phase 2");
            // Optimization2: If there are same (BindPose, Transform) pair, merge
            // This is optimization for RestPose bone merging
            var boneGrouping = new BoneInfoMap<List<Bone>>();
            foreach (var bone in meshInfo2.Bones)
                boneGrouping.TryAdd(bone, new List<Bone>())?.Add(bone);
            var boneMapping = new Dictionary<Bone, Bone>();
            foreach (var group in boneGrouping.Values)
            {
                var transform = group[0].Transform;
                if (transform == null) continue;
                primaryBones.TryGetValue(transform, out var primaryBone);
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

        /// <summary>
        /// Compute relative transform from one transform to another.
        ///
        /// Requires that `from` is a parent of `to`.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        private Matrix4x4 RelativeTransform(Transform from, Transform to)
        {
            // Same as the following, but supports zero scaling in parent
            // from.worldToLocalMatrix * to.localToWorldMatrix
            var result = Matrix4x4.identity;
            for (var current = to; current != from; current = current.parent)
            {
                if (current == null)
                {
                    throw new ArgumentException("to is not a child of from", nameof(to));
                }

                // accumulate localToWorldMatrix
                result = Matrix4x4.TRS(current.localPosition, current.localRotation, current.localScale) * result;
            }
            return result;
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

        private class BoneInfoMap<V>
        {
            private const float epsilon = 1f / (1 << 15);
            private const float translateScale = 1f / (1 << 2);

            private Dictionary<Transform, List<Entry>> _byBoneTransform = new();
            public IEnumerable<V> Values => _byBoneTransform.SelectMany(x => x.Value).Select(x => x.Value);

            private (List<Entry>? list, int index) FindEntry(Transform? transform, Matrix4x4 mat)
            {
                if (!ValidBone(transform, mat)) return (null, -1);
                if (!_byBoneTransform.TryGetValue(transform, out var list)) return (null, -1);

                var minIndex = -1;
                var minDistance = float.MaxValue;
                for (var i = 0; i < list.Count; i++)
                {
                    var key = list[i].Key;

                    var dist = Mathf.Max(
                        // rotation scale shear 3x3
                        Mathf.Abs(key.m00 - mat.m00),
                        Mathf.Abs(key.m01 - mat.m01),
                        Mathf.Abs(key.m02 - mat.m02),
                        Mathf.Abs(key.m10 - mat.m10),
                        Mathf.Abs(key.m11 - mat.m11),
                        Mathf.Abs(key.m12 - mat.m12),
                        Mathf.Abs(key.m20 - mat.m20),
                        Mathf.Abs(key.m21 - mat.m21),
                        Mathf.Abs(key.m22 - mat.m22),
                        // translate
                        Mathf.Abs(key.m03 - mat.m03) * translateScale,
                        Mathf.Abs(key.m13 - mat.m13) * translateScale,
                        Mathf.Abs(key.m23 - mat.m23) * translateScale
                    );

                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        minIndex = i;
                    }
                }

                if (minDistance > epsilon) return (list, -1);
                return (list, minIndex);
            }

            private bool ValidBone([NotNullWhen(true)] Transform? transform, Matrix4x4 mat)
            {
                if (transform == null) return false;
                if (mat.m30 != 0) return false;
                if (mat.m31 != 0) return false;
                if (mat.m32 != 0) return false;
                // ReSharper disable once CompareOfFloatsByEqualityOperator bindpose matrix is very stable
                if (mat.m33 != 1) return false;
                return true;
            }

            public V? TryAdd(Bone bone, V value)
            {
                if (!ValidBone(bone.Transform, bone.Bindpose)) return default;
                var entry = FindEntry(bone.Transform, bone.Bindpose);
                if (entry.index < 0)
                {
                    if (entry.list == null) 
                        _byBoneTransform.Add(bone.Transform, entry.list = new());

                    entry.list.Add(new()
                    {
                        Key = bone.Bindpose,
                        Value = value,
                    });
                    return value;
                }
                else
                {
                    // Entry already added
                    return entry.list![entry.index].Value;
                }
            }

            public bool TryGetValue(Bone bone, [NotNullWhen(true)] out V? o)
            {
                var entry = FindEntry(bone.Transform, bone.Bindpose);
                if (entry.list == null || entry.index < 0)
                {
                    o = default;
                    return false;
                }
                else
                {
                    o = entry.list[entry.index].Value!;
                    return true;
                }
            }

            private struct Entry
            {
                public Matrix4x4 Key;
                public V Value;
            }
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
