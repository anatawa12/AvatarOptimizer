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
            var maxTextureSizeComponents = context.GetComponents<MaxTextureSize>();
            if (maxTextureSizeComponents.Length == 0) return;

            var avatarRoot = context.AvatarRootTransform;

            // Build a map of renderer to its closest parent MaxTextureSize component
            var rendererToComponent = new Dictionary<Renderer, MaxTextureSize>();
            
            foreach (var renderer in context.GetComponents<Renderer>())
            {
                foreach (var transform in renderer.transform.ParentEnumerable(avatarRoot.parent, includeMe: true))
                {
                    var component = transform.GetComponent<MaxTextureSize>();
                    if (component != null)
                    {
                        rendererToComponent[renderer] = component;
                        break;
                    }
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
                var meshInfo = context.TryGetMeshInfoFor(renderer);
                if (meshInfo != null)
                {
                    materials = meshInfo.SubMeshes.SelectMany(x => x.SharedMaterials).Where(m => m != null)!;
                }
                else
                {
                    materials = renderer.sharedMaterials.Where(m => m != null);
                }

                // Also process animated materials using GetAllObjectProperties
                var animationComponent = context.GetAnimationComponent(renderer);
                var animatedMaterials = animationComponent.GetAllObjectProperties()
                    .SelectMany(x => x.node.Value.PossibleValues)
                    .OfType<Material>();

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
                            if (!textureToMaxSize.TryGetValue(texture2D, out int existingMax))
                            {
                                textureToMaxSize[texture2D] = maxSize;
                            }
                            else
                            {
                                // Use maximum size if texture is used by multiple materials with different limits
                                textureToMaxSize[texture2D] = Math.Max(existingMax, maxSize);
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

            while (currentWidth > maxSize)
            {
                currentWidth /= 2;
                widthLevel++;
            }

            while (currentHeight > maxSize)
            {
                currentHeight /= 2;
                heightLevel++;
            }

            var removeLevels = Math.Max(widthLevel, heightLevel);
            if (removeLevels == 0)
                return null;

            // Skip if texture doesn't have mipmaps
            if (original.mipmapCount <= removeLevels)
            {
                using (ErrorReport.WithContextObject(original))
                    BuildLog.LogWarning("MaxTextureSize:warning:insufficientMipmaps");
                return null;
            }

            var targetWidth = original.width >> removeLevels;
            var targetHeight = original.height >> removeLevels;

            // Extract the specific mipmap level directly from texture data
            return ExtractMipmapLevel(original, removeLevels, targetWidth, targetHeight);
        }

        private static Texture2D? ExtractMipmapLevel(Texture2D original, int removeLevels, int width, int height)
        {
            // Get readable version of texture (use Graphics.CopyTexture if not readable)
            Texture2D readableVersion;
            if (original.isReadable)
            {
                readableVersion = original;
            }
            else
            {
                readableVersion = new Texture2D(original.width, original.height, original.format, original.mipmapCount, !original.isDataSRGB);
                Graphics.CopyTexture(original, readableVersion);
                readableVersion.Apply(false);
            }
            var mipCount = original.mipmapCount - removeLevels;
            
            // Calculate number of mip levels before target level
            var offset = 0;
            for (var i = 0; i < removeLevels; i++)
            {
                var mipWidth = Math.Max(1, original.width >> i);
                var mipHeight = Math.Max(1, original.height >> i);
                offset += (int)GraphicsFormatUtility.ComputeMipmapSize(mipWidth, mipHeight, original.format);
            }
            
            // Get the raw data for the specific mipmap level using span
            var sourceData = readableVersion.GetRawTextureData<byte>();

            // Create a new texture with the same format
            // We previously used GraphicsFormat-based overload of Texture2D constructor since GraphicsFormat provides exact information of texture format,
            // which contains format and isSRGB information, while TextureFormat requires additional boolean to specify whether it's linear or srgb.
            // However, GraphicsFormat based constructor internally uses SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Sample) to check if format is valid,
            // but it returns information of current platform (editor platform) instead of target platform.
            // Therefore, on Windows Editor, it returns false for ASTC formats, which are vaild for android target.
            // To avoid this issue, we switched back to TextureFormat-based constructor with linear boolean parameter,
            // and manually set linear or srgb format based on original texture's isDataSRGB property.
            //
            // The behavior difference is not documented on Script Reference [1], but there is comment suggesting
            // behavior difference at ValidateFormat() in Reference Source [2,3].
            // [2]
            // > // *ONLY* GPU support is checked here. If it is not available, fail validation.
            // > // GraphicsFormat does not use fallbacks by design.
            // [3]
            // > // If GPU support is detected, pass validation. Caveat support can also be used for uncompressed/non-IEEE754 formats.
            // > // In contrast to GraphicsFormat, TextureFormat can use fallbacks by design.
            // [1]: https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Texture2D-ctor.html
            // [2]: https://github.com/Unity-Technologies/UnityCsReference/blob/6000.1.6f1/Runtime/Export/Graphics/Texture.cs#L739-L740
            // [3]: https://github.com/Unity-Technologies/UnityCsReference/blob/6000.1.6f1/Runtime/Export/Graphics/Texture.cs#L718-L719
            var newTexture = new Texture2D(width, height, original.format, mipCount: mipCount, linear: !original.isDataSRGB);
            newTexture.LoadRawTextureData(sourceData.AsSpan()[offset..]);
            newTexture.Apply(updateMipmaps: false, makeNoLongerReadable: !original.isReadable);
            
            // Clean up temporary readable version if we created one
            if (!original.isReadable)
            {
                UnityEngine.Object.DestroyImmediate(readableVersion);
            }
            
            // Copy texture settings
            newTexture.wrapModeU = original.wrapModeU;
            newTexture.wrapModeV = original.wrapModeV;
            newTexture.filterMode = original.filterMode;
            newTexture.anisoLevel = original.anisoLevel;
            newTexture.mipMapBias = original.mipMapBias;
            newTexture.SetStreamingMipMapSettings(original.GetStreamingMipMapSettings());
            newTexture.name = original.name + " (MaxTextureSize)";
            
            return newTexture;
        }
    }
}
