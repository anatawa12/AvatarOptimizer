using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    internal class MeshInfo
    {
        public Bounds Bounds;
        public readonly int[] Triangles;

        // ReSharper disable InconsistentNaming
        public readonly Vector3[] vertices;
        public readonly Vector3[] normals;
        public readonly Vector4[] tangents;
        public readonly Vector2[] uv;
        public readonly Vector2[] uv2;
        public readonly Vector2[] uv3;
        public readonly Vector2[] uv4;
        public readonly Vector2[] uv5;
        public readonly Vector2[] uv6;
        public readonly Vector2[] uv7;
        public readonly Vector2[] uv8;

        public readonly Color32[] colors32;

        // NativeSlice[vertexCount]?
        public NativeArray<byte> BonesPerVertex;
        public NativeArray<BoneWeight1> AllBoneWeights;
        public readonly SubMeshDescriptor[] SubMeshes;

        public readonly (string name, (Vector3[] vertices, Vector3[] normals, Vector3[] tangents, float weight))[]
            BlendShapes;

        public readonly Material[] SharedMaterials;
        public readonly Matrix4x4[] bindposes;

        public readonly Transform[] bones;
        // ReSharper restore InconsistentNaming

        public MeshInfo(
            Bounds bounds,
            int trianglesCount,
            int vertexCount,
            int uvCount,
            bool withColors,
            int subMeshCount,
            int bonesCount,
            (string name, float weight)[] blendShapes,
            Allocator allocator = Allocator.Temp)
        {
            Bounds = bounds;
            Triangles = new int[trianglesCount];
            vertices = new Vector3[vertexCount];
            normals = new Vector3[vertexCount];
            tangents = new Vector4[vertexCount];
            // @formatter:off
            switch (uvCount)
            {
                case 8: uv8 = new Vector2[vertexCount]; goto case 7;
                case 7: uv7 = new Vector2[vertexCount]; goto case 6;
                case 6: uv6 = new Vector2[vertexCount]; goto case 5;
                case 5: uv5 = new Vector2[vertexCount]; goto case 4;
                case 4: uv4 = new Vector2[vertexCount]; goto case 3;
                case 3: uv3 = new Vector2[vertexCount]; goto case 2;
                case 2: uv2 = new Vector2[vertexCount]; goto case 1;
                case 1: uv = new Vector2[vertexCount]; break;
            }
            // @formatter:on
            colors32 = withColors ? new Color32[vertexCount] : null;

            BonesPerVertex = new NativeArray<byte>(vertexCount, allocator);
            AllBoneWeights = new NativeArray<BoneWeight1>(0, allocator);

            BlendShapes = new (string, (Vector3[], Vector3[], Vector3[], float))[blendShapes.Length];
            for (var i = 0; i < blendShapes.Length; i++)
                BlendShapes[i] = (blendShapes[i].name,
                    (new Vector3[vertexCount], new Vector3[vertexCount], new Vector3[vertexCount],
                        blendShapes[i].weight));

            SubMeshes = new SubMeshDescriptor[subMeshCount];
            SharedMaterials = new Material[subMeshCount];

            bindposes = new Matrix4x4[bonesCount];
            Utils.FillArray(bindposes, Matrix4x4.identity);
            bones = new Transform[bonesCount];
        }

        public MeshInfo(SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh;
            Bounds = mesh.bounds;
            Triangles = mesh.triangles;
            vertices = mesh.vertices;
            normals = mesh.normals;
            tangents = mesh.tangents;
            uv = mesh.uv;
            uv2 = mesh.uv2;
            uv3 = mesh.uv3;
            uv4 = mesh.uv4;
            uv5 = mesh.uv5;
            uv6 = mesh.uv6;
            uv7 = mesh.uv7;
            uv8 = mesh.uv8;
            colors32 = mesh.colors32;

            BonesPerVertex = mesh.GetBonesPerVertex();
            AllBoneWeights = mesh.GetAllBoneWeights();

            BlendShapes = new (string, (Vector3[], Vector3[], Vector3[], float))[mesh.blendShapeCount];
            for (var i = 0; i < mesh.blendShapeCount; i++)
            {
                Assert.AreEqual(1, mesh.GetBlendShapeFrameCount(i));
                Assert.AreEqual(100.0f, mesh.GetBlendShapeFrameWeight(i, 0));
                var deltaVertices = new Vector3[vertices.Length];
                var deltaNormals = new Vector3[vertices.Length];
                var deltaTangents = new Vector3[vertices.Length];
                mesh.GetBlendShapeFrameVertices(i, 0, deltaVertices, deltaNormals, deltaTangents);
                var shapeName = mesh.GetBlendShapeName(i);
                var weight = renderer.GetBlendShapeWeight(i);
                BlendShapes[i] = (shapeName, (deltaVertices, deltaNormals, deltaTangents, weight));
            }

            SubMeshes = new SubMeshDescriptor[mesh.subMeshCount];
            for (var i = 0; i < SubMeshes.Length; i++)
                SubMeshes[i] = mesh.GetSubMesh(i);

            var sourceMaterials = renderer.sharedMaterials;
            SharedMaterials = new Material[mesh.subMeshCount];
            Array.Copy(sourceMaterials, SharedMaterials, Math.Min(sourceMaterials.Length, SharedMaterials.Length));

            bindposes = mesh.bindposes;

            var bones = renderer.bones;
            this.bones = new Transform[bindposes.Length];
            Array.Copy(bones, this.bones, Math.Min(bones.Length, this.bones.Length));
        }

        public MeshInfo(MeshRenderer renderer)
        {
            var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            Bounds = mesh.bounds;
            Triangles = mesh.triangles;
            vertices = mesh.vertices;
            normals = mesh.normals;
            tangents = mesh.tangents;
            uv = mesh.uv;
            uv2 = mesh.uv2;
            uv3 = mesh.uv3;
            uv4 = mesh.uv4;
            uv5 = mesh.uv5;
            uv6 = mesh.uv6;
            uv7 = mesh.uv7;
            uv8 = mesh.uv8;
            colors32 = mesh.colors32;
            BonesPerVertex = new NativeArray<byte>(vertices.Length, Allocator.Temp);
            AllBoneWeights = new NativeArray<BoneWeight1>(vertices.Length, Allocator.Temp);
            for (var i = 0; i < AllBoneWeights.Length; i++)
            {
                BonesPerVertex[i] = 1;
                AllBoneWeights[i] = new BoneWeight1 { weight = 1f, boneIndex = 0 };
            }

            BlendShapes = Array.Empty<(string, (Vector3[], Vector3[], Vector3[], float))>();

            SubMeshes = new SubMeshDescriptor[mesh.subMeshCount];
            for (var i = 0; i < SubMeshes.Length; i++)
                SubMeshes[i] = mesh.GetSubMesh(i);

            var sourceMaterials = renderer.sharedMaterials;
            SharedMaterials = new Material[mesh.subMeshCount];
            Array.Copy(sourceMaterials, SharedMaterials, Math.Min(sourceMaterials.Length, SharedMaterials.Length));

            bindposes = new[] { Matrix4x4.identity };
            bones = new[] { renderer.transform };
        }

        public void WriteToMesh(Mesh destMesh)
        {
            destMesh.vertices = vertices;
            destMesh.normals = normals;
            destMesh.tangents = tangents;
            destMesh.uv = uv;
            destMesh.uv2 = uv2;
            destMesh.uv3 = uv3;
            destMesh.uv4 = uv4;
            destMesh.uv5 = uv5;
            destMesh.uv6 = uv6;
            destMesh.uv7 = uv7;
            destMesh.uv8 = uv8;
            destMesh.colors32 = colors32;
            destMesh.triangles = Triangles;
            destMesh.bindposes = bindposes;
            destMesh.triangles = Triangles;
            destMesh.SetBoneWeights(BonesPerVertex, AllBoneWeights);

            destMesh.ClearBlendShapes();
            foreach (var (name, (vertice, normal, tangent, _)) in BlendShapes)
                destMesh.AddBlendShapeFrame(name, 100, vertice, normal, tangent);
            destMesh.subMeshCount = SubMeshes.Length;
            for (var i = 0; i < SubMeshes.Length; i++)
                destMesh.SetSubMesh(i, SubMeshes[i]);
        }
    }
}
