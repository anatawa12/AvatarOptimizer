using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    internal class RemoveMeshInBoxProcessor : EditSkinnedMeshProcessor<RemoveMeshInBox>
    {
        public RemoveMeshInBoxProcessor(RemoveMeshInBox component) : base(component)
        {
        }

        public override int ProcessOrder => -10000;

        public override void Process(OptimizerSession session, MeshInfo2 target, MeshInfo2Holder meshInfo2Holder)
        {
            var inBoxVertices = new HashSet<Vertex>();
            // Vertex.AdditionalTemporal: 0 if in box, 1 if out of box
            foreach (var vertex in target.Vertices)
            {
                var actualPosition = vertex.ComputeActualPosition(target, Target.transform.worldToLocalMatrix);
                if (Component.boxes.Any(x => x.ContainsVertex(actualPosition)))
                    inBoxVertices.Add(vertex);
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

                    if (inBoxVertices.Contains(v0) && inBoxVertices.Contains(v1) && inBoxVertices.Contains(v2))
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

            inBoxVertices.Clear();
            var usingVertices = inBoxVertices;
            foreach (var subMesh in target.SubMeshes)
            foreach (var vertex in subMesh.Triangles)
                usingVertices.Add(vertex);

            // remove unused vertices
            target.Vertices.RemoveAll(x => !usingVertices.Contains(x));
        }

        public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => upstream;
    }
}
