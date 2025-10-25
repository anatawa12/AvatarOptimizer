using System;
using UnityEngine;
using UnityEngine.Animations;

namespace Anatawa12.AvatarOptimizer
{
    /// <summary>
    /// Component to limit the maximum size of textures in the GameObject and its children.
    /// Uses mipmaps to resize textures without heavy recompression.
    /// </summary>
    [AddComponentMenu("Avatar Optimizer/AAO Max Texture Size")]
    [DisallowMultipleComponent]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/max-texture-size/")]
    internal sealed class MaxTextureSize : AvatarTagComponent
    {
        [NotKeyable]
        [AAOLocalized("MaxTextureSize:prop:maxTextureSize")]
        [SerializeField]
        public MaxTextureSizeValue maxTextureSize = MaxTextureSizeValue.Max2048;
    }

    internal enum MaxTextureSizeValue
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
