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

        public static IEnumerable<TextureFormat> AllTextureFormats()
        {
            return Enum.GetValues(typeof(TextureFormat)).Cast<TextureFormat>().Where(x => (int)x > 0);
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

        // (float bits, width, expected) — expected = floor(frac(x) * width) computed exactly
        public static IEnumerable<TestCaseData> UvToPixelTestCases()
        {
            float F(uint p0) => BitConverter.Int32BitsToSingle((int)p0);
            
            TestCaseData Case(float f, int i, int exp, string desc) => new TestCaseData(f, i, exp).SetName($"UvToPixel: {desc} ({f:G9}, {i}) = {exp}");

            // --- group 1: tiny negative x, frac rounds to 1.0 → naive returns width ---
            yield return Case(F(0x80000001u),     3,     2, "-smallest subnormal; breaks f32 AND f64 naive (both give 3)");
            yield return Case(F(0x80000001u), 32768, 32767, "same, w=32768 (naive: 32768)");
            yield return Case(F(0x8DA24260u), 32768, 32767, "-1e-30f; breaks f32 and f64 naive (32768)");
            yield return Case(F(0xB0800000u),     3,     2, "-2^-30; f32 naive gives 3");
            yield return Case(F(0xB0800000u), 32768, 32767, "-2^-30; f32 naive gives 32768");
            yield return Case(F(0xB3000000u),  1000,   999, "-2^-25; tie rounds 1-2^-25 to 1.0 in f32 (naive: 1000)");

            // --- group 2: x just below k/w, fp32 product rounds up to k ---
            yield return Case(F(0x3C23D70Au),   100,     0, "0.01f < 0.01; f32 naive gives 1");
            yield return Case(F(0x3EA8F5C2u),   100,    32, "just below 0.33; f32 naive gives 33");
            yield return Case(F(0x3EAAA955u), 32767, 10921, "just below 10921/32767; f32 naive gives 10922");
            yield return Case(F(0x38000100u), 32767,     0, "just below 1/32767; f32 naive gives 1");
            yield return Case(F(0x3A802008u),  1023,     0, "just below 1/1023; f32 naive gives 1");

            // --- controls: naive is correct here, but they pin down the semantics ---
            yield return Case(F(0xB3800000u),  1000,   999, "-2^-24 (1-2^-24 representable)");
            yield return Case(F(0x3F7FFFFFu),     3,     2, "1-2^-24 (positive N-eps)");
            yield return Case(F(0x3F7FFFFFu), 32768, 32767, "1-2^-24");
            yield return Case(F(0x47C34FFFu), 32768, 32512, "100000 - ulp");
            yield return Case(F(0xBF800001u), 32768, 32767, "-1 - ulp -> frac = 1-2^-23");
            yield return Case(F(0xBF7FFFFFu), 32768,     0, "-1 + ulp -> frac = 2^-24");
            yield return Case(F(0x4AFFFFFFu),     2,     1, "8388607.5 (last non-integer binade)");
            yield return Case(F(0xCAFFFFFFu),     2,     1, "-8388607.5");
            yield return Case(F(0x4B800000u), 32768,     0, "2^24 (integer)");
            yield return Case(F(0x80000000u), 32768,     0, "-0");            
        }

        [Test]
        [TestCaseSource(nameof(UvToPixelTestCases))]
        public void UvToPixel(float u, int width, int result)
        {
            Assert.That(Utils.UvToPixel(u, width), Is.EqualTo(result));
        }
    }
}
