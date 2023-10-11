using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEditor;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    internal class InternalAutoFreezeMeaninglessBlendShapeProcessor : EditSkinnedMeshProcessor<InternalAutoFreezeMeaninglessBlendShape>
    {
        public InternalAutoFreezeMeaninglessBlendShapeProcessor(InternalAutoFreezeMeaninglessBlendShape component) : base(component)
        {
        }

        public override EditSkinnedMeshProcessorOrder ProcessOrder => EditSkinnedMeshProcessorOrder.AutoConfigureFreezeBlendShape;

        public override void Process(BuildContext context, MeshInfo2 target)
        {
            var meaningfulBlendShapes = new HashSet<string>();
            var state = context.GetState<TraceAndOptimizes.TraceAndOptimizeState>();
            if (state.PreserveBlendShapes.TryGetValue(Target, out var preserve))
                meaningfulBlendShapes.UnionWith(preserve);

            foreach (var vertex in target.Vertices)
                meaningfulBlendShapes.UnionWith(vertex.BlendShapes.Keys);

            var freezeBlendShape = Target.GetComponent<FreezeBlendShape>();
            var set = freezeBlendShape.shapeKeysSet.GetAsSet();
            set.UnionWith(target.BlendShapes.Where(x => !meaningfulBlendShapes.Contains(x.name))
                .Select(x => x.name));
            freezeBlendShape.shapeKeysSet.SetValueNonPrefab(set);
        }

        // nothing to do
        public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => upstream;
    }
}
