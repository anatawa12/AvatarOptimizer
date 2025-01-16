using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Debug = System.Diagnostics.Debug;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    internal class AutoMergeSkinnedMesh : TraceAndOptimizePass<AutoMergeSkinnedMesh>
    {
        public override string DisplayName => "T&O: AutoMergeSkinnedMesh";

        private static void Log(string log, [CallerLineNumber] int lineNum = 0)
        {
            UnityEngine.Debug.Log($"[AutoMergeSkinnedMesh:{lineNum}] {log}");
        }

        protected override void Execute(BuildContext context, TraceAndOptimizeState state)
        {
            if (!state.MergeSkinnedMesh) return;

            var mergeMeshes = FilterMergeMeshes(context, state);
            if (mergeMeshes.Count == 0) return;

            CategoryMeshesForMerge(context, mergeMeshes, out var categorizedMeshes, out var orphanMeshes);
            
            Func<MeshInfo2[], (int[][], List<(MeshTopology, Material?)>)> createSubMeshes;

            // createSubMeshes must preserve first material to be the first material
            if (state.SkipMergeMaterials)
                createSubMeshes = CreateSubMeshesNoMerge;
            else if (state.AllowShuffleMaterialSlots)
                createSubMeshes = CreateSubMeshesMergeShuffling;
            else
                createSubMeshes = CreateSubMeshesMergePreserveOrder;

            Log("MergeMaterialSlots");
            foreach (var orphanMesh in orphanMeshes)
                MergeMaterialSlot(orphanMesh, createSubMeshes);
            Log("MergeMaterialSlots End");

            MergeMeshes(context, state, categorizedMeshes, createSubMeshes);
        }

        public static List<MeshInfo2> FilterMergeMeshes(BuildContext context, TraceAndOptimizeState state)
        {
            Profiler.BeginSample("Collect for Dependencies to not merge dependant objects");

            var componentInfos = new GCComponentInfoHolder(context);

            new ComponentDependencyCollector(context, false, componentInfos).CollectAllUsages();

            foreach (var componentInfo in componentInfos.AllInformation)
            {
                if (componentInfo.IsEntrypoint)
                {
                    var component = componentInfo.Component;

                    var markContext = new MarkObjectContext(componentInfos, component, x => x.DependantEntrypoint);
                    markContext.MarkComponent(component, GCComponentInfo.DependencyType.Normal);
                    markContext.MarkRecursively();
                }
            }

            // if MMD World Compatibility is enabled, body mesh will be animated by MMD World
            if (state.MmdWorldCompatibility)
            {
                var mmdBody = context.AvatarRootTransform.Find("Body");
                if (mmdBody != null)
                {
                    var mmdBodyRenderer = mmdBody.GetComponent<SkinnedMeshRenderer>();
                    if (mmdBodyRenderer != null)
                    {
                        componentInfos.GetInfo(mmdBodyRenderer)
                            .DependantEntrypoint
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
                    var dependants = componentInfo.DependantComponents.ToList();
                    if (dependants.Count != 1 || dependants[0] != meshRenderer)
                    {
                        if (state.GCDebug)
                            UnityEngine.Debug.Log(
                                $"EntryPoints of {meshRenderer}: [{string.Join(", ", componentInfo.DependantComponents)}]");
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

        public static void CategoryMeshesForMerge(BuildContext context, List<MeshInfo2> mergeMeshes,
            out Dictionary<CategorizationKey, List<MeshInfo2>> categorizedMeshes, out List<MeshInfo2> orphanMeshes)
        {
            Log("Begin CategoryMeshesForMerge");
            // then, group by mesh attributes
            categorizedMeshes = new Dictionary<CategorizationKey, List<MeshInfo2>>();
            foreach (var meshInfo2 in mergeMeshes)
            {
                Log($"Processing {meshInfo2}");
                var activenessInfo = GetActivenessInformation(context, meshInfo2.SourceRenderer);
                if (activenessInfo == null)
                {
                    Log($"Activeness Animation from non-animator Detected:  {meshInfo2}");
                    continue; // animating activeness with non animator is not supported
                }

                var (activeness, activenessAnimationLocations) = activenessInfo.Value;

                var rendererAnimationLocations =
                    GetAnimationLocationsForRendererAnimation(context, (SkinnedMeshRenderer)meshInfo2.SourceRenderer);
                if (rendererAnimationLocations == null)
                {
                    Log($"AnimatingRendererProperties Detected: {meshInfo2}");
                    continue; // animating renderer properties with non animator is not supported
                }

                var key = new CategorizationKey(meshInfo2, activeness, activenessAnimationLocations,
                    rendererAnimationLocations);
                Log($"Categorization Key for {meshInfo2}: {key}");
                Log($"Categorization Key Hash: {key.GetHashCode()}");
                if (!categorizedMeshes.TryGetValue(key, out var list))
                {
                    list = new List<MeshInfo2>();
                    categorizedMeshes[key] = list;
                    Log($"We could not find existing key; creating new (id: {RuntimeHelpers.GetHashCode(list)})");
                }
                else
                {
                    Log($"We find existing key, the key has meshes: [{string.Join(", ", list)}] (id: {RuntimeHelpers.GetHashCode(list)})");
                    var existingKey = categorizedMeshes.First(x => x.Value == list).Key;
                    Log($"existing key: {existingKey}");
                    Log($"existing key.Equals(current key): {existingKey.Equals(key)}");
                    Log($"existing key HashCode: {existingKey.GetHashCode()}");
                }

                list.Add(meshInfo2);
                Log($"Added the mesh to key");
            }

            orphanMeshes = new List<MeshInfo2>();

            // remove single mesh group
            foreach (var (key, list) in categorizedMeshes.ToArray())
            {
                if (list.Count == 1)
                {
                    Log($"The key has {list[0]} is single mesh group; removing: {key}");
                    categorizedMeshes.Remove(key);
                    orphanMeshes.Add(list[0]);
                }
            }
            Log("End CategoryMeshesForMerge");

            Profiler.EndSample();
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

        public static void MergeMeshes(BuildContext context, TraceAndOptimizeState state,
            Dictionary<CategorizationKey, List<MeshInfo2>> categorizedMeshes,
            Func<MeshInfo2[], (int[][], List<(MeshTopology, Material?)>)> createSubMeshes)
        {
            Log("Begin MergeMeshes");
            Profiler.BeginSample("Merge Meshes");

            var index = 0;
            Func<GameObject> gameObjectFactory = () => new GameObject($"$$AAO_AUTO_MERGE_SKINNED_MESH_{index++}");

            var mappingBuilder = context.GetMappingBuilder();

            foreach (var (key, meshInfos) in categorizedMeshes)
            {
                Log($"Note: next mesh {(key.RendererAnimationLocations.Count == 0 ? "doesn't have" : "have")} renderer animation");
                if (key.RendererAnimationLocations.Count != 0 && state.SkipMergeMaterialAnimatingSkinnedMesh)
                    continue;

                if (key.Activeness != Activeness.Animating)
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
                        MergeAnimatingSkinnedMesh(context, gameObjectFactory, key, meshInfos, createSubMeshes,
                            mappingBuilder);
                    }
                }
            }

            Profiler.EndSample();
            Log("End MergeMeshes");
        }

        private static void MergeStaticSkinnedMesh(
            BuildContext context,
            Func<GameObject> gameObjectFactory,
            CategorizationKey key,
            List<MeshInfo2> meshInfos,
            Func<MeshInfo2[], (int[][], List<(MeshTopology, Material?)>)> createSubMeshes
        )
        {
            Log($"Creating Merged Mesh (Static) for [{string.Join(", ", meshInfos)}]: {key}");
            // if there's no activeness animation, we merge them at root
            var newSkinnedMeshRenderer = CreateNewRenderer(gameObjectFactory, context.AvatarRootTransform, key);
            Log($"The new Mesh is: {newSkinnedMeshRenderer}");
            newSkinnedMeshRenderer.gameObject.SetActive(key.Activeness == Activeness.AlwaysActive);
            var newMeshInfo = context.GetMeshInfoFor(newSkinnedMeshRenderer);
            var meshInfosArray = meshInfos.ToArray();

            var (subMeshIndexMap, materials) = createSubMeshes(meshInfosArray);

            MergeSkinnedMeshProcessor.DoMerge(context, newMeshInfo, meshInfosArray, subMeshIndexMap, materials);

            // We process FindUnusedObjects after this pass so we wipe empty renderer object in that pass
            MergeSkinnedMeshProcessor.RemoveOldRenderers(newMeshInfo, meshInfosArray,
                removeEmptyRendererObject: false);
        }

        private static void MergeAnimatingSkinnedMesh(
            BuildContext context,
            Func<GameObject> gameObjectFactory,
            CategorizationKey key,
            List<MeshInfo2> meshInfos,
            Func<MeshInfo2[], (int[][], List<(MeshTopology, Material?)>)> createSubMeshes,
            ObjectMappingBuilder<PropertyInfo> mappingBuilder)
        {
            Log($"Creating Merged Mesh (Animating) for [{string.Join(", ", meshInfos)}]: {key}");
            // if there is activeness animation, we have to decide the parent of merged mesh

            var commonParent = ComputeCommonParent(meshInfos, context.AvatarRootTransform);
            Log($"Common Parent is {commonParent} (path: {Utils.RelativePath(context.AvatarRootTransform, commonParent)})");

            var activenessAnimatingProperties =
                GetActivenessAnimationPropertiesNotAffectsCommonParent(context, meshInfos[0], commonParent);

            Log($"ActivenessAnimatingProperties: [{string.Join(", ", activenessAnimatingProperties.Select(tuple => $"[{tuple.Item2}].{tuple.Item3} = {tuple.Item1}"))}]");

            // we have to have intermediate GameObject to simulate activeness animation 
            commonParent = CreateIntermediateGameObjects(context, activenessAnimatingProperties, gameObjectFactory,
                commonParent, keepPropertyCount: 2);

            Log($"Common Parent is: {commonParent} (path: {Utils.RelativePath(context.AvatarRootTransform, commonParent)})");
            var newSkinnedMeshRenderer = CreateNewRenderer(gameObjectFactory, commonParent, key);

            // process rest activeness animation
            if (activenessAnimatingProperties.Count > 0)
            {
                var (initial, sourceComponent, property) = activenessAnimatingProperties.RemoveLast();

                mappingBuilder.RecordCopyProperty(
                    sourceComponent, property,
                    newSkinnedMeshRenderer.gameObject, Props.IsActive);
                newSkinnedMeshRenderer.gameObject.SetActive(initial);
                Log("Added ActivenessAnimation for SetActive");
            }

            if (activenessAnimatingProperties.Count > 0)
            {
                var (initial, sourceComponent, property) = activenessAnimatingProperties.RemoveLast();

                mappingBuilder.RecordCopyProperty(
                    sourceComponent, property,
                    newSkinnedMeshRenderer, Props.EnabledFor(newSkinnedMeshRenderer));
                newSkinnedMeshRenderer.enabled = initial;
                Log("Added ActivenessAnimation for enabled");
            }

            Utils.Assert(activenessAnimatingProperties.Count == 0);

            Log("Merging Mesh to new renderer");
            var newMeshInfo = context.GetMeshInfoFor(newSkinnedMeshRenderer);
            var meshInfosArray = meshInfos.ToArray();

            var (subMeshIndexMap, materials) = createSubMeshes(meshInfosArray);

            MergeSkinnedMeshProcessor.DoMerge(context, newMeshInfo, meshInfosArray, subMeshIndexMap, materials);

            // We process FindUnusedObjects after this pass so we wipe empty renderer object in that pass
            MergeSkinnedMeshProcessor.RemoveOldRenderers(newMeshInfo, meshInfosArray,
                removeEmptyRendererObject: false);
        }

        private static Transform ComputeCommonParent(IReadOnlyList<MeshInfo2> meshInfos, Transform avatarRoot)
        {
            // if there is activeness animation, we have to decide the parent of merged mesh
            var commonParents = new HashSet<Transform>(
                meshInfos[0].SourceRenderer.transform.ParentEnumerable(root: avatarRoot));
            foreach (var meshInfo in meshInfos.Skip(1))
                commonParents.IntersectWith(
                    meshInfo.SourceRenderer.transform.ParentEnumerable(root: avatarRoot));

            Transform? commonParent = null;
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

        private static List<(bool, ComponentOrGameObject, string)>
            GetActivenessAnimationPropertiesNotAffectsCommonParent(
                BuildContext context, MeshInfo2 meshInfo, Transform commonParent)
        {
            var properties = new List<(bool, ComponentOrGameObject, string)>();

            {
                if (context.GetAnimationComponent(meshInfo.SourceRenderer).IsAnimatedFloat(Props.EnabledFor(meshInfo.SourceRenderer)))
                {
                    properties.Add((meshInfo.SourceRenderer.enabled, meshInfo.SourceRenderer, Props.EnabledFor(meshInfo.SourceRenderer)));
                }
            }
            foreach (var transform in
                     meshInfo.SourceRenderer.transform.ParentEnumerable(commonParent, includeMe: true))
            {
                var gameObject = transform.gameObject;
                if (context.GetAnimationComponent(gameObject).IsAnimatedFloat(Props.IsActive))
                {
                    properties.Add((gameObject.activeSelf, gameObject, Props.IsActive));
                }
            }

            return properties;
        }

        private static Transform CreateIntermediateGameObjects(
            BuildContext context,
            IList<(bool, ComponentOrGameObject, string)> activenessAnimatingProperties,
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

                var (initial, sourceComponent, property) = activenessAnimatingProperties.RemoveLast();
                context.GetMappingBuilder().RecordCopyProperty(
                    sourceComponent, property,
                    newIntermediateGameObject, Props.IsActive);
                newIntermediateGameObject.SetActive(initial);

                commonParent = newIntermediateGameObject.transform;
            }

            return commonParent;
        }

        private static (Activeness, EqualsHashSet<(bool initial, EqualsHashSet<AnimationLocation> animations)>)?
            GetActivenessInformation(BuildContext context, Renderer component)
        {
            var alwaysInactive = false;
            var locations = new HashSet<(bool initial, EqualsHashSet<AnimationLocation> animations)>();
            {
                var p = context.GetAnimationComponent(component).GetFloatNode(Props.EnabledFor(component));
                if (p.ApplyState != AnimatorParsersV2.ApplyState.Never)
                {
                    if (p.ComponentNodes.Any(x => x is not AnimatorParsersV2.AnimatorPropModNode<AnimatorParsersV2.FloatValueInfo>))
                        return null;
                    locations.Add((component.enabled,
                        new EqualsHashSet<AnimationLocation>(AnimationLocation.CollectAnimationLocation(p))));
                }
                else
                {
                    alwaysInactive |= !component.enabled;
                }
            }
            foreach (var transform in
                     component.transform.ParentEnumerable(context.AvatarRootTransform, includeMe: true))
            {
                var p = context.GetAnimationComponent(transform.gameObject).GetFloatNode(Props.IsActive);
                if (p.ApplyState != AnimatorParsersV2.ApplyState.Never)
                {
                    if (p.ComponentNodes.Any(x => x is not AnimatorParsersV2.AnimatorPropModNode<AnimatorParsersV2.FloatValueInfo>))
                        return null;
                    locations.Add((transform.gameObject.activeSelf,
                        new EqualsHashSet<AnimationLocation>(AnimationLocation.CollectAnimationLocation(p))));
                }
                else
                {
                    alwaysInactive |= !transform.gameObject.activeSelf;
                }
            }

            Activeness activeness;
            if (alwaysInactive)
                activeness = Activeness.AlwaysInactive;
            else if (locations.Count == 0)
                activeness = Activeness.AlwaysActive;
            else
                activeness = Activeness.Animating;

            return (activeness,
                new EqualsHashSet<(bool initial, EqualsHashSet<AnimationLocation> animations)>(locations));
        }


        // defaultValue will be null if the animation is always applied. this means default value does not matter
        private static EqualsHashSet<(string property, float? defaultValue, EqualsHashSet<AnimationLocation> locations)>?
            GetAnimationLocationsForRendererAnimation(
                BuildContext context, SkinnedMeshRenderer component)
        {
            var locations = new HashSet<(string property, float? defaultValue, EqualsHashSet<AnimationLocation> location)>();
            var animationComponent = context.GetAnimationComponent(component);

            foreach (var (property, node) in animationComponent.GetAllFloatProperties())
            {
                if (property == Props.EnabledFor(typeof(SkinnedMeshRenderer))) continue; // m_Enabled is proceed separatedly
                if (!node.ComponentNodes.Any()) continue; // skip empty nodes, likely means PPtr animation
                if (property.StartsWith("blendShape.", StringComparison.Ordinal)) continue; // blendShapes are renamed so we don't need to collect animation location
                if (node.ComponentNodes.Any(x => x is not AnimatorParsersV2.AnimatorPropModNode<AnimatorParsersV2.FloatValueInfo>))
                    return null;

                float? defaultValue;
                if (node.ApplyState == AnimatorParsersV2.ApplyState.Always)
                {
                    defaultValue = null;
                }
                else
                {
                    if (GetDefaultValue(property, context, component) is not { } value)
                        return null;
                    defaultValue = value;
                }

                locations.Add((property, defaultValue,
                    new EqualsHashSet<AnimationLocation>(AnimationLocation.CollectAnimationLocation(node))));
            }

            return new EqualsHashSet<(string property, float? defaultValue, EqualsHashSet<AnimationLocation> location)>(locations);
        }

        private static SkinnedMeshRenderer CreateNewRenderer(
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

        // must preserve first material to be the first material
        public static (int[][], List<(MeshTopology, Material?)>) CreateSubMeshesNoMerge(MeshInfo2[] meshInfos)
        {
            var subMeshIndexMap = new int[meshInfos.Length][];
            var materials = new List<(MeshTopology topology, Material? material)>();
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

        private static float? GetDefaultValue(string property, BuildContext context, SkinnedMeshRenderer component)
        {
            var meshInfo = context.GetMeshInfoFor(component);
            if (property.StartsWith("material.", StringComparison.Ordinal))
            {
                var materialProperty = property.Substring("material.".Length);
                var material = meshInfo
                    .SubMeshes.FirstOrDefault()
                    ?.SharedMaterials?.FirstOrDefault();

                // according to experiment, if the material is not set, the value becomes 0
                if (material == null) return 0;

                if (materialProperty.Length > 3 && materialProperty[^2] == '.')
                {
                    // xyzw or rgba
                    var propertyName = materialProperty[..^2];
                    var channel = materialProperty[^1];

                    return channel switch
                    {
                        'x' => material.SafeGetVector(propertyName).x,
                        'y' => material.SafeGetVector(propertyName).y,
                        'z' => material.SafeGetVector(propertyName).z,
                        'w' => material.SafeGetVector(propertyName).w,
                        'r' => material.SafeGetColor(propertyName).r,
                        'g' => material.SafeGetColor(propertyName).g,
                        'b' => material.SafeGetColor(propertyName).b,
                        'a' => material.SafeGetColor(propertyName).a,
                        _ => null
                    };
                }
                else
                {
                    // float
                    return material.SafeGetFloat(materialProperty);
                }
            }

            throw new InvalidOperationException($"AAO forgot to implement handling for {property}");
        }

        private static bool HasUnsupportedComponents(GameObject gameObject)
        {
            return !gameObject.GetComponents<Component>().All(component =>
                component is Transform
                || component is SkinnedMeshRenderer
                || component is AvatarTagComponent
                || component is Animator);
        }

        internal enum Activeness
        {
            AlwaysActive,
            AlwaysInactive,
            Animating,
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
        internal struct CategorizationKey : IEquatable<CategorizationKey>
        {
            public bool HasNormals;

            public EqualsHashSet<(bool initial, EqualsHashSet<AnimationLocation> animation)>
                ActivenessAnimationLocations;

            // defaultValue will be null if the animation is always applied. this means default value does not matter
            public EqualsHashSet<(string property, float? defaultValue, EqualsHashSet<AnimationLocation> locations)>
                RendererAnimationLocations;
            public Activeness Activeness;

            // renderer properties
            public Bounds Bounds;
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
                Activeness activeness,
                EqualsHashSet<(bool initial, EqualsHashSet<AnimationLocation> animation)> activenessAnimationLocations,
                EqualsHashSet<(string property, float? defaultValue, EqualsHashSet<AnimationLocation> locations)>
                    rendererAnimationLocations
            )
            {
                var renderer = (SkinnedMeshRenderer)meshInfo2.SourceRenderer;

                HasNormals = meshInfo2.HasNormals;
                ActivenessAnimationLocations = activenessAnimationLocations;
                RendererAnimationLocations = rendererAnimationLocations;
                Activeness = activeness;

                Bounds = RoundError.Bounds(meshInfo2.Bounds);
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
                       ActivenessAnimationLocations.Equals(other.ActivenessAnimationLocations) &&
                       RendererAnimationLocations.Equals(other.RendererAnimationLocations) &&
                       Activeness == other.Activeness &&
                       Bounds.Equals(other.Bounds) &&
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

            public override bool Equals(object? obj)
            {
                return obj is CategorizationKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hashCode = new HashCode();
                hashCode.Add(HasNormals);
                hashCode.Add(ActivenessAnimationLocations);
                hashCode.Add(RendererAnimationLocations);
                hashCode.Add(Activeness);
                hashCode.Add(Bounds);
                hashCode.Add(ShadowCastingMode);
                hashCode.Add(ReceiveShadows);
                hashCode.Add(LightProbeUsage);
                hashCode.Add(ReflectionProbeUsage);
                hashCode.Add(AllowOcclusionWhenDynamic);
                hashCode.Add(LightProbeProxyVolumeOverride);
                hashCode.Add(ProbeAnchor);
                hashCode.Add(Quality);
                hashCode.Add(UpdateWhenOffscreen);
                hashCode.Add(RootBone);
                hashCode.Add(SkinnedMotionVectors);
                return hashCode.ToHashCode();
            }

            private static string ToString(AnimationLocation location)
            {
                return
                    $"Component({location.Component.name}) " +
                    $"Playable#{location.PlayableLayerIndex} " +
                    $"Animator#{location.AnimationLayerIndex} " +
                    $"State({location.AnimatorState.name}) " +
                    $"Tree[{string.Join(", ", location.BlendTreeLocation)}] " +
                    $"Clip({location.Clip.name}) " +
                    $"Curve({ToString(location.Curve)})";
            }

            private static string ToString(AnimationCurve curve)
            {
                var builder = new System.Text.StringBuilder();
                builder.Append("[");
                for (var i = 0; i < curve.length; i++)
                {
                    if (i != 0) builder.Append(", ");
                    builder.Append("Key(")
                        .Append("time: ").Append(curve[i].time).Append(", ")
                        .Append("value: ").Append(curve[i].value).Append(", ")
                        .Append("inTangent: ").Append(curve[i].inTangent).Append(", ")
                        .Append("outTangent: ").Append(curve[i].outTangent).Append(", ")
                        .Append("leftTangentMode: ").Append(AnimationUtility.GetKeyLeftTangentMode(curve, i)).Append(", ")
                        .Append("rightTangentMode: ").Append(AnimationUtility.GetKeyRightTangentMode(curve, i)).Append(", ")
                        .Append("inWeight: ").Append(curve[i].inWeight).Append(", ")
                        .Append("outWeight: ").Append(curve[i].outWeight)
                        .Append(")");
                }
                builder.Append("]");
                return builder.ToString();
            }

            public override string ToString()
            {
                return $"CategorizationKey{{" +
                       $"HasNormals: {HasNormals}, " +
                       $"ActivenessAnimationLocations: [{string.Join(", ", ActivenessAnimationLocations.backedSet.Select(x => $"(initial: {x.initial}, animation: [{string.Join(",", x.animation.backedSet.Select(ToString))}])"))}], " +
                       $"RendererAnimationLocations: [{string.Join(", ", RendererAnimationLocations.backedSet.Select(x => $"(property: {x}, animation: [{string.Join(",", x.locations.backedSet.Select(ToString))}])"))}], " +
                       $"Activeness: {Activeness}, " +
                       $"Bounds: {{{Bounds}}}, " +
                       $"ShadowCastingMode: {ShadowCastingMode}, " +
                       $"ReceiveShadows: {ReceiveShadows}, " +
                       $"LightProbeUsage: {LightProbeUsage}, " +
                       $"ReflectionProbeUsage: {ReflectionProbeUsage}, " +
                       $"AllowOcclusionWhenDynamic: {AllowOcclusionWhenDynamic}, " +
                       $"LightProbeProxyVolumeOverride: {LightProbeProxyVolumeOverride}, " +
                       $"ProbeAnchor: {ProbeAnchor}, " +
                       $"Quality: {Quality}, " +
                       $"UpdateWhenOffscreen: {UpdateWhenOffscreen}, " +
                       $"RootBone: {RootBone}, " +
                       $"SkinnedMotionVectors: {SkinnedMotionVectors}}}";
            }
        }
    }
}
