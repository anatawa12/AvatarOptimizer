#if NADEMOFU
using System;
using Anatawa12.AvatarOptimizer.ErrorReporting;
using Anatawa12.AvatarOptimizer.ndmf;
using Anatawa12.AvatarOptimizer.Processors;
using nadena.dev.ndmf;
using nadena.dev.ndmf.builtin;

[assembly: ExportsPlugin(typeof(OptimizerPlugin))]

namespace Anatawa12.AvatarOptimizer.ndmf
{
    internal class OptimizerContext : IExtensionContext
    {
        private IDisposable buildReportScope;
        internal OptimizerSession session;
        
        public void OnActivate(BuildContext context)
        {
            buildReportScope = BuildReport.ReportingOnAvatar(context.AvatarDescriptor);
            session = new OptimizerSession(context.AvatarRootObject, false, false);
        }

        public void OnDeactivate(BuildContext context)
        {
            session.MarkDirtyAll();
            buildReportScope.Dispose();
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
                .WithRequiredExtension(typeof(OptimizerContext), seq =>
                {
                    seq.Run("Early: UnusedBonesByReference",
                            ctx => new Processors.UnusedBonesByReferencesToolEarlyProcessor().Process(ctx)
                        )
                        .Then.Run("Early: MakeChildren",
                            ctx => new Processors.MakeChildrenProcessor(early: true).Process(ctx)
                        )
                        .BeforePass(RemoveEditorOnlyPass.Instance);
                });

            // Run everything else in the optimize phase
            InPhase(BuildPhase.Optimizing)
                .WithRequiredExtension(typeof(OptimizerContext), seq =>
                {
                    seq.Run("TraceAndOptimize",
                            ctx =>
                            {
                                ctx.GetState<TraceAndOptimizeProcessor>().Process(ctx);
                            })
                        .Then.Run("ClearEndpointPosition",
                            ctx => new Processors.ClearEndpointPositionProcessor().Process(ctx)
                        )
                        .Then.Run("MergePhysBone",
                            ctx => new Processors.MergePhysBoneProcessor().Process(ctx)
                        )
                        .Then.Run("MakeChildrenProcessor",
                            ctx => new Processors.MakeChildrenProcessor(early: false).Process(ctx)
                        )
                        .Then.Run("TraceAndOptimize:ProcessLater",
                            ctx => ctx.GetState<TraceAndOptimizeProcessor>().ProcessLater(ctx))
                        .Then.Run("MergeBoneProcessor", ctx => new Processors.MergeBoneProcessor().Process(ctx))
                        .Then.Run("ApplyObjectMapping",
                            ctx => new Processors.ApplyObjectMapping().Apply(ctx)
                        )
                        .Then.Run("SaveMeshInfo2", ctx => ((OptimizerSession) ctx).SaveMeshInfo2());
                });
        }
    }
}
#endif