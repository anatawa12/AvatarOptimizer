using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using Anatawa12.AvatarOptimizer.ndmf;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using nadena.dev.ndmf;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;
using static Anatawa12.AvatarOptimizer.Test.AnimatorControllerGeneratorStatics;

namespace Anatawa12.AvatarOptimizer.Test;

public class MergeSkinnedMeshTest
{
    [Test]
    public void CopySourceAnimation()
    {
        #region setup

        // setup
        var avatar = TestUtils.NewAvatar();
        var renderer0GameObject = Utils.NewGameObject("Renderer0", avatar.transform);
        var renderer1GameObject = Utils.NewGameObject("Renderer1", avatar.transform);
        var renderer0 = renderer0GameObject.AddComponent<SkinnedMeshRenderer>();
        var renderer1 = renderer1GameObject.AddComponent<SkinnedMeshRenderer>();
        var mergedGameObject = Utils.NewGameObject("Merged", avatar.transform);
        var mergedRenderer = mergedGameObject.AddComponent<SkinnedMeshRenderer>();
        var merged = mergedGameObject.AddComponent<MergeSkinnedMesh>();

        merged.copyEnablementAnimation = true;
        merged.renderersSet.AddRange(new[] { renderer0, renderer1 });

        mergedRenderer.rootBone = mergedRenderer.transform;
        mergedRenderer.probeAnchor = mergedRenderer.transform;

        TestUtils.SetFxLayer(avatar, BuildAnimatorController("FX Layer")
            .AddLayer("Base Layer", sm => sm
                .NewClipState("State", s => s
                    .AddPropertyBinding(renderer0.name, typeof(GameObject), "m_IsActive",
                        new Keyframe(0, 0),
                        new Keyframe(0, 1))
                    .AddPropertyBinding(renderer1.name, typeof(SkinnedMeshRenderer), "m_Enabled",
                        new Keyframe(0, 0),
                        new Keyframe(0, 1))
                    .Build())
            )
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

        LogTestUtility.Test(_ =>
        {
            new MergeSkinnedMeshProcessor(merged).Process(context, context.GetMeshInfoFor(mergedRenderer));
        });

        var mapping = context.GetMappingBuilder().BuildObjectMapping();
        var animatorMapper = mapping.CreateAnimationMapper(avatar);
        var mapped = animatorMapper.MapBinding("Renderer0", typeof(GameObject), "m_IsActive");

        Assert.That(mapped, Is.Not.Null.And.Contains((merged.name, typeof(SkinnedMeshRenderer), "m_Enabled", 0)));
    }

    [Test]
    public void CopySourceAnimationErrorMergedEnablementAnimated()
    {
        #region setup

        // setup
        var avatar = TestUtils.NewAvatar();
        var renderer0GameObject = Utils.NewGameObject("Renderer0", avatar.transform);
        var renderer1GameObject = Utils.NewGameObject("Renderer1", avatar.transform);
        var renderer0 = renderer0GameObject.AddComponent<SkinnedMeshRenderer>();
        var renderer1 = renderer1GameObject.AddComponent<SkinnedMeshRenderer>();
        var mergedGameObject = Utils.NewGameObject("Merged", avatar.transform);
        var mergedRenderer = mergedGameObject.AddComponent<SkinnedMeshRenderer>();
        var merged = mergedGameObject.AddComponent<MergeSkinnedMesh>();

        merged.copyEnablementAnimation = true;
        merged.renderersSet.AddRange(new[] { renderer0, renderer1 });

        mergedRenderer.rootBone = mergedRenderer.transform;
        mergedRenderer.probeAnchor = mergedRenderer.transform;

        TestUtils.SetFxLayer(avatar, BuildAnimatorController("FX Layer")
            .AddLayer("Base Layer", sm => sm
                .NewClipState("State", s => s
                    .AddPropertyBinding(renderer0.name, typeof(GameObject), "m_IsActive",
                        new Keyframe(0, 0),
                        new Keyframe(0, 1))
                    .AddPropertyBinding(renderer1.name, typeof(SkinnedMeshRenderer), "m_Enabled",
                        new Keyframe(0, 0),
                        new Keyframe(0, 1))
                    .Build())
                .NewClipState("State2", s => s
                    .AddPropertyBinding("Merged", typeof(SkinnedMeshRenderer), "m_Enabled",
                        new Keyframe(0, 0),
                        new Keyframe(0, 1))
                    .Build())
            )
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

        LogTestUtility.Test(scope =>
        {
            scope.ExpectError(ErrorSeverity.Error,
                "MergeSkinnedMesh:copy-enablement-animation:error:enablement-of-merged-mesh-is-animated");

            new MergeSkinnedMeshProcessor(merged).Process(context, context.GetMeshInfoFor(mergedRenderer));
        });

        var mapping = context.GetMappingBuilder().BuildObjectMapping();
        var animatorMapper = mapping.CreateAnimationMapper(avatar);
        var mapped = animatorMapper.MapBinding("Merged", typeof(SkinnedMeshRenderer), "m_Enabled");

        Assert.That(mapped, Is.Not.Null.And.Empty);
    }

    [Test]
    public void CopySourceAnimationErrorTooManyActiveness()
    {
        #region setup

        // setup
        var avatar = TestUtils.NewAvatar();
        var renderer0GameObject = Utils.NewGameObject("Renderer0", avatar.transform);
        var renderer1GameObject = Utils.NewGameObject("Renderer1", avatar.transform);
        var renderer0 = renderer0GameObject.AddComponent<SkinnedMeshRenderer>();
        var renderer1 = renderer1GameObject.AddComponent<SkinnedMeshRenderer>();
        var mergedGameObject = Utils.NewGameObject("Merged", avatar.transform);
        var mergedRenderer = mergedGameObject.AddComponent<SkinnedMeshRenderer>();
        var merged = mergedGameObject.AddComponent<MergeSkinnedMesh>();

        merged.copyEnablementAnimation = true;
        merged.renderersSet.AddRange(new[] { renderer0, renderer1 });

        mergedRenderer.rootBone = mergedRenderer.transform;
        mergedRenderer.probeAnchor = mergedRenderer.transform;

        TestUtils.SetFxLayer(avatar, BuildAnimatorController("FX Layer")
            .AddLayer("Base Layer", sm => sm
                .NewClipState("State", s => s
                    .AddPropertyBinding("Renderer0", typeof(GameObject), "m_IsActive",
                        new Keyframe(0, 0),
                        new Keyframe(0, 1))
                    .AddPropertyBinding("Renderer1", typeof(SkinnedMeshRenderer), "m_Enabled",
                        new Keyframe(0, 0),
                        new Keyframe(0, 1))
                    .Build())
                .NewClipState("State2", s => s
                    .AddPropertyBinding("Renderer0", typeof(SkinnedMeshRenderer), "m_Enabled",
                        new Keyframe(0, 0),
                        new Keyframe(0, 1))
                    .Build())
            )
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

        LogTestUtility.Test(scope =>
        {
            scope.ExpectError(ErrorSeverity.Error,
                "MergeSkinnedMesh:copy-enablement-animation:error:too-many-activeness-animation");

            new MergeSkinnedMeshProcessor(merged).Process(context, context.GetMeshInfoFor(mergedRenderer));
        });
    }

    [Test]
    public void CopySourceAnimationErrorActivenessAnimationOfSourceMismatch()
    {
        #region setup

        // setup
        var avatar = TestUtils.NewAvatar();
        var renderer0GameObject = Utils.NewGameObject("Renderer0", avatar.transform);
        var renderer1GameObject = Utils.NewGameObject("Renderer1", avatar.transform);
        var renderer0 = renderer0GameObject.AddComponent<SkinnedMeshRenderer>();
        var renderer1 = renderer1GameObject.AddComponent<SkinnedMeshRenderer>();
        var mergedGameObject = Utils.NewGameObject("Merged", avatar.transform);
        var mergedRenderer = mergedGameObject.AddComponent<SkinnedMeshRenderer>();
        var merged = mergedGameObject.AddComponent<MergeSkinnedMesh>();

        merged.copyEnablementAnimation = true;
        merged.renderersSet.AddRange(new[] { renderer0, renderer1 });

        mergedRenderer.rootBone = mergedRenderer.transform;
        mergedRenderer.probeAnchor = mergedRenderer.transform;

        TestUtils.SetFxLayer(avatar, BuildAnimatorController("FX Layer")
            .AddLayer("Base Layer", sm => sm
                .NewClipState("State", s => s
                    .AddPropertyBinding("Renderer1", typeof(SkinnedMeshRenderer), "m_Enabled",
                        new Keyframe(0, 0),
                        new Keyframe(0, 1))
                    .Build())
                .NewClipState("State2", s => s
                    .AddPropertyBinding("Renderer0", typeof(SkinnedMeshRenderer), "m_Enabled",
                        new Keyframe(0, 0),
                        new Keyframe(0, 1))
                    .Build())
            )
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

        LogTestUtility.Test(scope =>
        {
            scope.ExpectError(ErrorSeverity.Error,
                "MergeSkinnedMesh:copy-enablement-animation:error:activeness-animation-of-source-mismatch");

            new MergeSkinnedMeshProcessor(merged).Process(context, context.GetMeshInfoFor(mergedRenderer));
        });
    }

    [Test]
    public void NestedMergeSkinnedMeshWithPartialBlendShapes()
    {
        var mesh1 = TestUtils.NewCubeMesh();
        mesh1.AddBlendShapeFrame("mesh1", 100, TestUtils.NewCubeBlendShapeFrame((0, Vector3.up)), null, null);
        var mesh2 = TestUtils.NewCubeMesh();
        mesh2.AddBlendShapeFrame("mesh2", 100, TestUtils.NewCubeBlendShapeFrame((0, Vector3.right)), null, null);
        var mesh3 = TestUtils.NewCubeMesh();
        mesh3.AddBlendShapeFrame("mesh3", 100, TestUtils.NewCubeBlendShapeFrame((0, Vector3.forward)), null, null);

        var avatar = TestUtils.NewAvatar();
        var renderer1GameObject = Utils.NewGameObject("Renderer1", avatar.transform);
        var renderer1 = renderer1GameObject.AddComponent<SkinnedMeshRenderer>();
        renderer1.sharedMesh = mesh1;
        var renderer2GameObject = Utils.NewGameObject("Renderer2", avatar.transform);
        var renderer2 = renderer2GameObject.AddComponent<SkinnedMeshRenderer>();
        renderer2.sharedMesh = mesh2;
        var renderer3GameObject = Utils.NewGameObject("Renderer3", avatar.transform);
        var renderer3 = renderer3GameObject.AddComponent<SkinnedMeshRenderer>();
        renderer3.sharedMesh = mesh3;
        var mergedIntermediateGameObject = Utils.NewGameObject("MergedIntermediate", avatar.transform);
        var mergedIntermediateRenderer = mergedIntermediateGameObject.AddComponent<SkinnedMeshRenderer>();
        var mergedFinalGameObject = Utils.NewGameObject("MergedFinal", avatar.transform);
        var mergedFinalRenderer = mergedFinalGameObject.AddComponent<SkinnedMeshRenderer>();

        var context = new BuildContext(avatar, null);
        context.ActivateExtensionContext<Processors.MeshInfo2Context>();
        context.ActivateExtensionContext<ObjectMappingContext>();
        context.ActivateExtensionContext<DestroyTracker.ExtensionContext>();
        context.ActivateExtensionContext<GCComponentInfoContext>();

        var meshInfo1 = context.GetMeshInfoFor(renderer1);
        var meshInfo2 = context.GetMeshInfoFor(renderer2);
        var meshInfo3 = context.GetMeshInfoFor(renderer3);
        var meshInfoIntermediate = context.GetMeshInfoFor(mergedIntermediateRenderer);
        var meshInfoFinal = context.GetMeshInfoFor(mergedFinalRenderer);

        MergeSkinnedMeshProcessor.DoMerge(context,
            meshInfoIntermediate,
            new[] { meshInfo1, meshInfo2 },
            new[] { new[] { 0 }, new[] { 0 } },
            new List<(MeshTopology, Material)>() { (MeshTopology.Triangles, null) }
        );

        MergeSkinnedMeshProcessor.DoMerge(context,
            meshInfoFinal,
            new[] { meshInfoIntermediate, meshInfo3 },
            new[] { new[] { 0 }, new[] { 0 } },
            new List<(MeshTopology, Material)>() { (MeshTopology.Triangles, null) }
        );
    }
}
