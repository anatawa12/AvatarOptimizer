using System;
using Anatawa12.AvatarOptimizer.API;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Editor.APIInternal;

public class MeshRemovalProviderImpl : MeshRemovalProvider
{
    [InitializeOnLoadMethod]
    private static void Register()
    {
        MeshRemovalProvider.GetForRendererImpl = GetForRendererImpl;
    }

    private static MeshRemovalProvider? GetForRendererImpl(SkinnedMeshRenderer renderer)
    {
        var removeMeshByMask = renderer.GetComponent<RemoveMeshByMask>();
        var removeMeshByBlendShape = renderer.GetComponent<RemoveMeshByBlendShape>();
        var removeMeshInBox = renderer.GetComponent<RemoveMeshInBox>();

        if (removeMeshByMask == null && removeMeshByBlendShape == null && removeMeshInBox == null)
            return null;

        return new MeshRemovalProviderImpl(renderer, removeMeshByMask, removeMeshByBlendShape, removeMeshInBox);
    }

    private MeshRemovalProviderImpl(
        SkinnedMeshRenderer renderer, 
        RemoveMeshByMask? removeMeshByMask, 
        RemoveMeshByBlendShape? removeMeshByBlendShape, 
        RemoveMeshInBox? removeMeshInBox)
    {
        // TODO: this implementation should be based on NDMF Mesh Preview I think
        throw new NotImplementedException();
    }

    public override bool WillRemovePrimitive(MeshTopology topology, Span<int> vertexIndices)
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

        throw new NotImplementedException();
    }
}
