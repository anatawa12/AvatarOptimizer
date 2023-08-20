using System.Collections.Generic;
using System.Linq;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    internal class RemoveMeshByBlendShapeProcessor : EditSkinnedMeshProcessor<RemoveMeshByBlendShape>
    {
        public RemoveMeshByBlendShapeProcessor(RemoveMeshByBlendShape component) : base(component)
        {
        }

        // This needs to be less than FreezeBlendshapeProcessor.ProcessOrder.
        public override EditSkinnedMeshProcessorOrder ProcessOrder => EditSkinnedMeshProcessorOrder.RemovingMesh;

        public override void Process(OptimizerSession session, MeshInfo2 target, MeshInfo2Holder meshInfo2Holder)
        {
            var byBlendShapeVertices = new HashSet<Vertex>();
            var sqrTolerance = Component.tolerance * Component.tolerance;

            foreach (var vertex in target.Vertices)
            foreach (var shapeName in Component.RemovingShapeKeys)
            {
                if (!vertex.BlendShapes.TryGetValue(shapeName, out var value)) continue;
                if (value.position.sqrMagnitude > sqrTolerance)
                    byBlendShapeVertices.Add(vertex);
            }

            foreach (var subMesh in target.SubMeshes)
            {
                int srcI = 0, dstI = 0;
                for (; srcI < subMesh.Triangles.Count; srcI += 3)
                {
                    // process 3 vertex in sub mesh at once to process one polygon
                    var v0 = subMesh.Triangles[srcI + 0];
                    var v1 = subMesh.Triangles[srcI + 1];
                    var v2 = subMesh.Triangles[srcI + 2];

                    if (byBlendShapeVertices.Contains(v0) || byBlendShapeVertices.Contains(v1) || byBlendShapeVertices.Contains(v2))
                        continue;

                    // no vertex is affected by the blend shape: 
                    subMesh.Triangles[dstI + 0] = v0;
                    subMesh.Triangles[dstI + 1] = v1;
                    subMesh.Triangles[dstI + 2] = v2;
                    dstI += 3;
                }
                subMesh.Triangles.RemoveRange(dstI, subMesh.Triangles.Count - dstI);
            }

            // remove unused vertices
            target.Vertices.RemoveAll(x => byBlendShapeVertices.Contains(x));

            // remove the blend shapes
            FreezeBlendShapeProcessor.FreezeBlendShapes(Target, session, target, Component.RemovingShapeKeys);
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
