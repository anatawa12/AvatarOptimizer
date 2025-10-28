using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.Processors;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
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

    #region CreateIslands

    [Test]
    [TestCase(null)]
    [TestCase(TextureWrapMode.Repeat)]
    [TestCase(TextureWrapMode.Clamp)]
    [TestCase(TextureWrapMode.MirrorOnce)]
    [TestCase(TextureWrapMode.Mirror)]
    public void CreateIslands_OnBorder(TextureWrapMode? wrapMode)
    {
        using var meshInfo2 = GetMesh(BorderMesh);

        var result = OptimizeTextureImpl.CreateIslands(
            new[] { new OptimizeTextureImpl.UVID(meshInfo2, 0, UVChannel.UV0) },
            wrapMode, wrapMode);

        Assert.That(result, Is.Null);
    }


    [Test]

    // unknown tiling only supports no tiing
    [TestCase(TiledMinus1Minus1, null)]
    [TestCase(Tiled11, null)]
    [TestCase(Tiled23, null)]

    // clamp does not support any tiling
    [TestCase(TiledMinus1Minus1, TextureWrapMode.Clamp)]
    [TestCase(Tiled11, TextureWrapMode.Clamp)]
    [TestCase(Tiled23, TextureWrapMode.Clamp)]

    // mirror once supports 0 or -1 tiling
    [TestCase(Tiled11, TextureWrapMode.MirrorOnce)]
    [TestCase(Tiled23, TextureWrapMode.MirrorOnce)]

    public void CreateIslands_Fails(string generator, TextureWrapMode? wrapMode)
    {
        using var meshInfo2 = GetMesh(generator);

        var result = OptimizeTextureImpl.CreateIslands(
            new[] { new OptimizeTextureImpl.UVID(meshInfo2, 0, UVChannel.UV0) },
            wrapMode, wrapMode);

        Assert.That(result, Is.Null);
    }

    [Test]

    // unknown tiling only supports no tiing
    [TestCase(NoTilingMesh, null, 0, 0, false, false)]

    // repeat supports any tiling with no flip
    [TestCase(NoTilingMesh, TextureWrapMode.Repeat, 0, 0, false, false)]
    [TestCase(TiledMinus1Minus1, TextureWrapMode.Repeat, -1, -1, false, false)]
    [TestCase(Tiled11, TextureWrapMode.Repeat, 1, 1, false, false)]
    [TestCase(Tiled23, TextureWrapMode.Repeat, 2, 3, false, false)]

    // clamp does not support any tiling
    [TestCase(NoTilingMesh, TextureWrapMode.Clamp, 0, 0, false, false)]

    // mirror supports any tiling with good flip
    [TestCase(NoTilingMesh, TextureWrapMode.Mirror, 0, 0, false, false)]
    [TestCase(TiledMinus1Minus1, TextureWrapMode.Mirror, -1, -1, true, true)]
    [TestCase(Tiled11, TextureWrapMode.Mirror, 1, 1, true, true)]
    [TestCase(Tiled23, TextureWrapMode.Mirror, 2, 3, false, true)]

    // mirror once supports 0 or -1 tiling
    [TestCase(NoTilingMesh, TextureWrapMode.MirrorOnce, 0, 0, false, false)]
    [TestCase(TiledMinus1Minus1, TextureWrapMode.MirrorOnce, -1, -1, true, true)]

    public void CreateIslands_Success(string generator, TextureWrapMode? wrapMode, int tileU, int tileV, bool flipU, bool flipV)
    {
        using var meshInfo2 = GetMesh(generator);

        var result = OptimizeTextureImpl.CreateIslands(
            new[] { new OptimizeTextureImpl.UVID(meshInfo2, 0, UVChannel.UV0) },
            wrapMode, wrapMode);

        AssertIsland(result, tileU, tileV, flipU, flipV);
    }

    #region Helpers

    private MeshInfo2 GetMesh(string name)
    {
        var mesh = new Mesh();
        mesh.vertices = new Vector3[6];

        var uv = new Vector2[6];
        uv[0] = new Vector2(0.1f, 0.1f);
        uv[1] = new Vector2(0.4f, 0.1f);
        uv[2] = new Vector2(0.1f, 0.4f);

        switch (name)
        {
            case NoTilingMesh:
                uv[3] = new Vector2(0.6f, 0.6f);
                uv[4] = new Vector2(0.6f, 0.9f);
                uv[5] = new Vector2(0.9f, 0.9f);
                break;
            case BorderMesh:
                uv[3] = new Vector2(0.6f, 0.6f);
                uv[4] = new Vector2(0.6f, 1.1f);
                uv[5] = new Vector2(1.1f, 1.1f);
                break;
            case Tiled11:
                uv[3] = new Vector2(1.6f, 1.6f);
                uv[4] = new Vector2(1.6f, 1.9f);
                uv[5] = new Vector2(1.9f, 1.9f);
                break;
            case Tiled23:
                uv[3] = new Vector2(2.6f, 3.6f);
                uv[4] = new Vector2(2.6f, 3.9f);
                uv[5] = new Vector2(2.9f, 3.9f);
                break;
            case TiledMinus1Minus1:
                uv[3] = new Vector2(-0.6f, -0.6f);
                uv[4] = new Vector2(-0.6f, -0.9f);
                uv[5] = new Vector2(-0.9f, -0.9f);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(name));
        }

        mesh.uv = uv;
        mesh.triangles = new[] { 0, 1, 2, 3, 4, 5 };
        var renderer = TestUtils.NewSkinnedMeshRenderer(mesh);
        return new MeshInfo2(renderer);
    }

    private const string NoTilingMesh = nameof(NoTilingMesh);
    private const string BorderMesh = nameof(BorderMesh);
    private const string Tiled11 = nameof(Tiled11);
    private const string Tiled23 = nameof(Tiled23);
    private const string TiledMinus1Minus1 = nameof(TiledMinus1Minus1);

    void AssertIsland(List<OptimizeTextureImpl.AtlasIsland> result, 
        int tileU, int tileV, bool flipU, bool flipV)
    {
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].OriginalIslands.Count, Is.EqualTo(1));
        Assert.That(result[0].OriginalIslands[0].tileU, Is.EqualTo(0));
        Assert.That(result[0].OriginalIslands[0].tileV, Is.EqualTo(0));
        Assert.That(result[0].OriginalIslands[0].flipU, Is.False);
        Assert.That(result[0].OriginalIslands[0].flipV, Is.False);
        Assert.That(result[1].OriginalIslands.Count, Is.EqualTo(1));
        Assert.That(result[1].OriginalIslands[0].tileU, Is.EqualTo(tileU));
        Assert.That(result[1].OriginalIslands[0].tileV, Is.EqualTo(tileV));
        Assert.That(result[1].OriginalIslands[0].flipU, Is.EqualTo(flipU));
        Assert.That(result[1].OriginalIslands[0].flipV, Is.EqualTo(flipV));
    }

    #endregion

    #endregion
}