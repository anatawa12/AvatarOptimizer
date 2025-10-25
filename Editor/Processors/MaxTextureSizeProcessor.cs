using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
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

            // Build a map of renderer to its closest parent MaxTextureSize component
            var rendererToComponent = new Dictionary<Renderer, MaxTextureSize>();
            
            foreach (var renderer in context.GetComponents<Renderer>())
            {
                // Find the closest parent MaxTextureSize component
                var transform = renderer.transform;
                while (transform != null)
                {
                    var component = transform.GetComponent<MaxTextureSize>();
                    if (component != null)
                    {
                        rendererToComponent[renderer] = component;
                        break;
                    }
                    transform = transform.parent;
                }
            }

            // Collect all materials and their textures with the required max size
            var textureToMaxSize = new Dictionary<Texture2D, int>();
            var materialsToProcess = new HashSet<Material>();

            foreach (var (renderer, component) in rendererToComponent)
            {
                var maxSize = (int)component.maxTextureSize;
                IEnumerable<Material> materials;

                // Use MeshInfo2 materials for SkinnedMeshRenderer
                if (renderer is SkinnedMeshRenderer skinnedMesh)
                {
                    var meshInfo = context.GetMeshInfoFor(skinnedMesh);
                    materials = meshInfo.SubMeshes.SelectMany(x => x.SharedMaterials);
                }
                else
                {
                    materials = renderer.sharedMaterials.Where(m => m != null);
                }

                // Also process animated materials
                var animationComponent = context.GetAnimationComponent(renderer);
                var animatedMaterials = Enumerable.Range(0, renderer.sharedMaterials.Length)
                    .SelectMany(i =>
                    {
                        var animation = animationComponent.GetObjectNode($"m_Materials.Array.data[{i}]");
                        return animation.Value.PossibleValues.OfType<Material>();
                    });

                materials = materials.Concat(animatedMaterials).Distinct();

                foreach (var material in materials)
                {
                    if (material == null) continue;
                    materialsToProcess.Add(material);

                    var propertyNames = material.GetTexturePropertyNames();
                    foreach (var propertyName in propertyNames)
                    {
                        var texture = material.GetTexture(propertyName);
                        if (texture is Texture2D texture2D)
                        {
                            if (!textureToMaxSize.ContainsKey(texture2D))
                            {
                                textureToMaxSize[texture2D] = maxSize;
                            }
                            else
                            {
                                // Use minimum size if texture is used by multiple materials with different limits
                                textureToMaxSize[texture2D] = Math.Min(textureToMaxSize[texture2D], maxSize);
                            }
                        }
                    }
                }
            }

            // Resize textures based on the collected size requirements
            var textureMapping = new Dictionary<Texture2D, Texture2D>();

            foreach (var (texture, maxSize) in textureToMaxSize)
            {
                var newTexture = ResizeTexture(texture, maxSize);
                if (newTexture != null)
                {
                    textureMapping[texture] = newTexture;
                }
            }

            // Replace textures in all processed materials
            foreach (var material in materialsToProcess)
            {
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

            // Skip crunched textures and report error
            if (GraphicsFormatUtility.IsCrunchFormat(original.format))
            {
                using (ErrorReport.WithContextObject(original))
                    BuildLog.LogWarning("MaxTextureSize:warning:crunchedNotSupported");
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
            return ExtractMipmapLevel(original, targetLevel, targetWidth, targetHeight);
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
