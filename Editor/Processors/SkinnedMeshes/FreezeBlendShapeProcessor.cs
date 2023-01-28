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

        public override void Process(OptimizerSession session, MeshInfo2 target, MeshInfo2Holder meshInfo2Holder)
        {
            var freezeNames = new HashSet<string>(Component.shapeKeys);
            var freezes = new BitArray(target.BlendShapes.Count);
            for (var i = 0; i < target.BlendShapes.Count; i++)
                freezes[i] = freezeNames.Contains(target.BlendShapes[i].name);

            foreach (var vertex in target.Vertices)
            {
                for (var i = 0; i < target.BlendShapes.Count; i++)
                {
                    if (!freezes[i]) continue;
                    var (name, weight) = target.BlendShapes[i];
                    if (!vertex.BlendShapes.TryGetValue(name, out var value)) continue;

                    vertex.Position += value.position * weight;
                    vertex.Normal += value.normal * weight;
                    var tangent = (Vector3)vertex.Tangent + value.tangent * weight;
                    vertex.Tangent = new Vector4(tangent.x, tangent.y, tangent.z, vertex.Tangent.w);
                    vertex.BlendShapes.Remove(name);
                }
            }

            {
                int srcI = 0, dstI = 0;
                for (; srcI < target.BlendShapes.Count; srcI++)
                    if (!freezes[srcI])
                        target.BlendShapes[dstI++] = target.BlendShapes[srcI];

                target.BlendShapes.RemoveRange(dstI, target.BlendShapes.Count - dstI);
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
