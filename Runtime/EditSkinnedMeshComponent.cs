using JetBrains.Annotations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    /// <summary>
    /// The base class for Edit Skinned Mesh Components.
    /// It's not expected to be used directly.
    /// </summary>
    [RequireComponent(typeof(SkinnedMeshRenderer))]
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
