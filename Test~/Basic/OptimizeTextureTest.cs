using System.Linq;
using Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;
using NUnit.Framework;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Test;

public class OptimizeTextureTest
{
    [Test]
    public void TestAfterAtlasSizesSmallToBigGenerator()
    {
        // 0.0078125 = 1 / 128
        Assert.That(
            OptimizeTextureImpl.AfterAtlasSizesSmallToBigGenerator(0.007813f, new Vector2(0.05f, 0.05f)).ToArray(),
            Is.EqualTo(new[]
            {
                // size: 1 / 128
                new Vector2(0.0625f, 0.125f),
                new Vector2(0.125f, 0.0625f),

                // size: 1 / 64
                new Vector2(0.0625f, 0.25f),
                new Vector2(0.125f, 0.125f),
                new Vector2(0.25f, 0.0625f),

                // size: 1 / 32
                new Vector2(0.0625f, 0.5f),
                new Vector2(0.125f, 0.25f),
                new Vector2(0.25f, 0.125f),
                new Vector2(0.5f, 0.0625f),

                // size: 1 / 16
                new Vector2(0.0625f, 1.0f),
                new Vector2(0.125f, 0.5f),
                new Vector2(0.25f, 0.25f),
                new Vector2(0.5f, 0.125f),
                new Vector2(1.0f, 0.0625f),
                
                // size: 1 / 8
                new Vector2(0.125f, 1.0f),
                new Vector2(0.25f, 0.5f),
                new Vector2(0.5f, 0.25f),
                new Vector2(1.0f, 0.125f),
                
                // size: 1 / 4
                new Vector2(0.25f, 1.0f),
                new Vector2(0.5f, 0.5f),
                new Vector2(1.0f, 0.25f),
                
                // size: 1 / 2
                new Vector2(0.5f, 1.0f),
                new Vector2(1.0f, 0.5f),
            }));
    }
}