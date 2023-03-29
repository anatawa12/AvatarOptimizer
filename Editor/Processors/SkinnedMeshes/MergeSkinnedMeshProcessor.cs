using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    internal class MergeSkinnedMeshProcessor : EditSkinnedMeshProcessor<MergeSkinnedMesh>
    {
        public MergeSkinnedMeshProcessor(MergeSkinnedMesh component) : base(component)
        {
        }

        public override int ProcessOrder => int.MinValue;

        public override void Process(OptimizerSession session, MeshInfo2 target, MeshInfo2Holder meshInfo2Holder)
        {
            var meshInfos = Component.renderersSet.GetAsList().Select(meshInfo2Holder.GetMeshInfoFor)
                .Concat(Component.staticRenderersSet.GetAsList().Select(meshInfo2Holder.GetMeshInfoFor))
                .ToArray();
            var sourceMaterials = meshInfos.Select(x => x.SubMeshes.Select(y => y.SharedMaterial).ToArray()).ToArray();

            var (subMeshIndexMap, materials) = CreateMergedMaterialsAndSubMeshIndexMapping(sourceMaterials);

            target.Clear();
            target.SubMeshes.Capacity = Math.Max(target.SubMeshes.Capacity, materials.Count);
            foreach (var material in materials)
                target.SubMeshes.Add(new SubMesh(material));

            TexCoordStatus TexCoordStatusMax(TexCoordStatus x, TexCoordStatus y) =>
                (TexCoordStatus)Math.Max((int)x, (int)y);

            for (var i = 0; i < meshInfos.Length; i++)
            {
                var meshInfo = meshInfos[i];

                meshInfo.AssertInvariantContract($"processing source #{i} of {Target.gameObject.name}");

                target.Vertices.AddRange(meshInfo.Vertices);
                for (var j = 0; j < 8; j++)
                    target.SetTexCoordStatus(j,
                        TexCoordStatusMax(target.GetTexCoordStatus(j), meshInfo.GetTexCoordStatus(j)));

                for (var j = 0; j < meshInfo.SubMeshes.Count; j++)
                    target.SubMeshes[subMeshIndexMap[i][j]].Triangles.AddRange(meshInfo.SubMeshes[j].Triangles);

                // add blend shape if not defined by name
                foreach (var (name, weight) in meshInfo.BlendShapes)
                    if (target.BlendShapes.FindIndex(x => x.name == name) == -1)
                        target.BlendShapes.Add((name, weight));

                target.Bones.AddRange(meshInfo.Bones);

                target.HasColor |= meshInfo.HasColor;

                target.AssertInvariantContract($"processing meshInfo {Target.gameObject.name}");
            }

            session.Destroy(Component);

            var boneTransforms = new HashSet<Transform>(target.Bones.Select(x => x.Transform));

            foreach (var renderer in Component.renderersSet.GetAsSet())
            {
                session.AddObjectMapping(renderer, Target);
                session.Destroy(renderer);

                // process removeEmptyRendererObject
                if (!Component.removeEmptyRendererObject) continue;
                // no other components should be exist
                if (!renderer.gameObject.GetComponents<Component>().All(x =>
                        x is AvatarTagComponent || x is Transform || x is SkinnedMeshRenderer)) continue;
                // no children is required
                if (renderer.transform.childCount != 0) continue;
                // the SkinnedMeshRenderer may also be used as bone. it's not good to remove
                if (boneTransforms.Contains(renderer.transform)) continue;
                session.Destroy(renderer.gameObject);
            }

            foreach (var renderer in Component.staticRenderersSet.GetAsSet())
            {
                session.Destroy(renderer.GetComponent<MeshFilter>());
                session.Destroy(renderer);
            }
        }

        private (int[][] mapping, List<Material> materials) CreateMergedMaterialsAndSubMeshIndexMapping(
            Material[][] sourceMaterials)
        {
            var doNotMerges = Component.doNotMergeMaterials.GetAsSet();
            var resultMaterials = new List<Material>();
            var resultIndices = new int[sourceMaterials.Length][];

            for (var i = 0; i < sourceMaterials.Length; i++)
            {
                var materials = sourceMaterials[i];
                var indices = resultIndices[i] = new int[materials.Length];

                for (var j = 0; j < materials.Length; j++)
                {
                    var material = materials[j];
                    var foundIndex = resultMaterials.IndexOf(material);
                    if (doNotMerges.Contains(material) || foundIndex == -1)
                    {
                        indices[j] = resultMaterials.Count;
                        resultMaterials.Add(material);
                    }
                    else
                    {
                        indices[j] = foundIndex;
                    }
                }
            }

            return (resultIndices, resultMaterials);
        }

        public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => new MeshInfoComputer(this);

        class MeshInfoComputer : IMeshInfoComputer
        {
            private readonly MergeSkinnedMeshProcessor _processor;

            public MeshInfoComputer(MergeSkinnedMeshProcessor processor) => _processor = processor;

            public string[] BlendShapes() =>
                _processor.Component.renderersSet.GetAsList()
                    .SelectMany(EditSkinnedMeshComponentUtil.GetBlendShapes)
                    .Distinct()
                    .ToArray();

            public Material[] Materials(bool fast = true)
            {
                var sourceMaterials = _processor.Component.renderersSet.GetAsList().Select(EditSkinnedMeshComponentUtil.GetMaterials)
                    .Concat(_processor.Component.staticRenderersSet.GetAsList().Select(x => x.sharedMaterials))
                    .ToArray();

                return _processor.CreateMergedMaterialsAndSubMeshIndexMapping(sourceMaterials)
                    .materials
                    .ToArray();
            }
        }
    }
}
