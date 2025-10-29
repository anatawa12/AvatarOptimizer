using System;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/AAO Remove Mesh By Mask")]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    [DisallowMultipleComponent]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/remove-mesh-by-mask/")]
    [PublicAPI]
    public sealed class RemoveMeshByMask : EditSkinnedMeshComponent, INoSourceEditSkinnedMeshComponent
    {
        [SerializeField]
        internal MaterialSlot[] materials = Array.Empty<MaterialSlot>();

        APIChecker _checker;

        internal RemoveMeshByMask()
        {
        }

        [Serializable]
        [PublicAPI]
        public struct MaterialSlot : IEquatable<MaterialSlot>
        {
            [SerializeField] [ToggleLeft] internal bool enabled;

            [SerializeField] [AAOLocalized("RemoveMeshByMask:prop:mask")]
            internal Texture2D? mask;

            [SerializeField] [AAOLocalized("RemoveMeshByMask:prop:mode")]
            internal RemoveMode mode;

            /// <summary>
            /// Gets or sets whether this material slot is enabled for mask removal.
            /// </summary>
            [PublicAPI]
            public bool Enabled
            {
                get => enabled;
                set => enabled = value;
            }

            /// <summary>
            /// Gets or sets the mask texture for this material slot.
            /// </summary>
            [PublicAPI]
            public Texture2D? Mask
            {
                get => mask;
                set => mask = value;
            }

            /// <summary>
            /// Gets or sets the removal mode for this material slot.
            /// </summary>
            [PublicAPI]
            public RemoveMode Mode
            {
                get => mode;
                set => mode = value;
            }

            public bool Equals(MaterialSlot other) =>
                enabled == other.enabled && Equals(mask, other.mask) && mode == other.mode;

            public override bool Equals(object? obj) => obj is MaterialSlot other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(enabled, mask, (int)mode);
            public static bool operator ==(MaterialSlot left, MaterialSlot right) => left.Equals(right);
            public static bool operator !=(MaterialSlot left, MaterialSlot right) => !left.Equals(right);
        }

        /// <summary>
        /// Specifies the removal mode for a material slot.
        /// </summary>
        [PublicAPI]
        public enum RemoveMode
        {
            /// <summary>
            /// Remove mesh where the mask texture is black (0,0,0).
            /// </summary>
            RemoveBlack,
            /// <summary>
            /// Remove mesh where the mask texture is white (1,1,1).
            /// </summary>
            RemoveWhite,
        }

        /// <summary>
        /// Initializes the RemoveMeshByMask with the specified default behavior version.
        ///
        /// As Described in the documentation, you have to call this method after `AddComponent` to make sure
        /// the default configuration is what you want.
        /// Without calling this method, the default configuration might be changed in the future.
        /// </summary>
        /// <param name="version">
        /// The default configuration version.
        /// Since 1.9.0, version 1 is supported.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">Unsupported configuration version</exception>
        [PublicAPI]
        public void Initialize(int version)
        {
            switch (version)
            {
                case 1:
                    // nothing to do
                    break; 
                default:
                    throw new ArgumentOutOfRangeException(nameof(version), $"unsupported version: {version}");
            }
            _checker.OnInitialize(version, this);
        }

        /// <summary>
        /// Gets or sets the material slots for mask removal.
        /// Each slot corresponds to a material slot on the SkinnedMeshRenderer.
        /// </summary>
        [PublicAPI]
        public MaterialSlot[] Materials
        {
            // clone them for future API changes
            get => _checker.OnAPIUsage(this, materials.ToArray());
            set => _checker.OnAPIUsage(this, materials = value.ToArray());
        }
    }
}
