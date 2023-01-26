using System.Collections;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    internal class RemoveMeshInBoxProcessor : EditSkinnedMeshProcessor<RemoveMeshInBox>
    {
        public RemoveMeshInBoxProcessor(RemoveMeshInBox component) : base(component)
        {
        }

        public override int ProcessOrder => -10000;

        public override void Process(OptimizerSession session)
        {
            var srcMesh = new MeshInfo(Target);
            var vertices = srcMesh.vertices;
            var inBoxFlag = ComputeInBoxVertices(vertices);
            if (inBoxFlag.CountTrue() == 0) return;
            var (destTriangles, indexMapping) = SweepTrianglesInBox(inBoxFlag, srcMesh.Triangles);
            var usingVertices = CollectUsingVertices(vertices.Length, destTriangles);
            var usingVerticesCount = usingVertices.CountTrue();

            //var mesh = session.MayInstantiate(Target.sharedMesh);

            var destMesh = new MeshInfo(
                bounds: srcMesh.Bounds,
                trianglesCount: destTriangles.Length,
                vertexCount: usingVerticesCount,
                uvCount: srcMesh.uvCount,
                withColors: srcMesh.colors32 != null && srcMesh.colors32.Length == 0,
                subMeshCount: srcMesh.SubMeshes.Length,
                bonesCount: srcMesh.bones.Length,
                blendShapes: srcMesh.BlendShapes.Select(x => (x.name, x.Item2.weight)).ToArray()
            );

            // sweep vertices
            SweepUnusedVertices(usingVertices, destMesh.vertices, srcMesh.vertices);
            SweepUnusedVertices(usingVertices, destMesh.normals, srcMesh.normals);
            SweepUnusedVertices(usingVertices, destMesh.tangents, srcMesh.tangents);
            SweepUnusedVertices(usingVertices, destMesh.uv, srcMesh.uv);
            SweepUnusedVertices(usingVertices, destMesh.uv2, srcMesh.uv2);
            SweepUnusedVertices(usingVertices, destMesh.uv3, srcMesh.uv3);
            SweepUnusedVertices(usingVertices, destMesh.uv4, srcMesh.uv4);
            SweepUnusedVertices(usingVertices, destMesh.uv5, srcMesh.uv5);
            SweepUnusedVertices(usingVertices, destMesh.uv6, srcMesh.uv6);
            SweepUnusedVertices(usingVertices, destMesh.uv7, srcMesh.uv7);
            SweepUnusedVertices(usingVertices, destMesh.uv8, srcMesh.uv8);
            SweepUnusedVertices(usingVertices, destMesh.colors32, srcMesh.colors32);
            for (var i = 0; i < srcMesh.BlendShapes.Length; i++)
            {
                SweepUnusedVertices(usingVertices, destMesh.BlendShapes[i].Item2.vertices,
                    srcMesh.BlendShapes[i].Item2.vertices);
                SweepUnusedVertices(usingVertices, destMesh.BlendShapes[i].Item2.normals,
                    srcMesh.BlendShapes[i].Item2.normals);
                SweepUnusedVertices(usingVertices, destMesh.BlendShapes[i].Item2.tangents,
                    srcMesh.BlendShapes[i].Item2.tangents);
            }

            var vertexIndexMapping = CreateVertexIndexMapping(usingVertices);
            for (var i = 0; i < destTriangles.Length; i++)
                destMesh.Triangles[i] = vertexIndexMapping[destTriangles[i]];

            // Bone Weights
            //*
            var boneWeightsCount = srcMesh.BonesPerVertex.Where((_, i) => usingVertices[i]).Sum(x => x);
            destMesh.AllBoneWeights = new NativeArray<BoneWeight1>(boneWeightsCount, Allocator.Temp);
            for (int srcI = 0, dstI = 0, srcBoneWeightBase = 0, dstBoneWeightBase = 0;
                 srcI < srcMesh.BonesPerVertex.Length;
                 srcI++)
            {
                if (usingVertices[srcI])
                {
                    int bones = destMesh.BonesPerVertex[dstI] = srcMesh.BonesPerVertex[srcI];
                    srcMesh.AllBoneWeights.AsReadOnlySpan().Slice(srcBoneWeightBase, bones)
                        .CopyTo(destMesh.AllBoneWeights.AsSpan().Slice(dstBoneWeightBase, bones));
                    dstBoneWeightBase += bones;
                    dstI++;
                }

                srcBoneWeightBase += srcMesh.BonesPerVertex[srcI];
            }
            // */

            for (var i = 0; i < destMesh.SubMeshes.Length; i++)
            {
                var srcSubMesh = srcMesh.SubMeshes[i];
                Assert.AreEqual(MeshTopology.Triangles, srcSubMesh.topology);
                var indexStart = srcSubMesh.indexStart;
                while (indexMapping[indexStart] == -1)
                    indexStart++;
                var indexEnd = srcSubMesh.indexStart + srcSubMesh.indexCount;
                while (indexMapping[indexEnd] == -1 && indexStart < indexEnd)
                    indexEnd--;
                destMesh.SubMeshes[i] = new SubMeshDescriptor(indexMapping[indexStart],
                    indexMapping[indexEnd] - indexMapping[indexStart]);
            }

            srcMesh.bindposes.CopyTo(destMesh.bindposes, 0);

            var mesh = session.MayInstantiate(Target.sharedMesh);
            destMesh.WriteToMesh(mesh);
            Target.sharedMesh = mesh;
        }

        private BitArray ComputeInBoxVertices(Vector3[] vertices)
        {
            var inBox = new BitArray(vertices.Length);

            for (var i = 0; i < vertices.Length; i++)
                inBox[i] = Component.boxes.Any(x => x.ContainsVertex(vertices[i]));

            return inBox;
        }

        private (int[] newTriangles, int[] triangleIndexMapping) SweepTrianglesInBox(BitArray inBoxVertices,
            int[] triangles)
        {
            // process triangles
            // -1 means removed triangle
            var triangleMapping = new int[triangles.Length + 1];
            int srcI = 0, dstI = 0;
            for (; srcI < triangles.Length; srcI += 3)
            {
                var remove =
                    inBoxVertices[triangles[srcI + 0]]
                    && inBoxVertices[triangles[srcI + 1]]
                    && inBoxVertices[triangles[srcI + 2]];
                if (remove)
                {
                    triangleMapping[srcI + 0] = triangleMapping[srcI + 1] = triangleMapping[srcI + 2] = -1;
                }
                else
                {
                    triangleMapping[srcI + 0] = dstI + 0;
                    triangleMapping[srcI + 1] = dstI + 1;
                    triangleMapping[srcI + 2] = dstI + 2;
                    triangles[dstI + 0] = triangles[srcI + 0];
                    triangles[dstI + 1] = triangles[srcI + 1];
                    triangles[dstI + 2] = triangles[srcI + 2];
                    dstI += 3;
                }
            }

            triangleMapping[srcI] = dstI;

            return (triangles.Take(dstI).ToArray(), triangleMapping);
        }

        private BitArray CollectUsingVertices(int verticesCount, int[] triangles)
        {
            var usingVertices = new BitArray(verticesCount);
            foreach (var vertexIndex in triangles)
                usingVertices[vertexIndex] = true;
            return usingVertices;
        }

        private void SweepUnusedVertices<T>(BitArray usingVertices, T[] result, T[] vertexAttribute)
        {
            if (vertexAttribute == null || result == null) return;

            for (int srcI = 0, dstI = 0; srcI < vertexAttribute.Length; srcI++)
                if (usingVertices[srcI])
                    result[dstI++] = vertexAttribute[srcI];
        }

        private int[] CreateVertexIndexMapping(BitArray usingVertices)
        {
            var result = new int[usingVertices.Length];
            for (int srcI = 0, dstI = 0; srcI < result.Length; srcI++) 
                result[srcI] = usingVertices[srcI] ? dstI++ : -1;
            return result;
        }

        public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => new MeshInfoComputer(this, upstream);

        class MeshInfoComputer : AbstractMeshInfoComputer
        {
            private readonly RemoveMeshInBoxProcessor _processor;

            public MeshInfoComputer(RemoveMeshInBoxProcessor processor, IMeshInfoComputer upstream) : base(upstream)
                => _processor = processor;
        }
    }
}
