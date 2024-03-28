using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;
using NUnit.Framework;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Test
{
    /// <summary>
    /// Currently this class only contains unit tests for <see cref="AutoMergeSkinnedMesh"/>.
    /// </summary>
    public class AutoMergeSkinnedMeshTest
    {
        [Test]
        public void CreateSubMeshesMergePreserveOrder()
        {
            var shader = Shader.Find("Standard");
            var material0 = new Material(shader);
            var material1 = new Material(shader);
            var material2 = new Material(shader);
            var material3 = new Material(shader);
            var material4 = new Material(shader);
            var (indexMap, meshes) = AutoMergeSkinnedMesh.CreateSubMeshesMergePreserveOrder(new[]
            {
                MakeMeshInfo2(material0, material1),
                MakeMeshInfo2(material0, material2),
                MakeMeshInfo2(material1, material2),
                MakeMeshInfo2(material0, material4),
                MakeMeshInfo2(material3, material4),
            });

            Assert.That(indexMap, Is.EquivalentTo(new []
            {
                new []{0, 1},
                new []{0, 2},
                new []{1, 2},
                new []{0, 4},
                new []{3, 4},
            }));

            Assert.That(meshes, Is.EquivalentTo(new []
            {
                (MeshTopology.Triangles, material0),
                (MeshTopology.Triangles, material1),
                (MeshTopology.Triangles, material2),
                (MeshTopology.Triangles, material3),
                (MeshTopology.Triangles, material4),
            }));
        }

        MeshInfo2 MakeMeshInfo2(params Material[] material)
        {
            var gameObject = new GameObject();
            var newMesh = new Mesh();
            newMesh.subMeshCount = material.Length;
            var renderer = gameObject.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMaterials = material;
            renderer.sharedMesh = newMesh;
            return new MeshInfo2(renderer);
        }
    }
}