#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    internal class RemoveMeshInBoxProcessor : EditSkinnedMeshProcessor<RemoveMeshInBox>
    {
        public RemoveMeshInBoxProcessor(RemoveMeshInBox component) : base(component)
        {
        }

        public override EditSkinnedMeshProcessorOrder ProcessOrder => EditSkinnedMeshProcessorOrder.RemovingMesh;

        public override void Process(BuildContext context, MeshInfo2 target)
        {
            var inBoxVertices = new HashSet<Vertex>();
            var originalState = context.GetState<OriginalState>();
            // Vertex.AdditionalTemporal: 0 if in box, 1 if out of box
            foreach (var vertex in target.Vertices)
            {
                var actualPosition = vertex.ComputeActualPosition(target, originalState.GetOriginalLocalToWorld, Target.transform.worldToLocalMatrix);
                if (Component.boxes.Any(x => x.ContainsVertex(actualPosition)))
                    inBoxVertices.Add(vertex);
            }

            Func<Vertex[], bool> condition = primitive => primitive.All(inBoxVertices.Contains);
            foreach (var subMesh in target.SubMeshes)
                subMesh.RemovePrimitives("RemoveMeshInBox", condition);

            // We don't need to reset AdditionalTemporal because if out of box, it always be used.
            // Vertex.AdditionalTemporal: 0 if unused, 1 if used

            inBoxVertices.Clear();
            var usingVertices = inBoxVertices;
            foreach (var subMesh in target.SubMeshes)
            foreach (var vertex in subMesh.Vertices)
                usingVertices.Add(vertex);

            // remove unused vertices
            target.Vertices.RemoveAll(x => !usingVertices.Contains(x));
        }

        public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => upstream;
    }
}
