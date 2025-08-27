using System.Linq;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using Anatawa12.AvatarOptimizer.ndmf;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using nadena.dev.ndmf;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;
using static Anatawa12.AvatarOptimizer.Test.AnimatorControllerGeneratorStatics;

namespace Anatawa12.AvatarOptimizer.Test;

public class AutoFreezeBlendShapeTest
{
    [Test]
    public void BasicTest()
    {
        #region setup

        // setup
        var avatar = TestUtils.NewAvatar();
        var renderer0GameObject = Utils.NewGameObject("Renderer0", avatar.transform);
        var renderer0 = renderer0GameObject.AddComponent<SkinnedMeshRenderer>();

        var mesh0 = TestUtils.NewCubeMesh();
        mesh0.AddBlendShapeFrame("mesh0", 100, TestUtils.NewCubeBlendShapeFrame((0, Vector3.up)), null, null);
        renderer0.sharedMesh = mesh0;

        TestUtils.SetFxLayer(avatar, BuildAnimatorController("").Build());

        #endregion

        var context = new BuildContext(avatar, null);

        context.ActivateExtensionContext<Processors.MeshInfo2Context>();
        context.ActivateExtensionContext<ObjectMappingContext>();
        context.ActivateExtensionContext<DestroyTracker.ExtensionContext>();
        context.GetMappingBuilder()
            .ImportModifications(new AnimatorParser(true).GatherAnimationModifications(context));
        context.GetState<AAOEnabled>().Enabled = true;
        context.ActivateExtensionContext<GCComponentInfoContext>();

        var autoFreeze = renderer0GameObject.AddComponent<InternalAutoFreezeNonAnimatedBlendShapes>();
        var freezeComponent = renderer0GameObject.AddComponent<FreezeBlendShape>();
        new InternalAutoFreezeNonAnimatedBlendShapesProcessor(autoFreeze)
            .Process(context, context.GetMeshInfoFor(renderer0));
        new FreezeBlendShapeProcessor(freezeComponent)
            .Process(context, context.GetMeshInfoFor(renderer0));

        context.DeactivateAllExtensionContexts();

        Assert.That(renderer0.sharedMesh.blendShapeCount, Is.EqualTo(0));
    }
    
    [Test]
    public void AnimatedToConstant()
    {
        #region setup

        // setup
        var avatar = TestUtils.NewAvatar();
        var renderer0GameObject = Utils.NewGameObject("Renderer0", avatar.transform);
        var renderer0 = renderer0GameObject.AddComponent<SkinnedMeshRenderer>();

        var mesh0 = TestUtils.NewCubeMesh();
        var frameDelta = TestUtils.NewCubeBlendShapeFrame((0, Vector3.up));
        mesh0.AddBlendShapeFrame("mesh0", 100, frameDelta, null, null);
        renderer0.sharedMesh = mesh0;

        TestUtils.SetFxLayer(avatar, BuildAnimatorController("")
            .AddLayer("base", layer => 
                layer.NewClipState("", c => c
                        .AddPropertyBinding("Renderer0", typeof(SkinnedMeshRenderer), "blendShape.mesh0",
                            AnimationCurve.Constant(0, 1, 100))))
            .Build());

        #endregion

        var context = new BuildContext(avatar, null);

        context.ActivateExtensionContext<Processors.MeshInfo2Context>();
        context.ActivateExtensionContext<ObjectMappingContext>();
        context.ActivateExtensionContext<DestroyTracker.ExtensionContext>();
        context.GetMappingBuilder()
            .ImportModifications(new AnimatorParser(true).GatherAnimationModifications(context));
        context.GetState<AAOEnabled>().Enabled = true;
        context.ActivateExtensionContext<GCComponentInfoContext>();

        var autoFreeze = renderer0GameObject.AddComponent<InternalAutoFreezeNonAnimatedBlendShapes>();
        var freezeComponent = renderer0GameObject.AddComponent<FreezeBlendShape>();
        new InternalAutoFreezeNonAnimatedBlendShapesProcessor(autoFreeze)
            .Process(context, context.GetMeshInfoFor(renderer0));
        new FreezeBlendShapeProcessor(freezeComponent)
            .Process(context, context.GetMeshInfoFor(renderer0));

        context.DeactivateAllExtensionContexts();

        Assert.That(renderer0.sharedMesh.blendShapeCount, Is.EqualTo(0));
        var vertices = renderer0.sharedMesh.vertices;
        Assert.That(vertices, Is.EqualTo(mesh0.vertices.Select((v, i) => v + frameDelta[i]).ToArray()));
    }
    
    [Test]
    public void AnimatedToDifferentConstantWithPartialWeight()
    {
        #region setup

        // setup
        var avatar = TestUtils.NewAvatar();
        var renderer0GameObject = Utils.NewGameObject("Renderer0", avatar.transform);
        var renderer0 = renderer0GameObject.AddComponent<SkinnedMeshRenderer>();

        var mesh0 = TestUtils.NewCubeMesh();
        var frameDelta = TestUtils.NewCubeBlendShapeFrame((0, Vector3.up));
        mesh0.AddBlendShapeFrame("mesh0", 100, frameDelta, null, null);
        renderer0.sharedMesh = mesh0;

        TestUtils.SetFxLayer(avatar, BuildAnimatorController("")
            .AddLayer("base", _ => {})
            .AddLayer("first", 0.5f, layer => 
                layer.NewClipState("", c => c
                        .AddPropertyBinding("Renderer0", typeof(SkinnedMeshRenderer), "blendShape.mesh0",
                            AnimationCurve.Constant(0, 1, 100))))
            .Build());

        #endregion

        var context = new BuildContext(avatar, null);

        context.ActivateExtensionContext<Processors.MeshInfo2Context>();
        context.ActivateExtensionContext<ObjectMappingContext>();
        context.ActivateExtensionContext<DestroyTracker.ExtensionContext>();
        context.GetMappingBuilder()
            .ImportModifications(new AnimatorParser(true).GatherAnimationModifications(context));
        context.GetState<AAOEnabled>().Enabled = true;
        context.ActivateExtensionContext<GCComponentInfoContext>();

        var autoFreeze = renderer0GameObject.AddComponent<InternalAutoFreezeNonAnimatedBlendShapes>();
        var freezeComponent = renderer0GameObject.AddComponent<FreezeBlendShape>();
        new InternalAutoFreezeNonAnimatedBlendShapesProcessor(autoFreeze)
            .Process(context, context.GetMeshInfoFor(renderer0));
        new FreezeBlendShapeProcessor(freezeComponent)
            .Process(context, context.GetMeshInfoFor(renderer0));

        context.DeactivateAllExtensionContexts();

        Assert.That(renderer0.sharedMesh.blendShapeCount, Is.EqualTo(1));
    }
    
    [Test]
    public void AnimatedToDefaultConstantWithPartialWeight()
    {
        #region setup

        // setup
        var avatar = TestUtils.NewAvatar();
        var renderer0GameObject = Utils.NewGameObject("Renderer0", avatar.transform);
        var renderer0 = renderer0GameObject.AddComponent<SkinnedMeshRenderer>();

        var mesh0 = TestUtils.NewCubeMesh();
        var frameDelta = TestUtils.NewCubeBlendShapeFrame((0, Vector3.up));
        mesh0.AddBlendShapeFrame("mesh0", 100, frameDelta, null, null);
        renderer0.sharedMesh = mesh0;
        renderer0.SetBlendShapeWeight(0, 100);

        TestUtils.SetFxLayer(avatar, BuildAnimatorController("")
            .AddLayer("base", _ => {})
            .AddLayer("first", 0.5f, layer => 
                layer.NewClipState("", c => c
                        .AddPropertyBinding("Renderer0", typeof(SkinnedMeshRenderer), "blendShape.mesh0",
                            AnimationCurve.Constant(0, 1, 100))))
            .Build());

        #endregion

        var context = new BuildContext(avatar, null);

        context.ActivateExtensionContext<Processors.MeshInfo2Context>();
        context.ActivateExtensionContext<ObjectMappingContext>();
        context.ActivateExtensionContext<DestroyTracker.ExtensionContext>();
        context.GetMappingBuilder()
            .ImportModifications(new AnimatorParser(true).GatherAnimationModifications(context));
        context.GetState<AAOEnabled>().Enabled = true;
        context.ActivateExtensionContext<GCComponentInfoContext>();

        var autoFreeze = renderer0GameObject.AddComponent<InternalAutoFreezeNonAnimatedBlendShapes>();
        var freezeComponent = renderer0GameObject.AddComponent<FreezeBlendShape>();
        new InternalAutoFreezeNonAnimatedBlendShapesProcessor(autoFreeze)
            .Process(context, context.GetMeshInfoFor(renderer0));
        new FreezeBlendShapeProcessor(freezeComponent)
            .Process(context, context.GetMeshInfoFor(renderer0));

        context.DeactivateAllExtensionContexts();

        Assert.That(renderer0.sharedMesh.blendShapeCount, Is.EqualTo(0));
    }
}