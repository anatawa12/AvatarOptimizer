#nullable enable

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
            var meaninglessBlendShapes = new HashSet<string>(target.BlendShapes.Select(x => x.name));
            var state = context.GetState<TraceAndOptimizes.TraceAndOptimizeState>();
            if (state.PreserveBlendShapes.TryGetValue(Target, out var preserve))
                meaninglessBlendShapes.ExceptWith(preserve);

            foreach (var vertex in target.Vertices)
                meaninglessBlendShapes.ExceptWith(vertex.BlendShapes.Keys);

            foreach (var meaninglessBlendShape in meaninglessBlendShapes)
                context.RecordRemoveProperty(Target, $"blendShape.{meaninglessBlendShape}");

            var freezeBlendShape = Target.GetComponent<FreezeBlendShape>();
            var set = freezeBlendShape.shapeKeysSet.GetAsSet();
            set.UnionWith(meaninglessBlendShapes);
            freezeBlendShape.shapeKeysSet.SetValueNonPrefab(set);
        }

        // nothing to do
        public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => upstream;
    }
}
