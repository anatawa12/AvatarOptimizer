using System;
using JetBrains.Annotations;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.EditModePreview
{
    /// <summary>
    /// Preview Context for RemoveMesh with Box.
    /// RuntimePreview of Remove Mesh with Box needs BlendShape applied vertex position so
    /// this class holds that information and update if nesessary
    /// </summary>
    class RemoveMeshWithBoxPreviewContext : IDisposable
    {
        public NativeArray<Vector3> Vertices => _boneAppliedVertices;

        private readonly BlendShapePreviewContext _blendShapePreviewContext;
        // not transformed vertices
        private NativeArray<Vector3> _originalVertices;
        // this should be blendShape transformed
        private NativeArray<Vector3> _blendShapeAppliedVertices;
        // this should be Bone transformed
        private NativeArray<Vector3> _boneAppliedVertices;

        // configured BlendShape weights
        [NotNull] private readonly float[] _blendShapeWeights;

        // about bone
        private readonly Matrix4x4[] _bindPoses;
        private Matrix4x4 _rendererWorldToLocal;
        private NativeArray<Matrix4x4> _boneTransform;
        private NativeArray<BoneWeight1> _boneWeights;
        private NativeArray<byte> _bonesPerVertex;
        private NativeArray<int> _boneIndexStart;

        public RemoveMeshWithBoxPreviewContext(BlendShapePreviewContext blendShapePreviewContext, Mesh originalMesh)
        {
            _blendShapePreviewContext = blendShapePreviewContext;
            _originalVertices = new NativeArray<Vector3>(originalMesh.vertices, Allocator.Persistent);

            // initialize with original vertices
            _rendererWorldToLocal = Matrix4x4.identity;
            _blendShapeAppliedVertices = new NativeArray<Vector3>(_originalVertices, Allocator.Persistent);
            _boneAppliedVertices = new NativeArray<Vector3>(_originalVertices, Allocator.Persistent);

            _blendShapeWeights = new float[originalMesh.blendShapeCount];

            _bindPoses = new Matrix4x4[originalMesh.bindposes.Length];
            for (var i = 0; i < _bindPoses.Length; i++)
                _bindPoses[i] = originalMesh.bindposes[i];
            _boneTransform = new NativeArray<Matrix4x4>(originalMesh.bindposes.Length, Allocator.Persistent);
            _boneTransform.AsSpan().Fill(Matrix4x4.identity);
            _boneWeights = originalMesh.GetAllBoneWeights();
            _bonesPerVertex = originalMesh.GetBonesPerVertex();
            _boneIndexStart = new NativeArray<int>(originalMesh.vertexCount, Allocator.Persistent);
            var start = 0;
            for (var i = 0; i < _bonesPerVertex.Length; i++)
            {
                _boneIndexStart[i] = start;
                start += _bonesPerVertex[i];
            }
        }

        public void OnUpdateSkinnedMeshRenderer(SkinnedMeshRenderer renderer)
        {
            var modified = false;
            for (var i = 0; i < _blendShapeWeights.Length; i++)
            {
                var currentWeight = renderer.GetBlendShapeWeight(i);
                if (Math.Abs(currentWeight - _blendShapeWeights[i]) > Mathf.Epsilon)
                {
                    _blendShapeWeights[i] = currentWeight;
                    modified = true;
                }
            }

            if (!modified) return;

            _blendShapePreviewContext.ComputeBlendShape(_blendShapeWeights, _originalVertices, _blendShapeAppliedVertices);
            ApplyBones();
        }

        public void OnUpdateBones(SkinnedMeshRenderer renderer)
        {
            var bones = renderer.bones;
            for (var i = 0; i < bones.Length && i < _boneTransform.Length; i++)
            {
                var transformMatrix = bones[i] ? (Matrix4x4)bones[i].localToWorldMatrix : Matrix4x4.identity;
                _boneTransform[i] = transformMatrix * _bindPoses[i];
            }
            for (var i = bones.Length; i < _boneTransform.Length; i++)
                _boneTransform[i] = _bindPoses[i];
            _rendererWorldToLocal = renderer.transform.worldToLocalMatrix;

            ApplyBones();
        }

        private void ApplyBones()
        {
            new ApplyBoneJob
            {
                RendererWorldToLocal = _rendererWorldToLocal,
                BoneMatrix = _boneTransform,
                BoneWeights = _boneWeights,
                BonesPerVertex = _bonesPerVertex,
                BoneIndexStart = _boneIndexStart,
                OriginalVertices = _blendShapeAppliedVertices,
                ResultVertices = _boneAppliedVertices,
            }.Schedule(_boneAppliedVertices.Length, 1).Complete();
        }

        [BurstCompile]
        struct ApplyBoneJob: IJobParallelFor
        {
            [ReadOnly]
            public Matrix4x4 RendererWorldToLocal;
            [ReadOnly]
            public NativeArray<Matrix4x4> BoneMatrix;
            [ReadOnly]
            public NativeArray<BoneWeight1> BoneWeights;
            [ReadOnly]
            public NativeArray<byte> BonesPerVertex;
            [ReadOnly]
            public NativeArray<int> BoneIndexStart;
            
            public NativeArray<Vector3> OriginalVertices;
            public NativeArray<Vector3> ResultVertices;

            public void Execute(int vertexIndex)
            {
                var weightOffset = BoneIndexStart[vertexIndex];
                var weightCount = BonesPerVertex[vertexIndex];
                var matrix = Matrix4x4.zero;
                for (var weightIndex = 0; weightIndex < weightCount; weightIndex++)
                {
                    var weight = BoneWeights[weightOffset + weightIndex];
                    matrix += BoneMatrix[weight.boneIndex] * weight.weight;
                }
                
                matrix = RendererWorldToLocal * matrix;

                ResultVertices[vertexIndex] = matrix.MultiplyPoint3x4(OriginalVertices[vertexIndex]);
            }
        }

        public void Dispose()
        {
            _originalVertices.Dispose();
            _blendShapeAppliedVertices.Dispose();
            _boneAppliedVertices.Dispose();

            _boneTransform.Dispose();
            _boneIndexStart.Dispose();
        }
    }
}