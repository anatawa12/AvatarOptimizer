using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using NUnit.Framework;
using UnityEngine;

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
        [TestCase("two-0-50-frame", 0, -1)]
        [TestCase("two-0-50-frame", 25, 0)]
        [TestCase("two-0-50-frame", 50, 1)]
        [TestCase("two-0-50-frame", 75, 2)]

        public void BlendShapeLerp(string name, float weight, float offset)
        {
            var mesh = TestUtils.GetAssetAt<Mesh>($"MeshInfo2/{name}.asset");
            var go = new GameObject();
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh;
            var meshInfo2 = new MeshInfo2(smr);

            var vertex = meshInfo2.Vertices.Find(x => x.Position == new Vector3(+1, +1, +1));

            vertex.TryGetBlendShape("test0", weight, out var position, out _, out _);

            Assert.That(position.x, Is.EqualTo(offset));
        }
    }
}