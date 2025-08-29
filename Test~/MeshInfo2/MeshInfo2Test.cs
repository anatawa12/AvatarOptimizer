using System;
using System.Linq;
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
            using var meshInfo2 = new MeshInfo2(smr);

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
            using var meshInfo2 = new MeshInfo2(smr);

            var newMesh = new Mesh();
            meshInfo2.WriteToMesh(newMesh, isSkinnedMesh: true);
        }

        [Test]
        public void RootBoneWithNoneMeshSkinnedMeshRenderer()
        {
            var go = new GameObject();
            var secondGo = new GameObject();
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.rootBone = secondGo.transform;
            using var meshInfo2 = new MeshInfo2(smr);
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

            using var meshInfo2 = new MeshInfo2(smr);

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

            using var meshInfo2 = new MeshInfo2(smr);

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

            using var meshInfo2 = new MeshInfo2(smr);

            meshInfo2.SubMeshes[0].Vertices.Clear();
            Utils.DisposeAll(meshInfo2.VerticesMutable);
            meshInfo2.VerticesMutable.Clear();

            var newMesh = new Mesh();
            meshInfo2.WriteToMesh(newMesh, isSkinnedMesh: true);

            Assert.That(newMesh.subMeshCount, Is.EqualTo(1));
        }

        [Test]
        public void ComputeActualPositionWithoutBones()
        {
            var mesh = TestUtils.NewCubeMesh();

            var go = new GameObject();
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh;

            using var meshInfo2 = new MeshInfo2(smr);

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

            using var meshInfo2 = new MeshInfo2(smr);
            meshInfo2.MakeBoned();

            foreach (var vertex in meshInfo2.Vertices)
            {
                var position = vertex.ComputeActualPosition(meshInfo2,
                    t => t.localToWorldMatrix, go.transform.worldToLocalMatrix);

                Assert.That(position, Is.EqualTo(vertex.Position));
            }
        }

        // test with binary-edited fbx which is originally exported from blender
        [Test]
        public void MultipleSameNameBlendShapeBlenderBinaryEdited()
        {
            var fbx = TestUtils.GetAssetAt<GameObject>("MeshInfo2/same-name-blendshape-blender-binary-edited.fbx");
            var renderer = fbx.GetComponent<SkinnedMeshRenderer>();
            var mesh = renderer.sharedMesh;

            // check the mesh has same name blendShape
            Assert.That(mesh.blendShapeCount, Is.EqualTo(2));
            Assert.That(mesh.GetBlendShapeName(0), Is.EqualTo("BlendShape1"));
            Assert.That(mesh.GetBlendShapeName(1), Is.EqualTo("BlendShape1"));
            
            using var meshInfo2 = new MeshInfo2(renderer);
            // we've checked no exception is thrown
            
            // second shape is renamed
            Assert.That(meshInfo2.BlendShapes[0].name, Is.EqualTo("BlendShape1"));
            Assert.That(meshInfo2.BlendShapes[1].name, Does.StartWith("BlendShape1-nameConflict-"));
            // and there is buffer for each vertex
            foreach (var vertex in meshInfo2.Vertices)
            {
                Assert.That(vertex.BlendShapeBuffer.Shapes[meshInfo2.BlendShapes[0].name], Is.Not.Null);
                Assert.That(vertex.BlendShapeBuffer.Shapes[meshInfo2.BlendShapes[1].name], Is.Not.Null);
            }
        }

        // test with real fbx
        [Test]
        public void MultipleSameNameBlendShape3dsMax()
        {
            var fbx = TestUtils.GetAssetAt<GameObject>("MeshInfo2/same-name-blendshape-3ds-max.fbx");
            var renderer = fbx.transform.Find("Box001").GetComponent<SkinnedMeshRenderer>();
            var mesh = renderer.sharedMesh;

            // check the mesh has same name blendShape
            Assert.That(mesh.blendShapeCount, Is.EqualTo(2));
            Assert.That(mesh.GetBlendShapeName(0), Is.EqualTo("Shape"));
            Assert.That(mesh.GetBlendShapeName(1), Is.EqualTo("Shape"));

            using var meshInfo2 = new MeshInfo2(renderer);
            // we've checked no exception is thrown

            // second shape is renamed
            Assert.That(meshInfo2.BlendShapes[0].name, Is.EqualTo("Shape"));
            Assert.That(meshInfo2.BlendShapes[1].name, Does.StartWith("Shape-nameConflict-"));
            // and there is buffer for each vertex
            foreach (var vertex in meshInfo2.Vertices)
            {
                Assert.That(vertex.BlendShapeBuffer.Shapes[meshInfo2.BlendShapes[0].name], Is.Not.Null);
                Assert.That(vertex.BlendShapeBuffer.Shapes[meshInfo2.BlendShapes[1].name], Is.Not.Null);
            }
        }

        [Test]
        public void SkinnedMeshWithOnlyBlendShapesRemovedAddsDummy()
        {
            // This test specifically reproduces the issue described:
            // "BlendShapeだけだったSkinneMeshからBlemdShapeを全部消すと" 
            // (When removing all BlendShapes from a SkinnedMesh that only had BlendShapes)
            
            var mesh = TestUtils.NewCubeMesh();
            
            // Add some blend shapes to simulate a mesh that originally had only blend shapes
            var deltas = new Vector3[8];
            deltas.AsSpan().Fill(new Vector3(1, 0, 0));
            mesh.AddBlendShapeFrame("OriginalShape1", 100, deltas, null, null);
            deltas.AsSpan().Fill(new Vector3(0, 1, 0));
            mesh.AddBlendShapeFrame("OriginalShape2", 100, deltas, null, null);

            var go = new GameObject();
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh;

            using var meshInfo2 = new MeshInfo2(smr);
            
            // Verify it originally had blend shapes but no bone weights
            Assert.That(meshInfo2.BlendShapes.Count, Is.EqualTo(2));
            Assert.That(meshInfo2.Vertices.All(v => v.BoneWeights.Count == 0), Is.True, "Should have no bone weights");
            
            // Simulate optimization removing all blend shapes
            meshInfo2.BlendShapes.Clear();
            
            var newMesh = new Mesh();
            meshInfo2.WriteToMesh(newMesh, isSkinnedMesh: true);

            // Should have exactly 1 dummy blend shape added to prevent Unity error
            Assert.That(newMesh.blendShapeCount, Is.EqualTo(1));
            Assert.That(newMesh.GetBlendShapeName(0), Is.EqualTo("AAO_DummyBlendShape"));
            
            // Verify this doesn't cause Unity's error by checking the mesh has the required data
            Assert.That(newMesh.blendShapeCount > 0 || newMesh.boneWeights.Length > 0, Is.True, 
                "Mesh should have either blend shapes or bone weights to avoid Unity error");
        }

        [Test]
        public void SkinnedMeshWithoutBlendShapesAddsDummy()
        {
            var mesh = TestUtils.NewCubeMesh();
            var go = new GameObject();
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh;

            using var meshInfo2 = new MeshInfo2(smr);
            
            // Remove all blend shapes to simulate the optimization scenario
            meshInfo2.BlendShapes.Clear();
            
            var newMesh = new Mesh();
            meshInfo2.WriteToMesh(newMesh, isSkinnedMesh: true);

            // Should have exactly 1 dummy blend shape added
            Assert.That(newMesh.blendShapeCount, Is.EqualTo(1));
            Assert.That(newMesh.GetBlendShapeName(0), Is.EqualTo("AAO_DummyBlendShape"));
            
            // The dummy blend shape should have one frame with weight 100 and all zero deltas
            Assert.That(newMesh.GetBlendShapeFrameCount(0), Is.EqualTo(1));
            Assert.That(newMesh.GetBlendShapeFrameWeight(0, 0), Is.EqualTo(100f));
            
            var deltaVertices = new Vector3[newMesh.vertexCount];
            var deltaNormals = new Vector3[newMesh.vertexCount];
            var deltaTangents = new Vector3[newMesh.vertexCount];
            newMesh.GetBlendShapeFrameVertices(0, 0, deltaVertices, deltaNormals, deltaTangents);
            
            // All deltas should be zero (dummy blend shape)
            foreach (var delta in deltaVertices)
                Assert.That(delta, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void SkinnedMeshWithExistingBlendShapesDoesNotAddDummy()
        {
            var mesh = TestUtils.NewCubeMesh();
            var deltas = new Vector3[8];
            deltas.AsSpan().Fill(new Vector3(1, 1, 1));
            mesh.AddBlendShapeFrame("ExistingShape", 100, deltas, null, null);

            var go = new GameObject();
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh;

            using var meshInfo2 = new MeshInfo2(smr);
            
            // Should have the original blend shape
            Assert.That(meshInfo2.BlendShapes.Count, Is.EqualTo(1));
            Assert.That(meshInfo2.BlendShapes[0].name, Is.EqualTo("ExistingShape"));
            
            var newMesh = new Mesh();
            meshInfo2.WriteToMesh(newMesh, isSkinnedMesh: true);

            // Should have exactly 1 blend shape (the original, no dummy added)
            Assert.That(newMesh.blendShapeCount, Is.EqualTo(1));
            Assert.That(newMesh.GetBlendShapeName(0), Is.EqualTo("ExistingShape"));
        }

        [Test]
        public void MeshRendererWithoutBlendShapesDoesNotAddDummy()
        {
            var mesh = TestUtils.NewCubeMesh();
            var go = new GameObject();
            var mr = go.AddComponent<MeshRenderer>();
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            using var meshInfo2 = new MeshInfo2(mr);
            
            // Ensure no blend shapes
            meshInfo2.BlendShapes.Clear();
            
            var newMesh = new Mesh();
            meshInfo2.WriteToMesh(newMesh, isSkinnedMesh: false);

            // Should have no blend shapes at all
            Assert.That(newMesh.blendShapeCount, Is.EqualTo(0));
        }
    }
}
