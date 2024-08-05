using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using nadena.dev.ndmf.preview;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.EditModePreview
{
    internal class RemoveMeshByBlendShapeRenderFilter : AAORenderFilterBase<RemoveMeshByBlendShape>
    {
        public static RemoveMeshByBlendShapeRenderFilter Instance { get; } = new();

        protected override AAORenderFilterNodeBase<RemoveMeshByBlendShape> CreateNode() =>
            new RemoveMeshByBlendShapeRendererNode();

        protected override bool SupportsMultiple() => false;
    }

    internal class RemoveMeshByBlendShapeRendererNode : AAORenderFilterNodeBase<RemoveMeshByBlendShape>
    {
        protected override ValueTask Process(SkinnedMeshRenderer original, SkinnedMeshRenderer proxy,
            RemoveMeshByBlendShape[] components,
            Mesh duplicated, ComputeContext context)
        {
            // we're removing vertices moving greater than tolerance, we're collecting min tolerance for each shape
            var toleranceSqrByShape = new Dictionary<string, double>();
            foreach (var component in components)
            {
                foreach (var shape in component.shapeKeysSet.GetAsSet())
                {
                    var toleranceSqr = component.tolerance * component.tolerance;
                    if (toleranceSqrByShape.TryGetValue(shape, out var oldToleranceSqr))
                        toleranceSqrByShape[shape] = Math.Min(oldToleranceSqr, toleranceSqr);
                    else
                        toleranceSqrByShape[shape] = toleranceSqr;
                }
            }

            using var shouldRemoveVertex = new NativeArray<bool>(duplicated.vertexCount, Allocator.TempJob);

            UnityEngine.Profiling.Profiler.BeginSample("CollectVertexData");
            {
                var deltaBuffer = new Vector3[duplicated.vertexCount];
                using var deltaBufferJob = new NativeArray<Vector3>(deltaBuffer.Length, Allocator.TempJob);

                for (var shapeIndex = 0; shapeIndex < original.sharedMesh.blendShapeCount; shapeIndex++)
                {
                    var shapeName = original.sharedMesh.GetBlendShapeName(shapeIndex);
                    if (!toleranceSqrByShape.TryGetValue(shapeName, out var toleranceSqr)) continue;

                    for (var frameIndex = 0;
                         frameIndex < original.sharedMesh.GetBlendShapeFrameCount(shapeIndex);
                         frameIndex++)
                    {
                        original.sharedMesh.GetBlendShapeFrameVertices(shapeIndex, frameIndex, deltaBuffer, null, null);
                        deltaBufferJob.CopyFrom(deltaBuffer);

                        new CheckRemoveVertexJob
                        {
                            toleranceSqr = toleranceSqr,
                            blendShapeDelta = deltaBufferJob,
                            shouldRemoveVertex = shouldRemoveVertex,
                        }.Schedule(duplicated.vertexCount, 32).Complete();
                    }
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();

            var uv = duplicated.uv;
            using var uvJob = new NativeArray<Vector2>(uv, Allocator.TempJob);

            for (var subMeshI = 0; subMeshI < duplicated.subMeshCount; subMeshI++)
            {
                var subMesh = duplicated.GetSubMesh(subMeshI);
                int vertexPerPrimitive;
                switch (subMesh.topology)
                {
                    case MeshTopology.Triangles:
                        vertexPerPrimitive = 3;
                        break;
                    case MeshTopology.Quads:
                        vertexPerPrimitive = 4;
                        break;
                    case MeshTopology.Lines:
                        vertexPerPrimitive = 2;
                        break;
                    case MeshTopology.Points:
                        vertexPerPrimitive = 1;
                        break;
                    case MeshTopology.LineStrip:
                    default:
                        // unsupported topology
                        continue;
                }

                var triangles = duplicated.GetTriangles(subMeshI);
                var primitiveCount = triangles.Length / vertexPerPrimitive;

                using var trianglesJob = new NativeArray<int>(triangles, Allocator.TempJob);
                using var shouldRemove = new NativeArray<bool>(primitiveCount, Allocator.TempJob);
                UnityEngine.Profiling.Profiler.BeginSample("JobLoop");
                var job = new ShouldRemovePrimitiveJob
                {
                    vertexPerPrimitive = vertexPerPrimitive,
                    triangles = trianglesJob,
                    shouldRemoveVertex = shouldRemoveVertex,
                    shouldRemove = shouldRemove,
                };
                job.Schedule(primitiveCount, 32).Complete();
                UnityEngine.Profiling.Profiler.EndSample();

                var modifiedTriangles = new List<int>(triangles.Length);

                UnityEngine.Profiling.Profiler.BeginSample("Inner Main Loop");
                for (var primitiveI = 0; primitiveI < primitiveCount; primitiveI++)
                    if (!shouldRemove[primitiveI])
                        for (var vertexI = 0; vertexI < vertexPerPrimitive; vertexI++)
                            modifiedTriangles.Add(triangles[primitiveI * vertexPerPrimitive + vertexI]);
                UnityEngine.Profiling.Profiler.EndSample();

                duplicated.SetTriangles(modifiedTriangles, subMeshI);
            }

            return default;
        }

        [BurstCompile]
        struct CheckRemoveVertexJob : IJobParallelFor
        {
            // ReSharper disable InconsistentNaming
            public double toleranceSqr;
            [ReadOnly] public NativeArray<Vector3> blendShapeDelta;
            public NativeArray<bool> shouldRemoveVertex;
            // ReSharper restore InconsistentNaming

            public void Execute(int vertexIndex)
            {
                shouldRemoveVertex[vertexIndex] = shouldRemoveVertex[vertexIndex] ||
                                                  blendShapeDelta[vertexIndex].sqrMagnitude > toleranceSqr;
            }
        }

        [BurstCompile]
        struct ShouldRemovePrimitiveJob : IJobParallelFor
        {
            // ReSharper disable InconsistentNaming
            public int vertexPerPrimitive;
            [ReadOnly] public NativeArray<int> triangles;
            [ReadOnly] public NativeArray<bool> shouldRemoveVertex;
            [WriteOnly] public NativeArray<bool> shouldRemove;
            // ReSharper restore InconsistentNaming

            public void Execute(int primitiveIndex)
            {
                var baseIndex = primitiveIndex * vertexPerPrimitive;
                var indices = triangles.Slice(baseIndex, vertexPerPrimitive);

                var result = false;
                foreach (var index in indices)
                {
                    if (shouldRemoveVertex[index])
                    {
                        result = true;
                        break;
                    }
                }

                shouldRemove[primitiveIndex] = result;
            }
        }
    }
}
