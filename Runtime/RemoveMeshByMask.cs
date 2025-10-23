using System;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/AAO Remove Mesh By Mask")]
    // [RequireComponent(typeof(SkinnedMeshRenderer) or typeof(MeshRenderer))] // handled in editor
    [DisallowMultipleComponent]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/remove-mesh-by-mask/")]
    internal sealed class RemoveMeshByMask : EditSkinnedMeshComponent, INoSourceEditSkinnedMeshComponent
    {
        [SerializeField]
        internal MaterialSlot[] materials = Array.Empty<MaterialSlot>();

        [Serializable]
        internal struct MaterialSlot : IEquatable<MaterialSlot>
        {
            [SerializeField] [ToggleLeft] public bool enabled;

            [SerializeField] [AAOLocalized("RemoveMeshByMask:prop:mask")]
            public Texture2D? mask;

            [SerializeField] [AAOLocalized("RemoveMeshByMask:prop:mode")]
            public RemoveMode mode;

            public bool Equals(MaterialSlot other) =>
                enabled == other.enabled && Equals(mask, other.mask) && mode == other.mode;

            public override bool Equals(object? obj) => obj is MaterialSlot other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(enabled, mask, (int)mode);
            public static bool operator ==(MaterialSlot left, MaterialSlot right) => left.Equals(right);
            public static bool operator !=(MaterialSlot left, MaterialSlot right) => !left.Equals(right);
        }

        internal enum RemoveMode
        {
            RemoveBlack,
            RemoveWhite,
        }
    }
}
