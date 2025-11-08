using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using nadena.dev.ndmf.preview;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.EditModePreview
{
    internal class RemoveMeshInBoxRenderFilter : AAORenderFilterBase<RemoveMeshInBox>
    {
        private RemoveMeshInBoxRenderFilter() : base("Remove Mesh by Box", "remove-mesh-in-box")
        {
        }

        public static RemoveMeshInBoxRenderFilter Instance { get; } = new();
        protected override AAORenderFilterNodeBase<RemoveMeshInBox> CreateNode() => new RemoveMeshInBoxRendererNode();
        protected override bool SupportsMultiple() => true;
    }

    internal class RemoveMeshInBoxRendererNode : AAORenderFilterNodeBase<RemoveMeshInBox>
    {
        public static bool ComputeShouldRemoveVertex(
            Renderer renderer,
            RemoveMeshInBox[] components,
            ComputeContext context,
            out NativeArray<bool> removeVertex
        )
        {
            Mesh mesh;
            Mesh bakedMesh;
            if (renderer is SkinnedMeshRenderer skinned)
            {
                mesh = skinned.sharedMesh;
                UnityEngine.Profiling.Profiler.BeginSample("BakeMesh");
                bakedMesh = new Mesh();
                skinned.BakeMesh(bakedMesh);
                UnityEngine.Profiling.Profiler.EndSample();
            }
            else if (renderer is MeshRenderer meshRenderer)
            {
                if (!meshRenderer.TryGetComponent<MeshFilter>(out var meshFilter))
                {
                    removeVertex = default;
                    return false;
                }

                mesh = meshFilter.sharedMesh;
                bakedMesh = mesh; // static mesh, no need to bake
            }
            else
            {
                removeVertex = default;
                return false;
            }

            var localToWorldMatrix = Matrix4x4.TRS(renderer.transform.position, renderer.transform.rotation, Vector3.one);


            removeVertex = new NativeArray<bool>(mesh.vertexCount, Allocator.TempJob);

            try
            {
                using var realPosition = new NativeArray<Vector3>(bakedMesh.vertices, Allocator.TempJob);

                UnityEngine.Profiling.Profiler.BeginSample("CollectVertexData");
                foreach (var component in components)
                {
                    var boxesArray = context.Observe(component, c => c.boxes.ToArray(), Enumerable.SequenceEqual);
                    var componentWorldToLocalMatrix = context.Observe(component.transform, c => c.worldToLocalMatrix);
                    var removeInBox = context.Observe(component, c => c.removeInBox);
                    using var boxes = new NativeArray<RemoveMeshInBox.BoundingBox>(boxesArray, Allocator.TempJob);

                    new CheckRemoveVertexJob
                    {
                        removeInBox = removeInBox,
                        boxes = boxes,
                        vertexPosition = realPosition,
                        removeVertex = removeVertex,
                        meshToBoxTransform = componentWorldToLocalMatrix * localToWorldMatrix,
                    }.Schedule(mesh.vertexCount, 32).Complete();
                }

                UnityEngine.Profiling.Profiler.EndSample();
            }
            catch
            {
                removeVertex.Dispose();
                throw;
            }

            return true;
        }

        protected override ValueTask<bool> Process(Renderer original, Renderer proxy,
            RemoveMeshInBox[] components,
            Mesh duplicated, ComputeContext context)
        {
            // Observe transform since the BakeMesh depends on the transform
            context.Observe(proxy.transform, t => t.localToWorldMatrix);

            if (!ComputeShouldRemoveVertex(proxy, components, context, out var vertexIsInBox)) return new ValueTask<bool>(false);
            using var _ = vertexIsInBox;

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
                    vertexIsInBox = vertexIsInBox,
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

            return new ValueTask<bool>(true);
        }

        [BurstCompile]
        struct CheckRemoveVertexJob : IJobParallelFor
        {
            // ReSharper disable InconsistentNaming
            public bool removeInBox;
            [ReadOnly] public NativeArray<RemoveMeshInBox.BoundingBox> boxes;
            [ReadOnly] public NativeArray<Vector3> vertexPosition;
            public NativeArray<bool> removeVertex;

            public Matrix4x4 meshToBoxTransform;
            // ReSharper restore InconsistentNaming

            public void Execute(int vertexIndex)
            {
                var inBox = false;

                var position = meshToBoxTransform.MultiplyPoint3x4(vertexPosition[vertexIndex]);
                foreach (var box in boxes)
                {
                    if (box.ContainsVertex(position))
                    {
                        inBox = true;
                        break;
                    }
                }

                if (removeInBox == inBox)
                    removeVertex[vertexIndex] = true;
            }
        }

        [BurstCompile]
        struct ShouldRemovePrimitiveJob : IJobParallelFor
        {
            // ReSharper disable InconsistentNaming
            public int vertexPerPrimitive;
            [ReadOnly] public NativeArray<int> triangles;
            [ReadOnly] public NativeArray<bool> vertexIsInBox;
            [WriteOnly] public NativeArray<bool> shouldRemove;
            // ReSharper restore InconsistentNaming

            public void Execute(int primitiveIndex)
            {
                var baseIndex = primitiveIndex * vertexPerPrimitive;
                var indices = triangles.Slice(baseIndex, vertexPerPrimitive);

                var result = true;
                foreach (var index in indices)
                {
                    if (!vertexIsInBox[index])
                    {
                        result = false;
                        break;
                    }
                }

                shouldRemove[primitiveIndex] = result;
            }
        }
    }
}
