using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class MaxTextureSizeProcessor : Pass<MaxTextureSizeProcessor>
    {
        public override string DisplayName => "MaxTextureSizeProcessor";

        protected override void Execute(BuildContext context)
        {
            var maxTextureSizeComponents = context.GetComponents<MaxTextureSize>().ToList();
            if (maxTextureSizeComponents.Count == 0) return;

            // Get the minimum max texture size from all components
            var maxTextureSize = maxTextureSizeComponents
                .Select(c => (int)c.maxTextureSize)
                .Min();

            // Collect all textures used in the avatar
            var processedTextures = new HashSet<Texture2D>();

            foreach (var renderer in context.GetComponents<Renderer>())
            {
                var materials = renderer.sharedMaterials;
                if (materials == null) continue;

                foreach (var material in materials)
                {
                    if (material == null) continue;

                    var propertyNames = material.GetTexturePropertyNames();
                    foreach (var propertyName in propertyNames)
                    {
                        var texture = material.GetTexture(propertyName);
                        if (texture is Texture2D texture2D && !processedTextures.Contains(texture2D))
                        {
                            processedTextures.Add(texture2D);
                            var newTexture = ResizeTexture(texture2D, maxTextureSize);
                            if (newTexture != null)
                            {
                                material.SetTexture(propertyName, newTexture);
                            }
                        }
                    }
                }
            }

            // Clean up components
            foreach (var component in maxTextureSizeComponents)
            {
                DestroyTracker.DestroyImmediate(component);
            }
        }

        private static Texture2D? ResizeTexture(Texture2D original, int maxSize)
        {
            // Skip if texture is already smaller than or equal to max size
            if (original.width <= maxSize && original.height <= maxSize)
                return null;

            // Skip if texture doesn't have mipmaps
            if (original.mipmapCount <= 1)
                return null;

            // Calculate the target mipmap level
            var widthLevel = 0;
            var heightLevel = 0;
            var currentWidth = original.width;
            var currentHeight = original.height;

            while (currentWidth > maxSize && widthLevel < original.mipmapCount - 1)
            {
                currentWidth /= 2;
                widthLevel++;
            }

            while (currentHeight > maxSize && heightLevel < original.mipmapCount - 1)
            {
                currentHeight /= 2;
                heightLevel++;
            }

            var targetLevel = Math.Max(widthLevel, heightLevel);
            if (targetLevel == 0)
                return null;

            var targetWidth = original.width >> targetLevel;
            var targetHeight = original.height >> targetLevel;

            // For compressed textures with mipmaps, try to extract the specific mipmap level
            if (GraphicsFormatUtility.IsCompressedFormat(original.format) && 
                !GraphicsFormatUtility.IsCrunchFormat(original.format) &&
                original.isReadable)
            {
                try
                {
                    return ExtractMipmapLevel(original, targetLevel, targetWidth, targetHeight);
                }
                catch
                {
                    // Fall back to rendering approach if mipmap extraction fails
                }
            }

            // For other textures or if mipmap extraction failed, use rendering approach
            return ResizeUsingRenderTexture(original, targetWidth, targetHeight);
        }

        private static Texture2D? ExtractMipmapLevel(Texture2D original, int level, int width, int height)
        {
            // Create a new texture with the same format
            var newTexture = new Texture2D(width, height, original.format, mipChain: false, linear: !original.isDataSRGB);
            
            // Get the raw data for the specific mipmap level
            var data = original.GetRawTextureData<byte>();
            
            // Calculate offset to the target mipmap level
            var offset = 0;
            for (var i = 0; i < level; i++)
            {
                var mipWidth = Math.Max(1, original.width >> i);
                var mipHeight = Math.Max(1, original.height >> i);
                offset += (int)GraphicsFormatUtility.ComputeMipmapSize(mipWidth, mipHeight, original.format);
            }
            
            var mipSize = (int)GraphicsFormatUtility.ComputeMipmapSize(width, height, original.format);
            var mipData = new byte[mipSize];
            
            // Copy the mipmap data
            for (var i = 0; i < mipSize; i++)
            {
                mipData[i] = data[offset + i];
            }
            
            newTexture.SetPixelData(mipData, 0);
            newTexture.Apply(updateMipmaps: false, makeNoLongerReadable: !original.isReadable);
            
            // Copy texture settings
            newTexture.wrapModeU = original.wrapModeU;
            newTexture.wrapModeV = original.wrapModeV;
            newTexture.filterMode = original.filterMode;
            newTexture.anisoLevel = original.anisoLevel;
            newTexture.mipMapBias = original.mipMapBias;
            newTexture.name = original.name + " (MaxTextureSize)";
            
            return newTexture;
        }

        private static Texture2D? ResizeUsingRenderTexture(Texture2D original, int width, int height)
        {
            var format = Utils.GetRenderingFormatForTexture(original.format, isSRGB: original.isDataSRGB);
            
            // Create a temporary render texture
            var tempRT = RenderTexture.GetTemporary(width, height, 0, format);
            var previousRT = RenderTexture.active;
            
            try
            {
                // Copy the texture to the render texture (this will use mipmaps)
                Graphics.Blit(original, tempRT);
                
                // Read back to a Texture2D
                RenderTexture.active = tempRT;
                var textureFormat = Utils.GetTextureFormatForReading(original.graphicsFormat);
                var newTexture = new Texture2D(width, height, textureFormat, mipChain: true, linear: !original.isDataSRGB);
                newTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                newTexture.Apply(updateMipmaps: true, makeNoLongerReadable: !original.isReadable);
                
                // Copy texture settings
                newTexture.wrapModeU = original.wrapModeU;
                newTexture.wrapModeV = original.wrapModeV;
                newTexture.filterMode = original.filterMode;
                newTexture.anisoLevel = original.anisoLevel;
                newTexture.mipMapBias = original.mipMapBias;
                newTexture.name = original.name + " (MaxTextureSize)";
                
                return newTexture;
            }
            finally
            {
                RenderTexture.active = previousRT;
                RenderTexture.ReleaseTemporary(tempRT);
            }
        }
    }
}
