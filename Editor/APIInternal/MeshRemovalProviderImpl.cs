using System;
using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.API;
using nadena.dev.ndmf.preview;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.APIInternal;

internal class MeshRemovalProviderImpl : MeshRemovalProvider
{
    [InitializeOnLoadMethod]
    private static void Register()
    {
        GetForRendererImpl = GetForRendererImplImpl;
    }

    private static MeshRemovalProvider? GetForRendererImplImpl(SkinnedMeshRenderer renderer)
    {
        var removeMeshByMask = renderer.GetComponent<RemoveMeshByMask>();
        var removeMeshByBlendShape = renderer.GetComponents<RemoveMeshByBlendShape>();
        var removeMeshInBox = renderer.GetComponents<RemoveMeshInBox>();
        var evacuate = renderer.GetComponent<InternalEvacuateUVChannel>();

        if (removeMeshByMask == null && removeMeshByBlendShape.Length == 0 && removeMeshInBox.Length == 0)
            return null;

        if (!renderer.sharedMesh.isReadable)
            return null;

        return new MeshRemovalProviderImpl(renderer, evacuate, removeMeshByMask, removeMeshByBlendShape, removeMeshInBox);
    }

    private NativeArray<bool> _removedByBlendShape;
    private NativeArray<bool> _removedByBox;
    private readonly Vector2[]? _uv;
    private readonly MaterialSlotInformation[]? _removedByMask;

    private readonly struct MaterialSlotInformation
    {
        public readonly bool[]? shouldRemove;
        public readonly int width;
        public readonly int height;

        public MaterialSlotInformation(bool[] shouldRemove, int width, int height)
        {
            this.shouldRemove = shouldRemove;
            this.width = width;
            this.height = height;
        }
    }

    private MeshRemovalProviderImpl(SkinnedMeshRenderer renderer,
        InternalEvacuateUVChannel? evacuate,
        RemoveMeshByMask? removeMeshByMask,
        RemoveMeshByBlendShape[] removeMeshByBlendShape,
        RemoveMeshInBox[] removeMeshInBox)
    {
        if (removeMeshByBlendShape.Length != 0)
        {
            _removedByBlendShape = EditModePreview.RemoveMeshByBlendShapeRendererNode.ComputeShouldRemoveVertex(renderer.sharedMesh, removeMeshByBlendShape);
        }

        if (removeMeshInBox.Length != 0)
        {
            _removedByBox = EditModePreview.RemoveMeshInBoxRendererNode.ComputeShouldRemoveVertex(renderer, removeMeshInBox, ComputeContext.NullContext);
        }

        if (removeMeshByMask != null)
        {
            _removedByMask = new MaterialSlotInformation[renderer.sharedMesh.subMeshCount];
            var evacuated = evacuate?.EvacuateIndex(0) ?? 0;
            _uv = evacuated switch
            {
                0 => renderer.sharedMesh.uv,
                1 => renderer.sharedMesh.uv2,
                2 => renderer.sharedMesh.uv3,
                3 => renderer.sharedMesh.uv4,
                4 => renderer.sharedMesh.uv5,
                5 => renderer.sharedMesh.uv6,
                6 => renderer.sharedMesh.uv7,
                7 => renderer.sharedMesh.uv8,
            };

            for (var index = 0; index < removeMeshByMask.materials.Length && index < _removedByMask.Length; index++)
            {
                var materialSetting = removeMeshByMask.materials[index];
                if (!materialSetting.enabled) continue;
                var maskTexture = materialSetting.mask;
                if (maskTexture == null) continue;
                if (!maskTexture.isReadable) continue;
                
                bool removeWhite;
                switch (materialSetting.mode)
                {
                    case RemoveMeshByMask.RemoveMode.RemoveWhite:
                        removeWhite = true;
                        break;
                    case RemoveMeshByMask.RemoveMode.RemoveBlack:
                        removeWhite = false;
                        break;
                    default:
                        BuildLog.LogError("RemoveMeshByMask:error:unknownMode");
                        continue;
                }

                var shouldRemoves = new bool[maskTexture.width * maskTexture.height];
                var pixels = maskTexture.GetPixels32();
                for (var y = 0; y < maskTexture.height; y++)
                {
                    for (var x = 0; x < maskTexture.width; x++)
                    {
                        var pixel = pixels[y * maskTexture.width + x];

                        var isWhite = Mathf.Max(pixel.r, pixel.g, pixel.b) > 127;

                        bool shouldRemove = removeWhite ? isWhite : !isWhite;
                        shouldRemoves[y * maskTexture.width + x] = shouldRemove;
                    }
                }
                _removedByMask[index] = new MaterialSlotInformation(
                    shouldRemoves,
                    maskTexture.width,
                    maskTexture.height);
            }
        }
    }

    public override bool WillRemovePrimitive(MeshTopology topology, int subMesh, Span<int> vertexIndices)
    {
        switch (topology)
        {
            case MeshTopology.Triangles:
                if (vertexIndices.Length != 3)
                    throw new ArgumentException("vertexIndices.Length must be 3 for triangles", nameof(vertexIndices));
                break;
            case MeshTopology.Quads:
                if (vertexIndices.Length != 4)
                    throw new ArgumentException("vertexIndices.Length must be 4 for quads", nameof(vertexIndices));
                break;
            case MeshTopology.Lines:
                if (vertexIndices.Length != 2)
                    throw new ArgumentException("vertexIndices.Length must be 2 for lines", nameof(vertexIndices));
                break;
            case MeshTopology.Points:
                if (vertexIndices.Length != 1)
                    throw new ArgumentException("vertexIndices.Length must be 1 for points", nameof(vertexIndices));
                break;
            case MeshTopology.LineStrip:
            default:
                return false; // unsupported
        }

        if (_removedByBlendShape.Length > 0)
        {
            foreach (var vertexIndex in vertexIndices)
                if (_removedByBlendShape[vertexIndex])
                    return true;
        }

        if (_removedByBox.Length > 0)
        {
            var removePrimitive = true;
            foreach (var vertexIndex in vertexIndices)
            {
                if (!_removedByBox[vertexIndex])
                {
                    removePrimitive = false;
                    break;
                }
            }

            if (removePrimitive) return true;
        }

        if (_uv != null && _removedByMask != null)
        {
            var uv = _uv;
            var mask = _removedByMask[subMesh];
            if (mask.shouldRemove == null) return false;

            var removePrimitive = true;
            foreach (var vertexIndex in vertexIndices)
            {
                var uvIndex = vertexIndex;
                if (uvIndex >= uv.Length) return false;
                var uvValue = uv[uvIndex];
                var x = Mathf.FloorToInt(Utils.Modulo(uvValue.x, 1) * mask.width);
                var y = Mathf.FloorToInt(Utils.Modulo(uvValue.y, 1) * mask.height);
                var shouldRemove = mask.shouldRemove[y * mask.width + x];
                if (!shouldRemove)
                {
                    removePrimitive = false;
                    break;
                }
            }

            if (removePrimitive) return true;
        }

        return false;
    }

    public override void Dispose()
    {
        if (_removedByBlendShape != default) _removedByBlendShape.Dispose();
        if (_removedByBox != default) _removedByBox.Dispose();
    }
}
