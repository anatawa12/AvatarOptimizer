using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Animations;

namespace Anatawa12.AvatarOptimizer
{
    /// <summary>
    /// Component to limit the maximum size of textures in the avatar.
    /// Uses mipmaps to resize textures without heavy recompression.
    /// </summary>
    [AddComponentMenu("Avatar Optimizer/AAO Max Texture Size")]
    [DisallowMultipleComponent]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/max-texture-size/")]
    [PublicAPI]
    public sealed class MaxTextureSize : AvatarGlobalComponent
    {
        internal MaxTextureSize()
        {
        }

        [NotKeyable]
        [AAOLocalized("MaxTextureSize:prop:maxTextureSize")]
        [SerializeField]
        internal MaxTextureSizeValue maxTextureSize = MaxTextureSizeValue.Max2048;
    }

    /// <summary>
    /// Maximum texture size values
    /// </summary>
    [PublicAPI]
    public enum MaxTextureSizeValue
    {
        [InspectorName("4096")]
        Max4096 = 4096,
        [InspectorName("2048")]
        Max2048 = 2048,
        [InspectorName("1024")]
        Max1024 = 1024,
        [InspectorName("512")]
        Max512 = 512,
        [InspectorName("256")]
        Max256 = 256,
        [InspectorName("128")]
        Max128 = 128,
        [InspectorName("64")]
        Max64 = 64,
    }
}
