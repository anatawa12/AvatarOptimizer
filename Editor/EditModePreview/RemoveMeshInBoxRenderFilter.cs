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
        protected override RenderAspects ReadAspects => RenderAspects.Mesh | RenderAspects.Shapes;

        /// <summary>
        /// Per-component in-box state of each vertex.
        /// A primitive is removed if, for any component, whether all vertices of the primitive are in the boxes of the
        /// component equals removeInBox of the component. This matches RemoveMeshInBoxProcessor.
        /// </summary>
        public sealed class BoxRemovalData : System.IDisposable
        {
            // inBox[componentIndex * VertexCount + vertexIndex]
            public NativeArray<bool> InBox;
            public NativeArray<bool> RemoveInBox;
            public readonly int VertexCount;
            public int ComponentCount => RemoveInBox.Length;

            public BoxRemovalData(int componentCount, int vertexCount)
            {
                VertexCount = vertexCount;
                InBox = new NativeArray<bool>(componentCount * vertexCount, Allocator.TempJob);
                RemoveInBox = new NativeArray<bool>(componentCount, Allocator.TempJob);
            }

            public bool ShouldRemovePrimitive(System.Span<int> vertexIndices)
            {
                for (var componentIndex = 0; componentIndex < ComponentCount; componentIndex++)
                {
                    var baseIndex = componentIndex * VertexCount;
                    var allInBox = true;
                    foreach (var vertexIndex in vertexIndices)
                    {
                        if (!InBox[baseIndex + vertexIndex])
                        {
                            allInBox = false;
                            break;
                        }
                    }

                    if (allInBox == RemoveInBox[componentIndex]) return true;
                }

                return false;
            }

            public void Dispose()
            {
                if (InBox.IsCreated) InBox.Dispose();
                if (RemoveInBox.IsCreated) RemoveInBox.Dispose();
            }
        }

        public static bool ComputeInBox(
            Renderer renderer,
            RemoveMeshInBox[] components,
            ComputeContext context,
            out BoxRemovalData? data
        )
        {
            data = null;
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
                if (!meshRenderer.TryGetComponent<MeshFilter>(out var meshFilter)) return false;
                mesh = meshFilter.sharedMesh;
                bakedMesh = mesh; // static mesh, no need to bake
            }
            else
            {
                return false;
            }

            var localToWorldMatrix = Matrix4x4.TRS(renderer.transform.position, renderer.transform.rotation, Vector3.one);

            var vertexCount = mesh.vertexCount;
            var result = new BoxRemovalData(components.Length, vertexCount);

            try
            {
                using var realPosition = new NativeArray<Vector3>(bakedMesh.vertices, Allocator.TempJob);

                UnityEngine.Profiling.Profiler.BeginSample("CollectVertexData");
                try
                {
                    for (var componentIndex = 0; componentIndex < components.Length; componentIndex++)
                    {
                        var component = components[componentIndex];
                        var boxesArray = context.Observe(component, c => c.boxes.ToArray(), Enumerable.SequenceEqual);
                        var componentWorldToLocalMatrix = context.Observe(component.transform, c => c.worldToLocalMatrix);
                        result.RemoveInBox[componentIndex] = context.Observe(component, c => c.removeInBox);
                        using var boxes = new NativeArray<RemoveMeshInBox.BoundingBox>(boxesArray, Allocator.TempJob);

                        new CheckInBoxJob
                        {
                            boxes = boxes,
                            vertexPosition = realPosition,
                            inBox = result.InBox.GetSubArray(componentIndex * vertexCount, vertexCount),
                            meshToBoxTransform = componentWorldToLocalMatrix * localToWorldMatrix,
                        }.Schedule(vertexCount, 32).Complete();
                    }
                }
                finally
                {
                    UnityEngine.Profiling.Profiler.EndSample();
                }
            }
            catch
            {
                result.Dispose();
                throw;
            }
            finally
            {
                if (renderer is SkinnedMeshRenderer)
                {
                    Object.DestroyImmediate(bakedMesh);
                }
            }

            data = result;
            return true;
        }

        protected override ValueTask<bool> Process(Renderer original, Renderer proxy,
            RemoveMeshInBox[] components,
            Mesh duplicated, ComputeContext context)
        {
            // Observe transform since the BakeMesh depends on the transform
            context.Observe(proxy.transform, t => t.localToWorldMatrix);

            if (!ComputeInBox(proxy, components, context, out var boxData) || boxData == null) return new ValueTask<bool>(false);
            using var _ = boxData;

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
                    inBox = boxData.InBox,
                    removeInBox = boxData.RemoveInBox,
                    vertexCount = boxData.VertexCount,
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
        struct CheckInBoxJob : IJobParallelFor
        {
            // ReSharper disable InconsistentNaming
            [ReadOnly] public NativeArray<RemoveMeshInBox.BoundingBox> boxes;
            [ReadOnly] public NativeArray<Vector3> vertexPosition;
            [WriteOnly] public NativeArray<bool> inBox;

            public Matrix4x4 meshToBoxTransform;
            // ReSharper restore InconsistentNaming

            public void Execute(int vertexIndex)
            {
                var result = false;

                var position = meshToBoxTransform.MultiplyPoint3x4(vertexPosition[vertexIndex]);
                foreach (var box in boxes)
                {
                    if (box.ContainsVertex(position))
                    {
                        result = true;
                        break;
                    }
                }

                inBox[vertexIndex] = result;
            }
        }

        [BurstCompile]
        struct ShouldRemovePrimitiveJob : IJobParallelFor
        {
            // ReSharper disable InconsistentNaming
            public int vertexPerPrimitive;
            public int vertexCount;
            [ReadOnly] public NativeArray<int> triangles;
            // inBox[componentIndex * vertexCount + vertexIndex]
            [ReadOnly] public NativeArray<bool> inBox;
            [ReadOnly] public NativeArray<bool> removeInBox;
            [WriteOnly] public NativeArray<bool> shouldRemove;
            // ReSharper restore InconsistentNaming

            public void Execute(int primitiveIndex)
            {
                var baseIndex = primitiveIndex * vertexPerPrimitive;
                var indices = triangles.Slice(baseIndex, vertexPerPrimitive);

                // same as RemoveMeshInBoxProcessor: each component removes primitives
                // if all vertices are in box == removeInBox
                var result = false;
                for (var componentIndex = 0; componentIndex < removeInBox.Length; componentIndex++)
                {
                    var componentBase = componentIndex * vertexCount;
                    var allInBox = true;
                    foreach (var index in indices)
                    {
                        if (!inBox[componentBase + index])
                        {
                            allInBox = false;
                            break;
                        }
                    }

                    if (allInBox == removeInBox[componentIndex])
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
