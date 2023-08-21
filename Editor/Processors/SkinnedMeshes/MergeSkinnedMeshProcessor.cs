using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.ErrorReporting;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    internal class MergeSkinnedMeshProcessor : EditSkinnedMeshProcessor<MergeSkinnedMesh>
    {
        public MergeSkinnedMeshProcessor(MergeSkinnedMesh component) : base(component)
        {
        }

        public override IEnumerable<SkinnedMeshRenderer> Dependencies => Component.renderersSet.GetAsList();

        public override int ProcessOrder => int.MinValue;

        public override void Process(OptimizerSession session, MeshInfo2 target, MeshInfo2Holder meshInfo2Holder)
        {
            var meshInfos = Component.renderersSet.GetAsList().Select(meshInfo2Holder.GetMeshInfoFor)
                .Concat(Component.staticRenderersSet.GetAsList().Select(meshInfo2Holder.GetMeshInfoFor))
                .ToArray();
            var sourceMaterials = meshInfos.Select(x => x.SubMeshes.Select(y => y.SharedMaterial).ToArray()).ToArray();

            var (subMeshIndexMap, materials) = CreateMergedMaterialsAndSubMeshIndexMapping(sourceMaterials);

            var sourceRootBone = target.RootBone;
            var updateBounds = sourceRootBone && target.Bounds == default;

            target.Clear();
            target.SubMeshes.Capacity = Math.Max(target.SubMeshes.Capacity, materials.Count);
            foreach (var material in materials)
                target.SubMeshes.Add(new SubMesh(material));

            TexCoordStatus TexCoordStatusMax(TexCoordStatus x, TexCoordStatus y) =>
                (TexCoordStatus)Math.Max((int)x, (int)y);

            var newBoundMin = Vector3.positiveInfinity;
            var newBoundMax = Vector3.negativeInfinity;

            var mappings = new List<(string, string)>();
            var weightMismatchBlendShapes = new HashSet<string>();

            for (var i = 0; i < meshInfos.Length; i++)
            {
                var meshInfo = meshInfos[i];
                mappings.Clear();

                meshInfo.AssertInvariantContract($"processing source #{i} of {Target.gameObject.name}");

                target.Vertices.AddRange(meshInfo.Vertices);
                for (var j = 0; j < 8; j++)
                    target.SetTexCoordStatus(j,
                        TexCoordStatusMax(target.GetTexCoordStatus(j), meshInfo.GetTexCoordStatus(j)));

                for (var j = 0; j < meshInfo.SubMeshes.Count; j++)
                {
                    var targetSubMeshIndex = subMeshIndexMap[i][j];
                    target.SubMeshes[targetSubMeshIndex].Triangles.AddRange(meshInfo.SubMeshes[j].Triangles);
                    mappings.Add(($"m_Materials.Array.data[{j}]",
                        $"m_Materials.Array.data[{targetSubMeshIndex}]"));
                }


                // add blend shape if not defined by name
                for (var sourceI = 0; sourceI < meshInfo.BlendShapes.Count; sourceI++)
                {
                    var (name, weight) = meshInfo.BlendShapes[sourceI];
                    var newIndex = target.BlendShapes.FindIndex(x => x.name == name);
                    if (newIndex == -1)
                    {
                        newIndex = target.BlendShapes.Count;
                        target.BlendShapes.Add((name, weight));
                    }
                    else
                    {
                        // ReSharper disable once CompareOfFloatsByEqualityOperator
                        if (weight != target.BlendShapes[newIndex].weight)
                            weightMismatchBlendShapes.Add(name);
                    }

                    mappings.Add((VProp.BlendShapeIndex(sourceI), VProp.BlendShapeIndex(newIndex)));
                }

                if (updateBounds && meshInfo.RootBone)
                {
                    foreach (var inSource in meshInfo.Bounds.Corners())
                    {
                        var vector3 = sourceRootBone.InverseTransformPoint(
                            meshInfo.RootBone.TransformPoint(inSource));

                        newBoundMin.x = Mathf.Min(vector3.x, newBoundMin.x);
                        newBoundMin.y = Mathf.Min(vector3.y, newBoundMin.y);
                        newBoundMin.z = Mathf.Min(vector3.z, newBoundMin.z);
                        newBoundMax.x = Mathf.Max(vector3.x, newBoundMax.x);
                        newBoundMax.y = Mathf.Max(vector3.y, newBoundMax.y);
                        newBoundMax.z = Mathf.Max(vector3.z, newBoundMax.z);
                    }

                }

                session.MappingBuilder.RecordMoveProperties(meshInfo.SourceRenderer, mappings.ToArray());

                target.Bones.AddRange(meshInfo.Bones);

                target.HasColor |= meshInfo.HasColor;
                target.HasTangent |= meshInfo.HasTangent;

                target.AssertInvariantContract($"processing meshInfo {Target.gameObject.name}");
            }

            foreach (var weightMismatchBlendShape in weightMismatchBlendShapes)
                BuildReport.LogWarning("MergeSkinnedMesh:warning:blendShapeWeightMismatch", weightMismatchBlendShape);

            if (updateBounds && newBoundMin != Vector3.positiveInfinity && newBoundMax != Vector3.negativeInfinity)
            {
                target.Bounds.SetMinMax(newBoundMin, newBoundMax);
            }

            var boneTransforms = new HashSet<Transform>(target.Bones.Select(x => x.Transform));

            foreach (var renderer in Component.renderersSet.GetAsSet())
            {
                // Avatars can have animation to hide source meshes.
                // Such a animation often intended to hide/show some accessories but
                // after we merge mesh, it affects to big merged mesh.
                // This often be a unexpected behavior so we invalidate changing m_Enabled
                // property for original mesh in animation.
                // This invalidation doesn't affect to m_Enabled property of merged mesh.
                session.MappingBuilder.RecordRemoveProperty(renderer, "m_Enabled");
                session.MappingBuilder.RecordMergeComponent(renderer, Target);
                var rendererGameObject = renderer.gameObject;
                Object.DestroyImmediate(renderer);

                // process removeEmptyRendererObject
                if (!Component.removeEmptyRendererObject) continue;
                // no other components should be exist
                if (!rendererGameObject.GetComponents<Component>().All(x =>
                        x is AvatarTagComponent || x is Transform || x is SkinnedMeshRenderer)) continue;
                // no children is required
                if (rendererGameObject.transform.childCount != 0) continue;
                // the SkinnedMeshRenderer may also be used as bone. it's not good to remove
                if (boneTransforms.Contains(rendererGameObject.transform)) continue;
                Object.DestroyImmediate(rendererGameObject);
            }

            foreach (var renderer in Component.staticRenderersSet.GetAsSet())
            {
                Object.DestroyImmediate(renderer.GetComponent<MeshFilter>());
                Object.DestroyImmediate(renderer);
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

            public (string, float)[] BlendShapes() =>
                _processor.Component.renderersSet.GetAsList()
                    .SelectMany(EditSkinnedMeshComponentUtil.GetBlendShapes)
                    .Distinct(BlendShapeNameComparator.Instance)
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

            private class BlendShapeNameComparator : IEqualityComparer<(string name, float weight)>
            {
                public static readonly BlendShapeNameComparator Instance = new BlendShapeNameComparator();

                public bool Equals((string name, float weight) x, (string name, float weight) y)
                {
                    return x.name == y.name;
                }

                public int GetHashCode((string name, float weight) obj)
                {
                    return obj.name?.GetHashCode() ?? 0;
                }
            }
        }
    }
}
