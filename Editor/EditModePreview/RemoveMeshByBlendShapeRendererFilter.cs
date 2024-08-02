using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using nadena.dev.ndmf.preview;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.EditModePreview
{
    internal class RemoveMeshByBlendShapeRendererFilter : IRenderFilter
    {
        public static RemoveMeshByBlendShapeRendererFilter Instance { get; } = new();

        public ImmutableList<RenderGroup> GetTargetGroups(ComputeContext ctx)
        {
            // currently remove meshes are only supported
            var rmByMask = ctx.GetComponentsByType<RemoveMeshByBlendShape>();

            var targets = new HashSet<Renderer>();

            foreach (var component in rmByMask)
            {
                if (component.GetComponent<MergeSkinnedMesh>())
                {
                    // the component applies to MergeSkinnedMesh, which is not supported for now
                    // TODO: rollup the remove operation to source renderers of MergeSkinnedMesh
                    continue;
                }

                var renderer = component.GetComponent<SkinnedMeshRenderer>();
                if (renderer == null) continue;
                if (renderer.sharedMesh == null) continue;

                targets.Add(renderer);
            }

            return targets.Select(RenderGroup.For).ToImmutableList();
        }

        public async Task<IRenderFilterNode> Instantiate(RenderGroup group, IEnumerable<(Renderer, Renderer)> proxyPairs, ComputeContext context)
        {
            var pair = proxyPairs.Single();
            if (!(pair.Item1 is SkinnedMeshRenderer original)) return null;
            if (!(pair.Item2 is SkinnedMeshRenderer proxy)) return null;

            // we modify the mesh so we need to clone the mesh

            var rmByMask = context.Observe(context.GetComponent<RemoveMeshByBlendShape>(original.gameObject));

            var node = new RemoveMeshByBlendShapeRendererNode();

            await node.Process(original, proxy, rmByMask, context);

            return node;
        }
    }

    internal class RemoveMeshByBlendShapeRendererNode : IRenderFilterNode
    {
        private Mesh _duplicated;

        public RenderAspects Reads => RenderAspects.Mesh | RenderAspects.Shapes;
        public RenderAspects WhatChanged => RenderAspects.Mesh | RenderAspects.Shapes;

        public async Task Process(
            SkinnedMeshRenderer original,
            SkinnedMeshRenderer proxy,
            [NotNull] RemoveMeshByBlendShape rmByBlensShape,
            ComputeContext context)
        {
            UnityEngine.Profiling.Profiler.BeginSample($"RemoveMeshByBlendShapeRendererNode.Process({original.name})");

            var duplicated = Object.Instantiate(proxy.sharedMesh);
            duplicated.name = proxy.sharedMesh.name + " (AAO Generated)";

            var blendShapes = rmByBlensShape.shapeKeysSet.GetAsSet();
            var toleranceSqr = rmByBlensShape.tolerance * rmByBlensShape.tolerance;
            using var shouldRemoveVertex = new NativeArray<bool>(duplicated.vertexCount, Allocator.TempJob);

            UnityEngine.Profiling.Profiler.BeginSample("CollectVertexData");
            {
                var deltaBuffer = new Vector3[duplicated.vertexCount];
                using var deltaBufferJob = new NativeArray<Vector3>(deltaBuffer.Length, Allocator.TempJob);
                
                for (var shapeIndex = 0; shapeIndex < original.sharedMesh.blendShapeCount; shapeIndex++)
                {
                    var shapeName = original.sharedMesh.GetBlendShapeName(shapeIndex);
                    if (!blendShapes.Contains(shapeName)) continue;

                    for (var frameIndex = 0; frameIndex < original.sharedMesh.GetBlendShapeFrameCount(shapeIndex); frameIndex++)
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

            proxy.sharedMesh = duplicated;
            _duplicated = duplicated;

            UnityEngine.Profiling.Profiler.EndSample();
        }

        [BurstCompile]
        struct CheckRemoveVertexJob : IJobParallelFor
        {
            // ReSharper disable InconsistentNaming
            public double toleranceSqr;
            [ReadOnly]
            public NativeArray<Vector3> blendShapeDelta;
            public NativeArray<bool> shouldRemoveVertex;
            // ReSharper restore InconsistentNaming

            public void Execute(int vertexIndex)
            {
                shouldRemoveVertex[vertexIndex] = shouldRemoveVertex[vertexIndex] || blendShapeDelta[vertexIndex].sqrMagnitude > toleranceSqr;
            }
        }

        [BurstCompile]
        struct ShouldRemovePrimitiveJob : IJobParallelFor
        {
            // ReSharper disable InconsistentNaming
            public int vertexPerPrimitive;
            [ReadOnly]
            public NativeArray<int> triangles;
            [ReadOnly]
            public NativeArray<bool> shouldRemoveVertex;
            [WriteOnly]
            public NativeArray<bool> shouldRemove;
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

        public void OnFrame(Renderer original, Renderer proxy)
        {
            if (_duplicated == null) return;
            if (proxy is SkinnedMeshRenderer skinnedMeshProxy)
                skinnedMeshProxy.sharedMesh = _duplicated;
        }

        public void Dispose()
        {
            if (_duplicated != null)
            {
                Object.DestroyImmediate(_duplicated);
                _duplicated = null;
            }
        }
    }
}
