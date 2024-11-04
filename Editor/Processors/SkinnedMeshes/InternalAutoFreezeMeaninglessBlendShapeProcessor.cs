using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    internal class InternalAutoFreezeMeaninglessBlendShapeProcessor : EditSkinnedMeshProcessor<InternalAutoFreezeMeaninglessBlendShape>
    {
        public InternalAutoFreezeMeaninglessBlendShapeProcessor(InternalAutoFreezeMeaninglessBlendShape component) : base(component)
        {
        }

        public override EditSkinnedMeshProcessorOrder ProcessOrder => EditSkinnedMeshProcessorOrder.AutoConfigureFreezeBlendShape;

        public override void Process(BuildContext context, MeshInfo2 target)
        {
            var meaninglessBlendShapes = new HashSet<string>(target.BlendShapes.Select(x => x.name));
            var state = context.GetState<TraceAndOptimizes.TraceAndOptimizeState>();
            if (state.PreserveBlendShapes.TryGetValue(Target, out var preserve))
                meaninglessBlendShapes.ExceptWith(preserve);

            var buffers = new Dictionary<BlendShapeBuffer, NativeArray<bool>>();
            using var dispose = Utils.NewMultiDisposable(() => buffers.Values);

            foreach (var vertex in target.Vertices)
            {
                var buffer = vertex.BlendShapeBuffer;
                if (!buffers.TryGetValue(buffer, out var value))
                    buffers.Add(buffer, value = new NativeArray<bool>(buffer.VertexCount, Allocator.TempJob));

                if (value.Length == 0) continue;
                value[vertex.BlendShapeBufferVertexIndex] = true;
            }

            Profiler.BeginSample("buffers loop");
            
            var shapeCount = buffers.Sum(x => x.Key.Shapes.Count);
            
            using var isMeaningfulArray = new NativeArray<bool>(shapeCount, Allocator.TempJob);
            List<(string, JobHandle, int)> meaningfulIndex = new List<(string, JobHandle, int)>(shapeCount);

            int bufIndexNext = 0;
            foreach (var (shapeBuffer, useIndices) in buffers)
            {
                foreach (var (shapeName, shapeShape) in shapeBuffer.Shapes)
                {
                    var bufIndex = bufIndexNext++;
                    
                    if (!meaninglessBlendShapes.Contains(shapeName)) continue;
                    
                    JobHandle completionHandle = default;

                    foreach (var bufferIndex in shapeShape.FramesBufferIndices)
                    {
                        var deltaVertices = shapeBuffer.DeltaVertices[bufferIndex];
                        var deltaNormals = shapeBuffer.DeltaNormals[bufferIndex];
                        var deltaTangents = shapeBuffer.DeltaTangents[bufferIndex];

                        if (bufIndex >= isMeaningfulArray.Length)
                        {
                            throw new IndexOutOfRangeException("bufIndex >= isMeaningfulArray.Length");
                        }
                        
                        var job = new ShapeAnalysisJob
                        {
                            isMeaningfulArray = isMeaningfulArray,
                            resultIndex = bufIndex,
                            deltaVertices = deltaVertices,
                            deltaNormals = deltaNormals,
                            deltaTangents = deltaTangents,
                            useIndices = useIndices,
                        }.Schedule(deltaVertices.Length, 64);
                        completionHandle = JobHandle.CombineDependencies(job, completionHandle);
                    }
                    
                    meaningfulIndex.Add((shapeName, completionHandle, bufIndex));
                }
            }
            Profiler.BeginSample("Await completion");
            foreach (var (_, handle, _) in meaningfulIndex)
            {
                handle.Complete();
            }
            foreach (var (shapeName, _, bufIndex) in meaningfulIndex)
            {
                if (isMeaningfulArray[bufIndex])
                    meaninglessBlendShapes.Remove(shapeName);
            }
            Profiler.EndSample();
            
            Profiler.EndSample();

            foreach (var meaninglessBlendShape in meaninglessBlendShapes)
                context.RecordRemoveProperty(Target, $"blendShape.{meaninglessBlendShape}");

            var freezeBlendShape = Target.GetComponent<FreezeBlendShape>();
            var set = freezeBlendShape.shapeKeysSet.GetAsSet();
            set.UnionWith(meaninglessBlendShapes);
            freezeBlendShape.shapeKeysSet.SetValueNonPrefab(set);
        }
        
        [BurstCompile]
        struct ShapeAnalysisJob : IJobParallelFor
        {
            // We will issue a separate job for each blend shape frame, and want them to run in parallel. However,
            // normally unity will prevent us from using the same NativeArray in multiple such jobs. Since we're
            // writing only, and the order in which the writes occurs doesn't matter, _and_ this is an atomic primitive
            // type, we can get away with disabling this safety check.
            [NativeDisableContainerSafetyRestriction]
            [WriteOnly]
            public NativeArray<bool> isMeaningfulArray;

            public int resultIndex;
            
            [ReadOnly] public NativeArray<Vector3> deltaVertices;
            [ReadOnly] public NativeArray<Vector3> deltaNormals;
            [ReadOnly] public NativeArray<Vector3> deltaTangents;
            [ReadOnly] public NativeArray<bool> useIndices;

            public void Execute(int vertIndex)
            {
                if (!useIndices[vertIndex]) return;
                
                if (deltaVertices[vertIndex] != Vector3.zero || deltaNormals[vertIndex] != Vector3.zero || deltaTangents[vertIndex] != Vector3.zero)
                {
                    isMeaningfulArray[resultIndex] = true;
                }
            }
        }

        // nothing to do
        public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => upstream;
    }
}
