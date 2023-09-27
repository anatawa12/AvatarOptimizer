using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.EditModePreview
{
    class RemoveMeshByBlendShapePreviewContext : IDisposable
    {
        // PerVertexBlendShapeRemoveFlags[vertexIndex / 32 + blendShapeIndex * _rowVertexCount / 32] & 1 << (vertexIndex % 32)
        // blendshape vertex transforms. _blendShapeVertices[vertexIndex + blendShapeIndex * vertexCount]
        public NativeArray<Vector3> BlendShapeMovements => _blendShapeMovements;

        private NativeArray<Vector3> _blendShapeMovements;

        private HashSet<string> _previousRemovingShapeKeys;

        public RemoveMeshByBlendShapePreviewContext(BlendShapePreviewContext blendShapePreviewContext,
            Mesh originalMesh)
        {
            var vertexCount = originalMesh.vertexCount;
            var blendShapeCount = originalMesh.blendShapeCount;

            _blendShapeMovements = new NativeArray<Vector3>(blendShapeCount * vertexCount, Allocator.Persistent);

            try
            {
                using (var zeros = new NativeArray<Vector3>(vertexCount, Allocator.TempJob))
                {
                    var weights = new float[blendShapeCount];
                    for (var i = 0; i < blendShapeCount; i++)
                    {
                        weights[i] = 100;
                        blendShapePreviewContext.ComputeBlendShape(weights,
                            zeros,
                            _blendShapeMovements.Slice(i * vertexCount, vertexCount)
                        );
                        weights[i] = 0;
                    }
                }
            }
            catch
            {
                _blendShapeMovements.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            _blendShapeMovements.Dispose();
        }
    }
}