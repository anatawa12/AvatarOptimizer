using nadena.dev.ndmf;
using nadena.dev.ndmf.platform;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Test.E2E
{
    public class MaxTextureSizeTest
    {
        [Test]
        public void MaxTextureSize_ResizesLargeTextures()
        {
            // Create an avatar
            var avatar = TestUtils.NewAvatar();
            TestUtils.SetFxLayer(avatar, new AnimatorController());
            var rendererGO = new GameObject("Renderer");
            rendererGO.transform.SetParent(avatar.transform, false);
            var meshFilter = rendererGO.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = TestUtils.NewCubeMesh();
            var renderer = rendererGO.AddComponent<MeshRenderer>();

            // Create a large texture with mipmaps
            var largeTexture = new Texture2D(2048, 2048, TextureFormat.RGBA32, mipChain: true);
            for (var y = 0; y < 2048; y++)
            {
                for (var x = 0; x < 2048; x++)
                {
                    largeTexture.SetPixel(x, y, Color.white);
                }
            }
            largeTexture.Apply(updateMipmaps: true);
            largeTexture.name = "TestTexture";

            // Create a material with the texture
            var material = new Material(Shader.Find("Standard"));
            material.mainTexture = largeTexture;
            renderer.sharedMaterial = material;

            // Add MaxTextureSize component with max size 1024
            var maxTexSizeComponent = avatar.AddComponent<MaxTextureSize>();
            maxTexSizeComponent.maxTextureSize = MaxTextureSizeValue.Max1024;

            // Run NDMF
            var context = AvatarProcessor.ProcessAvatar(avatar, AmbientPlatform.DefaultPlatform);

            // Verify the texture was resized
            var resultTexture = renderer.sharedMaterial.mainTexture as Texture2D;
            Assert.IsNotNull(resultTexture, "Material should still have a texture");
            Assert.That(resultTexture.width, Is.LessThanOrEqualTo(1024), "Texture width should be <= 1024");
            Assert.That(resultTexture.height, Is.LessThanOrEqualTo(1024), "Texture height should be <= 1024");
            Assert.That(context.ErrorReport.Errors, Is.Empty);
        }

        [Test]
        public void MaxTextureSize_SkipsTexturesWithoutMipmaps()
        {
            // Create an avatar
            var avatar = TestUtils.NewAvatar();
            TestUtils.SetFxLayer(avatar, new AnimatorController());
            var rendererGO = new GameObject("Renderer");
            rendererGO.transform.SetParent(avatar.transform, false);
            var meshFilter = rendererGO.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = TestUtils.NewCubeMesh();
            var renderer = rendererGO.AddComponent<MeshRenderer>();

            // Create a large texture WITHOUT mipmaps
            var largeTexture = new Texture2D(2048, 2048, TextureFormat.RGBA32, mipChain: false);
            for (var y = 0; y < 2048; y++)
            {
                for (var x = 0; x < 2048; x++)
                {
                    largeTexture.SetPixel(x, y, Color.white);
                }
            }
            largeTexture.Apply(updateMipmaps: false);
            largeTexture.name = "TestTextureNoMips";

            // Create a material with the texture
            var material = new Material(Shader.Find("Standard"));
            material.mainTexture = largeTexture;
            renderer.sharedMaterial = material;

            // Add MaxTextureSize component with max size 1024
            var maxTexSizeComponent = avatar.AddComponent<MaxTextureSize>();
            maxTexSizeComponent.maxTextureSize = MaxTextureSizeValue.Max1024;

            // Run NDMF
            var context = AvatarProcessor.ProcessAvatar(avatar, AmbientPlatform.DefaultPlatform);

            // Verify the texture was NOT resized (since it has no mipmaps)
            var resultTexture = renderer.sharedMaterial.mainTexture as Texture2D;
            Assert.IsNotNull(resultTexture, "Material should still have a texture");
            // The texture should remain the same (not resized) since it has no mipmaps
            Assert.That(resultTexture.width, Is.EqualTo(2048), "Texture width should remain 2048 without mipmaps");
            Assert.That(resultTexture.height, Is.EqualTo(2048), "Texture height should remain 2048 without mipmaps");
            Assert.That(context.ErrorReport.Errors, Has.Count.EqualTo(1));
            Assert.That((context.ErrorReport.Errors[0].TheError as SimpleError)?.TitleKey, Is.EqualTo("MaxTextureSize:warning:insufficientMipmaps"));
        }

        [Test]
        public void MaxTextureSize_SkipsSmallTextures()
        {
            // Create an avatar
            var avatar = TestUtils.NewAvatar();
            TestUtils.SetFxLayer(avatar, new AnimatorController());
            var rendererGO = new GameObject("Renderer");
            rendererGO.transform.SetParent(avatar.transform, false);
            var meshFilter = rendererGO.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = TestUtils.NewCubeMesh();
            var renderer = rendererGO.AddComponent<MeshRenderer>();

            // Create a small texture with mipmaps
            var smallTexture = new Texture2D(512, 512, TextureFormat.RGBA32, mipChain: true);
            for (var y = 0; y < 512; y++)
            {
                for (var x = 0; x < 512; x++)
                {
                    smallTexture.SetPixel(x, y, Color.white);
                }
            }
            smallTexture.Apply(updateMipmaps: true);
            smallTexture.name = "SmallTexture";

            // Create a material with the texture
            var material = new Material(Shader.Find("Standard"));
            material.mainTexture = smallTexture;
            renderer.sharedMaterial = material;

            // Add MaxTextureSize component with max size 1024
            var maxTexSizeComponent = avatar.AddComponent<MaxTextureSize>();
            maxTexSizeComponent.maxTextureSize = MaxTextureSizeValue.Max1024;

            // Run NDMF
            var context = AvatarProcessor.ProcessAvatar(avatar, AmbientPlatform.DefaultPlatform);

            // Verify the texture was NOT resized (since it's already small enough)
            var resultTexture = renderer.sharedMaterial.mainTexture as Texture2D;
            Assert.IsNotNull(resultTexture, "Material should still have a texture");
            Assert.That(resultTexture.width, Is.EqualTo(512), "Texture width should remain 512");
            Assert.That(resultTexture.height, Is.EqualTo(512), "Texture height should remain 512");
            Assert.That(context.ErrorReport.Errors, Is.Empty);
        }
    }
}
