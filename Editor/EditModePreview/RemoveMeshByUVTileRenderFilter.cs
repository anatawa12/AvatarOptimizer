using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using nadena.dev.ndmf.preview;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.EditModePreview
{
    internal class RemoveMeshByUVTileRenderFilter : AAORenderFilterBase<RemoveMeshByUVTile>
    {
        private RemoveMeshByUVTileRenderFilter() : base("Remove Mesh by UV Tile", "remove-mesh-by-uv-tile")
        {
        }

        public static RemoveMeshByUVTileRenderFilter Instance { get; } = new();
        protected override AAORenderFilterNodeBase<RemoveMeshByUVTile> CreateNode() => new RemoveMeshByUVTileRendererNode();
        protected override bool SupportsMultiple() => false;
    }

    internal class RemoveMeshByUVTileRendererNode : AAORenderFilterNodeBase<RemoveMeshByUVTile>
    {
        protected override ValueTask<bool> Process(Renderer original, Renderer proxy,
            RemoveMeshByUVTile[] components,
            Mesh duplicated, ComputeContext context)
        {
            var component = components[0];


            var materialSettings = context.Observe(component, c => c.materials.ToArray(), (a, b) => a.SequenceEqual(b));
            for (var subMeshI = 0; subMeshI < duplicated.subMeshCount; subMeshI++)
            {
                if (subMeshI < materialSettings.Length)
                {
                    var materialSetting = materialSettings[subMeshI];
                    if (!materialSetting.RemoveAnyTile) continue;

                    Vector2[] uv;
                    switch (materialSetting.uvChannel)
                    {
                        case RemoveMeshByUVTile.UVChannel.TexCoord0:
                            uv = duplicated.uv;
                            break;
                        case RemoveMeshByUVTile.UVChannel.TexCoord1:
                            uv = duplicated.uv2;
                            break;
                        case RemoveMeshByUVTile.UVChannel.TexCoord2:
                            uv = duplicated.uv3;
                            break;
                        case RemoveMeshByUVTile.UVChannel.TexCoord3:
                            uv = duplicated.uv4;
                            break;
                        case RemoveMeshByUVTile.UVChannel.TexCoord4:
                            uv = duplicated.uv5;
                            break;
                        case RemoveMeshByUVTile.UVChannel.TexCoord5:
                            uv = duplicated.uv6;
                            break;
                        case RemoveMeshByUVTile.UVChannel.TexCoord6:
                            uv = duplicated.uv7;
                            break;
                        case RemoveMeshByUVTile.UVChannel.TexCoord7:
                            uv = duplicated.uv8;
                            break;
                        default:
                            continue;
                    }
                    if (uv.Length == 0) continue;
                    using var uvJob = new NativeArray<Vector2>(uv, Allocator.TempJob);

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
                    var job = new CheckRemovePolygonJob
                    {
                        vertexPerPrimitive = vertexPerPrimitive,
                        materialSetting = materialSetting,
                        triangles = trianglesJob,
                        uv = uvJob,
                        shouldRemove = shouldRemove,
                    };
                    job.Schedule(primitiveCount, 32).Complete();
                    UnityEngine.Profiling.Profiler.EndSample();

                    var modifiedTriangles = new List<int>(triangles.Length);

                    UnityEngine.Profiling.Profiler.BeginSample("Inner Main Loop");
                    for (var primitiveI = 0; primitiveI < triangles.Length; primitiveI += vertexPerPrimitive)
                    {
                        if (!shouldRemove[primitiveI / vertexPerPrimitive])
                        {
                            for (var vertexI = 0; vertexI < vertexPerPrimitive; vertexI++)
                                modifiedTriangles.Add(triangles[primitiveI + vertexI]);
                        }
                    }

                    UnityEngine.Profiling.Profiler.EndSample();

                    duplicated.SetTriangles(modifiedTriangles, subMeshI);
                }
            }

            return new ValueTask<bool>(true);
        }

        [BurstCompile]
        struct CheckRemovePolygonJob : IJobParallelFor
        {
            // ReSharper disable InconsistentNaming
            public int vertexPerPrimitive;
            [ReadOnly] public NativeArray<int> triangles;
            [ReadOnly] public NativeArray<Vector2> uv;
            [WriteOnly] public NativeArray<bool> shouldRemove;

            public RemoveMeshByUVTile.MaterialSlot materialSetting;
            // ReSharper restore InconsistentNaming

            public void Execute(int primitiveIndex)
            {
                var baseIndex = primitiveIndex * vertexPerPrimitive;
                var indices = triangles.Slice(baseIndex, vertexPerPrimitive);

                var result = false;
                foreach (var index in indices)
                {
                    if (ShouldRemoveVertex(index))
                    {
                        result = true;
                        break;
                    }
                }

                shouldRemove[primitiveIndex] = result;
            }

            bool ShouldRemoveVertex(int index)
            {
                var x = Mathf.FloorToInt(uv[index].x);
                var y = Mathf.FloorToInt(uv[index].y);
                if (x is < 0 or >= 4) return false;
                if (y is < 0 or >= 4) return false;
                var tile = x + y * 4;
                return materialSetting.GetTile(tile);
            }
        }
    }
}
