using System;
using Anatawa12.AvatarOptimizer.ErrorReporting;
using Anatawa12.AvatarOptimizer.ndmf;
using nadena.dev.ndmf;
using nadena.dev.ndmf.builtin;

[assembly: ExportsPlugin(typeof(OptimizerPlugin))]

namespace Anatawa12.AvatarOptimizer.ndmf
{
    internal class OptimizerContext : IExtensionContext
    {
        internal OptimizerSession session;
        
        public void OnActivate(BuildContext context)
        {
            session = new OptimizerSession(context.AvatarRootObject, false);
        }

        public void OnDeactivate(BuildContext context)
        {
            session.SaveMeshInfo2();
        }
    }
    
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
                .WithRequiredExtensions(new [] {typeof(OptimizerContext), typeof(BuildReportContext)}, seq =>
                {
                    seq.Run("TraceAndOptimize",
                            ctx =>
                            {
                                ctx.GetState<Processors.TraceAndOptimizeProcessor>().Process(ctx);
                            })
                        .Then.Run(Processors.ClearEndpointPositionProcessor.Instance)
                        .Then.Run(Processors.MergePhysBoneProcessor.Instance)
                        .Then.Run(Processors.EditSkinnedMeshComponentProcessor.Instance)
                        .Then.Run("MakeChildrenProcessor",
                            ctx => new Processors.MakeChildrenProcessor(early: false).Process(ctx)
                        )
                        .Then.Run("TraceAndOptimize:ProcessLater",
                            ctx => ctx.GetState<Processors.TraceAndOptimizeProcessor>().ProcessLater(ctx))
                        .Then.Run(Processors.MergeBoneProcessor.Instance)
                        .Then.Run(Processors.ApplyObjectMapping.Instance);
                });
        }

        protected override void OnUnhandledException(Exception e)
        {
            BuildReport.ReportInternalError(e);
        }
    }
}
