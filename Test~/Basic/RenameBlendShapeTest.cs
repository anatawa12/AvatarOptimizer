using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Test;

public class RenameBlendShapeTest
{
    [Test]
    public void TestDoRenameSimpleRename()
    {
        // prepare test mesh
        var cube = TestUtils.NewCubeMesh();
        cube.AddBlendShapeFrame("test0", 100, NewFrame((0, Vector3.up)), null, null);
        cube.AddBlendShapeFrame("test1", 100, NewFrame((0, Vector3.down)), null, null);
        cube.AddBlendShapeFrame("test2", 100, NewFrame((0, Vector3.left)), null, null);
        cube.AddBlendShapeFrame("test3", 100, NewFrame((0, Vector3.right)), null, null);

        var newRenderer = TestUtils.NewSkinnedMeshRenderer(cube);
        using var meshInfo2 = new MeshInfo2(newRenderer);

        // do process
        var mapping = new List<(string, float, string)>
        {
            ("renamed0", 0, "test0"),
            ("test1", 10, "test1"),
            ("renamed1", 20, "test2"),
            ("test3", 30, "test3"),
        };
        RenameBlendShapeProcessor.DoRenameBlendShapes(meshInfo2, mapping);

        // check
        var newMesh = new Mesh();
        meshInfo2.WriteToMesh(newMesh, isSkinnedMesh: true);

        Assert.That(newMesh.blendShapeCount, Is.EqualTo(4));

        // check names
        Assert.That(newMesh.GetBlendShapeName(0), Is.EqualTo("renamed0"));
        Assert.That(newMesh.GetBlendShapeName(1), Is.EqualTo("test1"));
        Assert.That(newMesh.GetBlendShapeName(2), Is.EqualTo("renamed1"));
        Assert.That(newMesh.GetBlendShapeName(3), Is.EqualTo("test3"));

        // check frame
        var frame = new Vector3[8];
        newMesh.GetBlendShapeFrameVertices(0, 0, frame, null, null);
        Assert.That(frame, Is.EqualTo(NewFrame((0, Vector3.up))));

        newMesh.GetBlendShapeFrameVertices(1, 0, frame, null, null);
        Assert.That(frame, Is.EqualTo(NewFrame((0, Vector3.down))));

        newMesh.GetBlendShapeFrameVertices(2, 0, frame, null, null);
        Assert.That(frame, Is.EqualTo(NewFrame((0, Vector3.left))));

        newMesh.GetBlendShapeFrameVertices(3, 0, frame, null, null);
        Assert.That(frame, Is.EqualTo(NewFrame((0, Vector3.right))));
    }

    [Test]
    public void TestCollectSimpleRename()
    {
        var mapping = RenameBlendShapeProcessor.CollectBlendShapeSources(new List<(string, float)>()
            {
                ("test0", 0),
                ("test1", 10),
                ("test2", 20),
                ("test3", 30),
            },
            new Dictionary<string, string>
            {
                { "test0", "renamed0" },
                { "test2", "renamed1" },
            });

        Assert.That(mapping, Is.EqualTo(new List<(string, float, string)>
        {
            ("renamed0", 0, "test0"),
            ("test1", 10, "test1"),
            ("renamed1", 20, "test2"),
            ("test3", 30, "test3"),
        }).UsingTupleAdapter());
    }

    [Test]
    public void TestDoRenameSwapName()
    {
        // prepare test mesh
        var cube = TestUtils.NewCubeMesh();
        cube.AddBlendShapeFrame("test0", 100, NewFrame((0, Vector3.up)), null, null);
        cube.AddBlendShapeFrame("test1", 100, NewFrame((0, Vector3.down)), null, null);
        cube.AddBlendShapeFrame("test2", 100, NewFrame((0, Vector3.left)), null, null);

        var newRenderer = TestUtils.NewSkinnedMeshRenderer(cube);
        using var meshInfo2 = new MeshInfo2(newRenderer);

        // do process
        var mapping = new List<(string, float, string)>
        {
            ("test1", 0, "test0"),
            ("test0", 10, "test1"),
            ("test2", 20, "test2"),
        };
        RenameBlendShapeProcessor.DoRenameBlendShapes(meshInfo2, mapping);

        // check
        var newMesh = new Mesh();
        meshInfo2.WriteToMesh(newMesh, isSkinnedMesh: true);

        Assert.That(newMesh.blendShapeCount, Is.EqualTo(3));

        // check names
        Assert.That(newMesh.GetBlendShapeName(0), Is.EqualTo("test1"));
        Assert.That(newMesh.GetBlendShapeName(1), Is.EqualTo("test0"));
        Assert.That(newMesh.GetBlendShapeName(2), Is.EqualTo("test2"));

        // check frame
        var frame = new Vector3[8];
        newMesh.GetBlendShapeFrameVertices(0, 0, frame, null, null);
        Assert.That(frame, Is.EqualTo(NewFrame((0, Vector3.up))));

        newMesh.GetBlendShapeFrameVertices(1, 0, frame, null, null);
        Assert.That(frame, Is.EqualTo(NewFrame((0, Vector3.down))));

        newMesh.GetBlendShapeFrameVertices(2, 0, frame, null, null);
        Assert.That(frame, Is.EqualTo(NewFrame((0, Vector3.left))));
    }

    [Test]
    public void TestCollectSwapName()
    {
        var mapping = RenameBlendShapeProcessor.CollectBlendShapeSources(new List<(string, float)>()
            {
                ("test0", 0),
                ("test1", 10),
                ("test2", 20),
            },
            new Dictionary<string, string>
            {
                { "test0", "test1" },
                { "test1", "test0" },
            });

        Assert.That(mapping, Is.EqualTo(new List<(string, float, string)>
        {
            ("test1", 0, "test0"),
            ("test0", 10, "test1"),
            ("test2", 20, "test2"),
        }).UsingTupleAdapter());
    }

    // merge feature is removed
    // https://github.com/anatawa12/AvatarOptimizer/issues/1250
    /*
    [Test]
    public void TestDoRenameRenameToMerge()
    {
        // prepare test mesh
        var cube = TestUtils.NewCubeMesh();
        cube.AddBlendShapeFrame("test0", 100, NewFrame((0, Vector3.up)), null, null);
        cube.AddBlendShapeFrame("test1", 100, NewFrame((0, Vector3.down)), null, null);
        cube.AddBlendShapeFrame("test2", 100, NewFrame((0, Vector3.left)), null, null);
        cube.AddBlendShapeFrame("test3", 100, NewFrame((0, Vector3.right)), null, null);

        var newRenderer = TestUtils.NewSkinnedMeshRenderer(cube);
        using var meshInfo2 = new MeshInfo2(newRenderer);

        // do process
        var mapping = new List<(string, float, List<string>)>
        {
            ("renamed0", 10, new List<string> { "test0", "test2" }),
            ("test1", 20, new List<string> { "test1" }),
            ("test3", 30, new List<string> { "test3" }),
        };
        RenameBlendShapeProcessor.DoRenameBlendShapes(meshInfo2, mapping);

        // check
        var newMesh = new Mesh();
        meshInfo2.WriteToMesh(newMesh, isSkinnedMesh: true);

        Assert.That(newMesh.blendShapeCount, Is.EqualTo(3));

        // check names
        Assert.That(newMesh.GetBlendShapeName(0), Is.EqualTo("renamed0").UsingTupleAdapter());
        Assert.That(newMesh.GetBlendShapeName(1), Is.EqualTo("test1").UsingTupleAdapter());
        Assert.That(newMesh.GetBlendShapeName(2), Is.EqualTo("test3").UsingTupleAdapter());

        // check frame
        var frame = new Vector3[8];
        newMesh.GetBlendShapeFrameVertices(0, 0, frame, null, null);
        Assert.That(frame, Is.EqualTo(NewFrame((0, Vector3.up + Vector3.left))));

        newMesh.GetBlendShapeFrameVertices(1, 0, frame, null, null);
        Assert.That(frame, Is.EqualTo(NewFrame((0, Vector3.down))));

        newMesh.GetBlendShapeFrameVertices(2, 0, frame, null, null);
        Assert.That(frame, Is.EqualTo(NewFrame((0, Vector3.right))));
    }

    [Test]
    public void TestCollectRenameToMerge()
    {
        var mapping = RenameBlendShapeProcessor.CollectBlendShapeSources(new List<(string, float)>()
            {
                ("test0", 10),
                ("test1", 20),
                ("test2", 10),
                ("test3", 30),
            },
            new Dictionary<string, string>
            {
                { "test0", "renamed0" },
                { "test2", "renamed0" },
            });

        Assert.That(mapping, Is.EqualTo(new List<(string, float, List<string>)>
        {
            ("renamed0", 10, new List<string> { "test0", "test2" }),
            ("test1", 20, new List<string> { "test1" }),
            ("test3", 30, new List<string> { "test3" }),
        }).UsingTupleAdapter());
    }
    */

    private static Vector3[] NewFrame(params (int index, Vector3 delta)[] deltas)
    {
        var frame = new Vector3[8];
        foreach (var (index, delta) in deltas) frame[index] = delta;
        return frame;
    }
}
