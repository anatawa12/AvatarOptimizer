using JetBrains.Annotations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    /// <summary>
    /// The base class for Edit Skinned Mesh Components.
    /// It's not expected to be used directly.
    ///
    /// Since Avatar Optimizer 1.9.0, besides the name, some of EditSkinnedMeshComponents may support MeshRenderer as well as SkinnedMeshRenderer.
    /// However, due to historical reasons, the name is not changed.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    [PublicAPI]
    public abstract class EditSkinnedMeshComponent : AvatarTagComponent
    {
        private protected EditSkinnedMeshComponent()
        {
        }
    }

    // marker interface for source skinned mesh components
    internal interface ISourceSkinnedMeshComponent {}
    // marker interface for edit skinned mesh components that does not support source skinned mesh component
    internal interface INoSourceEditSkinnedMeshComponent {}
}
