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
    public class EditSkinnedMeshComponent : AvatarTagComponent
    {
        private protected EditSkinnedMeshComponent()
        {
        }
    }
}
