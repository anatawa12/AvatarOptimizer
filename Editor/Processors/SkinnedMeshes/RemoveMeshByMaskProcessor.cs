using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEditor;
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
                    var originalMeshImporter = GetImporter(ObjectRegistry.GetReference(mask).Object as Texture2D);

                    TextureImporter? GetImporter(Texture2D? importingMesh)
                    {
                        if (!importingMesh) return null;
                        var path = AssetDatabase.GetAssetPath(importingMesh);
                        if (string.IsNullOrEmpty(path)) return null;
                        return AssetImporter.GetAtPath(path) as TextureImporter;
                    }

                    if (originalMeshImporter == null)
                    {
                        BuildLog.LogError("RemoveMeshByMask:error:maskIsNotReadable", mask);
                    }
                    else
                    {
                        void AutoFix()
                        {
                            originalMeshImporter.isReadable = true;
                            originalMeshImporter.SaveAndReimport();
                        }

                        BuildLog.LogErrorWithAutoFix("RemoveMeshByMask:error:maskIsNotReadable", AutoFix, mask);
                    }

                    continue;
                }

                int GetValue(float u, float v)
                {
                    var x = Mathf.FloorToInt(Utils.Modulo(u, 1) * textureWidth);
                    var y = Mathf.FloorToInt(Utils.Modulo(v, 1) * textureHeight);
                    if (y * textureWidth + x < 0 || y * textureWidth + x >= pixels.Length) throw new IndexOutOfRangeException($"x: {x}, y: {y}, u: {u}, v: {v}, w: {textureWidth}, h: {textureHeight}, l: {pixels.Length}");
                    var pixel = pixels[y * textureWidth + x];
                    return Mathf.Max(Mathf.Max(pixel.r, pixel.g), pixel.b);
                }

                Func<float, float, bool> isRemoved;

                switch (materialSetting.mode)
                {
                    case RemoveMeshByMask.RemoveMode.RemoveWhite:
                        isRemoved = (u, v) => GetValue(u, v) > 127;
                        break;
                    case RemoveMeshByMask.RemoveMode.RemoveBlack:
                        isRemoved = (u, v) => GetValue(u, v) <= 127;
                        break;
                    default:
                        BuildLog.LogError("RemoveMeshByMask:error:unknownMode");
                        continue;
                }

                submesh.RemovePrimitives("Remove Mesh By Mask",
                    vertices => vertices.All(v => isRemoved(v.TexCoord0.x, v.TexCoord0.y)));
            }

            target.RemoveUnusedVertices();
        }

        public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => upstream;
    }
}
