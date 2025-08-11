using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using nadena.dev.ndmf;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    internal class MergeMaterialSlots : TraceAndOptimizePass<MergeMaterialSlots>
    {
        public override string DisplayName => "T&O: MergeMaterialSlots";

        protected override void Execute(BuildContext context, TraceAndOptimizeState state)
        {
            if (!state.RemoveUnusedObjects) return;
            if (state.SkipMergeMaterials) return;

            var mergeMeshes = FilterMergeMeshes(context, state);
            if (mergeMeshes.Count == 0) return;

            Func<MeshInfo2[], (int[][], List<(MeshTopology, Material?)>)> createSubMeshes;

            // createSubMeshes must preserve first material to be the first material
            if (state.AllowShuffleMaterialSlots)
                createSubMeshes = CreateSubMeshesMergeShuffling;
            else
                createSubMeshes = CreateSubMeshesMergePreserveOrder;

            foreach (var orphanMesh in mergeMeshes)
                MergeMaterialSlot(orphanMesh, createSubMeshes);
        }

        public static List<MeshInfo2> FilterMergeMeshes(BuildContext context, TraceAndOptimizeState state)
        {
            Profiler.BeginSample("Collect for Dependencies to not merge dependant objects");

            var componentInfos = context.Extension<GCComponentInfoContext>();

            var entryPointMap = DependantMap.CreateEntrypointsMap(context);

            // if MMD World Compatibility is enabled, body mesh will be animated by MMD World
            if (state.MmdWorldCompatibility)
            {
                var mmdBody = context.AvatarRootTransform.Find("Body");
                if (mmdBody != null)
                {
                    if (mmdBody.TryGetComponent<SkinnedMeshRenderer>(out var mmdBodyRenderer))
                    {
                        entryPointMap[componentInfos.GetInfo(mmdBodyRenderer)]
                            .Add(context.AvatarRootTransform, GCComponentInfo.DependencyType.Normal);
                    }
                }
            }

            Profiler.EndSample();

            Profiler.BeginSample("Collect Merging Targets");
            var mergeMeshes = new List<MeshInfo2>();

            // first, filter Renderers
            foreach (var meshRenderer in context.GetComponents<SkinnedMeshRenderer>())
            {
                if (state.Exclusions.Contains(meshRenderer.gameObject)) continue;

                // if the renderer is referenced by other components, we can't merge it
                var componentInfo = componentInfos.TryGetInfo(meshRenderer);
                if (componentInfo != null)
                {
                    var dependants = entryPointMap[componentInfo].Keys.ToList();
                    if (dependants.Count != 1 || dependants[0] != meshRenderer)
                    {
                        continue;
                    }
                }

                var meshInfo = context.GetMeshInfoFor(meshRenderer);
                if (
                    // MakeBoned can break the mesh in extremely rare cases with complex shader gimmicks
                    // so we can't call in T&O
                    meshInfo.Bones.Count > 0
                    // FlattenMultiPassRendering will increase polygon count by VRChat so it's not good for T&O
                    && meshInfo.SubMeshes.All(x => x.SharedMaterials.Length == 1)
                    // Since 1.8.0 (targets 2022) we merge meshes with BlendShapes with RenameToAvoidConflict
                    // && meshInfo.BlendShapes.Count == 0
                    // Animating renderer is not supported by this optimization
                    && !IsAnimatedForbidden(context.GetAnimationComponent(meshRenderer))
                    // any other components are not supported
                    && !HasUnsupportedComponents(meshRenderer.gameObject)
                    // root bone must be defined
                    && meshInfo.RootBone != null
                    // light probe usage must be defined if reflection probe usage is defined
                    && (meshRenderer.lightProbeUsage == LightProbeUsage.Off
                        && meshRenderer.reflectionProbeUsage == ReflectionProbeUsage.Off
                        || meshRenderer.probeAnchor != null)
                    // light probe proxy volume override must be defined if light probe usage is UseProxyVolume
                    && (meshRenderer.lightProbeUsage != LightProbeUsage.UseProxyVolume
                        || meshRenderer.lightProbeProxyVolumeOverride != null)
                    // shader must not use vertex index.
                    // Even if the original mesh is already shuffled, we won't merge automatically because
                    // user may configure material to match with affected vertex index.
                    // Note for users reading this comment: Vertex Index after remove mesh or merge mesh is 
                    // not guaranteed so upgrading Avatar Optimizer may break your avatar if you rely on vertex index
                    // after Remove Mesh By **** or Merge Skinned Mesh.
                    && !context.GetAllPossibleMaterialFor(meshRenderer)
                        .Any(x => context.GetMaterialInformation(x)?.UseVertexIndex ?? false)

                    // other notes:
                    // - activeness animation can be ignored here because we'll combine based on activeness animation
                    // - normals existence can be ignored because we'll combine based on normals
                )
                {
                    mergeMeshes.Add(meshInfo);
                }
            }

            return mergeMeshes;
        }

        private void MergeMaterialSlot(MeshInfo2 orphanMesh,
            Func<MeshInfo2[], (int[][], List<(MeshTopology, Material?)>)> createSubMeshes)
        {
            var (mapping, subMeshInfos) = createSubMeshes(new[] { orphanMesh });
            var subMeshes = orphanMesh.SubMeshes.ToList();

            orphanMesh.SubMeshes.Clear();
            foreach (var (meshTopology, material) in subMeshInfos)
                orphanMesh.SubMeshes.Add(new SubMesh(material, meshTopology));

            for (var i = 0; i < subMeshes.Count; i++)
                orphanMesh.SubMeshes[mapping[0][i]].Vertices.AddRange(subMeshes[i].Vertices);
        }

        public static (int[][], List<(MeshTopology, Material?)>) CreateSubMeshesMergeShuffling(MeshInfo2[] meshInfos) =>
            MergeSkinnedMeshProcessor.GenerateSubMeshMapping(meshInfos, new HashSet<Material>());

        // must preserve first material to be the first material
        public static (int[][], List<(MeshTopology, Material?)>) CreateSubMeshesMergePreserveOrder(MeshInfo2[] meshInfos)
        {
            // merge consecutive submeshes with same material to one for simpler logic
            // note: both start and end are inclusive
            var reducedMeshInfos =
                new LinkedList<((MeshTopology topology, Material? material) info, (int start, int end) actualIndices)>
                    [meshInfos.Length];

            for (var meshI = 0; meshI < meshInfos.Length; meshI++)
            {
                var meshInfo = meshInfos[meshI];
                var reducedMeshInfo =
                    new LinkedList<((MeshTopology topology, Material? material) info, (int start, int end) actualIndices
                        )>();

                if (meshInfo.SubMeshes.Count > 0)
                {
                    reducedMeshInfo.AddLast(((meshInfo.SubMeshes[0].Topology, meshInfo.SubMeshes[0].SharedMaterial),
                        (0, 0)));

                    for (var subMeshI = 1; subMeshI < meshInfo.SubMeshes.Count; subMeshI++)
                    {
                        var info = (meshInfo.SubMeshes[subMeshI].Topology, meshInfo.SubMeshes[subMeshI].SharedMaterial);
                        var last = reducedMeshInfo.Last.Value;
                        if (last.info.Equals(info))
                        {
                            last.actualIndices.end = subMeshI;
                            reducedMeshInfo.Last.Value = last;
                        }
                        else
                        {
                            reducedMeshInfo.AddLast((info, (subMeshI, subMeshI)));
                        }
                    }
                }

                reducedMeshInfos[meshI] = reducedMeshInfo;
            }

            var subMeshIndexMap = new int[reducedMeshInfos.Length][];
            for (var i = 0; i < meshInfos.Length; i++)
                subMeshIndexMap[i] = new int[meshInfos[i].SubMeshes.Count];

            var materials = new List<(MeshTopology topology, Material? material)>();


            while (reducedMeshInfos.Any(x => x.First != null))
            {
                var meshIndex = GetNextAddingMeshIndex();

                var meshInfo = reducedMeshInfos[meshIndex];
                var currentNode = meshInfo.First;

                var destMaterialIndex = materials.Count;
                materials.Add(currentNode.Value.info);

                for (var index = 0; index < reducedMeshInfos.Length; index++)
                {
                    var reducedMeshInfo = reducedMeshInfos[index];
                    if (reducedMeshInfo.First != null && reducedMeshInfo.First.Value.info == currentNode.Value.info)
                    {
                        var actualIndex = reducedMeshInfo.First.Value.actualIndices;
                        for (var subMeshI = actualIndex.start; subMeshI <= actualIndex.end; subMeshI++)
                            subMeshIndexMap[index][subMeshI] = destMaterialIndex;

                        reducedMeshInfo.RemoveFirst();
                    }
                }
            }

            return (subMeshIndexMap, materials);

            int GetNextAddingMeshIndex()
            {
                // first, try to find the first material that is not used by other (non-first)
                for (var meshIndex = 0; meshIndex < reducedMeshInfos.Length; meshIndex++)
                {
                    var meshInfo = reducedMeshInfos[meshIndex];
                    var currentNode = meshInfo.First;
                    if (currentNode == null) continue;

                    if (!UsedByRest(currentNode.Value.info))
                    {
                        return meshIndex;
                    }
                }

                // then, find most-used material
                var mostUsedMaterial = reducedMeshInfos
                    .Select((value, meshIndex) => (value, meshIndex))
                    .Where(x => x.value.First != null)
                    .GroupBy(x => x.value.First.Value.info)
                    .OrderByDescending(x => x.Count())
                    .First()
                    .First()
                    .meshIndex;

                return mostUsedMaterial;
            }

            bool UsedByRest((MeshTopology topology, Material? material) subMesh)
            {
                foreach (var meshInfo in reducedMeshInfos)
                {
                    var currentNode = meshInfo.First;
                    if (currentNode == null) continue;

                    if (currentNode.Value.info == subMesh)
                        currentNode = currentNode.Next; // skip same material at first

                    if (currentNode == null) continue;

                    // returns true if the material is used by other subMesh
                    while (currentNode != null)
                    {
                        if (currentNode.Value.info == subMesh)
                            return true;
                        currentNode = currentNode.Next;
                    }
                }

                return false;
            }
        }

        private static bool IsAnimatedForbidden(AnimationComponentInfo<PropertyInfo> component)
        {
            // any of object / pptr / material animation is forbidden
            if (component.GetAllObjectProperties().Any(x => x.node.ComponentNodes.Any()))
                return true;

            foreach (var (name, node) in component.GetAllFloatProperties())
            {
                // skip non animating ones
                if (!node.ComponentNodes.Any()) continue;
                // m_Enabled is allowed
                if (name == Props.EnabledFor(typeof(SkinnedMeshRenderer))) continue;

                // Note: when you added some other allowed properties,
                // You have to add default value handling in GetDefaultValue below

                // blendShapes are renamed to avoid conflict, so it's allowed
                if (name.StartsWith("blendShape.", StringComparison.Ordinal)) continue;
                // material properties are allowed, will be merged if animated similarly
                if (name.StartsWith("material.", StringComparison.Ordinal)) continue;
                // other float properties are forbidden
                return true;
            }

            return false;
        }

        private static bool HasUnsupportedComponents(GameObject gameObject)
        {
            return !gameObject.GetComponents<Component>().All(component =>
                component is Transform
                || component is SkinnedMeshRenderer
                || component is AvatarTagComponent
                || component is Animator);
        }
    }
}
