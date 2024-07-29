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

        [Serializable]
        [PublicAPI]
        public struct MaterialSlot
        {
            [SerializeField]
            [ToggleLeft]
            internal bool enabled;
            [SerializeField]
            [AAOLocalized("RemoveMeshByMask:prop:mask")]
            internal Texture2D mask;
            [SerializeField]
            [AAOLocalized("RemoveMeshByMask:prop:mode")]
            internal RemoveMode mode;

            [PublicAPI]
            public bool Enabled
            {
                get => enabled;
                set => enabled = value;
            }

            [PublicAPI]
            public Texture2D Mask
            {
                get => mask;
                set => mask = value;
            }

            [PublicAPI]
            public RemoveMode Mode
            {
                get => mode;
                set => mode = value;
            }

        }

        [PublicAPI]
        public enum RemoveMode
        {
            RemoveBlack,
            RemoveWhite,
        }

        [PublicAPI]
        public MaterialSlot[] Materials
        {
            get => materials.ToArray();
            set => materials = value.ToArray();
        }
    }
}
