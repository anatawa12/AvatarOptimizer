using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Anatawa12.AvatarOptimizer.Test;

public class RemoveMeshByMaskAPITest
{
    [Test]
    public void TestInitializeMethod()
    {
        var gameObject = new GameObject();
        var renderer = gameObject.AddComponent<SkinnedMeshRenderer>();
        var component = gameObject.AddComponent<RemoveMeshByMask>();

        // Should not throw
        Assert.DoesNotThrow(() => component.Initialize(1));
    }

    [Test]
    public void TestInitializeWithUnsupportedVersion()
    {
        var gameObject = new GameObject();
        var renderer = gameObject.AddComponent<SkinnedMeshRenderer>();
        var component = gameObject.AddComponent<RemoveMeshByMask>();

        // Should throw for unsupported version
        Assert.Throws<ArgumentOutOfRangeException>(() => component.Initialize(999));
    }

    [Test]
    public void TestMaterialsProperty()
    {
        var gameObject = new GameObject();
        var renderer = gameObject.AddComponent<SkinnedMeshRenderer>();
        var component = gameObject.AddComponent<RemoveMeshByMask>();
        component.Initialize(1);

        // Create test material slots
        var materials = new RemoveMeshByMask.MaterialSlot[]
        {
            new RemoveMeshByMask.MaterialSlot
            {
                Enabled = true,
                Mask = null,
                Mode = RemoveMeshByMask.RemoveMode.RemoveBlack
            },
            new RemoveMeshByMask.MaterialSlot
            {
                Enabled = false,
                Mask = null,
                Mode = RemoveMeshByMask.RemoveMode.RemoveWhite
            }
        };

        // Set materials
        component.Materials = materials;

        // Get materials and verify
        var retrieved = component.Materials;
        Assert.That(retrieved.Length, Is.EqualTo(2));
        Assert.That(retrieved[0].Enabled, Is.True);
        Assert.That(retrieved[0].Mode, Is.EqualTo(RemoveMeshByMask.RemoveMode.RemoveBlack));
        Assert.That(retrieved[1].Enabled, Is.False);
        Assert.That(retrieved[1].Mode, Is.EqualTo(RemoveMeshByMask.RemoveMode.RemoveWhite));
    }

    [Test]
    public void TestMaterialSlotProperties()
    {
        var gameObject = new GameObject();
        var renderer = gameObject.AddComponent<SkinnedMeshRenderer>();

        var slot = new RemoveMeshByMask.MaterialSlot
        {
            Enabled = true,
            Mask = null,
            Mode = RemoveMeshByMask.RemoveMode.RemoveBlack
        };

        Assert.That(slot.Enabled, Is.True);
        Assert.That(slot.Mask, Is.Null);
        Assert.That(slot.Mode, Is.EqualTo(RemoveMeshByMask.RemoveMode.RemoveBlack));

        // Test setters
        slot.Enabled = false;
        slot.Mode = RemoveMeshByMask.RemoveMode.RemoveWhite;

        Assert.That(slot.Enabled, Is.False);
        Assert.That(slot.Mode, Is.EqualTo(RemoveMeshByMask.RemoveMode.RemoveWhite));
    }

    [Test]
    public void TestMaterialsPropertyReturnsClone()
    {
        var gameObject = new GameObject();
        var renderer = gameObject.AddComponent<SkinnedMeshRenderer>();
        var component = gameObject.AddComponent<RemoveMeshByMask>();
        component.Initialize(1);

        var materials = new RemoveMeshByMask.MaterialSlot[]
        {
            new RemoveMeshByMask.MaterialSlot
            {
                Enabled = true,
                Mask = null,
                Mode = RemoveMeshByMask.RemoveMode.RemoveBlack
            }
        };

        component.Materials = materials;

        // Modify the original array
        materials[0] = new RemoveMeshByMask.MaterialSlot
        {
            Enabled = false,
            Mask = null,
            Mode = RemoveMeshByMask.RemoveMode.RemoveWhite
        };

        // Retrieved should not be affected
        var retrieved = component.Materials;
        Assert.That(retrieved[0].Enabled, Is.True);
        Assert.That(retrieved[0].Mode, Is.EqualTo(RemoveMeshByMask.RemoveMode.RemoveBlack));
    }

    [Test]
    public void TestAPIUsageWarning()
    {
        var gameObject = new GameObject();
        var renderer = gameObject.AddComponent<SkinnedMeshRenderer>();
        var component = gameObject.AddComponent<RemoveMeshByMask>();

        // Using API before Initialize should log a warning
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*used before initialization.*"));
        var _ = component.Materials;
    }
}
