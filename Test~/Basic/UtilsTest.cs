using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Anatawa12.AvatarOptimizer.Test
{
    public class UtilsTest
    {
        #region FindSubProps

        [Test]
        public void FindSubProps()
        {
            Assert.That(Utils.FindSubPaths("", '.').ToList(), Is.EquivalentTo(new [] { ("", "") }));
            Assert.That(Utils.FindSubPaths("test", '.').ToList(), 
                Is.EquivalentTo(new [] { ("test", "") }));

            Assert.That(Utils.FindSubPaths("test.collection", '.').ToList(), Is.EquivalentTo(new []
            {
                ("test.collection", ""),
                ("test", ".collection"),
            }));
            
            Assert.That(Utils.FindSubPaths("test.collection.sub", '.').ToList(), Is.EquivalentTo(new []
            {
                ("test.collection.sub", ""),
                ("test.collection", ".sub"),
                ("test", ".collection.sub"),
            }));
        }

        #endregion

        #region TextureFormat

        private const TextureFormat InvalidTextureFormat = (TextureFormat)(-127);

        public static IEnumerable<TextureFormat> AllTextureFormats()
        {
            return Enum.GetValues(typeof(TextureFormat)).Cast<TextureFormat>().Where(x => x != 0 && x != InvalidTextureFormat);
        }

        public static IEnumerable<GraphicsFormat> GraphicsFormatsForAllTextureFormats() =>
            AllTextureFormats()
                .SelectMany(format => new []
                {
                    GraphicsFormatUtility.GetGraphicsFormat(format, isSRGB: true),
                    GraphicsFormatUtility.GetGraphicsFormat(format, isSRGB: false)
                })
                .Distinct();

        [Test]
        [TestCaseSource(nameof(AllTextureFormats))]
        public void GetRenderingFormatForTextureReturnsValidFormat(TextureFormat format)
        {
            foreach (var isSrgb in new [] { true, false })
            {
                var graphicsFormat = GraphicsFormatUtility.GetGraphicsFormat(format, isSRGB: isSrgb);
                if (graphicsFormat == GraphicsFormat.None || GraphicsFormatUtility.IsSRGBFormat(graphicsFormat) != isSrgb) continue;
             
                var result = Utils.GetRenderingFormatForTexture(format, isSRGB: isSrgb);
                Assert.That(result, Is.Not.EqualTo(GraphicsFormat.None), 
                    $"Getting Rendering Format for TextureFormat {format} returned None. This is problem of Avatar Optimizer, please report this issue.");   
            }
        }

        [Test]
        [TestCaseSource(nameof(GraphicsFormatsForAllTextureFormats))]
        public void GetTextureFormatForReadingReturnsValidFormat(GraphicsFormat format)
        {
            var result = Utils.GetTextureFormatForReading(format);
            Assert.That(result, Is.Not.EqualTo(0), 
                $"Getting Texture Format for ReadPixels for GraphicsFormat {format} returned None. This is problem of Avatar Optimizer, please report this issue.");
        }

        #endregion
    }
}
