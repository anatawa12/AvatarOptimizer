#nullable enable

using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;

namespace Anatawa12.AvatarOptimizer.API
{
    /// <summary>
    /// This class provides a mesh primitive will be removed or not.
    /// </summary>
    /// <remarks>
    /// This class only provides the prediction of the removal of the primitives.
    /// This class is designed to not have false-positive removal prediction.
    /// if the mesh might be kept, this class will report as not removed.
    ///
    /// You should not change the mesh data after creating this class except for evacuated UV channels registered with <see cref="UVUsageCompabilityAPI"/>.
    ///
    /// For example, current implementation does not support removing mesh after merging mesh with MergeSkinnedMesh
    /// but primitives merged after MergeSkinnedMesh will be reported as not removed.
    /// 
    /// however, some other Non-Destructive tools might change the data for prediction so that the prediction might be wrong.
    /// Be careful when using this class.
    /// </remarks>
    [PublicAPI]
    public abstract class MeshRemovalProvider : IDisposable
    {
        private protected MeshRemovalProvider()
        {
        }

        /// <summary>
        /// Create MeshRemovalProvider for the renderer.
        ///
        /// If the primitives of the mesh will not be removed by Avatar Optimizer or not supported by this API, return null.
        /// </summary>
        /// <param name="renderer">The renderer to create MeshRemovalProvider for.</param>
        /// <returns>The MeshRemovalProvider for the renderer or null if no primitives will be removed.</returns>
        [PublicAPI]
        public static MeshRemovalProvider? GetForRenderer(SkinnedMeshRenderer renderer)
        {
            if (renderer == null) throw new ArgumentNullException(nameof(renderer));
            return GetForRendererImpl?.Invoke(renderer);
        }

        // returns true if a primitive consisits of the specified vertices will be removed by AvatarOptimizer
        // returns false if not.
        // If there are Processors between this call and Avatar Optimizer processes,
        // this value might be incorrect.
        /// <summary>
        /// Returns if a primitive consists of the specified vertices (specified with vertex index) will be removed by Avatar Optimizer.
        /// 
        /// If there are some Non-Destructive tools between this call and Avatar Optimizer processes, this value might be incorrect.
        /// </summary>
        /// <param name="topology">The topology of the primitive.</param>
        /// <param name="subMesh">The submesh index of the primitive.</param>
        /// <param name="vertexIndices">The vertex indices of the primitive.</param>
        /// <returns>Returns true if the primitive will be removed, false if might not.</returns>
        [PublicAPI]
        public abstract bool WillRemovePrimitive(MeshTopology topology, int subMesh, Span<int> vertexIndices);

        internal static Func<SkinnedMeshRenderer, MeshRemovalProvider?>? GetForRendererImpl { get; set; } = null;

        /// <summary>
        /// Dispose the MeshRemovalProvider.
        ///
        /// You should call this method when you don't need the MeshRemovalProvider anymore.
        /// </summary>
        [PublicAPI]
        public abstract void Dispose();
    }
}
