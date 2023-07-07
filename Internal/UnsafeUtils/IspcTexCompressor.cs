using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    internal static class IspcTexCompressor
    {
        private static TextureFormat AstcFormatByBlockSize(int blockSize)
        {
            switch (blockSize)
            {
                case 4:
                    return TextureFormat.ASTC_4x4;
                case 5:
                    return TextureFormat.ASTC_5x5;
                case 6:
                    return TextureFormat.ASTC_6x6;
                case 8:
                    return TextureFormat.ASTC_8x8;
                case 10:
                    throw new ArgumentOutOfRangeException(nameof(blockSize), blockSize, 
                        "ASTC 10x10 is not supported");
                case 12:
                    throw new ArgumentOutOfRangeException(nameof(blockSize), blockSize, 
                        "ASTC 12x12 is not supported");
                default:
                    throw new ArgumentOutOfRangeException(nameof(blockSize), blockSize, "invalid ASTC block size");
            }
        }

        public static Texture2D GenerateAstc(Texture2D texture, int blockSize)
        {
            System.Diagnostics.Debug.Assert(texture.format == TextureFormat.RGBA32);
            System.Diagnostics.Debug.Assert(blockSize <= 8);

            var format = AstcFormatByBlockSize(blockSize);

            var srcData = texture.GetRawTextureData<Color32>();
            var dstTexture = new Texture2D(texture.width, texture.height, format, texture.mipmapCount, false);
            var dstData = dstTexture.GetRawTextureData<ASTCBlock>();

            var srcWidth = texture.width;
            var srcHeight = texture.height;
            var srcOffset = 0;
            var dstOffset = 0;

            for (var mipmap = 0; mipmap < texture.mipmapCount; mipmap++)
            {
                var blockWidth = (srcWidth + blockSize - 1) / blockSize;
                var blockHeight = (srcHeight + blockSize - 1) / blockSize;

                GenerateASTCOneMipMap(
                    blockSize: blockSize,
                    srcWidth: srcWidth,
                    srcHeight: srcHeight,
                    blockWidth: blockWidth,
                    blockHeight: blockHeight,
                    src: srcData.AsSpan().Slice(srcOffset, srcWidth * srcHeight),
                    dst: dstData.AsSpan().Slice(dstOffset, blockWidth * blockHeight));

                srcOffset += srcWidth * srcHeight;
                dstOffset += blockWidth * blockHeight;
                srcWidth = (srcWidth + 1) / 2;
                srcHeight = (srcHeight + 1) / 2;
            }

            dstTexture.Apply();
            return dstTexture;
        }

        private static unsafe void GenerateASTCOneMipMap(
            int blockSize,
            int srcWidth,
            int srcHeight,
            int blockWidth,
            int blockHeight,
            ReadOnlySpan<Color32> src,
            Span<ASTCBlock> dst
        ) {
            System.Diagnostics.Debug.Assert(blockSize <= 8);
            System.Diagnostics.Debug.Assert(src.Length == srcWidth * srcHeight);
            System.Diagnostics.Debug.Assert(dst.Length == blockWidth * blockHeight);

            var finalTexWidth = blockWidth * blockSize;
            var finalTexHeight = blockHeight * blockSize;
            System.Diagnostics.Debug.Assert(srcWidth <= finalTexWidth);
            System.Diagnostics.Debug.Assert(srcHeight <= finalTexHeight);

            src = Resize(src, srcWidth, srcHeight, finalTexWidth, finalTexHeight);

            fixed (Color32* srcPtr = src)
            fixed (ASTCBlock* dstPtr = dst)
            {
                var settings = new astc_enc_settings();
                GetProfile_astc_alpha_fast(&settings, blockSize, blockSize);

                var surface = new rgba_surface
                {
                    ptr = (byte*)srcPtr,
                    width = finalTexWidth,
                    height = finalTexHeight,
                    stride = finalTexWidth * sizeof(Color32),
                };

                CompressBlocksASTC(&surface, (byte*)dstPtr, &settings);
            }
        }

        private static ReadOnlySpan<Color32> Resize(ReadOnlySpan<Color32> src, int srcWidth, int srcHeight, int finalTexWidth, int finalTexHeight)
        {
            if (finalTexWidth == srcWidth && finalTexHeight == srcHeight) return src;
            
            System.Diagnostics.Debug.Assert(srcWidth <= finalTexWidth);
            System.Diagnostics.Debug.Assert(srcHeight <= finalTexHeight);

            System.Diagnostics.Debug.Assert(src.Length >= srcWidth * srcHeight);

            // fill 0..srcHeight-th line
            var result = new Color32[finalTexWidth * finalTexHeight];
            if (srcWidth == finalTexWidth)
            {
                src.Slice(0, srcWidth * srcHeight).CopyTo(result);
            }
            else
            {
                for (var y = 0; y < srcHeight; y++)
                {
                    src.Slice(srcWidth * y, srcWidth).CopyTo(
                        result.AsSpan(finalTexWidth * y));

                    for (var x = srcWidth; x < finalTexWidth; x++)
                        result[finalTexWidth * y + x] = src[srcWidth * y + srcWidth - 1];
                }
            }

            if (srcHeight != finalTexHeight)
            {
                var line = result.AsSpan(finalTexWidth * (srcHeight - 1), finalTexWidth);
                for (var y = srcHeight; y < finalTexHeight; y++)
                    line.CopyTo(result.AsSpan(finalTexWidth * y));
            }

            return result;
        }

        private const string IscpTexCompDll = "ispc_texcomp";
        //private const string IscpTexCompDll = "__Internal";

        [DllImport(IscpTexCompDll)]
        private static extern unsafe void GetProfile_astc_alpha_fast(astc_enc_settings* settings, int blockWidth, int blockHeight);
        [DllImport(IscpTexCompDll)]
        private static extern unsafe void CompressBlocksASTC(rgba_surface* src, byte* dst, astc_enc_settings* settings);
        
        // ReSharper disable InconsistentNaming
        unsafe struct rgba_surface
        {
            public byte* ptr;
            public int width;
            public int height;
            public int stride; // in bytes
        };

        struct astc_enc_settings
        {
            int block_width;
            int block_height;
            int channels;

            int fastSkipTreshold;
            int refineIterations;
        };

        // 16 bytes data
        struct ASTCBlock
        {
            private int data0;
            private int data1;
            private int data2;
            private int data3;
        }
        // ReSharper restore InconsistentNaming
    }
}