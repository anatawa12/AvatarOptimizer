using System;
using UnityEngine;

namespace Anatawa12.Merger
{
    [AddComponentMenu("Anatawa12/Merge Skinned Mesh")]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    [DisallowMultipleComponent]
    public class MergeSkinnedMesh : AvatarTagComponent
    {
        public SkinnedMeshRenderer[] renderers = Array.Empty<SkinnedMeshRenderer>();
    }
}
