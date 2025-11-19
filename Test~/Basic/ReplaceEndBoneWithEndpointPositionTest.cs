using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using Anatawa12.AvatarOptimizer.ndmf;
using Anatawa12.AvatarOptimizer.Processors;
using nadena.dev.ndmf;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.PhysBone.Components;
using DependencyType = Anatawa12.AvatarOptimizer.GCComponentInfo.DependencyType;

namespace Anatawa12.AvatarOptimizer.Test;

public class ReplaceEndBoneWithEndpointPositionTest
{
    #region General Cases

    [Test]
    public void BasicCase()
    {
        var avatar = TestUtils.NewAvatar();
        TestUtils.SetFxLayer(avatar, new AnimatorController());
        var pbRoot = avatar.AddChild("PhysBoneRoot");
        var pbChain1 = pbRoot.AddChild("PBChain1", localPosition: Vector3.up);
        var pbChain2 = pbChain1.AddChild("PBChain2", localPosition: Vector3.up);
        var pbChain3 = pbChain2.AddChild("PBChain3", localPosition: Vector3.up);
        var mesh = pbChain3.AddChild("Mesh");
        var physBone = pbRoot.AddComponent<VRCPhysBone>();
        physBone.ignoreTransforms.Add(mesh.transform);
        pbRoot.AddComponent<ReplaceEndBoneWithEndpointPosition>();
        var meshRenderer = mesh.AddComponent<MeshRenderer>();
        var meshFilter = mesh.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = TestUtils.NewCubeMesh();
        meshRenderer.material = new Material(Shader.Find("Standard"));

        var context = new BuildContext(avatar, null);
        context.GetState<AAOEnabled>().Enabled = true;
        context.ActivateExtensionContext<DestroyTracker.ExtensionContext>();
        context.ActivateExtensionContext<ObjectMappingContext>();
        ParseAnimator.RunPass(context);
        var componentInfos = context.ActivateExtensionContext<GCComponentInfoContext>();

        // pre-run assertions
        var entrypointMap = DependantMap.CreateEntrypointsMap(context);
        Assert.That(componentInfos.GetInfo(pbChain3.transform).Dependencies.Keys, Has.Member(physBone));
        Assert.That(componentInfos.GetInfo(physBone).Dependencies.Keys, Has.Member(pbChain3.transform));
        Assert.That(entrypointMap.MergedUsages(componentInfos.GetInfo(pbChain3.transform)), Is.EqualTo(DependencyType.PhysBone | DependencyType.Parent));
        AssertAnimatedBy(context, pbChain3.transform, physBone);

        // run processor
        LogTestUtility.Test(scope =>
        {
            ReplaceEndBoneWithEndpointPositionProcessor.ExecuteImpl(context);
        });

        // post-run assertions
        entrypointMap = DependantMap.CreateEntrypointsMap(context);
        Assert.That(physBone.endpointPosition, Is.EqualTo(Vector3.up));
        Assert.That(physBone.ignoreTransforms, Has.Member(pbChain3.transform));
        Assert.That(componentInfos.GetInfo(pbChain3.transform).Dependencies.Keys, Does.Not.Contains(physBone));
        Assert.That(componentInfos.GetInfo(physBone).Dependencies.Keys, Does.Not.Contains(pbChain3.transform));
        Assert.That(entrypointMap.MergedUsages(componentInfos.GetInfo(pbChain3.transform)), Is.EqualTo(DependencyType.Parent));
        AssertNonAnimatedBy(context, pbChain3.transform, physBone);
    }

    [Test]
    public void WithRootTransform()
    {
        var avatar = TestUtils.NewAvatar();
        TestUtils.SetFxLayer(avatar, new AnimatorController());
        var pbRoot = avatar.AddChild("PhysBoneRoot");
        var pbChain1 = pbRoot.AddChild("PBChain1", localPosition: Vector3.up);
        var pbChain2 = pbChain1.AddChild("PBChain2", localPosition: Vector3.up);
        var pbChain3 = pbChain2.AddChild("PBChain3", localPosition: Vector3.up);
        var physBoneGo = avatar.AddChild("PhysBoneGo");
        var mesh = pbChain3.AddChild("Mesh");
        var physBone = physBoneGo.AddComponent<VRCPhysBone>();
        physBone.rootTransform = pbRoot.transform;
        physBone.ignoreTransforms.Add(mesh.transform);
        physBoneGo.AddComponent<ReplaceEndBoneWithEndpointPosition>();
        var meshRenderer = mesh.AddComponent<MeshRenderer>();
        var meshFilter = mesh.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = TestUtils.NewCubeMesh();
        meshRenderer.material = new Material(Shader.Find("Standard"));

        var context = new BuildContext(avatar, null);
        context.GetState<AAOEnabled>().Enabled = true;
        context.ActivateExtensionContext<DestroyTracker.ExtensionContext>();
        context.ActivateExtensionContext<ObjectMappingContext>();
        ParseAnimator.RunPass(context);
        var componentInfos = context.ActivateExtensionContext<GCComponentInfoContext>();

        // pre-run assertions
        var entrypointMap = DependantMap.CreateEntrypointsMap(context);
        Assert.That(componentInfos.GetInfo(pbChain3.transform).Dependencies.Keys, Has.Member(physBone));
        Assert.That(componentInfos.GetInfo(physBone).Dependencies.Keys, Has.Member(pbChain3.transform));
        Assert.That(entrypointMap.MergedUsages(componentInfos.GetInfo(pbChain3.transform)), Is.EqualTo(DependencyType.PhysBone | DependencyType.Parent));
        AssertAnimatedBy(context, pbChain3.transform, physBone);

        // run processor
        LogTestUtility.Test(scope =>
        {
            ReplaceEndBoneWithEndpointPositionProcessor.ExecuteImpl(context);
        });

        // post-run assertions
        entrypointMap = DependantMap.CreateEntrypointsMap(context);
        Assert.That(physBone.endpointPosition, Is.EqualTo(Vector3.up));
        Assert.That(physBone.ignoreTransforms, Has.Member(pbChain3.transform));
        Assert.That(componentInfos.GetInfo(pbChain3.transform).Dependencies.Keys, Does.Not.Contains(physBone));
        Assert.That(componentInfos.GetInfo(physBone).Dependencies.Keys, Does.Not.Contains(pbChain3.transform));
        Assert.That(entrypointMap.MergedUsages(componentInfos.GetInfo(pbChain3.transform)), Is.EqualTo(DependencyType.Parent));
        AssertNonAnimatedBy(context, pbChain3.transform, physBone);
    }

    [Test]
    public void NoChildPB()
    {
        var avatar = TestUtils.NewAvatar();
        TestUtils.SetFxLayer(avatar, new AnimatorController());
        var pbRoot = avatar.AddChild("PhysBoneRoot");
        var physBone = pbRoot.AddComponent<VRCPhysBone>();
        pbRoot.AddComponent<ReplaceEndBoneWithEndpointPosition>();

        var context = new BuildContext(avatar, null);
        context.GetState<AAOEnabled>().Enabled = true;
        context.ActivateExtensionContext<DestroyTracker.ExtensionContext>();
        context.ActivateExtensionContext<ObjectMappingContext>();
        ParseAnimator.RunPass(context);
        context.ActivateExtensionContext<GCComponentInfoContext>();

        // run processor
        LogTestUtility.Test(scope =>
        {
            ReplaceEndBoneWithEndpointPositionProcessor.ExecuteImpl(context);
        });

        // post-run assertions
        Assert.That(physBone.endpointPosition, Is.EqualTo(Vector3.zero));
        Assert.That(physBone.ignoreTransforms, Is.Empty);
        AssertAnimatedBy(context, physBone.transform, physBone);
    }

    [Test]
    public void ComponentsOnLeafBone()
    {
        var avatar = TestUtils.NewAvatar();
        TestUtils.SetFxLayer(avatar, new AnimatorController());
        var pbRoot = avatar.AddChild("PhysBoneRoot");
        var pbChain1 = pbRoot.AddChild("PBChain1", localPosition: Vector3.up);
        var pbChain2 = pbChain1.AddChild("PBChain2", localPosition: Vector3.up);
        var pbChain3 = pbChain2.AddChild("PBChain3", localPosition: Vector3.up);
        var physBone = pbRoot.AddComponent<VRCPhysBone>();
        pbRoot.AddComponent<ReplaceEndBoneWithEndpointPosition>();
        var meshRenderer = pbChain3.AddComponent<MeshRenderer>();
        var meshFilter = pbChain3.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = TestUtils.NewCubeMesh();
        meshRenderer.material = new Material(Shader.Find("Standard"));

        var context = new BuildContext(avatar, null);
        context.GetState<AAOEnabled>().Enabled = true;
        context.ActivateExtensionContext<DestroyTracker.ExtensionContext>();
        context.ActivateExtensionContext<ObjectMappingContext>();
        ParseAnimator.RunPass(context);
        var componentInfos = context.ActivateExtensionContext<GCComponentInfoContext>();

        // pre-run assertions
        Assert.That(componentInfos.GetInfo(pbChain3.transform).Dependencies.Keys, Has.Member(physBone));
        Assert.That(componentInfos.GetInfo(physBone).Dependencies.Keys, Has.Member(pbChain3.transform));
        AssertAnimatedBy(context, pbChain3.transform, physBone);

        // run processor
        LogTestUtility.Test(scope =>
        {
            ReplaceEndBoneWithEndpointPositionProcessor.ExecuteImpl(context);
        });

        // post-run assertions
        Assert.That(physBone.endpointPosition, Is.EqualTo(Vector3.up));
        Assert.That(physBone.ignoreTransforms, Has.Member(pbChain3.transform));
        Assert.That(componentInfos.GetInfo(pbChain3.transform).Dependencies.Keys, Does.Not.Contains(physBone));
        Assert.That(componentInfos.GetInfo(physBone).Dependencies.Keys, Does.Not.Contains(pbChain3.transform));
        AssertNonAnimatedBy(context, pbChain3.transform, physBone);
    }

    [Test]
    public void EndpointPositionSet()
    {
        var avatar = TestUtils.NewAvatar();
        TestUtils.SetFxLayer(avatar, new AnimatorController());
        var pbRoot = avatar.AddChild("PhysBoneRoot");
        var pbChain1 = pbRoot.AddChild("PBChain1", localPosition: Vector3.up);
        var pbChain2 = pbChain1.AddChild("PBChain2", localPosition: Vector3.up);
        var pbChain3 = pbChain2.AddChild("PBChain3", localPosition: Vector3.up);
        var physBone = pbRoot.AddComponent<VRCPhysBone>();
        physBone.endpointPosition = Vector3.one;
        pbRoot.AddComponent<ReplaceEndBoneWithEndpointPosition>();

        var context = new BuildContext(avatar, null);
        context.GetState<AAOEnabled>().Enabled = true;
        context.ActivateExtensionContext<DestroyTracker.ExtensionContext>();
        context.ActivateExtensionContext<ObjectMappingContext>();
        ParseAnimator.RunPass(context);
        var componentInfos = context.ActivateExtensionContext<GCComponentInfoContext>();

        // run processor
        LogTestUtility.Test(scope =>
        {
            ReplaceEndBoneWithEndpointPositionProcessor.ExecuteImpl(context);
            scope.ExpectError(ErrorSeverity.Error, "ReplaceEndBoneWithEndpointPosition:validation:endpointPositionAlreadySet");
        });
    }

    // Not specific to Ignore, but ignore is the best place for testing
    [Test]
    public void Warn_MultiChildIgnore_MixedEndPoint()
    {
        var avatar = TestUtils.NewAvatar();
        TestUtils.SetFxLayer(avatar, new AnimatorController());
        var pbRoot = avatar.AddChild("PhysBoneRoot");
        var pbChain11 = pbRoot.AddChild("PBChain11", localPosition: Vector3.up);
        var pbChain12 = pbChain11.AddChild("PBChain12", localPosition: Vector3.up);
        var pbChain13 = pbChain12.AddChild("PBChain23", localPosition: Vector3.up);
        var pbChain21 = pbRoot.AddChild("PBChain21", localPosition: Vector3.up);
        var pbChain22 = pbChain21.AddChild("PBChain22", localPosition: Vector3.up);
        var pbChain23 = pbChain22.AddChild("PBChain23", localPosition: Vector3.left);
        var physBone = pbRoot.AddComponent<VRCPhysBone>();
        physBone.multiChildType = VRCPhysBoneBase.MultiChildType.Ignore;
        pbRoot.AddComponent<ReplaceEndBoneWithEndpointPosition>();

        var context = new BuildContext(avatar, null);
        context.GetState<AAOEnabled>().Enabled = true;
        context.ActivateExtensionContext<DestroyTracker.ExtensionContext>();
        context.ActivateExtensionContext<ObjectMappingContext>();
        ParseAnimator.RunPass(context);
        context.ActivateExtensionContext<GCComponentInfoContext>();

        // pre-run assertions
        AssertAnimatedBy(context, pbChain13.transform, physBone);
        AssertAnimatedBy(context, pbChain23.transform, physBone);

        // run processor
        LogTestUtility.Test(scope =>
        {
            ReplaceEndBoneWithEndpointPositionProcessor.ExecuteImpl(context);
            scope.ExpectError(ErrorSeverity.NonFatal, "ReplaceEndBoneWithEndpointPosition:validation:inequivalentPositions");
        });

        // post-run assertions
        Assert.That(physBone.endpointPosition, Is.EqualTo((Vector3.up + Vector3.left) * 0.5f));
        Assert.That(physBone.ignoreTransforms, Has.Member(pbChain13.transform));
        Assert.That(physBone.ignoreTransforms, Has.Member(pbChain23.transform));
        AssertNonAnimatedBy(context, pbChain13.transform, physBone);
        AssertNonAnimatedBy(context, pbChain23.transform, physBone);
    }

    // Not specific to Ignore, but ignore is the best place for testing
    [Test]
    public void Successful_MultiChildIgnore_MixedEndPoint_WithManual()
    {
        var avatar = TestUtils.NewAvatar();
        TestUtils.SetFxLayer(avatar, new AnimatorController());
        var pbRoot = avatar.AddChild("PhysBoneRoot");
        var pbChain11 = pbRoot.AddChild("PBChain11", localPosition: Vector3.up);
        var pbChain12 = pbChain11.AddChild("PBChain12", localPosition: Vector3.up);
        var pbChain13 = pbChain12.AddChild("PBChain23", localPosition: Vector3.up);
        var pbChain21 = pbRoot.AddChild("PBChain21", localPosition: Vector3.up);
        var pbChain22 = pbChain21.AddChild("PBChain22", localPosition: Vector3.up);
        var pbChain23 = pbChain22.AddChild("PBChain23", localPosition: Vector3.left);
        var physBone = pbRoot.AddComponent<VRCPhysBone>();
        physBone.multiChildType = VRCPhysBoneBase.MultiChildType.Ignore;
        var replaceEndBoneWithEndpointPosition = pbRoot.AddComponent<ReplaceEndBoneWithEndpointPosition>();
        replaceEndBoneWithEndpointPosition.kind = ReplaceEndBoneWithEndpointPositionKind.Manual;
        replaceEndBoneWithEndpointPosition.manualReplacementPosition = (Vector3.up + Vector3.left) * 0.5f;

        var context = new BuildContext(avatar, null);
        context.GetState<AAOEnabled>().Enabled = true;
        context.ActivateExtensionContext<DestroyTracker.ExtensionContext>();
        context.ActivateExtensionContext<ObjectMappingContext>();
        ParseAnimator.RunPass(context);
        context.ActivateExtensionContext<GCComponentInfoContext>();

        // pre-run assertions
        AssertAnimatedBy(context, pbChain13.transform, physBone);
        AssertAnimatedBy(context, pbChain23.transform, physBone);

        // run processor
        LogTestUtility.Test(scope =>
        {
            ReplaceEndBoneWithEndpointPositionProcessor.ExecuteImpl(context);
        });

        // post-run assertions
        Assert.That(physBone.endpointPosition, Is.EqualTo((Vector3.up + Vector3.left) * 0.5f));
        Assert.That(physBone.ignoreTransforms, Has.Member(pbChain13.transform));
        Assert.That(physBone.ignoreTransforms, Has.Member(pbChain23.transform));
        AssertNonAnimatedBy(context, pbChain13.transform, physBone);
        AssertNonAnimatedBy(context, pbChain23.transform, physBone);
    }

    #endregion

    #region Multi Child Ignore

    [Test]
    public void SuccessfulMultiChildIgnore()
    {
        var avatar = TestUtils.NewAvatar();
        TestUtils.SetFxLayer(avatar, new AnimatorController());
        var pbRoot = avatar.AddChild("PhysBoneRoot");
        var pbChain11 = pbRoot.AddChild("PBChain11", localPosition: Vector3.up);
        var pbChain12 = pbChain11.AddChild("PBChain12", localPosition: Vector3.up);
        var pbChain13 = pbChain12.AddChild("PBChain23", localPosition: Vector3.up);
        var pbChain21 = pbRoot.AddChild("PBChain21", localPosition: Vector3.up);
        var pbChain22 = pbChain21.AddChild("PBChain22", localPosition: Vector3.up);
        var pbChain23 = pbChain22.AddChild("PBChain23", localPosition: Vector3.up);
        var physBone = pbRoot.AddComponent<VRCPhysBone>();
        physBone.multiChildType = VRCPhysBoneBase.MultiChildType.Ignore;
        pbRoot.AddComponent<ReplaceEndBoneWithEndpointPosition>();

        var context = new BuildContext(avatar, null);
        context.GetState<AAOEnabled>().Enabled = true;
        context.ActivateExtensionContext<DestroyTracker.ExtensionContext>();
        context.ActivateExtensionContext<ObjectMappingContext>();
        ParseAnimator.RunPass(context);
        context.ActivateExtensionContext<GCComponentInfoContext>();

        // pre-run assertions
        AssertAnimatedBy(context, pbChain13.transform, physBone);
        AssertAnimatedBy(context, pbChain23.transform, physBone);

        // run processor
        LogTestUtility.Test(scope =>
        {
            ReplaceEndBoneWithEndpointPositionProcessor.ExecuteImpl(context);
        });

        // post-run assertions
        Assert.That(physBone.endpointPosition, Is.EqualTo(Vector3.up));
        Assert.That(physBone.ignoreTransforms, Has.Member(pbChain13.transform));
        Assert.That(physBone.ignoreTransforms, Has.Member(pbChain23.transform));
        AssertNonAnimatedBy(context, pbChain13.transform, physBone);
        AssertNonAnimatedBy(context, pbChain23.transform, physBone);
    }

    [Test]
    public void Successful_MultiChildIgnore_MultiChildIsLeafBone()
    {
        var avatar = TestUtils.NewAvatar();
        TestUtils.SetFxLayer(avatar, new AnimatorController());
        var pbRoot = avatar.AddChild("PhysBoneRoot");
        var pbChain11 = pbRoot.AddChild("PBChain11", localPosition: Vector3.up);
        var pbChain12 = pbChain11.AddChild("PBChain12", localPosition: Vector3.up);
        var pbChain13 = pbChain12.AddChild("PBChain23", localPosition: Vector3.up);
        var pbChain21 = pbRoot.AddChild("PBChain21", localPosition: Vector3.up);
        var pbChain22 = pbChain21.AddChild("PBChain22", localPosition: Vector3.up);
        var pbChain23 = pbChain22.AddChild("PBChain23", localPosition: Vector3.up);
        var pbChain31 = pbRoot.AddChild("PBChain31", localPosition: Vector3.up);
        var physBone = pbRoot.AddComponent<VRCPhysBone>();
        physBone.multiChildType = VRCPhysBoneBase.MultiChildType.Ignore;
        pbRoot.AddComponent<ReplaceEndBoneWithEndpointPosition>();

        var context = new BuildContext(avatar, null);
        context.GetState<AAOEnabled>().Enabled = true;
        context.ActivateExtensionContext<DestroyTracker.ExtensionContext>();
        context.ActivateExtensionContext<ObjectMappingContext>();
        ParseAnimator.RunPass(context);
        context.ActivateExtensionContext<GCComponentInfoContext>();

        // pre-run assertions
        AssertAnimatedBy(context, pbChain13.transform, physBone);
        AssertAnimatedBy(context, pbChain23.transform, physBone);
        AssertAnimatedBy(context, pbChain31.transform, physBone);

        // run processor
        LogTestUtility.Test(scope =>
        {
            ReplaceEndBoneWithEndpointPositionProcessor.ExecuteImpl(context);
        });

        // post-run assertions
        Assert.That(physBone.endpointPosition, Is.EqualTo(Vector3.up));
        Assert.That(physBone.ignoreTransforms, Has.Member(pbChain13.transform));
        Assert.That(physBone.ignoreTransforms, Has.Member(pbChain23.transform));
        Assert.That(physBone.ignoreTransforms, Has.Member(pbChain31.transform));
        AssertNonAnimatedBy(context, pbChain13.transform, physBone);
        AssertNonAnimatedBy(context, pbChain23.transform, physBone);
        AssertNonAnimatedBy(context, pbChain31.transform, physBone);
    }

    [Test]
    public void Error_MultiChildIgnore_MultiChildIsLeafBone()
    {
        var avatar = TestUtils.NewAvatar();
        TestUtils.SetFxLayer(avatar, new AnimatorController());
        var pbRoot = avatar.AddChild("PhysBoneRoot");
        var pbChain11 = pbRoot.AddChild("PBChain11", localPosition: Vector3.up);
        var pbChain12 = pbChain11.AddChild("PBChain12", localPosition: Vector3.up);
        var pbChain13 = pbChain12.AddChild("PBChain23", localPosition: Vector3.up);
        var pbChain21 = pbRoot.AddChild("PBChain21", localPosition: Vector3.up);
        var physBone = pbRoot.AddComponent<VRCPhysBone>();
        physBone.multiChildType = VRCPhysBoneBase.MultiChildType.Ignore;
        pbRoot.AddComponent<ReplaceEndBoneWithEndpointPosition>();

        var context = new BuildContext(avatar, null);
        context.GetState<AAOEnabled>().Enabled = true;
        context.ActivateExtensionContext<DestroyTracker.ExtensionContext>();
        context.ActivateExtensionContext<ObjectMappingContext>();
        ParseAnimator.RunPass(context);
        context.ActivateExtensionContext<GCComponentInfoContext>();

        // pre-run assertions
        AssertAnimatedBy(context, pbChain13.transform, physBone);
        AssertAnimatedBy(context, pbChain21.transform, physBone);

        // run processor
        LogTestUtility.Test(scope =>
        {
            ReplaceEndBoneWithEndpointPositionProcessor.ExecuteImpl(context);
            scope.ExpectError(ErrorSeverity.Error, "ReplaceEndBoneWithEndpointPosition:validation:unsafeMultiChild");
        });

        // post-run assertions: no changes
        Assert.That(physBone.endpointPosition, Is.EqualTo(Vector3.zero));
        Assert.That(physBone.ignoreTransforms, Is.Empty);
        AssertAnimatedBy(context, pbChain13.transform, physBone);
        AssertAnimatedBy(context, pbChain21.transform, physBone);
    }

    #endregion

    #region Multi Child First

    [Test]
    public void SuccessfulMultiChildFirst()
    {
        var avatar = TestUtils.NewAvatar();
        TestUtils.SetFxLayer(avatar, new AnimatorController());
        var pbRoot = avatar.AddChild("PhysBoneRoot");
        var pbChain11 = pbRoot.AddChild("PBChain11", localPosition: Vector3.up);
        var pbChain12 = pbChain11.AddChild("PBChain12", localPosition: Vector3.up);
        var pbChain13 = pbChain12.AddChild("PBChain23", localPosition: Vector3.up);
        var pbChain21 = pbRoot.AddChild("PBChain21", localPosition: Vector3.up);
        var pbChain22 = pbChain21.AddChild("PBChain22", localPosition: Vector3.up);
        var pbChain23 = pbChain22.AddChild("PBChain23", localPosition: Vector3.up);
        var physBone = pbRoot.AddComponent<VRCPhysBone>();
        physBone.multiChildType = VRCPhysBoneBase.MultiChildType.First;
        pbRoot.AddComponent<ReplaceEndBoneWithEndpointPosition>();

        var context = new BuildContext(avatar, null);
        context.GetState<AAOEnabled>().Enabled = true;
        context.ActivateExtensionContext<DestroyTracker.ExtensionContext>();
        context.ActivateExtensionContext<ObjectMappingContext>();
        ParseAnimator.RunPass(context);
        context.ActivateExtensionContext<GCComponentInfoContext>();

        // pre-run assertions
        AssertAnimatedBy(context, pbChain13.transform, physBone);
        AssertAnimatedBy(context, pbChain23.transform, physBone);

        // run processor
        LogTestUtility.Test(scope =>
        {
            ReplaceEndBoneWithEndpointPositionProcessor.ExecuteImpl(context);
        });

        // post-run assertions
        Assert.That(physBone.endpointPosition, Is.EqualTo(Vector3.up));
        Assert.That(physBone.ignoreTransforms, Has.Member(pbChain13.transform));
        Assert.That(physBone.ignoreTransforms, Has.Member(pbChain23.transform));
        AssertNonAnimatedBy(context, pbChain13.transform, physBone);
        AssertNonAnimatedBy(context, pbChain23.transform, physBone);
    }

    [Test]
    public void Successful_MultiChildFirst_NonFirstLeaf()
    {
        var avatar = TestUtils.NewAvatar();
        TestUtils.SetFxLayer(avatar, new AnimatorController());
        var pbRoot = avatar.AddChild("PhysBoneRoot");
        var pbChain11 = pbRoot.AddChild("PBChain11", localPosition: Vector3.up);
        var pbChain12 = pbChain11.AddChild("PBChain12", localPosition: Vector3.up);
        var pbChain13 = pbChain12.AddChild("PBChain23", localPosition: Vector3.up);
        var pbChain21 = pbRoot.AddChild("PBChain21", localPosition: Vector3.up);
        var physBone = pbRoot.AddComponent<VRCPhysBone>();
        physBone.multiChildType = VRCPhysBoneBase.MultiChildType.First;
        pbRoot.AddComponent<ReplaceEndBoneWithEndpointPosition>();

        var context = new BuildContext(avatar, null);
        context.GetState<AAOEnabled>().Enabled = true;
        context.ActivateExtensionContext<DestroyTracker.ExtensionContext>();
        context.ActivateExtensionContext<ObjectMappingContext>();
        ParseAnimator.RunPass(context);
        context.ActivateExtensionContext<GCComponentInfoContext>();

        // pre-run assertions
        AssertAnimatedBy(context, pbChain13.transform, physBone);
        AssertAnimatedBy(context, pbChain21.transform, physBone);

        // run processor
        LogTestUtility.Test(scope =>
        {
            ReplaceEndBoneWithEndpointPositionProcessor.ExecuteImpl(context);
        });

        // post-run assertions: no changes
        Assert.That(physBone.endpointPosition, Is.EqualTo(Vector3.up));
        Assert.That(physBone.ignoreTransforms, Has.Member(pbChain13.transform));
        Assert.That(physBone.ignoreTransforms, Has.Member(pbChain21.transform));
        AssertNonAnimatedBy(context, pbChain13.transform, physBone);
        AssertNonAnimatedBy(context, pbChain21.transform, physBone);
    }

    [Test]
    public void Error_MultiChildFirst_FirstLeaf()
    {
        var avatar = TestUtils.NewAvatar();
        TestUtils.SetFxLayer(avatar, new AnimatorController());
        var pbRoot = avatar.AddChild("PhysBoneRoot");
        var pbChain11 = pbRoot.AddChild("PBChain21", localPosition: Vector3.up);
        var pbChain21 = pbRoot.AddChild("PBChain11", localPosition: Vector3.up);
        var pbChain22 = pbChain21.AddChild("PBChain12", localPosition: Vector3.up);
        var pbChain23 = pbChain22.AddChild("PBChain23", localPosition: Vector3.up);
        var physBone = pbRoot.AddComponent<VRCPhysBone>();
        physBone.multiChildType = VRCPhysBoneBase.MultiChildType.First;
        pbRoot.AddComponent<ReplaceEndBoneWithEndpointPosition>();

        var context = new BuildContext(avatar, null);
        context.GetState<AAOEnabled>().Enabled = true;
        context.ActivateExtensionContext<DestroyTracker.ExtensionContext>();
        context.ActivateExtensionContext<ObjectMappingContext>();
        ParseAnimator.RunPass(context);
        context.ActivateExtensionContext<GCComponentInfoContext>();

        // pre-run assertions
        AssertAnimatedBy(context, pbChain23.transform, physBone);
        AssertAnimatedBy(context, pbChain11.transform, physBone);

        // run processor
        LogTestUtility.Test(scope =>
        {
            ReplaceEndBoneWithEndpointPositionProcessor.ExecuteImpl(context);
            scope.ExpectError(ErrorSeverity.Error, "ReplaceEndBoneWithEndpointPosition:validation:unsafeMultiChild");
        });

        // post-run assertions: no changes
        Assert.That(physBone.endpointPosition, Is.EqualTo(Vector3.zero));
        Assert.That(physBone.ignoreTransforms, Is.Empty);
        AssertAnimatedBy(context, pbChain23.transform, physBone);
        AssertAnimatedBy(context, pbChain11.transform, physBone);
    }

    #endregion

    #region Multi Child Average

    [Test]
    public void Successful_MultiChildAverage()
    {
        var avatar = TestUtils.NewAvatar();
        TestUtils.SetFxLayer(avatar, new AnimatorController());
        var pbRoot = avatar.AddChild("PhysBoneRoot");
        var pbChain11 = pbRoot.AddChild("PBChain11", localPosition: Vector3.up);
        var pbChain12 = pbChain11.AddChild("PBChain12", localPosition: Vector3.up);
        var pbChain13 = pbChain12.AddChild("PBChain23", localPosition: Vector3.up);
        var pbChain21 = pbRoot.AddChild("PBChain21", localPosition: Vector3.up);
        var pbChain22 = pbChain21.AddChild("PBChain22", localPosition: Vector3.up);
        var pbChain23 = pbChain22.AddChild("PBChain23", localPosition: Vector3.up);
        var physBone = pbRoot.AddComponent<VRCPhysBone>();
        physBone.multiChildType = VRCPhysBoneBase.MultiChildType.Average;
        pbRoot.AddComponent<ReplaceEndBoneWithEndpointPosition>();

        var context = new BuildContext(avatar, null);
        context.GetState<AAOEnabled>().Enabled = true;
        context.ActivateExtensionContext<DestroyTracker.ExtensionContext>();
        context.ActivateExtensionContext<ObjectMappingContext>();
        ParseAnimator.RunPass(context);
        context.ActivateExtensionContext<GCComponentInfoContext>();

        // pre-run assertions
        AssertAnimatedBy(context, pbChain13.transform, physBone);
        AssertAnimatedBy(context, pbChain23.transform, physBone);

        // run processor
        LogTestUtility.Test(scope =>
        {
            ReplaceEndBoneWithEndpointPositionProcessor.ExecuteImpl(context);
        });

        // post-run assertions
        Assert.That(physBone.endpointPosition, Is.EqualTo(Vector3.up));
        Assert.That(physBone.ignoreTransforms, Has.Member(pbChain13.transform));
        Assert.That(physBone.ignoreTransforms, Has.Member(pbChain23.transform));
        AssertNonAnimatedBy(context, pbChain13.transform, physBone);
        AssertNonAnimatedBy(context, pbChain23.transform, physBone);
    }

    [Test]
    public void Error_MultiChildAverage_AnyLeaf()
    {
        var avatar = TestUtils.NewAvatar();
        TestUtils.SetFxLayer(avatar, new AnimatorController());
        var pbRoot = avatar.AddChild("PhysBoneRoot");
        var pbChain11 = pbRoot.AddChild("PBChain21", localPosition: Vector3.up);
        var pbChain21 = pbRoot.AddChild("PBChain11", localPosition: Vector3.up);
        var pbChain22 = pbChain21.AddChild("PBChain12", localPosition: Vector3.up);
        var pbChain23 = pbChain22.AddChild("PBChain23", localPosition: Vector3.up);
        var physBone = pbRoot.AddComponent<VRCPhysBone>();
        physBone.multiChildType = VRCPhysBoneBase.MultiChildType.Average;
        pbRoot.AddComponent<ReplaceEndBoneWithEndpointPosition>();

        var context = new BuildContext(avatar, null);
        context.GetState<AAOEnabled>().Enabled = true;
        context.ActivateExtensionContext<DestroyTracker.ExtensionContext>();
        context.ActivateExtensionContext<ObjectMappingContext>();
        ParseAnimator.RunPass(context);
        context.ActivateExtensionContext<GCComponentInfoContext>();

        // pre-run assertions
        AssertAnimatedBy(context, pbChain23.transform, physBone);
        AssertAnimatedBy(context, pbChain11.transform, physBone);

        // run processor
        LogTestUtility.Test(scope =>
        {
            ReplaceEndBoneWithEndpointPositionProcessor.ExecuteImpl(context);
            scope.ExpectError(ErrorSeverity.Error, "ReplaceEndBoneWithEndpointPosition:validation:unsafeMultiChild");
        });

        // post-run assertions: no changes
        Assert.That(physBone.endpointPosition, Is.EqualTo(Vector3.zero));
        Assert.That(physBone.ignoreTransforms, Is.Empty);
        AssertAnimatedBy(context, pbChain23.transform, physBone);
        AssertAnimatedBy(context, pbChain11.transform, physBone);
    }

    #endregion

    #region Overwrap Warnings

    [Test]
    public void Warn_OverwrappingPhysBones_NoTargetTransform()
    {
        var avatar = TestUtils.NewAvatar();
        TestUtils.SetFxLayer(avatar, new AnimatorController());
        var child1 = avatar.AddChild("Child1", localPosition: Vector3.up);
        var child2 = child1.AddChild("Child2", localPosition: Vector3.up);
        var child3 = child2.AddChild("Child3", localPosition: Vector3.up);
        var child4 = child3.AddChild("Child4", localPosition: Vector3.up);

        // PB1: child 1..=3
        var physBone1 = child1.AddComponent<VRCPhysBone>();
        child1.AddComponent<ReplaceEndBoneWithEndpointPosition>();
        // PB2: child 3..=4
        var physBone2 = child3.AddComponent<VRCPhysBone>();
        physBone1.ignoreTransforms.Add(child4.transform);

        var context = new BuildContext(avatar, null);
        context.GetState<AAOEnabled>().Enabled = true;
        context.ActivateExtensionContext<DestroyTracker.ExtensionContext>();
        context.ActivateExtensionContext<ObjectMappingContext>();
        ParseAnimator.RunPass(context);
        context.ActivateExtensionContext<GCComponentInfoContext>();

        // run processor
        LogTestUtility.Test(scope =>
        {
            ReplaceEndBoneWithEndpointPositionProcessor.ExecuteImpl(context);
            scope.ExpectError(ErrorSeverity.NonFatal, "ReplaceEndBoneWithEndpointPosition:validation:overlappedPhysBone");
        });

        // post-run assertions
        Assert.That(physBone1.endpointPosition, Is.EqualTo(Vector3.up));
        Assert.That(physBone1.ignoreTransforms, Has.Member(child3.transform));
        AssertNonAnimatedBy(context, child3.transform, physBone1);
    }

    [Test]
    public void Warn_OverwrappingPhysBones_WithTargetTransform()
    {
        var avatar = TestUtils.NewAvatar();
        TestUtils.SetFxLayer(avatar, new AnimatorController());
        var child1 = avatar.AddChild("Child1", localPosition: Vector3.up);
        var child2 = child1.AddChild("Child2", localPosition: Vector3.up);
        var child3 = child2.AddChild("Child3", localPosition: Vector3.up);
        var child4 = child3.AddChild("Child4", localPosition: Vector3.up);


        // PB1: child 1..=3
        var pbGO1 = avatar.AddChild("PB1");
        var physBone1 = pbGO1.AddComponent<VRCPhysBone>();
        pbGO1.AddComponent<ReplaceEndBoneWithEndpointPosition>();
        // PB2: child 3..=4
        var pbGO2 = avatar.AddChild("PB2");
        var physBone2 = pbGO2.AddComponent<VRCPhysBone>();
        physBone1.rootTransform = child1.transform;
        physBone1.ignoreTransforms.Add(child4.transform);
        physBone2.rootTransform = child3.transform;

        var context = new BuildContext(avatar, null);
        context.GetState<AAOEnabled>().Enabled = true;
        context.ActivateExtensionContext<DestroyTracker.ExtensionContext>();
        context.ActivateExtensionContext<ObjectMappingContext>();
        ParseAnimator.RunPass(context);
        context.ActivateExtensionContext<GCComponentInfoContext>();

        // run processor
        LogTestUtility.Test(scope =>
        {
            ReplaceEndBoneWithEndpointPositionProcessor.ExecuteImpl(context);
            scope.ExpectError(ErrorSeverity.NonFatal, "ReplaceEndBoneWithEndpointPosition:validation:overlappedPhysBone");
        });

        // post-run assertions
        Assert.That(physBone1.endpointPosition, Is.EqualTo(Vector3.up));
        Assert.That(physBone1.ignoreTransforms, Has.Member(child3.transform));
        AssertNonAnimatedBy(context, child3.transform, physBone1);
    }

    [Test]
    public void Successful_OverwrappingPhysBones_NestedButNonOverwrapping()
    {
        var avatar = TestUtils.NewAvatar();
        TestUtils.SetFxLayer(avatar, new AnimatorController());
        var child1 = avatar.AddChild("Child1", localPosition: Vector3.up);
        var child2 = child1.AddChild("Child2", localPosition: Vector3.up);
        var child3 = child2.AddChild("Child3", localPosition: Vector3.up);
        var child4 = child3.AddChild("Child4", localPosition: Vector3.up);
        var child5 = child4.AddChild("Child5", localPosition: Vector3.up);

        // PB1: child 1..=3
        var pbGO1 = avatar.AddChild("PB1");
        var physBone1 = pbGO1.AddComponent<VRCPhysBone>();
        pbGO1.AddComponent<ReplaceEndBoneWithEndpointPosition>();
        // PB2: child 4..=5
        var pbGO2 = avatar.AddChild("PB2");
        var physBone2 = pbGO2.AddComponent<VRCPhysBone>();
        physBone1.rootTransform = child1.transform;
        physBone1.ignoreTransforms.Add(child4.transform);
        physBone2.rootTransform = child4.transform;

        var context = new BuildContext(avatar, null);
        context.GetState<AAOEnabled>().Enabled = true;
        context.ActivateExtensionContext<DestroyTracker.ExtensionContext>();
        context.ActivateExtensionContext<ObjectMappingContext>();
        ParseAnimator.RunPass(context);
        context.ActivateExtensionContext<GCComponentInfoContext>();

        // run processor
        LogTestUtility.Test(scope =>
        {
            ReplaceEndBoneWithEndpointPositionProcessor.ExecuteImpl(context);
        });

        // post-run assertions
        Assert.That(physBone1.endpointPosition, Is.EqualTo(Vector3.up));
        Assert.That(physBone1.ignoreTransforms, Has.Member(child3.transform));
        AssertNonAnimatedBy(context, child3.transform, physBone1);
    }

    #endregion

    #region utils

    private void AssertAnimatedBy(BuildContext context, Transform transform, Component animator)
    {
        Assert.That(context.GetAnimationComponent(transform).GetFloatNode("m_LocalPosition.x").SourceComponents, Has.Member(animator));
        Assert.That(context.GetAnimationComponent(transform).GetFloatNode("m_LocalPosition.y").SourceComponents, Has.Member(animator));
        Assert.That(context.GetAnimationComponent(transform).GetFloatNode("m_LocalPosition.z").SourceComponents, Has.Member(animator));
        Assert.That(context.GetAnimationComponent(transform).GetFloatNode("m_LocalRotation.x").SourceComponents, Has.Member(animator));
        Assert.That(context.GetAnimationComponent(transform).GetFloatNode("m_LocalRotation.y").SourceComponents, Has.Member(animator));
        Assert.That(context.GetAnimationComponent(transform).GetFloatNode("m_LocalRotation.z").SourceComponents, Has.Member(animator));
        Assert.That(context.GetAnimationComponent(transform).GetFloatNode("m_LocalRotation.w").SourceComponents, Has.Member(animator));
    }

    private void AssertNonAnimatedBy(BuildContext context, Transform transform, Component animator)
    {
        Assert.That(context.GetAnimationComponent(transform).GetFloatNode("m_LocalPosition.x").SourceComponents, Does.Not.Contains(animator));
        Assert.That(context.GetAnimationComponent(transform).GetFloatNode("m_LocalPosition.y").SourceComponents, Does.Not.Contains(animator));
        Assert.That(context.GetAnimationComponent(transform).GetFloatNode("m_LocalPosition.z").SourceComponents, Does.Not.Contains(animator));
        Assert.That(context.GetAnimationComponent(transform).GetFloatNode("m_LocalRotation.x").SourceComponents, Does.Not.Contains(animator));
        Assert.That(context.GetAnimationComponent(transform).GetFloatNode("m_LocalRotation.y").SourceComponents, Does.Not.Contains(animator));
        Assert.That(context.GetAnimationComponent(transform).GetFloatNode("m_LocalRotation.z").SourceComponents, Does.Not.Contains(animator));
        Assert.That(context.GetAnimationComponent(transform).GetFloatNode("m_LocalRotation.w").SourceComponents, Does.Not.Contains(animator));
    }

    #endregion
}
