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
                    seq.Run(Processors.UnusedBonesByReferencesToolEarlyProcessor.Instance)
                        .Then.Run("Early: MakeChildren",
                            ctx => new Processors.MakeChildrenProcessor(early: true).Process(ctx)
                        )
                        .BeforePass(RemoveEditorOnlyPass.Instance);
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
                        seq.Run(Processors.TraceAndOptimizes.LoadTraceAndOptimizeConfiguration.Instance)
                            .Then.Run(Processors.TraceAndOptimizes.ParseAnimator.Instance)
                            .Then.Run(Processors.TraceAndOptimizes.AutoFreezeBlendShape.Instance)
                            .Then.Run(Processors.ClearEndpointPositionProcessor.Instance)
                            .Then.Run(Processors.MergePhysBoneProcessor.Instance)
                            .Then.Run(Processors.EditSkinnedMeshComponentProcessor.Instance)
                            .Then.Run("MakeChildrenProcessor",
                                ctx => new Processors.MakeChildrenProcessor(early: false).Process(ctx)
                            )
                            .Then.Run(Processors.TraceAndOptimizes.FindUnusedObjects.Instance)
                            .Then.Run(Processors.MergeBoneProcessor.Instance);
                    });
                    seq.Run("EmptyPass for Context Ordering", _ => {});
                });
        }

        protected override void OnUnhandledException(Exception e)
        {
            BuildReport.ReportInternalError(e);
        }
    }
}
