using System.Linq;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    internal class RemoveMeshInBoxProcessor : EditSkinnedMeshProcessor<RemoveMeshInBox>
    {
        public RemoveMeshInBoxProcessor(RemoveMeshInBox component) : base(component)
        {
        }

        public override int ProcessOrder => -10000;

        public override void Process(OptimizerSession session, MeshInfo2 target)
        {
            // Vertex.AdditionalTemporal: 0 if in box, 1 if out of box
            foreach (var vertex in target.Vertices)
                vertex.AdditionalTemporal =
                    Component.boxes.Any(x => x.ContainsVertex(vertex.Position)) ? 0 : 1;

            foreach (var subMesh in target.SubMeshes)
            {
                int srcI = 0, dstI = 0;
                for (; srcI < subMesh.Triangles.Count; srcI += 3)
                {
                    // process 3 vertex in sub mesh at once to process one polygon
                    var v0 = subMesh.Triangles[srcI + 0];
                    var v1 = subMesh.Triangles[srcI + 1];
                    var v2 = subMesh.Triangles[srcI + 2];

                    if (v0.AdditionalTemporal == 0 && v1.AdditionalTemporal == 0 && v2.AdditionalTemporal == 0)
                        continue;

                    // some vertex is not in box: 
                    subMesh.Triangles[dstI + 0] = v0;
                    subMesh.Triangles[dstI + 1] = v1;
                    subMesh.Triangles[dstI + 2] = v2;
                    dstI += 3;
                }
                subMesh.Triangles.RemoveRange(dstI, subMesh.Triangles.Count - dstI);
            }

            // We don't need to reset AdditionalTemporal because if out of box, it always be used.
            // Vertex.AdditionalTemporal: 0 if unused, 1 if used

            foreach (var subMesh in target.SubMeshes)
            foreach (var vertex in subMesh.Triangles)
                vertex.AdditionalTemporal = 1;

            // remove unused vertices
            target.Vertices.RemoveAll(x => x.AdditionalTemporal == 0);
        }

        public override void Process(OptimizerSession session) => ProcessWithNew(session);

        public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => new MeshInfoComputer(this, upstream);

        class MeshInfoComputer : AbstractMeshInfoComputer
        {
            private readonly RemoveMeshInBoxProcessor _processor;

            public MeshInfoComputer(RemoveMeshInBoxProcessor processor, IMeshInfoComputer upstream) : base(upstream)
                => _processor = processor;
        }
    }
}
