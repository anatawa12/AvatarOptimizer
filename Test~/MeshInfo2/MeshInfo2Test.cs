using System;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace Anatawa12.AvatarOptimizer.Test
{
    public class MeshInfo2Test
    {
        [TestCase("single-negative", -200, -20)]
        [TestCase("single-negative", -100, -10)]
        [TestCase("single-negative", -25, -2.5f)]
        [TestCase("single-negative", 0, 0)]
        [TestCase("single-negative", 25, 2.5f)]
        [TestCase("single-negative", 100, 10)]
        [TestCase("single-negative", 200, 20)]

        [TestCase("single-positive", -200, -20)]
        [TestCase("single-positive", -100, -10)]
        [TestCase("single-positive", -25, -2.5f)]
        [TestCase("single-positive", 0, 0)]
        [TestCase("single-positive", 25, 2.5f)]
        [TestCase("single-positive", 100, 10)]
        [TestCase("single-positive", 200, 20)]

        [TestCase("two-positive-frame", -100, -10)]
        [TestCase("two-positive-frame", -25, -2.5f)]
        [TestCase("two-positive-frame", 0, 0)]
        [TestCase("two-positive-frame", 25, 2.5f)]
        [TestCase("two-positive-frame", 100, 10)]
        [TestCase("two-positive-frame", 150, 55)]
        [TestCase("two-positive-frame", 200, 100)]
        [TestCase("two-positive-frame", 250, 145)]

        [TestCase("two-negative-frame", -250, -145)]
        [TestCase("two-negative-frame", -200, -100)]
        [TestCase("two-negative-frame", -150, -55)]
        [TestCase("two-negative-frame", -100, -10)]
        [TestCase("two-negative-frame", -25, -2.5f)]
        [TestCase("two-negative-frame", 0, 0)]
        [TestCase("two-negative-frame", 25, 2.5f)]
        [TestCase("two-negative-frame", 100, 10)]

        [TestCase("two-0-50-frame", -25, -2)]
        [TestCase("two-0-50-frame", 0, 0)]
        [TestCase("two-0-50-frame", 25, 0)]
        [TestCase("two-0-50-frame", 50, 1)]
        [TestCase("two-0-50-frame", 75, 2)]

        [TestCase("single-0", -100, float.PositiveInfinity)]
        [TestCase("single-0", -1, float.PositiveInfinity)]
        [TestCase("single-0", 0, 0)]
        [TestCase("single-0", 1, float.NegativeInfinity)]
        [TestCase("single-0", 100, float.NegativeInfinity)]        

        public void BlendShapeLerp(string name, float weight, float offset)
        {
            var mesh = TestUtils.GetAssetAt<Mesh>($"MeshInfo2/{name}.asset");
            var go = new GameObject();
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh;
            var meshInfo2 = new MeshInfo2(smr);

            var vertex = meshInfo2.Vertices.First(x => x.Position == new Vector3(+1, +1, +1));

            vertex.TryGetBlendShape("test0", weight, out var position, out _, out _);

            Assert.That(position.x, Is.EqualTo(offset));
        }
        
        [TestCase("single-negative")]
        [TestCase("single-positive")]
        [TestCase("single-0")]
        [TestCase("two-positive-frame")]
        [TestCase("two-negative-frame")]
        [TestCase("two-0-50-frame")]
        public void ParseAndEmit(string name)
        {
            var mesh = TestUtils.GetAssetAt<Mesh>($"MeshInfo2/{name}.asset");
            var go = new GameObject();
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh;
            var meshInfo2 = new MeshInfo2(smr);

            var newMesh = new Mesh();
            meshInfo2.WriteToMesh(newMesh);
        }

        [Test]
        public void RootBoneWithNoneMeshSkinnedMeshRenderer()
        {
            var go = new GameObject();
            var secondGo = new GameObject();
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.rootBone = secondGo.transform;
            var meshInfo2 = new MeshInfo2(smr);
            Assert.That(meshInfo2.RootBone, Is.EqualTo(secondGo.transform));
        }

        [Test]
        public void MultiFrameBlendShapeWithPartiallyIdentity()
        {
            var mesh = TestUtils.NewCubeMesh();
            var deltas = new Vector3[8];
            deltas.AsSpan().Fill(new Vector3(1, 2, 3));
            mesh.AddBlendShapeFrame("shape", 0, new Vector3[8], null, null);
            mesh.AddBlendShapeFrame("shape", 1, new Vector3[8], null, null);
            mesh.AddBlendShapeFrame("shape", 2, new Vector3[8], null, null);
            mesh.AddBlendShapeFrame("shape", 3, deltas, null, null);
            mesh.AddBlendShapeFrame("shape", 4, new Vector3[8], null, null);

            var go = new GameObject();
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh;

            var meshInfo2 = new MeshInfo2(smr);

            foreach (var vertex in meshInfo2.Vertices)
            {
                var buffer = vertex.BlendShapeBuffer;
                var shapeShape = buffer.Shapes["shape"];
                Assert.That(shapeShape.Frames.Length, Is.EqualTo(5));
                for (var i = 0; i < shapeShape.Frames.Length; i++)
                {
                    var frameInfo = shapeShape.Frames[i];
                    Assert.That(frameInfo.Weight, Is.EqualTo((float)i));
                    var position = buffer.DeltaVertices[frameInfo.BufferIndex][vertex.BlendShapeBufferVertexIndex];
                    Assert.That(position, Is.EqualTo(i == 3 ? new Vector3(1, 2, 3) : new Vector3()));
                }
            }
        }

        [Test]
        public void BlendShapeWithFrameAtZero()
        {
            var mesh = TestUtils.NewCubeMesh();
            var deltas = new Vector3[8];
            deltas.AsSpan().Fill(new Vector3(1, 2, 3));
            mesh.AddBlendShapeFrame("shape", 0, deltas, null, null);
            mesh.AddBlendShapeFrame("shape", 1, deltas, null, null);

            var go = new GameObject();
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh;

            var meshInfo2 = new MeshInfo2(smr);

            Vector3 position;
            var vertex = meshInfo2.Vertices[0];
            Assert.That(vertex.TryGetBlendShape("shape", 0, out position, out _, out _), Is.False);
            Assert.That(position, Is.EqualTo(new Vector3(0, 0, 0)));

            Assert.That(vertex.TryGetBlendShape("shape", 0, out position, out _, out _, getDefined: true), Is.True);
            Assert.That(position, Is.EqualTo(new Vector3(1, 2, 3)));
        }

        [Test]
        public void WriteEmptySubMesh()
        {
            var mesh = TestUtils.NewCubeMesh();

            var go = new GameObject();
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh;

            var meshInfo2 = new MeshInfo2(smr);

            meshInfo2.SubMeshes[0].Vertices.Clear();
            meshInfo2.VerticesMutable.Clear();

            var newMesh = new Mesh();
            meshInfo2.WriteToMesh(newMesh);

            Assert.That(newMesh.subMeshCount, Is.EqualTo(1));
        }

        [Test]
        public void ComputeActualPositionWithoutBones()
        {
            var mesh = TestUtils.NewCubeMesh();

            var go = new GameObject();
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh;

            var meshInfo2 = new MeshInfo2(smr);

            foreach (var vertex in meshInfo2.Vertices)
            {
                var position = vertex.ComputeActualPosition(meshInfo2,
                    t => t.localToWorldMatrix, go.transform.worldToLocalMatrix);

                Assert.That(position, Is.EqualTo(vertex.Position));
            }
        }

        [Test]
        public void ComputeActualPositionWithBones()
        {
            var mesh = TestUtils.NewCubeMesh();

            var go = new GameObject();
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh;

            var meshInfo2 = new MeshInfo2(smr);
            meshInfo2.MakeBoned();

            foreach (var vertex in meshInfo2.Vertices)
            {
                var position = vertex.ComputeActualPosition(meshInfo2,
                    t => t.localToWorldMatrix, go.transform.worldToLocalMatrix);

                Assert.That(position, Is.EqualTo(vertex.Position));
            }
        }
    }
}
