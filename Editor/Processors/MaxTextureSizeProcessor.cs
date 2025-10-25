using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEditor;
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

            // Build a map of renderer to max texture size
            var rendererToMaxSize = new Dictionary<Renderer, int>();
            
            foreach (var component in maxTextureSizeComponents)
            {
                var componentTransform = component.transform;
                var maxSize = (int)component.maxTextureSize;
                
                // Apply to all renderers in this GameObject and its children
                foreach (var renderer in component.GetComponentsInChildren<Renderer>(true))
                {
                    if (!rendererToMaxSize.ContainsKey(renderer))
                    {
                        rendererToMaxSize[renderer] = maxSize;
                    }
                    else
                    {
                        // Use the minimum max size if multiple components apply
                        rendererToMaxSize[renderer] = Math.Min(rendererToMaxSize[renderer], maxSize);
                    }
                }
            }

            // Collect all textures used by affected renderers and resize them
            var textureMapping = new Dictionary<Texture2D, Texture2D>();

            // First pass: find all textures and create resized versions
            foreach (var (renderer, maxSize) in rendererToMaxSize)
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
                        if (texture is Texture2D texture2D && !textureMapping.ContainsKey(texture2D))
                        {
                            var newTexture = ResizeTexture(texture2D, maxSize);
                            if (newTexture != null)
                            {
                                textureMapping[texture2D] = newTexture;
                            }
                        }
                    }
                }
            }

            // Second pass: replace all textures in all materials
            foreach (var (renderer, _) in rendererToMaxSize)
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
                        if (texture is Texture2D texture2D && textureMapping.TryGetValue(texture2D, out var newTexture))
                        {
                            material.SetTexture(propertyName, newTexture);
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

            // Skip crunched textures and warn
            if (GraphicsFormatUtility.IsCrunchFormat(original.format))
            {
                Debug.LogWarning($"AAO Max Texture Size: Skipping crunched texture '{original.name}' - crunched format resizing is not supported.");
                return null;
            }

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

            // Extract the specific mipmap level directly from texture data
            try
            {
                return ExtractMipmapLevel(original, targetLevel, targetWidth, targetHeight);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"AAO Max Texture Size: Failed to extract mipmap from texture '{original.name}': {e.Message}");
                return null;
            }
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
    }
}
