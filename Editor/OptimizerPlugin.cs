using System;
using Anatawa12.AvatarOptimizer.ErrorReporting;
using Anatawa12.AvatarOptimizer.ndmf;
using nadena.dev.ndmf;
using nadena.dev.ndmf.builtin;

[assembly: ExportsPlugin(typeof(OptimizerPlugin))]

namespace Anatawa12.AvatarOptimizer.ndmf
{
    internal class OptimizerPlugin : Plugin<OptimizerPlugin>
    {
        public override string DisplayName => "Anatawa12's Avatar Optimizer";

        public override string QualifiedName => "com.anatawa12.avatar-optimizer";

        protected override void Configure()
        {
            // Run early steps before EditorOnly objects are purged
            InPhase(BuildPhase.Resolving)
                .WithRequiredExtensions(new [] {typeof(BuildReportContext)}, seq =>
                {
                    seq.Run("Info if AAO is Out of Date", ctx =>
                        {
                            // we skip check for update 
                            var components = ctx.AvatarRootObject.GetComponentInChildren<AvatarTagComponent>(true);
                            if (components && CheckForUpdate.OutOfDate)
                                BuildReport.LogInfo("CheckForUpdate:out-of-date", 
                                    CheckForUpdate.LatestVersionName, CheckForUpdate.CurrentVersionName);
                        })
                        .Then.Run(Processors.UnusedBonesByReferencesToolEarlyProcessor.Instance)
                        .Then.Run("Early: MakeChildren",
                            ctx => new Processors.MakeChildrenProcessor(early: true).Process(ctx)
                        )
                        .BeforePass(RemoveEditorOnlyPass.Instance);
                });
            InPhase(BuildPhase.Resolving)
                .WithRequiredExtensions(new [] {typeof(BuildReportContext)}, seq =>
                {
                    seq.Run(Processors.FetchOriginalStatePass.Instance);
                });

            // Run everything else in the optimize phase
            InPhase(BuildPhase.Optimizing)
                .WithRequiredExtension(typeof(BuildReportContext), seq =>
                {
                    seq.Run("EmptyPass for Context Ordering", _ => {});
                    seq.WithRequiredExtensions(new[]
                    {
                        typeof(Processors.MeshInfo2Context),
                        typeof(ObjectMappingContext),
                    }, _ =>
                    {
                        seq.Run("Check if AAO is active", ctx =>
                            {
                                ctx.GetState<AAOEnabled>().Enabled = ctx.AvatarRootObject.GetComponent<AvatarTagComponent>();
                            })
                            .Then.Run(Processors.TraceAndOptimizes.LoadTraceAndOptimizeConfiguration.Instance)
                            .Then.Run(Processors.ParseAnimator.Instance)
                            .Then.Run(Processors.TraceAndOptimizes.AutoFreezeBlendShape.Instance)
#if AAO_VRCSDK3_AVATARS
                            .Then.Run(Processors.ClearEndpointPositionProcessor.Instance)
                            .Then.Run(Processors.MergePhysBoneProcessor.Instance)
#endif
                            .Then.Run(Processors.EditSkinnedMeshComponentProcessor.Instance)
                            .Then.Run("MakeChildrenProcessor",
                                ctx => new Processors.MakeChildrenProcessor(early: false).Process(ctx)
                            )
#if AAO_VRCSDK3_AVATARS
                            .Then.Run(Processors.TraceAndOptimizes.OptimizePhysBone.Instance)
#endif
                            .Then.Run(Processors.TraceAndOptimizes.FindUnusedObjects.Instance)
                            .Then.Run(Processors.TraceAndOptimizes.ConfigureRemoveZeroSizedPolygon.Instance)
                            .Then.Run(Processors.MergeBoneProcessor.Instance)
                            .Then.Run(Processors.RemoveZeroSizedPolygonProcessor.Instance)
                            ;
                    });
                    seq.Run("EmptyPass for Context Ordering", _ => {});
                });
        }

        protected override void OnUnhandledException(Exception e)
        {
            BuildReport.ReportInternalError(e);
        }
    }

    internal class AAOEnabled
    {
        public bool Enabled { get; set; }
    }
}
