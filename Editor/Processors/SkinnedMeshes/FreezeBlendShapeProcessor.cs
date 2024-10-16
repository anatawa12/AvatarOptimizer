using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using nadena.dev.ndmf;
using UnityEngine;
using UnityEngine.Profiling;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    internal class FreezeBlendShapeProcessor : EditSkinnedMeshProcessor<FreezeBlendShape>
    {
        public FreezeBlendShapeProcessor(FreezeBlendShape component) : base(component)
        {
        }

        public override EditSkinnedMeshProcessorOrder ProcessOrder => EditSkinnedMeshProcessorOrder.AfterRemoveMesh;

        public override void Process(BuildContext context, MeshInfo2 target)
        {
            FreezeBlendShapes(Target, context, target, Component.FreezingShapeKeys, true);
        }

        public static void FreezeBlendShapes(
            SkinnedMeshRenderer targetSMR,
            BuildContext context,
            MeshInfo2 target,
            HashSet<string> freezeNames,
            bool withWarning = false
        )
        {
            // Warn for blendShape animation
            if (withWarning) {
                var modified = new HashSet<string>();
                var sources = new HashSet<object>();
                var animationComponent = context.GetAnimationComponent(targetSMR);

                foreach (var blendShape in freezeNames)
                {
                    // do not create warning for constant animation
                    var propModNode = animationComponent.GetFloatNode($"blendShape.{blendShape}");
                    if (propModNode.ApplyState != ApplyState.Never)
                    {
                        var weight = target.BlendShapes.Find(r => r.name == blendShape);
                        if (weight.name == null) continue; // no such blendShape 
                        var values = propModNode.Value.PossibleValues;
                        if (values != null)
                        {
                            // animated to constant.
                            // we think the constant is the original constant value.
                            // we assume user want to override it.
                            if (values.Length == 1) continue;
                            // animated to two constant and one is current.
                            // we think the one is created during creating the new animation and
                            // the other is the original constant value.
                            // and we assume user want to override it.
                            if (values.Length == 2 && values.Contains(weight.weight)) continue;
                        } 

                        modified.Add(blendShape);
                        sources.Add(propModNode.ContextReferences);
                    }
                }

                if (modified.Count != 0)
                {
                    // ReSharper disable once CoVariantArrayConversion
                    BuildLog.LogWarning("FreezeBlendShape:warning:animation", string.Join(", ", modified),
                            targetSMR, sources);
                }
            }

            Profiler.BeginSample("DoFreezeBlendShape New");
            Profiler.BeginSample("CollectVerticesByBuffer");
            var vertexByBuffer = target.Vertices.GroupBy(x => x.BlendShapeBuffer)
                .Select(x => (buffer: x.Key, vertices: x.ToArray()))
                .ToList();
            Profiler.EndSample();

            var freezingShapes = target.BlendShapes.Where((shape, _) => freezeNames.Contains(shape.name)).ToArray();

            foreach (var (buffer, vertices) in vertexByBuffer)
            {
                Profiler.BeginSample("CollectFrames");
                var frames = freezingShapes
                    .SelectMany(shape => buffer.GetApplyFramesInfo(shape.name, shape.weight))
                    .ToList();
                Profiler.EndSample();

                Profiler.BeginSample("ApplyFramesForEachVertex");
                foreach (var vertex in vertices)
                {
                    Profiler.BeginSample("ProcessVertex");
                    var position = vertex.Position;
                    var normal = vertex.Normal;
                    var tangent = (Vector3)vertex.Tangent;
                    foreach (var frame in frames)
                    {
                        Profiler.BeginSample("ApplyFrame");
                        position += buffer.DeltaVertices[frame.FrameIndex][vertex.BlendShapeBufferVertexIndex] * frame.ApplyWeight;
                        normal += buffer.DeltaNormals[frame.FrameIndex][vertex.BlendShapeBufferVertexIndex] * frame.ApplyWeight;
                        tangent += buffer.DeltaTangents[frame.FrameIndex][vertex.BlendShapeBufferVertexIndex] * frame.ApplyWeight;
                        Profiler.EndSample();
                    }
                    
                    vertex.Position = position;
                    vertex.Normal = normal;
                    vertex.Tangent = new Vector4(tangent.x, tangent.y, tangent.z, vertex.Tangent.w);
                    Profiler.EndSample();
                }
                Profiler.EndSample();

                foreach (var (name, _) in freezingShapes)
                    buffer.RemoveBlendShape(name);
            }
            Profiler.EndSample();

            Profiler.BeginSample("MoveProperties");
            {
                int srcI = 0, dstI = 0;
                for (; srcI < target.BlendShapes.Count; srcI++)
                {
                    if (!freezeNames.Contains(target.BlendShapes[srcI].name))
                    {
                        // for keep prop: move the BlendShape index. name is not changed.
                        context.RecordMoveProperty(targetSMR, VProp.BlendShapeIndex(srcI), VProp.BlendShapeIndex(dstI));
                        target.BlendShapes[dstI++] = target.BlendShapes[srcI];
                    }
                    else
                    {
                        // for frozen prop: remove that BlendShape
                        context.RecordRemoveProperty(targetSMR, VProp.BlendShapeIndex(srcI));
                        context.RecordRemoveProperty(targetSMR, $"blendShape.{target.BlendShapes[srcI].name}");
                    }
                }

                target.BlendShapes.RemoveRange(dstI, target.BlendShapes.Count - dstI);
            }
            Profiler.EndSample();
        }

        public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => new MeshInfoComputer(this, upstream);

        class MeshInfoComputer : AbstractMeshInfoComputer
        {
            private readonly FreezeBlendShapeProcessor _processor;

            public MeshInfoComputer(FreezeBlendShapeProcessor processor, IMeshInfoComputer upstream) : base(upstream)
                => _processor = processor;

            public override (string, float)[] BlendShapes()
            {
                var set = _processor.Component.FreezingShapeKeys;
                return base.BlendShapes().Where(x => !set.Contains(x.name)).ToArray();
            }
        }
    }
}
