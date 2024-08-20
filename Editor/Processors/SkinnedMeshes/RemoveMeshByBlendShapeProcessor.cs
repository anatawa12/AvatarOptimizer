#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    internal class RemoveMeshByBlendShapeProcessor : EditSkinnedMeshProcessor<RemoveMeshByBlendShape>
    {
        public RemoveMeshByBlendShapeProcessor(RemoveMeshByBlendShape component) : base(component)
        {
        }

        // This needs to be less than FreezeBlendshapeProcessor.ProcessOrder.
        public override EditSkinnedMeshProcessorOrder ProcessOrder => EditSkinnedMeshProcessorOrder.RemovingMesh;

        public override void Process(BuildContext context, MeshInfo2 target)
        {
            var byBlendShapeVertices = new HashSet<Vertex>();
            var sqrTolerance = Component.tolerance * Component.tolerance;

            foreach (var vertex in target.Vertices)
            foreach (var shapeName in Component.RemovingShapeKeys)
            {
                if (!vertex.BlendShapes.TryGetValue(shapeName, out var value)) continue;
                if (value.Any(f => f.Position.sqrMagnitude > sqrTolerance))
                    byBlendShapeVertices.Add(vertex);
            }

            Func<Vertex[], bool> condition = primitive => primitive.Any(byBlendShapeVertices.Contains);
            foreach (var subMesh in target.SubMeshes)
                subMesh.RemovePrimitives("RemoveMeshByBlendShape", condition);

            // remove unused vertices
            target.Vertices.RemoveAll(x => byBlendShapeVertices.Contains(x));

            // remove the BlendShapes
            FreezeBlendShapeProcessor.FreezeBlendShapes(Target, context, target, Component.RemovingShapeKeys);
        }

        public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => new MeshInfoComputer(this, upstream);

        class MeshInfoComputer : AbstractMeshInfoComputer
        {
            private readonly RemoveMeshByBlendShapeProcessor _processor;

            public MeshInfoComputer(RemoveMeshByBlendShapeProcessor processor, IMeshInfoComputer upstream) : base(upstream)
                => _processor = processor;

            public override (string, float)[] BlendShapes()
            {
                var set = _processor.Component.RemovingShapeKeys;
                return base.BlendShapes().Where(x => !set.Contains(x.name)).ToArray();
            }
        }
    }
}
