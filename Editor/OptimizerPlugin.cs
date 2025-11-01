using System;
using nadena.dev.ndmf;
using nadena.dev.ndmf.builtin;

[assembly: ExportsPlugin(typeof(\uFFDC\uFFDC\uFFDC.Anatawa12.AvatarOptimizer.ndmf.OptimizerPlugin))]

// NDMF sorts plugins by their 'FullName' of plugin name which is combination of namespace and class name with ordinal ordering. [1]
// Avatar Optimizer is designed to run as late as possible, so having "Anatawa12.AvatarOptimizer.ndmf" namespace
// is not good idea since 'A' is very early in the alphabet.
// Therefore, we use special namespace only for the OptimizerPlugin class.
//
// C# allows '_' and any characters in Letter or Letter Number (`L` or `Nl`) category in the identifier start and
// current C# compiler (Roslyn) only supports non-Surrogate / only BMP characters in the identifier.
// The last block in BMP contains those category is "Halfwidth and Fullwidth Forms" which is at U+FF00–FFEF.
// (The last block in BMP is "Specials" which is at U+FFF0–FFFF, but it contains only 16 characters and not usable for identifiers.)
// The last latter in the block is "HALFWIDTH HANGUL LETTER I" at U+FFDC, which is a Other Letter (`Lo`) character.
// Therefore we prepend the namespace with \uFFDC to make it sort later than any other plugins as possible,
//
// [1]: https://github.com/bdunderscore/ndmf/blob/1ae9bc1d92363229d2cc54156fbec2cb9a8aa19a/Editor/API/Solver/PluginResolver.cs#L21-L32
// [2]: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/lexical-structure#643-identifiers
namespace \uFFDC\uFFDC\uFFDC.Anatawa12.AvatarOptimizer.ndmf
{
    [RunsOnAllPlatforms]
    internal class OptimizerPlugin : global::Anatawa12.AvatarOptimizer.ndmf.OptimizerPluginImpl<OptimizerPlugin>
    {
    }
}

namespace Anatawa12.AvatarOptimizer.ndmf
{
    [RunsOnAllPlatforms]
    internal abstract class OptimizerPluginImpl<T> : Plugin<T> where T : OptimizerPluginImpl<T>, new()
    {
        public override string DisplayName => $"AAO: Avatar Optimizer ({CheckForUpdate.Checker.CurrentVersionName})";

        public override string QualifiedName => "com.anatawa12.avatar-optimizer";

        protected override void Configure()
        {
            // Run early steps before EditorOnly objects are purged
            InPhase(BuildPhase.Resolving)
                .Run("Info if AAO is Out of Date", ctx =>
                {
                    // we skip check for update 
                    var components = ctx.AvatarRootObject.GetComponentInChildren<AvatarTagComponent>(true);
                    if (components && CheckForUpdate.Checker.OutOfDate && CheckForUpdate.MenuItems.CheckForUpdateEnabled)
                        BuildLog.LogInfo("CheckForUpdate:out-of-date",
                            CheckForUpdate.Checker.LatestVersionName, CheckForUpdate.Checker.CurrentVersionName);
                })
                .Then.Run(Processors.UnusedBonesByReferencesToolEarlyProcessor.Instance)
                .Then.Run("Early: MakeChildren",
                    ctx => new Processors.MakeChildrenProcessor(early: true).Process(ctx)
                )
                .BeforePass(RemoveEditorOnlyPass.Instance);

            InPhase(BuildPhase.Resolving).Run(Processors.FetchOriginalStatePass.Instance);
            ;

            // Run everything else in the optimize phase
            var mainSequence = InPhase(BuildPhase.Optimizing);
            mainSequence
                .WithRequiredExtensions(new[]
                {
                    typeof(Processors.MeshInfo2Context),
                    typeof(ObjectMappingContext),
                    typeof(DestroyTracker.ExtensionContext),
                }, seq =>
                {
                    seq.Run("Initial Step for Avatar Optimizer",
                            ctx =>
                            {
                                ctx.GetState<AAOEnabled>().Enabled =
                                    ctx.AvatarRootObject.GetComponentInChildren<AvatarTagComponent>(true);
                                // invalidate ComponentInfoRegistry cache to support newly added assets
                                AssetDescription.Reload();
                            })
                        .Then.Run("Validation", (ctx) => ComponentValidation.ValidateAll(ctx.AvatarRootObject))
                        .Then.Run(Processors.TraceAndOptimizes.LoadTraceAndOptimizeConfiguration.Instance)
                        .Then.Run(Processors.TraceAndOptimizes.OptimizationWarnings.Instance)
                        .Then.Run(Processors.DupliacteAssets.Instance)
                        .Then.Run(Processors.ParseAnimator.Instance)
                        .Then.Run(Processors.GatherShaderMaterialInformation.Instance)
                        .Then.WithRequiredExtension(typeof(GCComponentInfoContext), seq => seq
                            .Run(new Processors.GCDebugPass(InternalGcDebugPosition.AtTheBeginning))
                            .Then.Run(Processors.TraceAndOptimizes.RemoveSubMeshesWithoutMaterial.Instance)
                            .Then.Run(Processors.TraceAndOptimizes.AddRemoveEmptySubMesh.Instance)
                            .Then.Run(Processors.TraceAndOptimizes.AutoFreezeBlendShape.Instance)
#if AAO_VRCSDK3_AVATARS
                            .Then.Run(Processors.TraceAndOptimizes.AutoMergeCompatiblePhysBone.Instance)
                            .Then.Run(Processors.ClearEndpointPositionProcessor.Instance)
                            .Then.Run(Processors.MergePhysBoneProcessor.Instance)
                            .Then.Run(new Processors.GCDebugPass(InternalGcDebugPosition.AfterPhysBone))
#endif
                            .Then.Run(Processors.EditSkinnedMeshComponentProcessor.Instance)
                            .PreviewingWith(EditModePreview.RemoveMeshByMaskRenderFilter.Instance)
                            .PreviewingWith(EditModePreview.RemoveMeshByBlendShapeRenderFilter.Instance)
                            .PreviewingWith(EditModePreview.RemoveMeshInBoxRenderFilter.Instance)
                            .PreviewingWith(EditModePreview.RemoveMeshByUVTileRenderFilter.Instance)
                            .Then.Run("MakeChildrenProcessor",
                                ctx => new Processors.MakeChildrenProcessor(early: false).Process(ctx)
                            )
                            .Then.Run(new Processors.GCDebugPass(InternalGcDebugPosition.AfterMeshProcessing))
#if AAO_VRCSDK3_AVATARS
                            .Then.Run(Processors.TraceAndOptimizes.OptimizePhysBone.Instance)
#endif
                            .Then.Run(Processors.TraceAndOptimizes.AutoMergeSkinnedMesh.Instance)
                            .Then.Run(Processors.TraceAndOptimizes.MergeMaterialSlots.Instance)
                            .Then.Run(new Processors.GCDebugPass(InternalGcDebugPosition.AfterAutoMergeSkinnedMesh))
                            .Then.Run(Processors.TraceAndOptimizes.FindUnusedObjects.Instance)
                            .Then.Run(new Processors.GCDebugPass(InternalGcDebugPosition.AfterGcComponents))
                            .Then.Run(Processors.TraceAndOptimizes.ConfigureRemoveZeroSizedPolygon.Instance)
                            .Then.Run(Processors.TraceAndOptimizes.RemoveUnusedMaterialProperties.Instance)
                            .Then.Run(Processors.TraceAndOptimizes.AutoMergeBlendShape.Instance)
                            .Then.Run(Processors.MergeBoneProcessor.Instance)
                            .Then.Run(Processors.RemoveZeroSizedPolygonProcessor.Instance)
                            .Then.Run(Processors.TraceAndOptimizes.OptimizeTexture.Instance)
                            .Then.Run(Processors.AnimatorOptimizer.RemoveInvalidProperties.Instance)
                            .Then.Run(new Processors.GCDebugPass(InternalGcDebugPosition.AtTheEnd))
                        );
                });

            // animator optimizer is written in newer C# so requires 2021.3 or newer 
            mainSequence.Run(Processors.AnimatorOptimizer.InitializeAnimatorOptimizer.Instance)
                .Then.Run(Processors.AnimatorOptimizer.AnyStateToEntryExit.Instance)
#if AAO_VRCSDK3_AVATARS
                // EntryExit to BlendTree optimization heavily depends on VRChat's behavior
                .Then.Run(Processors.AnimatorOptimizer.EntryExitToBlendTree.Instance)
#endif
                .Then.Run(Processors.AnimatorOptimizer.MergeBlendTreeLayer.Instance)
                .Then.Run(Processors.AnimatorOptimizer.RemoveMeaninglessLayer.Instance)
                ;
        }

        protected override void OnUnhandledException(Exception e)
        {
            ErrorReport.ReportException(e);
        }
    }

    internal class AAOEnabled
    {
        public bool Enabled { get; set; }
    }
}
