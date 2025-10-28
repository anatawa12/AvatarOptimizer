using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Test;

public class RemoveUnusedMaterialPropertiesTest
{
    [Test]
    [TestCase("FewUnusedProperties")]
    [TestCase("ManyUnusedProperties")]
    [TestCase("FewUnusedProperties")]
    public void RemoveUnusedProperties(string name)
    {
        var material = TestUtils.GetAssetAt<Material>("Basic/RemoveUnusedMaterialProperties/" + name + ".mat");
        material = Object.Instantiate(material);

        var expectedMainTex = TestUtils.GetAssetAt<Texture>("Basic/RemoveUnusedMaterialProperties/maintex.png");
        var expectedMainTex2nd = TestUtils.GetAssetAt<Texture>("Basic/RemoveUnusedMaterialProperties/maintex2nd.png");
        var expectedColor = new Color(1, 0, 0, 1);
        var expectedColor2nd = new Color(0, 1, 0, 1);

        // remove unused properties
        RemoveUnusedMaterialProperties.RemoveUnusedProperties(material);

        // check after removing
        Assert.That(material.GetTexture("_MainTex"), Is.EqualTo(expectedMainTex));
        Assert.That(material.GetTexture("_MainTex2nd"), Is.EqualTo(expectedMainTex2nd));
        Assert.That(material.GetColor("_Color"), Is.EqualTo(expectedColor));
        Assert.That(material.GetColor("_Color2nd"), Is.EqualTo(expectedColor2nd));

        Assert.That(GetMaterialPropertyNames(material),
            Is.EquivalentTo(new[] { "_MainTex", "_MainTex2nd", "_Color", "_Color2nd" }));
    }

    private IEnumerable<string> GetMaterialPropertyNames(Material material)
    {
        using var so = new SerializedObject(material);
        var properties = new[]
        {
            so.FindProperty("m_SavedProperties.m_TexEnvs"),
            so.FindProperty("m_SavedProperties.m_Floats"),
            so.FindProperty("m_SavedProperties.m_Colors"),
        };

        foreach (var property in properties)
        {
            if (property == null) continue;
            for (var i = 0; i < property.arraySize; i++)
            {
                var name = property.GetArrayElementAtIndex(i).FindPropertyRelative("first").stringValue;
                yield return name;
            }
        }
    }
}
