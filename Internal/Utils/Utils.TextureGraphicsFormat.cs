using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Anatawa12.AvatarOptimizer
{
    partial class Utils
    {
        // Utilities for texture formats and graphics formats
        //
        // We generally use GraphicsFormatUtility and SystemInfo to get formats.
        // However, there are some cases where we need to ensure compatibility or avoid precision loss.
        // This file contains utility methods to handle texture formats and graphics formats without precision loss as much as possible.

        public static GraphicsFormat GetRenderingFormatForTexture(TextureFormat format, bool isSRGB)
        {
            var result = (format, isSRGB) switch
            {
                // We use 16bit float for RGB9e5Float to avoid precision loss.
                // Unity chooses G10G11R11 Unsigned Float packed float for RGB9e5Float, but precision loss can occur so we use R16G16B16_SFloat instead.
                (TextureFormat.RGB9e5Float, false) => SystemInfo.GetCompatibleFormat(GraphicsFormat.R16G16B16_SFloat,
                    FormatUsage.Render),
                // I don't know the best format for YUV2, but to expect highest precision, we use float32 format
                (TextureFormat.YUY2, false) => SystemInfo.GetCompatibleFormat(GraphicsFormat.R32G32B32A32_SFloat, FormatUsage.Render),
                _ => SystemInfo.GetCompatibleFormat(GraphicsFormatUtility.GetGraphicsFormat(format, isSRGB: isSRGB),
                    FormatUsage.Render)
            };
            if (result == GraphicsFormat.None) throw new InvalidOperationException($"Getting Rendering Format for TextureFormat {format} with isSRGB={isSRGB} returned None. This is problem of Avatar Optimizer, please report this issue.");
            return result;
        }

        public static TextureFormat GetTextureFormatForReading(GraphicsFormat format)
        {
            var textureFormat = format switch
            {
                GraphicsFormat.E5B9G9R9_UFloatPack32 => TextureFormat.RGB9e5Float,
                GraphicsFormat.YUV2 => TextureFormat.YUY2,
                GraphicsFormat.R_EAC_SNorm => TextureFormat.EAC_R_SIGNED,
                GraphicsFormat.RG_EAC_SNorm => TextureFormat.EAC_RG_SIGNED,
                _ => GraphicsFormatUtility.GetTextureFormat(
                    SystemInfo.GetCompatibleFormat(format, FormatUsage.ReadPixels))
            };
            if (textureFormat == 0) throw new InvalidOperationException($"Getting Texture Format for ReadPixels for GraphicsFormat {format} returned None. This is problem of Avatar Optimizer, please report this issue.");
            return textureFormat;
        }
    }
}
