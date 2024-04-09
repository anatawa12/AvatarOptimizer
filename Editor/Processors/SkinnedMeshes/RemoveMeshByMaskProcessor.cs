using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    internal class RemoveMeshByMaskProcessor : EditSkinnedMeshProcessor<RemoveMeshByMask>
    {
        public RemoveMeshByMaskProcessor(RemoveMeshByMask component) : base(component)
        {
        }

        // This needs to be less than FreezeBlendshapeProcessor.ProcessOrder.
        public override EditSkinnedMeshProcessorOrder ProcessOrder => EditSkinnedMeshProcessorOrder.RemovingMesh;

        public override void Process(BuildContext context, MeshInfo2 target)
        {
            var submeshes = target.SubMeshes;
            var materialSettings = Component.materials;

            // process each submesh
            for (var i = 0; i < submeshes.Count && i < materialSettings.Length; i++)
            {
                var submesh = submeshes[i];
                var materialSetting = materialSettings[i];
                if (!materialSetting.enabled) continue;

                if (materialSetting.mask == null)
                {
                    BuildLog.LogError("RemoveMeshByMask:error:maskIsNone", i);
                    continue;
                }

                var mask = materialSetting.mask;
                var textureWidth = mask.width;
                var textureHeight = mask.height;
                Color32[] pixels;

                if (mask.isReadable)
                {
                    pixels = mask.GetPixels32();
                }
                else
                {
                    BuildLog.LogError("RemoveMeshByMask:error:maskIsNotReadable", mask);
                    continue;
                }

                Func<float, float, bool> isRemoved;

                switch (materialSetting.mode)
                {
                    case RemoveMeshByMask.RemoveMode.RemoveWhite:
                        isRemoved = (x, y) =>
                        {
                            var pixel = pixels[(int)(y * textureHeight) * textureWidth + (int)(x * textureWidth)];
                            var v = Mathf.Max(Mathf.Max(pixel.r, pixel.g), pixel.b);
                            return v > 127;
                        };
                        break;
                    case RemoveMeshByMask.RemoveMode.RemoveBlack:
                        isRemoved = (x, y) =>
                        {
                            var pixel = pixels[(int)(y * textureHeight) * textureWidth + (int)(x * textureWidth)];
                            var v = Mathf.Max(Mathf.Max(pixel.r, pixel.g), pixel.b);
                            return v <= 127;
                        };
                        break;
                    default:
                        BuildLog.LogError("RemoveMeshByMask:error:unknownMode");
                        continue;
                }

                submesh.RemovePrimitives("Remove Mesh By Mask",
                    vertices => vertices.Any(v => isRemoved(v.TexCoord0.x, v.TexCoord0.y)));
            }

            // remove submeshes that have no vertices
            target.SubMeshes.RemoveAll(submesh => submesh.Vertices.Count == 0);

            // GC vertices
            var usingVertices = new HashSet<Vertex>();
            foreach (var subMesh in target.SubMeshes)
            foreach (var vertex in subMesh.Vertices)
                usingVertices.Add(vertex);

            target.Vertices.RemoveAll(x => !usingVertices.Contains(x));
        }

        public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => upstream;
    }
}
