using System;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/AAO Remove Mesh By Mask")]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    [DisallowMultipleComponent]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/remove-mesh-by-mask/")]
    internal sealed class RemoveMeshByMask : EditSkinnedMeshComponent
    {
        [SerializeField]
        internal MaterialSlot[] materials = Array.Empty<MaterialSlot>();

        [Serializable]
        internal struct MaterialSlot
        {
            [SerializeField]
            public bool enabled;
            [SerializeField]
            public Texture2D mask;
            [SerializeField]
            public RemoveMode mode;
        }

        internal enum RemoveMode
        {
            RemoveBlack,
            RemoveWhite,
        }
    }
}
