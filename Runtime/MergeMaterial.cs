using System;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/AAO Merge Material")]
    [DisallowMultipleComponent]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/merge-material/")]
    internal class MergeMaterial : EditSkinnedMeshComponent
    {
        public MergeInfo[] merges = Array.Empty<MergeInfo>();

        [Serializable]
        internal class MergeInfo
        {
            public Material? referenceMaterial;
            public MergeSource[] source = Array.Empty<MergeSource>();

            public MergedTextureFormat mergedFormat;

            // must be power of two. 13 (2^13 = 8192) is upper limit.
            public Vector2Int textureSize = new Vector2Int(2048, 2048);

            public TextureConfigOverride[] textureConfigOverrides = Array.Empty<TextureConfigOverride>();
        }

        [Serializable]
        internal class MergeSource
        {
            public Material? material;
            public Rect targetRect = new Rect(0, 0, 1, 1);
        }

        [Serializable]
        internal struct TextureConfigOverride
        {
            public string? textureName;
            public Vector2Int sizeOverride;
            public MergedTextureFormat formatOverride;
        }

        /// <summary>
        /// Subset of <see cref="UnityEngine.TextureFormat"/>
        /// </summary>
        public enum MergedTextureFormat
        {
            // There We forced non-linear color space for now so I disabled floating point image formats.
#if UNITY_ANDROID || UNITY_IOS
        [InspectorName("Default (ASTC 6x6)")]
#else
            [InspectorName("Default (DXT5)")]
#endif
            Default = 0,

            // ReSharper disable InconsistentNaming
            Alpha8 = TextureFormat.Alpha8,
            ARGB4444 = TextureFormat.ARGB4444,
            RGB24 = TextureFormat.RGB24,
            RGBA32 = TextureFormat.RGBA32,
            ARGB32 = TextureFormat.ARGB32,
            RGB565 = TextureFormat.RGB565,
            R16 = TextureFormat.R16,
            DXT1 = TextureFormat.DXT1,
            DXT5 = TextureFormat.DXT5,
            RGBA4444 = TextureFormat.RGBA4444,
            BGRA32 = TextureFormat.BGRA32,

            //RHalf = TextureFormat.RHalf, // floating point
            //RGHalf = TextureFormat.RGHalf, // floating point
            //RGBAHalf = TextureFormat.RGBAHalf, // floating point
            //RFloat = TextureFormat.RFloat, // floating point
            //RGFloat = TextureFormat.RGFloat, // floating point
            //RGBAFloat = TextureFormat.RGBAFloat, // floating point
            //YUY2 = TextureFormat.YUY2,
            //RGB9e5Float = TextureFormat.RGB9e5Float,
            //BC6H = TextureFormat.BC6H,
            BC7 = TextureFormat.BC7,
            BC4 = TextureFormat.BC4,
            BC5 = TextureFormat.BC5,

            //DXT1Crunched = TextureFormat.DXT1Crunched,
            //DXT5Crunched = TextureFormat.DXT5Crunched,
            //PVRTC_RGB2 = TextureFormat.PVRTC_RGB2,
            //PVRTC_RGBA2 = TextureFormat.PVRTC_RGBA2,
            //PVRTC_RGB4 = TextureFormat.PVRTC_RGB4,
            //PVRTC_RGBA4 = TextureFormat.PVRTC_RGBA4,
            //ETC_RGB4 = TextureFormat.ETC_RGB4,
            //EAC_R = TextureFormat.EAC_R,
            //EAC_R_SIGNED = TextureFormat.EAC_R_SIGNED,
            //EAC_RG = TextureFormat.EAC_RG,
            //EAC_RG_SIGNED = TextureFormat.EAC_RG_SIGNED,
            //ETC2_RGB = TextureFormat.ETC2_RGB,
            //ETC2_RGBA1 = TextureFormat.ETC2_RGBA1,
            //ETC2_RGBA8 = TextureFormat.ETC2_RGBA8,
            ASTC_4x4 = TextureFormat.ASTC_4x4,

            //ASTC_RGB_4x4 = TextureFormat.ASTC_RGB_4x4,
            ASTC_5x5 = TextureFormat.ASTC_5x5,

            //ASTC_RGB_5x5 = TextureFormat.ASTC_RGB_5x5,
            ASTC_6x6 = TextureFormat.ASTC_6x6,

            //ASTC_RGB_6x6 = TextureFormat.ASTC_RGB_6x6,
            ASTC_8x8 = TextureFormat.ASTC_8x8,

            //ASTC_RGB_8x8 = TextureFormat.ASTC_RGB_8x8,
            ASTC_10x10 = TextureFormat.ASTC_10x10,

            //ASTC_RGB_10x10 = TextureFormat.ASTC_RGB_10x10,
            ASTC_12x12 = TextureFormat.ASTC_12x12,

            //ASTC_RGB_12x12 = TextureFormat.ASTC_RGB_12x12,
            //ASTC_RGBA_4x4 = TextureFormat.ASTC_RGBA_4x4,
            //ASTC_RGBA_5x5 = TextureFormat.ASTC_RGBA_5x5,
            //ASTC_RGBA_6x6 = TextureFormat.ASTC_RGBA_6x6,
            //ASTC_RGBA_8x8 = TextureFormat.ASTC_RGBA_8x8,
            //ASTC_RGBA_10x10 = TextureFormat.ASTC_RGBA_10x10,
            //ASTC_RGBA_12x12 = TextureFormat.ASTC_RGBA_12x12,
            RG16 = TextureFormat.RG16,
            R8 = TextureFormat.R8,
            //ETC_RGB4Crunched = TextureFormat.ETC_RGB4Crunched,
            //ETC2_RGBA8Crunched = TextureFormat.ETC2_RGBA8Crunched,
            //ASTC_HDR_4x4 = TextureFormat.ASTC_HDR_4x4,
            //ASTC_HDR_5x5 = TextureFormat.ASTC_HDR_5x5,
            //ASTC_HDR_6x6 = TextureFormat.ASTC_HDR_6x6,
            //ASTC_HDR_8x8 = TextureFormat.ASTC_HDR_8x8,
            //ASTC_HDR_10x10 = TextureFormat.ASTC_HDR_10x10,
            //ASTC_HDR_12x12 = TextureFormat.ASTC_HDR_12x12,
            //RG32 = TextureFormat.RG32,
            //RGB48 = TextureFormat.RGB48,
            //RGBA64 = TextureFormat.RGBA64,
            // ReSharper restore InconsistentNaming
        }
    }
}
