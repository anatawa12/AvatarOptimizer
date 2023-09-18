using System;
using JetBrains.Annotations;
using Unity.Collections;
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
        public NativeArray<Vector3> Vertices => _vertices;

        private readonly BlendShapePreviewContext _blendShapePreviewContext;
        // not transformed vertices
        private NativeArray<Vector3> _originalVertices;
        // this should be blendShape transformed
        private NativeArray<Vector3> _vertices;
        // configured BlendShape weights
        [NotNull] private readonly float[] _blendShapeWeights;

        public RemoveMeshWithBoxPreviewContext(BlendShapePreviewContext blendShapePreviewContext, Mesh originalMesh)
        {
            _blendShapePreviewContext = blendShapePreviewContext;
            _originalVertices = new NativeArray<Vector3>(originalMesh.vertices, Allocator.Persistent);

            // initialize with original vertices
            _vertices = new NativeArray<Vector3>(_originalVertices, Allocator.Persistent);

            _blendShapeWeights = new float[originalMesh.blendShapeCount];
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

            _blendShapePreviewContext.ComputeBlendShape(_blendShapeWeights, _originalVertices, _vertices);
        }

        public void Dispose()
        {
            _originalVertices.Dispose();
            _vertices.Dispose();
        }
    }
}