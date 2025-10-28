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
                if (!removeZeroSizedPolygon.TryGetComponent<SkinnedMeshRenderer>(out var mesh)) continue;
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
                    // if any vertex has blend shape with non-zero delta, it's not zero-sized.
                    if (poly.Any(v =>
                        {
                            var buffer = v.BlendShapeBuffer;
                            return buffer.Shapes.Any(shape => shape.Value.FramesBufferIndices.Any(
                                bufferIndex =>
                                    buffer.DeltaVertices[bufferIndex][v.BlendShapeBufferVertexIndex] != Vector3.zero
                                    || buffer.DeltaNormals[bufferIndex][v.BlendShapeBufferVertexIndex] != Vector3.zero
                                    || buffer.DeltaNormals[bufferIndex][v.BlendShapeBufferVertexIndex] !=
                                    Vector3.zero));
                        }))
                        return false;
                    var first = poly[0];
                    var firstWeights = new HashSet<(Bone bone, float weight)>(first.BoneWeights);
                    return poly.Skip(1).All(v => first.Position.Equals(v.Position) && firstWeights.SetEquals(v.BoneWeights));
                });
            }
        }
    }
}
