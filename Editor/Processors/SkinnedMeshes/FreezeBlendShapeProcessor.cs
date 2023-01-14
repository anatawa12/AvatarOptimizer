using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    internal class FreezeBlendShapeProcessor : EditSkinnedMeshProcessor<FreezeBlendShape>
    {
        public FreezeBlendShapeProcessor(FreezeBlendShape component) : base(component)
        {
        }

        public override int ProcessOrder => -10000;

        public override void Process(OptimizerSession session)
        {
            var freezes = new HashSet<string>(Component.shapeKeys);
            var mesh = session.AddToAsset(Object.Instantiate(Target.sharedMesh));
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var tangents = mesh.tangents;
            var verticesCount = mesh.vertices.Length;
            var blendShapes = new (string, Vector3[], Vector3[], Vector3[], float)[mesh.blendShapeCount];
            for (var i = 0; i < mesh.blendShapeCount; i++)
            {
                Assert.AreEqual(1, mesh.GetBlendShapeFrameCount(i));
                Assert.AreEqual(100.0f, mesh.GetBlendShapeFrameWeight(i, 0));
                var deltaVertices = new Vector3[verticesCount];
                var deltaNormals = new Vector3[verticesCount];
                var deltaTangents = new Vector3[verticesCount];
                mesh.GetBlendShapeFrameVertices(i, 0, deltaVertices, deltaNormals, deltaTangents);
                var shapeName = mesh.GetBlendShapeName(i);
                var weight = Target.GetBlendShapeWeight(i);
                blendShapes[i] = (shapeName, deltaVertices, deltaNormals, deltaTangents, weight);
            }

            mesh.ClearBlendShapes();
            Target.sharedMesh = mesh;

            for (int i = 0, j = 0; i < blendShapes.Length; i++)
            {
                var (shapeName, deltaVertices, deltaNormals, deltaTangents, weight) = blendShapes[i];
                if (freezes.Contains(shapeName))
                {
                    var weightZeroOne = weight / 100f;
                    for (var k = 0; k < verticesCount; k++)
                    {
                        vertices[k] += deltaVertices[k] * weightZeroOne;
                        normals[k] += deltaNormals[k] * weightZeroOne;
                        var t = (Vector3)tangents[k] + deltaTangents[k] * weightZeroOne;
                        tangents[k] = new Vector4(t.x, t.y, t.z, tangents[k].w);
                    }
                }
                else
                {
                    mesh.AddBlendShapeFrame(shapeName, 100.0f, deltaVertices, deltaNormals, deltaTangents);
                    Target.SetBlendShapeWeight(j++, weight);
                }
            }
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.tangents = tangents;

            session.Destroy(Component);
        }

        public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => new MeshInfoComputer(this, upstream);

        class MeshInfoComputer : AbstractMeshInfoComputer
        {
            private readonly FreezeBlendShapeProcessor _processor;

            public MeshInfoComputer(FreezeBlendShapeProcessor processor, IMeshInfoComputer upstream) : base(upstream)
                => _processor = processor;

            public override string[] BlendShapes() =>
                base.BlendShapes().Where(x => !_processor.Component.shapeKeys.Contains(x)).ToArray();
        }
    }
}
