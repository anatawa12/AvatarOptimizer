using Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;
using NUnit.Framework;
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

        // check before removing
        //Assert.That(material.GetTexture("_MainTex"), Is.EqualTo(expectedMainTex));
        //Assert.That(material.GetTexture("_MainTex2nd"), Is.EqualTo(expectedMainTex2nd));
        //Assert.That(material.GetColor("_Color"), Is.EqualTo(expectedColor));
        //Assert.That(material.GetColor("_Color2nd"), Is.EqualTo(expectedColor2nd));

        // remove unused properties
        RemoveUnusedMaterialProperties.RemoveUnusedProperties(material);

        // check after removing
        Assert.That(material.GetTexture("_MainTex"), Is.EqualTo(expectedMainTex));
        Assert.That(material.GetTexture("_MainTex2nd"), Is.EqualTo(expectedMainTex2nd));
        Assert.That(material.GetColor("_Color"), Is.EqualTo(expectedColor));
        Assert.That(material.GetColor("_Color2nd"), Is.EqualTo(expectedColor2nd));
    }
}
