using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

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

            foreach (var (shapeBuffer, useIndices) in buffers)
            {
                foreach (var (shapeName, shapeShape) in shapeBuffer.Shapes)
                {
                    if (!meaninglessBlendShapes.Contains(shapeName)) continue;

                    var meaningfull = false;

                    foreach (var bufferIndex in shapeShape.FramesBufferIndices)
                    {
                        var deltaVertices = shapeBuffer.DeltaVertices[bufferIndex];
                        var deltaNormals = shapeBuffer.DeltaNormals[bufferIndex];
                        var deltaTangents = shapeBuffer.DeltaTangents[bufferIndex];

                        for (var i = 0; i < deltaVertices.Length; i++)
                        {
                            if (!useIndices[i]) continue;
                            if (deltaVertices[i] != Vector3.zero || deltaNormals[i] != Vector3.zero || deltaTangents[i] != Vector3.zero)
                            {
                                meaningfull = true;
                                break;
                            }
                        }
                    }

                    if (meaningfull)
                        meaninglessBlendShapes.Remove(shapeName);
                }
            }

            foreach (var meaninglessBlendShape in meaninglessBlendShapes)
                context.RecordRemoveProperty(Target, $"blendShape.{meaninglessBlendShape}");

            var freezeBlendShape = Target.GetComponent<FreezeBlendShape>();
            var set = freezeBlendShape.shapeKeysSet.GetAsSet();
            set.UnionWith(meaninglessBlendShapes);
            freezeBlendShape.shapeKeysSet.SetValueNonPrefab(set);
        }

        // nothing to do
        public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => upstream;
    }
}
