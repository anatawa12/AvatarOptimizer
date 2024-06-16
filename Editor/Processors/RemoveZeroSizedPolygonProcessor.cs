using System.Collections.Generic;
using System.Linq;
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
                subMesh.RemovePrimitives("RemoveZeroSizedPolygon", poly =>
                {
                    if (poly.Any(v => v.BlendShapes.Count != 0)) return false;
                    var first = poly[0];
                    var firstWeights = new HashSet<(Bone bone, float weight)>(first.BoneWeights);
                    return poly.Skip(1).All(v => first.Position == v.Position && firstWeights.SetEquals(v.BoneWeights));
                });
            }
        }
    }
}
