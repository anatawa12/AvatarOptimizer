using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorParserTest
{
    /// <summary>
    /// This class generates mesh for this test. You can execute with `AvatarOptimizer/AnimatorParserMeshGeneration`
    /// </summary>
    public class MeshGeneration
    {
        [MenuItem("AvatarOptimizer/AnimatorParserMeshGeneration")]
        static void Generate()
        {
            var mesh = new Mesh();

            mesh.vertices = new[]
            {
                new Vector3(-1, -1, 0),
                new Vector3(0, 1, 0),
                new Vector3(1, -1, 0),
            };

            mesh.triangles = new[]{0, 1, 2, 2, 1, 0};

            for (var i = 0; i < 20; i++)
            {
                mesh.AddBlendShapeFrame($"shape{i}", 100,
                    new[] { Vector3.forward, Vector3.forward, Vector3.forward }, null, null);
            }

            var meshPath = TestUtils.GetAssetPath("AnimatorParser/TestMesh.asset");
            AssetDatabase.CreateAsset(mesh, meshPath);
        }
    }
}