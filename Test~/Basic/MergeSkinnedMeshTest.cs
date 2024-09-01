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

        merged.Initialize(2);
        merged.copyEnablementAnimation = true;
        merged.SourceSkinnedMeshRenderers.Add(renderer0);
        merged.SourceSkinnedMeshRenderers.Add(renderer1);

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

        new MergeSkinnedMeshProcessor(merged).Process(context, context.GetMeshInfoFor(mergedRenderer));

        var mapping = context.GetMappingBuilder().BuildObjectMapping();
        var animatorMapper = mapping.CreateAnimationMapper(avatar);
        var mapped = animatorMapper.MapBinding("Renderer0", typeof(GameObject), "m_IsActive");

        Assert.That(mapped, Is.Not.Null.And.Contains((merged.name, typeof(SkinnedMeshRenderer), "m_Enabled")));
    }
}