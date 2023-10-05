using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    // TODO move to somewhere else? e.g. in editor module if possible
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    internal class InternalAutoFreezeMeaninglessBlendShape : EditSkinnedMeshComponent
    {
        [CanBeNull] internal HashSet<string> Preserve;
    }
}
