using System.Linq;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class EditSkinnedMeshComponentProcessor
    {
        public void Process(OptimizerSession session)
        {
            foreach (var component in session.GetComponents<EditSkinnedMeshComponent>())
                EditSkinnedMeshComponentUtil.OnAwake(component);
            var renderers = session.GetComponents<SkinnedMeshRenderer>();
            var processorLists = EditSkinnedMeshComponentUtil.GetSortedProcessors(renderers);
            foreach (var processors in processorLists)
            {
                var target = new MeshInfo2(processors.Target);

                foreach (var processor in processors.GetSorted())
                    processor.Process(session, target);

                var mesh = processors.Target.sharedMesh
                    ? session.MayInstantiate(processors.Target.sharedMesh)
                    : session.AddToAsset(new Mesh());
                target.WriteToMesh(mesh);
                processors.Target.sharedMesh = mesh;
                for (var i = 0; i < target.BlendShapes.Count; i++)
                    processors.Target.SetBlendShapeWeight(i, target.BlendShapes[i].weight);
                processors.Target.sharedMaterials = target.SubMeshes.Select(x => x.SharedMaterial).ToArray();
                processors.Target.bones = target.Bones.Select(x => x.Transform).ToArray();
            }
        }
    }
}
