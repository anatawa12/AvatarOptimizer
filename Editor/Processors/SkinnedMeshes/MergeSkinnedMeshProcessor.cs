using System;
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

        public override void Process(OptimizerSession session, MeshInfo2 target)
        {
            var meshInfos = Component.renderers.Select(x => new MeshInfo2(x))
                .Concat(Component.staticRenderers.Select(x => new MeshInfo2(x)))
                .ToArray();

            var (subMeshIndexMap, subMeshesTotalCount) = CreateSubMeshIndexMapping(Component.merges, meshInfos);

            target.Clear();
            target.SubMeshes.Capacity = Math.Max(target.SubMeshes.Capacity, subMeshesTotalCount);
            for (var i = 0; i < subMeshesTotalCount; i++)
                target.SubMeshes.Add(new SubMesh());

            TexCoordStatus TexCoordStatusMax(TexCoordStatus x, TexCoordStatus y) =>
                (TexCoordStatus)Math.Max((int)x, (int)y);

            for (var i = 0; i < meshInfos.Length; i++)
            {
                var meshInfo = meshInfos[i];
                target.Vertices.AddRange(meshInfo.Vertices);
                for (var j = 0; j < 8; j++)
                    target.SetTexCoordStatus(j,
                        TexCoordStatusMax(target.GetTexCoordStatus(j), meshInfo.GetTexCoordStatus(j)));

                for (var j = 0; j < meshInfo.SubMeshes.Count; j++)
                {
                    target.SubMeshes[subMeshIndexMap[i][j]].SharedMaterial = meshInfo.SubMeshes[j].SharedMaterial;
                    target.SubMeshes[subMeshIndexMap[i][j]].Triangles.AddRange(meshInfo.SubMeshes[j].Triangles);
                }

                // add blend shape if not defined by name
                foreach (var (name, weight) in meshInfo.BlendShapes)
                    if (target.BlendShapes.FindIndex(x => x.name == name) != -1)
                        target.BlendShapes.Add((name, weight));

                target.Bones.AddRange(meshInfo.Bones);

                target.HasColor |= meshInfo.HasColor;
            }

            session.Destroy(Component);

            foreach (var renderer in Component.renderers)
            {
                session.AddObjectMapping(renderer, Target);
                session.Destroy(renderer);
                if (Component.removeEmptyRendererObject
                    && renderer.gameObject.GetComponents<Component>()
                        .All(x => x is AvatarTagComponent || x is Transform || x is SkinnedMeshRenderer))
                    session.Destroy(renderer.gameObject);
            }

            foreach (var renderer in Component.staticRenderers)
            {
                session.Destroy(renderer.GetComponent<MeshFilter>());
                session.Destroy(renderer);
            }
        }

        private (int[][] mapping, int subMeshTotalCount)
            CreateSubMeshIndexMapping(MergeSkinnedMesh.MergeConfig[] merges, MeshInfo2[] infos)
        {
            var result = new int[infos.Length][];

            // initialize with -1
            for (var i = 0; i < infos.Length; i++)
            {
                result[i] = new int[infos[i].SubMeshes.Count];
                for (var j = 0; j < result[i].Length; j++)
                    result[i][j] = -1;
            }

            for (var i = 0; i < merges.Length; i++)
                foreach (var pair in merges[i].merges)
                    result[(int)(pair >> 32)][(int)pair] = i;

            var nextIndex = merges.Length;

            foreach (var t in result)
                for (var j = 0; j < t.Length; j++)
                    if (t[j] == -1)
                        t[j] = nextIndex++;

            return (result, nextIndex);
        }

        public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => new MeshInfoComputer(this);

        class MeshInfoComputer : IMeshInfoComputer
        {
            private readonly MergeSkinnedMeshProcessor _processor;

            public MeshInfoComputer(MergeSkinnedMeshProcessor processor) => _processor = processor;

            public string[] BlendShapes() =>
                _processor.Component.renderers
                    .SelectMany(EditSkinnedMeshComponentUtil.GetBlendShapes)
                    .Distinct()
                    .ToArray();

            public Material[] Materials(bool fast = true) =>
                _processor.Component.merges
                    .Select(x => x.target)
                    .Concat(
                        _processor.Component.renderers
                            .SelectMany(EditSkinnedMeshComponentUtil.GetMaterials)
                            .Where(x => _processor.Component.merges.All(y => y.target != x)))
                    .ToArray();
        }
    }
}
