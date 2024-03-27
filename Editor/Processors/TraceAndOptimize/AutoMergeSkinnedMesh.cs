using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using JetBrains.Annotations;
using nadena.dev.ndmf;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

using Debug = System.Diagnostics.Debug;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    internal class AutoMergeSkinnedMesh : TraceAndOptimizePass<AutoMergeSkinnedMesh>
    {
        public override string DisplayName => "T&O: AutoMergeSkinnedMesh";

        protected override void Execute(BuildContext context, TraceAndOptimizeState state)
        {
            if (!state.MergeSkinnedMesh) return;

            Profiler.BeginSample("Collect Merging Targets");
            var mergeMeshes = new List<MeshInfo2>();

            // first, filter Renderers
            foreach (var meshRenderer in context.GetComponents<SkinnedMeshRenderer>())
            {
                var meshInfo = context.GetMeshInfoFor(meshRenderer);
                if (
                    // MakeBoned can break the mesh in extremely rare cases with complex shader gimmicks
                    // so we can't call in T&O
                    meshInfo.Bones.Count > 0
                    // FlattenMultiPassRendering will increase polygon count by VRChat so it's not good for T&O
                    && meshInfo.SubMeshes.All(x => x.SharedMaterials.Length == 1)
                    // Merging Meshes with BlendShapes can increase rendering cost or break the animation
                    && meshInfo.BlendShapes.Count == 0
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

                    // other notes:
                    // - activeness animation can be ignored here because we'll combine based on activeness animation
                    // - normals existence can be ignored because we'll combine based on normals
                )
                {
                    mergeMeshes.Add(meshInfo);
                }
            }

            // then, group by mesh attributes
            var categorizedMeshes = new Dictionary<CategorizationKey, List<MeshInfo2>>();
            foreach (var meshInfo2 in mergeMeshes)
            {
                var activenessAnimationLocations = GetAnimationLocations(context, meshInfo2.SourceRenderer);
                if (activenessAnimationLocations == null)
                    continue; // animating activeness with non animator is not supported

                var rendererAnimationLocations =
                    GetAnimationLocationsForRendererAnimation(context, meshInfo2.SourceRenderer);

                var key = new CategorizationKey(meshInfo2, activenessAnimationLocations, rendererAnimationLocations);
                if (!categorizedMeshes.TryGetValue(key, out var list))
                {
                    list = new List<MeshInfo2>();
                    categorizedMeshes[key] = list;
                }

                list.Add(meshInfo2);
            }

            // remove single mesh group
            foreach (var (key, list) in categorizedMeshes.ToArray())
                if (list.Count == 1)
                    categorizedMeshes.Remove(key);

            Profiler.EndSample();

            if (categorizedMeshes.Count == 0) return;

            Profiler.BeginSample("Merge Meshes");

            Func<MeshInfo2[], (int[][], List<(MeshTopology, Material)>)> createSubMeshes;

            if (state.SkipMergeMaterials)
                createSubMeshes = CreateSubMeshesNoMerge;
            else if (state.AllowShuffleMaterialSlots)
                createSubMeshes = CreateSubMeshesMergeShuffling;
            else
                createSubMeshes = CreateSubMeshesMergePreserveOrder;

            var index = 0;
            Func<GameObject> gameObjectFactory = () => new GameObject($"$$AAO_AUTO_MERGE_SKINNED_MESH_{index++}");

            var mappingBuilder = context.GetMappingBuilder();

            foreach (var (key, meshInfos) in categorizedMeshes)
            {
                if (key.RendererAnimationLocations.Count != 0 && state.SkipMergeMaterialAnimatingSkinnedMesh)
                    continue;

                if (key.ActivenessAnimationLocations.Count == 0)
                {
                    if (!state.SkipMergeStaticSkinnedMesh)
                    {
                        MergeStaticSkinnedMesh(context, gameObjectFactory, key, meshInfos, createSubMeshes);
                    }
                }
                else
                {
                    if (!state.SkipMergeAnimatingSkinnedMesh)
                    {
                        MergeAnimatingSkinnedMesh(context, gameObjectFactory, key, meshInfos, createSubMeshes, mappingBuilder);
                    }
                }
            }

            Profiler.EndSample();
        }

        private void MergeStaticSkinnedMesh(
            BuildContext context,
            Func<GameObject> gameObjectFactory,
            CategorizationKey key,
            List<MeshInfo2> meshInfos,
            Func<MeshInfo2[], (int[][], List<(MeshTopology, Material)>)> createSubMeshes
        )
        {
            // if there's no activeness animation, we merge them at root
            var newSkinnedMeshRenderer = CreateNewRenderer(gameObjectFactory, context.AvatarRootTransform, key);
            var newMeshInfo = context.GetMeshInfoFor(newSkinnedMeshRenderer);
            var meshInfosArray = meshInfos.ToArray();

            var (subMeshIndexMap, materials) = createSubMeshes(meshInfosArray);

            MergeSkinnedMeshProcessor.DoMerge(context, newMeshInfo, meshInfosArray, subMeshIndexMap,
                materials);

            // We process FindUnusedObjects after this pass so we wipe empty renderer object in that pass
            MergeSkinnedMeshProcessor.RemoveOldRenderers(newMeshInfo, meshInfosArray,
                removeEmptyRendererObject: false);
        }

        private void MergeAnimatingSkinnedMesh(
            BuildContext context,
            Func<GameObject> gameObjectFactory,
            CategorizationKey key,
            List<MeshInfo2> meshInfos,
            Func<MeshInfo2[], (int[][], List<(MeshTopology, Material)>)> createSubMeshes,
            ObjectMappingBuilder<PropertyInfo> mappingBuilder)
        {
            // if there is activeness animation, we have to decide the parent of merged mesh

            var commonParent = ComputeCommonParent(meshInfos, context.AvatarRootTransform);

            var activenessAnimatingProperties =
                GetActivenessAnimationPropertiesNotAffectsCommonParent(context, meshInfos[0], commonParent);

            // we have to have intermediate GameObject to simulate activeness animation 
            commonParent = CreateIntermediateGameObjects(context, activenessAnimatingProperties, gameObjectFactory,
                commonParent, keepPropertyCount: 2);

            var newSkinnedMeshRenderer = CreateNewRenderer(gameObjectFactory, commonParent, key);

            // process rest activeness animation
            if (activenessAnimatingProperties.Count > 0)
            {
                var (sourceComponent, property) = activenessAnimatingProperties.RemoveLast();

                mappingBuilder.RecordCopyProperty(
                    sourceComponent, property,
                    newSkinnedMeshRenderer.gameObject, "m_IsActive");
            }

            if (activenessAnimatingProperties.Count > 0)
            {
                var (sourceComponent, property) = activenessAnimatingProperties.RemoveLast();

                mappingBuilder.RecordCopyProperty(
                    sourceComponent, property,
                    newSkinnedMeshRenderer, "m_Enabled");
            }

            Debug.Assert(activenessAnimatingProperties.Count == 0);

            var newMeshInfo = context.GetMeshInfoFor(newSkinnedMeshRenderer);
            var meshInfosArray = meshInfos.ToArray();

            var (subMeshIndexMap, materials) = createSubMeshes(meshInfosArray);

            MergeSkinnedMeshProcessor.DoMerge(context, newMeshInfo, meshInfosArray, subMeshIndexMap,
                materials);

            // We process FindUnusedObjects after this pass so we wipe empty renderer object in that pass
            MergeSkinnedMeshProcessor.RemoveOldRenderers(newMeshInfo, meshInfosArray,
                removeEmptyRendererObject: false);
        }

        [NotNull]
        private static Transform ComputeCommonParent(IReadOnlyList<MeshInfo2> meshInfos, [NotNull] Transform avatarRoot)
        {
            // if there is activeness animation, we have to decide the parent of merged mesh
            var commonParents = new HashSet<Transform>(
                meshInfos[0].SourceRenderer.transform.ParentEnumerable(root: avatarRoot));
            foreach (var meshInfo in meshInfos.Skip(1))
                commonParents.IntersectWith(
                    meshInfo.SourceRenderer.transform.ParentEnumerable(root: avatarRoot));

            Transform commonParent = null;
            // we merge at the child-most common parent
            foreach (var someCommonParent in commonParents)
            {
                if (someCommonParent.DirectChildrenEnumerable().All(c => !commonParents.Contains(c)))
                {
                    commonParent = someCommonParent;
                    break;
                }
            }

            // if there's no common parent, we merge them at root
            if (commonParent == null)
                commonParent = avatarRoot;

            return commonParent;
        }

        private static List<(ComponentOrGameObject, string)> GetActivenessAnimationPropertiesNotAffectsCommonParent(
            BuildContext context, MeshInfo2 meshInfo, Transform commonParent)
        {
            var activenessAnimationPropertiesNotAffectsCommonParent =
                new List<(ComponentOrGameObject, string)>();

            {
                if (context.GetAnimationComponent(meshInfo.SourceRenderer).ContainsFloat("m_Enabled"))
                {
                    activenessAnimationPropertiesNotAffectsCommonParent.Add((meshInfo.SourceRenderer,
                        "m_Enabled"));
                }
            }
            foreach (var transform in
                     meshInfo.SourceRenderer.transform.ParentEnumerable(commonParent, includeMe: true))
            {
                if (context.GetAnimationComponent(transform.gameObject).ContainsFloat("m_IsActive"))
                {
                    activenessAnimationPropertiesNotAffectsCommonParent.Add((transform.gameObject,
                        "m_IsActive"));
                }
            }

            return activenessAnimationPropertiesNotAffectsCommonParent;
        }

        private static Transform CreateIntermediateGameObjects(
            BuildContext context,
            IList<(ComponentOrGameObject, string)> activenessAnimatingProperties,
            Func<GameObject> gameObjectFactory,
            Transform commonParent,
            int keepPropertyCount)
        {
            while (activenessAnimatingProperties.Count > keepPropertyCount)
            {
                var newIntermediateGameObject = gameObjectFactory();
                newIntermediateGameObject.transform.SetParent(commonParent);
                newIntermediateGameObject.transform.localPosition = Vector3.zero;
                newIntermediateGameObject.transform.localRotation = Quaternion.identity;
                newIntermediateGameObject.transform.localScale = Vector3.one;

                var (sourceComponent, property) = activenessAnimatingProperties.RemoveLast();
                context.GetMappingBuilder().RecordCopyProperty(
                    sourceComponent, property,
                    newIntermediateGameObject, "m_IsActive");

                commonParent = newIntermediateGameObject.transform;
            }

            return commonParent;
        }

        [CanBeNull]
        private static HashSet<AnimationLocation> GetAnimationLocations(BuildContext context, Component component)
        {
            var locations = new HashSet<AnimationLocation>();
            {
                if (context.GetAnimationComponent(component).TryGetFloat("m_Enabled", out var p))
                {
                    if (p.ComponentNodes.Any(x => !(x is AnimatorParsersV2.AnimatorPropModNode<float>)))
                        return null;
                    locations.UnionWith(AnimationLocation.CollectAnimationLocation(p));
                }
            }
            foreach (var transform in
                     component.transform.ParentEnumerable(context.AvatarRootTransform, includeMe: true))
            {
                if (context.GetAnimationComponent(transform.gameObject).TryGetFloat("m_IsActive", out var p))
                {
                    if (p.ComponentNodes.Any(x => !(x is AnimatorParsersV2.AnimatorPropModNode<float>)))
                        return null;
                    locations.UnionWith(AnimationLocation.CollectAnimationLocation(p));
                }
            }

            return locations;
        }


        [CanBeNull]
        private static HashSet<(string property, AnimationLocation location)> GetAnimationLocationsForRendererAnimation(
            BuildContext context, Component component)
        {
            var locations = new HashSet<(string property, AnimationLocation location)>();
            var animationComponent = context.GetAnimationComponent(component);

            foreach (var (property, node) in animationComponent.GetAllFloatProperties())
            {
                if (property == "m_Enabled") continue; // m_Enabled is proceed separatedly
                if (node.ComponentNodes.Any(x => !(x is AnimatorParsersV2.AnimatorPropModNode<float>)))
                    return null;
                locations.UnionWith(AnimationLocation.CollectAnimationLocation(node)
                    .Select(location => (property, location)));
            }

            return locations;
        }

        private SkinnedMeshRenderer CreateNewRenderer(
            Func<GameObject> gameObjectFactory,
            Transform parent,
            CategorizationKey key
        )
        {
            var newRenderer = gameObjectFactory();
            newRenderer.transform.SetParent(parent);
            newRenderer.transform.localPosition = Vector3.zero;
            newRenderer.transform.localRotation = Quaternion.identity;
            newRenderer.transform.localScale = Vector3.one;

            var newSkinnedMeshRenderer = newRenderer.AddComponent<SkinnedMeshRenderer>();
            newSkinnedMeshRenderer.localBounds = key.Bounds;
            newSkinnedMeshRenderer.enabled = key.Enabled;
            newSkinnedMeshRenderer.shadowCastingMode = key.ShadowCastingMode;
            newSkinnedMeshRenderer.receiveShadows = key.ReceiveShadows;
            newSkinnedMeshRenderer.lightProbeUsage = key.LightProbeUsage;
            newSkinnedMeshRenderer.reflectionProbeUsage = key.ReflectionProbeUsage;
            newSkinnedMeshRenderer.allowOcclusionWhenDynamic = key.AllowOcclusionWhenDynamic;
            newSkinnedMeshRenderer.lightProbeProxyVolumeOverride = key.LightProbeProxyVolumeOverride;
            newSkinnedMeshRenderer.probeAnchor = key.ProbeAnchor;
            newSkinnedMeshRenderer.quality = key.Quality;
            newSkinnedMeshRenderer.updateWhenOffscreen = key.UpdateWhenOffscreen;
            newSkinnedMeshRenderer.rootBone = key.RootBone;
            newSkinnedMeshRenderer.skinnedMotionVectors = key.SkinnedMotionVectors;

            return newSkinnedMeshRenderer;
        }

        public static (int[][], List<(MeshTopology, Material)>) CreateSubMeshesNoMerge(MeshInfo2[] meshInfos)
        {
            var subMeshIndexMap = new int[meshInfos.Length][];
            var materials = new List<(MeshTopology topology, Material material)>();
            for (var i = 0; i < meshInfos.Length; i++)
            {
                var meshInfo = meshInfos[i];
                var indices = subMeshIndexMap[i] = new int[meshInfo.SubMeshes.Count];
                for (var j = 0; j < meshInfo.SubMeshes.Count; j++)
                {
                    indices[j] = materials.Count;
                    materials.Add(
                        (meshInfo.SubMeshes[j].Topology, meshInfo.SubMeshes[j].SharedMaterials[0]));
                }
            }

            return (subMeshIndexMap, materials);
        }

        public static (int[][], List<(MeshTopology, Material)>) CreateSubMeshesMergeShuffling(MeshInfo2[] meshInfos) =>
            MergeSkinnedMeshProcessor.GenerateSubMeshMapping(meshInfos, new HashSet<Material>());

        public static (int[][], List<(MeshTopology, Material)>) CreateSubMeshesMergePreserveOrder(MeshInfo2[] meshInfos)
        {
            // merge consecutive submeshes with same material to one for simpler logic
            // note: both start and end are inclusive
            var reducedMeshInfos =
                new LinkedList<((MeshTopology topology, Material material) info, (int start, int end) actualIndices)>
                    [meshInfos.Length];

            for (var meshI = 0; meshI < meshInfos.Length; meshI++)
            {
                var meshInfo = meshInfos[meshI];
                var reducedMeshInfo =
                    new LinkedList<((MeshTopology topology, Material material) info, (int start, int end) actualIndices
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

            var materials = new List<(MeshTopology topology, Material material)>();


            while (true)
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

                if (reducedMeshInfos.All(x => x.First == null)) break;
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
                    .Where(x => x.First != null)
                    .Select((x, meshIndex) => (x.First.Value, meshIndex))
                    .GroupBy(x => x.Value.info)
                    .OrderByDescending(x => x.Count())
                    .First()
                    .First()
                    .meshIndex;

                return mostUsedMaterial;
            }

            bool UsedByRest((MeshTopology topology, Material material) subMesh)
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

        private bool IsAnimatedForbidden(AnimationComponentInfo<PropertyInfo> component)
        {
            // any of object / pptr / material animation is forbidden
            if (component.GetAllObjectProperties().Any())
                return true;

            foreach (var (name, _) in component.GetAllFloatProperties())
            {
                // m_Enabled is allowed
                if (name == "m_Enabled") continue;
                // blendShapes are removed so it's allowed
                if (name.StartsWith("blendShapes.", StringComparison.Ordinal)) continue;
                // material properties are allowed, will be merged if animated similarly
                if (name.StartsWith("material.", StringComparison.Ordinal)) continue;
                // other float properties are forbidden
                return true;
            }

            return false;
        }

        private bool HasUnsupportedComponents(GameObject gameObject)
        {
            return !gameObject.GetComponents<Component>().All(component =>
                component is Transform
                || component is SkinnedMeshRenderer
                || component is AvatarTagComponent
                || component is Animator);
        }

        // Here's the all list of properties in SkinnedMeshRenderer
        // Renderer:
        // - bounds (local bounds) - must be same
        // - enabled - must be same
        // - shadowCastingMode - must be same
        // - receiveShadows - must be same
        // - forceRenderingOff - skip: not saved
        // - staticShadowCaster - no meaning for Built-in Render Pipeline
        // - motionVectorGenerationMode - no meaning for Skinned Mesh Renderer
        // - lightProbeUsage - must be same
        // - reflectionProbeUsage - must be same
        // - renderingLayerMask - no meaning for Built-in Render Pipeline
        // - rendererPriority - no meaning for Built-in Render Pipeline
        // - rayTracingMode - no meaning for Built-in Render Pipeline
        // - sortingLayerName - no meaning for Skinned Mesh Renderer
        // - sortingOrder - no meaning for Skinned Mesh Renderer
        // - allowOcclusionWhenDynamic - must be same
        // - lightProbeProxyVolumeOverride - must be same, null if lightProbeUsage is not ProxyVolume
        // - probeAnchor - must be same, null if lightProbeUsage and reflectionProbeUsage are off
        // - lightmapIndex - no meaning for Skinned Mesh Renderer
        // - realtimeLightmapIndex - no meaning for Skinned Mesh Renderer
        // - lightmapScaleOffset - no meaning for Skinned Mesh Renderer
        // - realtimeLightmapScaleOffset - no meaning for Skinned Mesh Renderer
        // - (shared)materials - merge
        // SkinnedMeshRenderer:
        // - quality - must be same
        // - updateWhenOffscreen - must be same
        // - forceMatrixRecalculationPerRender - skip: not saved
        // - rootBone - must be same
        // - bones - merge
        // - sharedMesh - merge
        // - skinnedMotionVectors - must be same
        // - blendShapes - always empty
        private struct CategorizationKey : IEquatable<CategorizationKey>
        {
            public bool HasNormals;
            [NotNull] public HashSet<AnimationLocation> ActivenessAnimationLocations;
            [NotNull] public HashSet<(string property, AnimationLocation location)> RendererAnimationLocations;

            // renderer properties
            public Bounds Bounds;
            public bool Enabled;
            public ShadowCastingMode ShadowCastingMode;
            public bool ReceiveShadows;
            public LightProbeUsage LightProbeUsage;
            public ReflectionProbeUsage ReflectionProbeUsage;
            public bool AllowOcclusionWhenDynamic;
            public GameObject LightProbeProxyVolumeOverride;
            public Transform ProbeAnchor;

            // skinned mesh renderer properties
            public SkinQuality Quality;
            public bool UpdateWhenOffscreen;
            public Transform RootBone;
            public bool SkinnedMotionVectors;

            public CategorizationKey(
                MeshInfo2 meshInfo2,
                HashSet<AnimationLocation> activenessAnimationLocations,
                HashSet<(string property, AnimationLocation location)> rendererAnimationLocations
            )
            {
                var renderer = (SkinnedMeshRenderer)meshInfo2.SourceRenderer;

                HasNormals = meshInfo2.HasNormals;
                ActivenessAnimationLocations = activenessAnimationLocations;
                RendererAnimationLocations = rendererAnimationLocations;

                Bounds = meshInfo2.Bounds;
                Enabled = renderer.enabled;
                ShadowCastingMode = renderer.shadowCastingMode;
                ReceiveShadows = renderer.receiveShadows;
                LightProbeUsage = renderer.lightProbeUsage;
                ReflectionProbeUsage = renderer.reflectionProbeUsage;
                AllowOcclusionWhenDynamic = renderer.allowOcclusionWhenDynamic;
                LightProbeProxyVolumeOverride = renderer.lightProbeProxyVolumeOverride;
                ProbeAnchor = renderer.probeAnchor;

                Quality = renderer.quality;
                UpdateWhenOffscreen = renderer.updateWhenOffscreen;
                RootBone = renderer.rootBone;
                SkinnedMotionVectors = renderer.skinnedMotionVectors;
            }

            public bool Equals(CategorizationKey other)
            {
                return HasNormals == other.HasNormals &&
                       ActivenessAnimationLocations.SetEquals(other.ActivenessAnimationLocations) &&
                       RendererAnimationLocations.SetEquals(other.RendererAnimationLocations) &&
                       Bounds.Equals(other.Bounds) &&
                       Enabled == other.Enabled &&
                       ShadowCastingMode == other.ShadowCastingMode &&
                       ReceiveShadows == other.ReceiveShadows &&
                       LightProbeUsage == other.LightProbeUsage &&
                       ReflectionProbeUsage == other.ReflectionProbeUsage &&
                       AllowOcclusionWhenDynamic == other.AllowOcclusionWhenDynamic &&
                       Equals(LightProbeProxyVolumeOverride, other.LightProbeProxyVolumeOverride) &&
                       Equals(ProbeAnchor, other.ProbeAnchor) &&
                       Quality == other.Quality &&
                       UpdateWhenOffscreen == other.UpdateWhenOffscreen &&
                       Equals(RootBone, other.RootBone) &&
                       SkinnedMotionVectors == other.SkinnedMotionVectors;
            }

            public override bool Equals(object obj)
            {
                return obj is CategorizationKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = HasNormals.GetHashCode();
                    hashCode = (hashCode * 397) ^ ActivenessAnimationLocations.GetHashCode2();
                    hashCode = (hashCode * 397) ^ RendererAnimationLocations.GetHashCode2();
                    hashCode = (hashCode * 397) ^ Bounds.GetHashCode();
                    hashCode = (hashCode * 397) ^ Enabled.GetHashCode();
                    hashCode = (hashCode * 397) ^ (int)ShadowCastingMode;
                    hashCode = (hashCode * 397) ^ ReceiveShadows.GetHashCode();
                    hashCode = (hashCode * 397) ^ (int)LightProbeUsage;
                    hashCode = (hashCode * 397) ^ (int)ReflectionProbeUsage;
                    hashCode = (hashCode * 397) ^ AllowOcclusionWhenDynamic.GetHashCode();
                    hashCode = (hashCode * 397) ^ (LightProbeProxyVolumeOverride != null
                        ? LightProbeProxyVolumeOverride.GetHashCode()
                        : 0);
                    hashCode = (hashCode * 397) ^ (ProbeAnchor != null ? ProbeAnchor.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (int)Quality;
                    hashCode = (hashCode * 397) ^ UpdateWhenOffscreen.GetHashCode();
                    hashCode = (hashCode * 397) ^ (RootBone != null ? RootBone.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ SkinnedMotionVectors.GetHashCode();
                    return hashCode;
                }
            }
        }
    }
}