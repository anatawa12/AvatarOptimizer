using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    internal class RemoveMeshByUVTileProcessor : EditSkinnedMeshProcessor<RemoveMeshByUVTile>
    {
        public RemoveMeshByUVTileProcessor(RemoveMeshByUVTile component) : base(component)
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
                if (!materialSetting.RemoveAnyTile) continue;
                var uvChannel = (int) materialSetting.uvChannel;
                if (uvChannel is < 0 or >= 8)
                {
                    BuildLog.LogError("RemoveMeshByUVTile:error:uvChannelOutOfRange", i);
                    continue;
                }
                if (target.GetTexCoordStatus(uvChannel) == TexCoordStatus.NotDefined) continue;

                submesh.RemovePrimitives("Remove Mesh By UV Tile",
                    vertices => vertices.Any(v =>
                    {
                        var texCoord = v.GetTexCoord(uvChannel);
                        var x = Mathf.FloorToInt(texCoord.x);
                        var y = Mathf.FloorToInt(texCoord.y);
                        if (x is < 0 or >= 4) return false;
                        if (y is < 0 or >= 4) return false;
                        var tile = x + y * 4;
                        return materialSetting.GetTile(tile);
                    }));
            }

            target.RemoveUnusedVertices();
        }

        public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => upstream;
    }
}
