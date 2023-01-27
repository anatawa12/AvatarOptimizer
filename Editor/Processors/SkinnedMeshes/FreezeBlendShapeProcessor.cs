using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    internal class FreezeBlendShapeProcessor : EditSkinnedMeshProcessor<FreezeBlendShape>
    {
        public FreezeBlendShapeProcessor(FreezeBlendShape component) : base(component)
        {
        }

        public override int ProcessOrder => -10000;

        public override void Process(OptimizerSession session) => ProcessWithNew(session);

        public override void Process(OptimizerSession session, MeshInfo2 target)
        {
            var freezeNames = new HashSet<string>(Component.shapeKeys);
            var freezes = new BitArray(target.BlendShapes.Length);
            for (var i = 0; i < target.BlendShapes.Length; i++)
                freezes[i] = freezeNames.Contains(target.BlendShapes[i].name);

            foreach (var vertex in target.Vertices)
            {
                int srcI = 0, dstI = 0;
                for (; srcI < vertex.BlendShapes.Length; srcI++)
                {
                    if (freezes[srcI])
                    {
                        vertex.Position += vertex.BlendShapes[srcI].position * target.BlendShapes[srcI].weight;
                        vertex.Normal += vertex.BlendShapes[srcI].normal * target.BlendShapes[srcI].weight;
                        var tangent = (Vector3)vertex.Tangent + vertex.BlendShapes[srcI].tangent * target.BlendShapes[srcI].weight;
                        vertex.Tangent = new Vector4(tangent.x, tangent.y, tangent.z, vertex.Tangent.w);
                    }
                    else
                    {
                        vertex.BlendShapes[dstI++] = vertex.BlendShapes[srcI];
                    }
                }

                vertex.BlendShapes = vertex.BlendShapes.AsSpan().Slice(0, dstI).ToArray();
            }

            {
                int srcI = 0, dstI = 0;
                for (; srcI < target.BlendShapes.Length; srcI++)
                {
                    if (!freezes[srcI])
                    {
                        target.BlendShapes[dstI++] = target.BlendShapes[srcI];
                    }
                }
                target.BlendShapes = target.BlendShapes.AsSpan().Slice(0, dstI).ToArray();
            }
        }

        public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => new MeshInfoComputer(this, upstream);

        class MeshInfoComputer : AbstractMeshInfoComputer
        {
            private readonly FreezeBlendShapeProcessor _processor;

            public MeshInfoComputer(FreezeBlendShapeProcessor processor, IMeshInfoComputer upstream) : base(upstream)
                => _processor = processor;

            public override string[] BlendShapes() =>
                base.BlendShapes().Where(x => !_processor.Component.shapeKeys.Contains(x)).ToArray();
        }
    }
}
