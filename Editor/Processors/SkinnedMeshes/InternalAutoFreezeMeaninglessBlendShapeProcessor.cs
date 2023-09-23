using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    internal class InternalAutoFreezeMeaninglessBlendShapeProcessor : EditSkinnedMeshProcessor<InternalAutoFreezeMeaninglessBlendShape>
    {
        public InternalAutoFreezeMeaninglessBlendShapeProcessor(InternalAutoFreezeMeaninglessBlendShape component) : base(component)
        {
        }

        public override EditSkinnedMeshProcessorOrder ProcessOrder => EditSkinnedMeshProcessorOrder.AutoConfigureFreezeBlendShape;

        public override void Process(OptimizerSession session, MeshInfo2 target)
        {
            var freezeBlendShape = Target.GetComponent<FreezeBlendShape>();
            var serialized = new SerializedObject(freezeBlendShape);
            var editorUtil = PrefabSafeSet.EditorUtil<string>.Create(
                serialized.FindProperty(nameof(FreezeBlendShape.shapeKeysSet)),
                0, p => p.stringValue, (p, v) => p.stringValue = v);
            foreach (var meaningLess in target.BlendShapeData.GetZeroInfluenceBlendshapes())
                editorUtil.GetElementOf(meaningLess).EnsureAdded();
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // nothing to do
        public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => upstream;
    }
}
