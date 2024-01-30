using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class RemoveZeroSizedPolygonProcessor : Pass<RemoveZeroSizedPolygonProcessor>
    {
        public override string DisplayName => "RemoveZeroSizedPolygonProcessor";

        protected override void Execute(BuildContext context)
        {
            foreach (var removeZeroSizedPolygon in context.GetComponents<RemoveZeroSizedPolygon>())
            {
                var mesh = removeZeroSizedPolygon.GetComponent<SkinnedMeshRenderer>();
                if (!mesh) continue;
                Process(context.GetMeshInfoFor(mesh), removeZeroSizedPolygon);
                DestroyTracker.DestroyImmediate(removeZeroSizedPolygon);
            }
        }

        private static void Process(MeshInfo2 meshInfo2, RemoveZeroSizedPolygon _)
        {
            foreach (var subMesh in meshInfo2.SubMeshes)
            {
                var dstI = 0;
                for (var srcI = 0; srcI < subMesh.Triangles.Count; srcI += 3)
                {
                    if (!IsPolygonEmpty(subMesh.Triangles[srcI], subMesh.Triangles[srcI + 1], subMesh.Triangles[srcI + 2]))
                    {
                        subMesh.Triangles[dstI] = subMesh.Triangles[srcI];
                        subMesh.Triangles[dstI + 1] = subMesh.Triangles[srcI + 1];
                        subMesh.Triangles[dstI + 2] = subMesh.Triangles[srcI + 2];
                        dstI += 3;
                    }
                }

                subMesh.Triangles.RemoveRange(dstI, subMesh.Triangles.Count - dstI);
            }
        }

        private static bool IsPolygonEmpty(Vertex a, Vertex b, Vertex c)
        {
            // BlendShapes are hard to check so disallow it
            // TODO: check BlendShape delta is same
            if (a.BlendShapes.Count != 0) return false;
            if (b.BlendShapes.Count != 0) return false;
            if (c.BlendShapes.Count != 0) return false;

            // check three points are at same position
            // TODO: should we use cross product instead?
            if (a.Position != b.Position) return false;
            if (a.Position != c.Position) return false;

            // check bone and bone weights are same
            var aWeights = new HashSet<(Bone bone, float weight)>(a.BoneWeights);
            if (!aWeights.SetEquals(b.BoneWeights)) return false;
            if (!aWeights.SetEquals(c.BoneWeights)) return false;
            return true;
        }
    }
}