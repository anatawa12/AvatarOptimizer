using System.Collections;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;
using UnityEngine.Profiling;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    internal class RenameBlendShapeProcessor : EditSkinnedMeshProcessor<RenameBlendShape>
    {
        public RenameBlendShapeProcessor(RenameBlendShape component) : base(component)
        {
        }

        public override EditSkinnedMeshProcessorOrder ProcessOrder => EditSkinnedMeshProcessorOrder.AfterFreezeBlendShape;

        public override void Process(BuildContext context, MeshInfo2 target)
        {
            // TODO
        }

        public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => new MeshInfoComputer(this, upstream);

        class MeshInfoComputer : AbstractMeshInfoComputer
        {
            private readonly RenameBlendShapeProcessor _processor;

            public MeshInfoComputer(RenameBlendShapeProcessor processor, IMeshInfoComputer upstream) : base(upstream)
                => _processor = processor;

            public override (string, float)[] BlendShapes()
            {
                var mapping = _processor.Component.nameMap.GetAsMap();
                var blendShapes = base.BlendShapes();
                var removed = new BitArray(blendShapes.Length);
                var removeCount = 0;

                for (var i = 0; i < blendShapes.Length; i++)
                {
                    if (!mapping.TryGetValue(blendShapes[i].Item1, out var newName) || newName == null)
                        newName = blendShapes[i].Item1;

                    // find existing
                    for (var j = 0; j < i; j++)
                    {
                        if (removed[j]) continue;
                        if (blendShapes[j].Item1 == newName)
                        {
                            // merge
                            removed[i] = true;
                            removeCount++;
                            goto finishMap;
                        }
                    }

                    // there is no existing; rename
                    blendShapes[i].Item1 = newName;

                    finishMap:;
                }

                if (removeCount == 0) return blendShapes;

                var newBlendShapes = new (string, float)[blendShapes.Length - removeCount];

                for (int i = 0, j = 0; i < blendShapes.Length; i++)
                {
                    if (removed[i]) continue;
                    newBlendShapes[j++] = blendShapes[i];
                }

                return newBlendShapes;
            }
        }
    }
}
