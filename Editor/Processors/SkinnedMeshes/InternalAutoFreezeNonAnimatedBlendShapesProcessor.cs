using System;
using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using nadena.dev.ndmf;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    internal class InternalAutoFreezeNonAnimatedBlendShapesProcessor : EditSkinnedMeshProcessor<InternalAutoFreezeNonAnimatedBlendShapes>
    {
        public InternalAutoFreezeNonAnimatedBlendShapesProcessor(InternalAutoFreezeNonAnimatedBlendShapes component) : base(component)
        {
        }

        public override EditSkinnedMeshProcessorOrder ProcessOrder => EditSkinnedMeshProcessorOrder.AutoConfigureFreezeBlendShape;

        public override void Process(BuildContext context, MeshInfo2 target)
        {
            var modifies = context.GetAnimationComponent(Target);

            var unchanged = new HashSet<string>();

            for (var i = 0; i < target.BlendShapes.Count; i++)
            {
                var (name, weight) = target.BlendShapes[i];
                if (IsUnchangedBlendShape(name, weight, out var newWeight))
                {
                    unchanged.Add(name);
                    target.BlendShapes[i] = (name, newWeight);
                }
            }

            bool IsUnchangedBlendShape(string name, float weight, out float newWeight)
            {
                newWeight = weight;
                var prop = modifies.GetFloatNode($"blendShape.{name}");

                switch (prop.ApplyState)
                {
                    case ApplyState.Always:
                    {
                        if (prop.Value.TryGetConstantValue(weight, out var constWeight))
                        {
                            newWeight = constWeight;
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    case ApplyState.Partially:
                    {
                        return prop.Value.TryGetConstantValue(weight, out var constWeight) && constWeight.Equals(weight);
                    }
                    case ApplyState.Never:
                        return true;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (unchanged.Count == 0) return;

            var freeze = Target.GetComponent<FreezeBlendShape>();
            var shapeKeys = freeze.shapeKeysSet.GetAsSet();
            shapeKeys.UnionWith(unchanged);
            freeze.shapeKeysSet.SetValueNonPrefab(shapeKeys);
        }

        // nothing to do
        public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => upstream;
    }
}
