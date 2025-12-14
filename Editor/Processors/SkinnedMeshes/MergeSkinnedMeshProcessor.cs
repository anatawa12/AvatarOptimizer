using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using nadena.dev.ndmf;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Debug = System.Diagnostics.Debug;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    internal class MergeSkinnedMeshProcessor : EditSkinnedMeshProcessor<MergeSkinnedMesh>
    {
        public MergeSkinnedMeshProcessor(MergeSkinnedMesh component) : base(component)
        {
        }

        public override IEnumerable<SkinnedMeshRenderer> Dependencies => SkinnedMeshRenderers;

        private IEnumerable<SkinnedMeshRenderer> SkinnedMeshRenderers =>
            Component.renderersSet.GetAsList().Except(new[] { Target });
        
        private IEnumerable<MeshRenderer> BasicMeshRenderers =>
            Component.staticRenderersSet.GetAsList();

        public override EditSkinnedMeshProcessorOrder ProcessOrder => EditSkinnedMeshProcessorOrder.Generation;

        public override void Process(BuildContext context, MeshInfo2 target)
        {
            var (disposableList, meshInfos) = CollectMeshInfos(context);
            using var _ = disposableList;

            GenerateWarningsOrErrors(context, Component.copyEnablementAnimation, Component.blendShapeMode, target, meshInfos);

            var (subMeshIndexMap, materials) =
                GenerateSubMeshMapping(meshInfos, Component.doNotMergeMaterials.GetAsSet());

            if (Component.copyEnablementAnimation)
                CopyEnablementAnimation(context, target, meshInfos);

            var targetAnimatedProperties = context.GetAnimationComponent(target.SourceRenderer)
                .GetAllFloatProperties()
                .Where(x => x.node.ComponentNodes.Any())
                .Where(x => x.property.StartsWith("material.", StringComparison.Ordinal))
                .Select(x => x.property["material.".Length..])
                .ToHashSet();

            DoMerge(context, target, meshInfos, subMeshIndexMap, materials, Component.blendShapeMode, targetAnimatedProperties);
            MergeBounds(target, meshInfos);

            RemoveOldRenderers(target, meshInfos, Component.removeEmptyRendererObject);

            var gcInfo = context.Extension<GCComponentInfoContext>().GetInfo(target.SourceRenderer);
            gcInfo.ClearDependencies();
            APIInternal.SkinnedMeshRendererInformation.AddDependencyInformation(gcInfo, target);
        }

        public (DisposableList<MeshInfo2>, MeshInfo2[] meshInfos) CollectMeshInfos(BuildContext context)
        {
            List<SkinnedMeshRenderer> skinnedMeshRenderers;
            List<MeshRenderer> basicMeshRenderers;
            if (Component.skipEnablementMismatchedRenderers)
            {
                bool RendererEnabled(Renderer x)
                {
                    if (!x.enabled) return false;
                    for (var transform = x.transform;
                         transform != null && transform != context.AvatarRootTransform;
                         transform = transform.parent)
                        if (!transform.gameObject.activeSelf)
                            return false;
                    return true;
                }

                var enabledSelf = RendererEnabled(Target);
                skinnedMeshRenderers = SkinnedMeshRenderers.Where(x => RendererEnabled(x) == enabledSelf).ToList();
                basicMeshRenderers = BasicMeshRenderers.Where(x => RendererEnabled(x) == enabledSelf).ToList();
            }
            else
            {
                skinnedMeshRenderers = SkinnedMeshRenderers.ToList();
                basicMeshRenderers = BasicMeshRenderers.ToList();
            }

            Profiler.BeginSample("Collect MeshInfos");
            // Owns staticRendererMeshInfos
            var basicRendererMeshInfos = basicMeshRenderers.Select(renderer => new MeshInfo2(renderer)).ToDisposableList();
            try
            {
                var meshInfos = skinnedMeshRenderers.Select(context.GetMeshInfoFor)
                    .Concat(basicRendererMeshInfos)
                    .ToArray();

                foreach (var meshInfo2 in meshInfos) meshInfo2.FlattenMultiPassRendering("Merge Skinned Mesh");
                foreach (var meshInfo2 in meshInfos) meshInfo2.MakeBoned(evenIfBasicMesh: true);
                Profiler.EndSample();

                return (basicRendererMeshInfos, meshInfos);
            }
            catch
            {
                basicRendererMeshInfos.Dispose();
                throw;
            }
        }

        public static void GenerateWarningsOrErrors(BuildContext context,
            bool copyEnablementAnimation,
            MergeSkinnedMesh.BlendShapeMode componentBlendShapeMode,
            MeshInfo2 target,
            MeshInfo2[] meshInfos
        ) {
            Profiler.BeginSample("Material / Shader Parameter Animation Warnings");
            MaterialParameterAnimationWarnings(meshInfos, target, context);
            Profiler.EndSample();

            if (componentBlendShapeMode == MergeSkinnedMesh.BlendShapeMode.MergeSameName)
            {
                // error if multiple renderers have animated for one blend shape
                var animatedComponentByBlendShape = new Dictionary<string, HashSet<MeshInfo2>>();

                foreach (var meshInfo in meshInfos.Concat(new[] { target }))
                foreach (var (name, _) in meshInfo.BlendShapes)
                {
                    var node = context.GetAnimationComponent(meshInfo.SourceRenderer).GetFloatNode($"blendShape.{name}");
                    if (!node.ComponentNodes.Any()) continue;

                    if (!animatedComponentByBlendShape.TryGetValue(name, out var set))
                        animatedComponentByBlendShape.Add(name, set = new HashSet<MeshInfo2>());
                    set.Add(meshInfo);
                }

                foreach (var (name, renderers) in animatedComponentByBlendShape.Where(x => x.Value.Count > 1))
                {
                    var locations = renderers.Select(renderer =>
                            AnimationLocation.CollectAnimationLocation(context
                                    .GetAnimationComponent(renderer.SourceRenderer)
                                    .GetFloatNode($"blendShape.{name}"))
                                .ToEqualsHashSet())
                        .ToArray();

                    if (locations.Distinct().LongCount() >= 1)
                    {
                        BuildLog.LogError("MergeSkinnedMesh:error:blendShape-animated-by-multiple-renderers",
                            name, renderers);
                    }
                }
            }

            Profiler.BeginSample("Material Normal Configuration Check");
            // check normal information.
            int hasNormal = 0;
            foreach (var meshInfo2 in meshInfos)
            {
                if (meshInfo2.Vertices.Count != 0)
                    hasNormal |= meshInfo2.HasNormals ? 1 : 2;
            }

            if (hasNormal == 3)
            {
                // collect (skinned) mesh renderers who doesn't have normal
                // to show the list on the error reporting
                BuildLog.LogError("MergeSkinnedMesh:error:mix-normal-existence",
                    from meshInfo2 in meshInfos
                    where meshInfo2.Vertices.Count != 0 && !meshInfo2.HasNormals
                    select meshInfo2.SourceRenderer);
            }

            Profiler.EndSample();

            Profiler.BeginSample("Generate RootBone / Anchor Override Warning");
            // we see rootBone of SkinnedMeshRenderer since MeshInfo2 have renderer as a RootBone
            if (((SkinnedMeshRenderer)target.SourceRenderer).rootBone == null)
                BuildLog.LogWarning("MergeSkinnedMesh:warning:no-root-bone", target.SourceRenderer);
            if (target.SourceRenderer.probeAnchor == null &&
                (target.SourceRenderer.lightProbeUsage != LightProbeUsage.Off ||
                 target.SourceRenderer.reflectionProbeUsage != ReflectionProbeUsage.Off))
                BuildLog.LogWarning("MergeSkinnedMesh:warning:no-probe-anchor", target.SourceRenderer);
            Profiler.EndSample();

            Profiler.BeginSample("Generate ActivenessWarning");
            if (copyEnablementAnimation)
                CheckForCopyEnablementAnimation(context, target, meshInfos);
            else
                ActivenessAnimationWarning(meshInfos.Select(x => x.SourceRenderer).Where(x => x), target.SourceRenderer, context);
            Profiler.EndSample();

            Profiler.BeginSample("Warn / Error Unsupported Components");
            foreach (var renderer in meshInfos.Select(x => x.SourceRenderer).Where(x => x && x is SkinnedMeshRenderer))
            {
                if (renderer.TryGetComponent<RemoveZeroSizedPolygon>(out var removeZeroSizedPolygon))
                {
                    BuildLog.LogWarning("MergeSkinnedMesh:warning:removeZeroSizedPolygonOnSources",
                        removeZeroSizedPolygon);
                    DestroyTracker.DestroyImmediate(removeZeroSizedPolygon);
                }

                if (renderer.TryGetComponent<Cloth>(out var cloth))
                {
                    BuildLog.LogError("MergeSkinnedMesh:error:clothOnSources", cloth);
                    DestroyTracker.DestroyImmediate(cloth);
                }
            }

            Profiler.EndSample();

            Profiler.BeginSample("BlendShape Default Weight Mismatch Warning");
            var defaultWeights = new Dictionary<string, float>();

            foreach (var meshInfo in meshInfos)
            {
                // add BlendShape if not defined by name
                foreach (var (name, weight) in meshInfo.BlendShapes)
                {
                    if (defaultWeights.TryGetValue(name, out var existingWeight))
                    {
                        // ReSharper disable once CompareOfFloatsByEqualityOperator
                        // this value is likely to be a integer value without any arithmetic operation
                        if (existingWeight != weight)
                            BuildLog.LogWarning("MergeSkinnedMesh:warning:blendShapeWeightMismatch", name);
                    }
                    else
                    {
                        defaultWeights.Add(name, weight);
                    }
                }
            }

            Profiler.EndSample();

        }

        public static void CopyEnablementAnimation(
            BuildContext context,
            MeshInfo2 target,
            MeshInfo2[] meshInfos)
        {
            // This implementation assumes that no errors in previous checks.
            // if there is errors, this might create unexpected behavior.

            if (meshInfos.Length == 0) return;

            var commonRoot = Utils.CommonRoot(meshInfos.Select(x => x.SourceRenderer.transform).Append(target.SourceRenderer.transform));

            if (commonRoot == null) return;

            var locations = GetActivenessAnimationLocations(context, meshInfos[0].SourceRenderer, commonRoot).FirstOrDefault();
            if (locations.Item2 == null) return; // this means no activeness animation

            var builder = context.GetMappingBuilder();
            builder.RecordRemoveProperty(target.SourceRenderer, Props.EnabledFor(target.SourceRenderer));

            if (locations.Item1.Value is Renderer c)
            {
                builder.RecordCopyProperty(
                    c, Props.EnabledFor(c),
                    target.SourceRenderer, Props.EnabledFor(target.SourceRenderer));
                target.SourceRenderer.enabled = c.enabled;
            }
            else if (locations.Item1.Value is GameObject go)
            {
                builder.RecordCopyProperty(
                    go, Props.IsActive, 
                    target.SourceRenderer, Props.EnabledFor(target.SourceRenderer));
                target.SourceRenderer.enabled = go.activeSelf;
            }
        }

        // must preserve first material to be the first material for AutoMergeSkinnedMesh
        public static (int[][] subMeshIndexMap, List<(MeshTopology topology, Material? material)> materials)
            GenerateSubMeshMapping(
                MeshInfo2[] meshInfos,
                HashSet<Material> doNotMerges)
        {
            Profiler.BeginSample("Merge Material Indices");
            var sourceMaterials = meshInfos
                .Select(x => x.SubMeshes.Select(y => (y.Topology, y.SharedMaterial)).ToArray()).ToArray();
            var (subMeshIndexMap, materials) =
                CreateMergedMaterialsAndSubMeshIndexMapping(sourceMaterials, doNotMerges);
            Profiler.EndSample();

            return (subMeshIndexMap, materials);
        }

        public static void DoMerge(BuildContext context,
            MeshInfo2 target,
            MeshInfo2[] meshInfos,
            int[][] subMeshIndexMap,
            List<(MeshTopology topology, Material? material)> materials,
            MergeSkinnedMesh.BlendShapeMode componentBlendShapeMode = MergeSkinnedMesh.BlendShapeMode.RenameToAvoidConflict,
            ICollection<string>? targetAnimatedProperties = null) {
            targetAnimatedProperties ??= Array.Empty<string>();

            target.ClearMeshData();
            target.SubMeshes.Capacity = Math.Max(target.SubMeshes.Capacity, materials.Count);
            foreach (var material in materials)
            {
                target.SubMeshes.Add(new SubMesh(material.material, material.topology));
                if (material.material != null
                    && context.GetMaterialInformation(material.material) is { } information)
                    information.UserRenderers.Add(target.SourceRenderer);
            }

            TexCoordStatus TexCoordStatusMax(TexCoordStatus x, TexCoordStatus y) =>
                (TexCoordStatus)Math.Max((int)x, (int)y);

            var mappings = new List<(string, string)>();

            var rendererPrefixes = BlendShapePrefixComputer.Create();

            for (var i = 0; i < meshInfos.Length; i++)
            {
                Profiler.BeginSample($"Process MeshInfo#{i}");
                var meshInfo = meshInfos[i];
                mappings.Clear();

                meshInfo.AssertInvariantContract($"processing source #{i} of {target.SourceRenderer.gameObject.name}");

                // borrows
                var copiedVertices = meshInfo.Vertices.ToList();

                target.VerticesMutable.AddRange(meshInfo.Vertices);
                meshInfo.VerticesMutable.Clear();

                for (var j = 0; j < 8; j++)
                    target.SetTexCoordStatus(j,
                        TexCoordStatusMax(target.GetTexCoordStatus(j), meshInfo.GetTexCoordStatus(j)));

                for (var j = 0; j < meshInfo.SubMeshes.Count; j++)
                {
                    var targetSubMeshIndex = subMeshIndexMap[i][j];
                    var targetSubMesh = target.SubMeshes[targetSubMeshIndex];
                    var sourceSubMesh = meshInfo.SubMeshes[j];
                    Utils.Assert(targetSubMesh.Topology == sourceSubMesh.Topology);
                    targetSubMesh.Vertices.AddRange(sourceSubMesh.Vertices);
                    mappings.Add(($"m_Materials.Array.data[{j}]",
                        $"m_Materials.Array.data[{targetSubMeshIndex}]"));
                }
                meshInfo.SubMeshes.Clear();

                // rename if componentBlendShapeMode is RenameToAvoidConflict
                if (componentBlendShapeMode == MergeSkinnedMesh.BlendShapeMode.RenameToAvoidConflict)
                {
                    var prefix = rendererPrefixes.GetPrefix(meshInfo.SourceRenderer.gameObject.name);
                    var buffers = copiedVertices.Select(x => x.BlendShapeBuffer).Distinct().ToList();

                    for (var sourceI = 0; sourceI < meshInfo.BlendShapes.Count; sourceI++)
                    {
                        var (name, weight) = meshInfo.BlendShapes[sourceI];
                        var newName = $"{prefix}{name}";
                        meshInfo.BlendShapes[sourceI] = (newName, weight);

                        foreach (var buffer in buffers)
                        {
                            if (buffer.Shapes.Remove(name, out var shape))
                                buffer.Shapes[newName] = shape;
                        }

                        mappings.Add(($"blendShape.{name}", $"blendShape.{newName}"));
                    }
                }

                // add BlendShape if not defined by name
                for (var sourceI = 0; sourceI < meshInfo.BlendShapes.Count; sourceI++)
                {
                    var (name, weight) = meshInfo.BlendShapes[sourceI];
                    var newIndex = target.BlendShapes.FindIndex(x => x.name == name);
                    if (newIndex == -1)
                    {
                        newIndex = target.BlendShapes.Count;
                        target.BlendShapes.Add((name, weight));
                    }

                    mappings.Add((VProp.BlendShapeIndex(sourceI), VProp.BlendShapeIndex(newIndex)));
                }

                context.RecordMoveProperties(meshInfo.SourceRenderer, mappings.ToArray());
                
                // Avatars can have animation to hide source meshes.
                // Such a animation often intended to hide/show some accessories but
                // after we merge mesh, it affects to big merged mesh.
                // This often be a unexpected behavior so we invalidate changing m_Enabled
                // property for original mesh in animation.
                // This invalidation doesn't affect to m_Enabled property of merged mesh.
                context.RecordRemoveProperty(meshInfo.SourceRenderer, Props.EnabledFor(meshInfo.SourceRenderer));

                // If both source and target have animation, it will conflict so we remove it from source,
                // and forcibly keep target's animation.
                foreach (var targetAnimatedProperty in targetAnimatedProperties)
                    context.RecordRemoveProperty(meshInfo.SourceRenderer, $"material.{targetAnimatedProperty}");

                context.RecordMergeComponent(meshInfo.SourceRenderer, target.SourceRenderer);

                target.Bones.AddRange(meshInfo.Bones);

                target.HasColor |= meshInfo.HasColor;
                target.HasNormals |= meshInfo.HasNormals;
                target.HasTangent |= meshInfo.HasTangent;

                target.AssertInvariantContract($"processing meshInfo {target.SourceRenderer.gameObject.name}");
                Profiler.EndSample();
            }
        }

        private struct BlendShapePrefixComputer
        {
            private HashSet<string> _usedPrefixes;

            public static BlendShapePrefixComputer Create()
            {
                return new BlendShapePrefixComputer
                {
                    _usedPrefixes = new HashSet<string>()
                };
            }

            public string GetPrefix(string name)
            {
                var prefix = $"{name.Length}_{name}__";

                if (_usedPrefixes.Contains(prefix))
                {
                    var j = 1;
                    do
                    {
                        prefix = $"{name.Length}_{name}_{j}__";
                        j++;
                    } while (_usedPrefixes.Contains(prefix));
                }
                _usedPrefixes.Add(prefix);

                return prefix;
            }
        }

        public static void MergeBounds(
            MeshInfo2 target,
            MeshInfo2[] meshInfos
        ) {

            Profiler.BeginSample("Update Bounds");
            var sourceRootBone = target.RootBone;

            if (sourceRootBone != null && target.Bounds == default)
            {
                var newBoundMin = Vector3.positiveInfinity;
                var newBoundMax = Vector3.negativeInfinity;
                foreach (var meshInfo in meshInfos)
                {
                    if (meshInfo.RootBone != null)
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
                }

                if (Utils.IsFinite(newBoundMin.x)
                    && Utils.IsFinite(newBoundMin.y)
                    && Utils.IsFinite(newBoundMin.z)
                    && Utils.IsFinite(newBoundMax.x)
                    && Utils.IsFinite(newBoundMax.y)
                    && Utils.IsFinite(newBoundMax.z))
                {
                    target.Bounds.SetMinMax(newBoundMin, newBoundMax);
                }
            }

            Profiler.EndSample();
        }

        public static void RemoveOldRenderers(
            MeshInfo2 target,
            MeshInfo2[] meshInfos,
            bool removeEmptyRendererObject
        ) {
            Profiler.BeginSample("Postprocess Source Renderers");
            if (removeEmptyRendererObject)
            {
                var boneTransforms = new HashSet<Transform?>(target.Bones.Select(x => x.Transform));

                foreach (var rendererGameObject in 
                         from meshInfo in meshInfos
                         let renderer = meshInfo.SourceRenderer
                         where renderer != null && renderer is SkinnedMeshRenderer
                         let rendererGameObject = renderer.gameObject
                         where rendererGameObject.GetComponents<Component>()
                             .All(x => x is AvatarTagComponent || x is Transform || x is SkinnedMeshRenderer)
                         where rendererGameObject.transform.childCount == 0
                         where !boneTransforms.Contains(rendererGameObject.transform)
                         select rendererGameObject)
                {
                    DestroyTracker.DestroyImmediate(rendererGameObject);
                }
            }

            foreach (var renderer in 
                     from meshInfo in meshInfos
                     let renderer = meshInfo.SourceRenderer
                     where renderer != null
                     select renderer)
            {
                DestroyTracker.DestroyImmediate(renderer.GetComponent<MeshFilter>());
                DestroyTracker.DestroyImmediate(renderer);
            }
            Profiler.EndSample();
        }

        private static void MaterialParameterAnimationWarnings(MeshInfo2[] sourceRenderers, MeshInfo2 target,
            BuildContext context)
        {
            var targetAnimatedProperties = context.GetAnimationComponent(target.SourceRenderer)
                .GetAllFloatProperties()
                .Where(x => x.node.ComponentNodes.Any())
                .Where(x => x.property.StartsWith("material.", StringComparison.Ordinal))
                .Select(x => x.property["material.".Length..])
                .ToHashSet();
            var properties = new Dictionary<string, List<(RootPropModNode<FloatValueInfo>, MeshInfo2)>>();
            var materialByMeshInfo2 = new List<(MeshInfo2 meshInfo2, List<Material> materials)>();
            foreach (var meshInfo2 in sourceRenderers)
            {
                var component = context.GetAnimationComponent(meshInfo2.SourceRenderer);
                foreach (var (name, property) in component.GetAllFloatProperties())
                {
                    if (!name.StartsWith("material.", StringComparison.Ordinal)) continue;
                    if (!property.ComponentNodes.Any()) continue; // skip empty nodes
                    var materialPropertyName = name.Substring("material.".Length);

                    if (!properties.TryGetValue(materialPropertyName, out var list))
                        properties.Add(materialPropertyName, list = new List<(RootPropModNode<FloatValueInfo>, MeshInfo2)>());

                    list.Add((property, meshInfo2));
                }
                var materials = new List<Material>();
                for (var i = 0; i < meshInfo2.SubMeshes.Count; i++)
                {
                    var objectNode = component.GetObjectNode($"m_Materials.Array.data[{i}]");
                    materials.AddRange(objectNode.Value.PossibleValues.OfType<Material>().Where(x => x));
                    if (meshInfo2.SubMeshes[i].SharedMaterial is {} newMaterial)
                        materials.Add(newMaterial);
                }
                materialByMeshInfo2.Add((meshInfo2, materials));
            }

            var animatedProperties = new List<string>();

            foreach (var (propertyName, animatingProperties) in properties)
            {
                if (targetAnimatedProperties.Contains(propertyName)) continue;
                var rendererBySource = new Dictionary<AnimationLocation, HashSet<MeshInfo2>>();

                foreach (var (property, renderer) in animatingProperties)
                foreach (var animationLocation in AnimationLocation.CollectAnimationLocation(property))
                {
                    if (!rendererBySource.TryGetValue(animationLocation, out HashSet<MeshInfo2> renderers))
                        rendererBySource.Add(animationLocation, renderers = new HashSet<MeshInfo2>());
                    renderers.Add(renderer);
                }

                var animatedPartially = rendererBySource.Values.Any(renderers =>
                {
                    return materialByMeshInfo2
                            .Where(x => !renderers.Contains(x.Item1))
                            .SelectMany(x => x.materials.Select(material => (x.meshInfo2, material)))
                            .Any(x => ShaderKnowledge.IsParameterAnimationAffected(x.material, x.meshInfo2,
                                propertyName))
                        ;
                });

                if (animatedPartially)
                    animatedProperties.Add(propertyName);
            }

            if (animatedProperties.Count != 0)
                BuildLog.LogWarning("MergeSkinnedMesh:warning:material-animation-differently",
                    string.Join(",", animatedProperties));
        }

        private static void CheckForCopyEnablementAnimation(BuildContext context, MeshInfo2 target, MeshInfo2[] meshInfos)
        {
            var commonRoot = Utils.CommonRoot(meshInfos.Select(x => x.SourceRenderer.transform).Append(target.SourceRenderer.transform));

            if (commonRoot == null || !commonRoot.IsChildOf(context.AvatarRootTransform))
            {
                BuildLog.LogError("MergeSkinnedMesh:error:some-source-is-out-of-avatar",
                    meshInfos
                        .Select(x => x.SourceRenderer.transform)
                        .Where(x => !x.IsChildOf(context.AvatarRootTransform))
                );
                return;
            }

            // if there is animation, we should warn it.
            var propModNode = context.GetAnimationComponent(target.SourceRenderer).GetFloatNode(Props.EnabledFor(target.SourceRenderer));
            if (propModNode.ComponentNodes.Any())
            {
                BuildLog.LogError("MergeSkinnedMesh:copy-enablement-animation:error:enablement-of-merged-mesh-is-animated",
                    target.SourceRenderer, propModNode);
            }

            if (meshInfos.Length == 0) return;

            var locations = meshInfos
                .Select(m => (mesh: m, locations: GetActivenessAnimationLocations(context, m.SourceRenderer, commonRoot).ToArray())).ToList();

            // check if single
            var problematicMeshes = locations.Where(x => x.locations.Length >= 2).Select(x => x.mesh).ToList();

            if (problematicMeshes.Count != 0)
            {
                BuildLog.LogError("MergeSkinnedMesh:copy-enablement-animation:error:too-many-activeness-animation", problematicMeshes);
            }
            else
            {
                // check for count mismatch
                var difference = locations
                    .Select(x => x.locations.FirstOrDefault().Item2 ?? new HashSet<AnimationLocation>())
                    .ZipWithNext()
                    .Any(p => !p.Item1.SetEquals(p.Item2));

                if (difference)
                {
                    BuildLog.LogError("MergeSkinnedMesh:copy-enablement-animation:error:activeness-animation-of-source-mismatch");
                }
            }
        }

        private static IEnumerable<(ComponentOrGameObject target, HashSet<AnimationLocation> animaions)>
            GetActivenessAnimationLocations(BuildContext context, Renderer component, Transform root)
        {
            {
                if (context.GetAnimationComponent(component).GetFloatNode(Props.EnabledFor(component))
                        .CollectAnimationLocation().ToHashSet() is { Count: > 0 } locations)
                    yield return (component, locations);
            }
            foreach (var transform in component.transform.ParentEnumerable(root, includeMe: true))
                if (context.GetAnimationComponent(transform.gameObject).GetFloatNode(Props.IsActive)
                        .CollectAnimationLocation().ToHashSet() is { Count: > 0 } locations)
                    yield return (transform.gameObject, locations);
        }

        private static void ActivenessAnimationWarning(IEnumerable<Renderer> renderers, Renderer target,
            BuildContext context)
        {
            // collect activeness animation for the merged object
            var animationLocationsForMerged = GetAnimationLocations(context, target);

            var sources = new List<object>();

            foreach (var renderer in renderers)
            {
                var animationLocationsForSource = GetAnimationLocations(context, renderer);
                if (animationLocationsForSource.Count == 0) continue; // skip if no animation
                if (animationLocationsForSource.SetEquals(animationLocationsForMerged)) continue;

                // if the source has different activeness animation, warn it.
                animationLocationsForSource.ExceptWith(animationLocationsForMerged);
                sources.Add(renderer);
                sources.Add(animationLocationsForSource);
            }

            if (sources.Count != 0)
                BuildLog.LogWarning("MergeSkinnedMesh:warning:animation-mesh-hide", sources);
        }

        private static HashSet<AnimationLocation> GetAnimationLocations(BuildContext context, Component component)
        {
            var locations = new HashSet<AnimationLocation>();
            {
                locations.UnionWith(context.GetAnimationComponent(component)
                    .GetFloatNode(Props.EnabledFor(component)).CollectAnimationLocation());
            }
            foreach (var transform in component.transform.ParentEnumerable(context.AvatarRootTransform, includeMe: true))
                locations.UnionWith(context.GetAnimationComponent(transform.gameObject)
                    .GetFloatNode(Props.IsActive).CollectAnimationLocation());
            return locations;
        }

        // must preserve first material to be the first material for AutoMergeSkinnedMesh
        private static (int[][] mapping, List<(MeshTopology topology, Material? material)> materials)
            CreateMergedMaterialsAndSubMeshIndexMapping((MeshTopology topology, Material? material)[][] sourceMaterials,
                HashSet<Material> doNotMerges)
        {
            var resultMaterials = new List<(MeshTopology, Material?)>();
            var resultIndices = new int[sourceMaterials.Length][];

            for (var i = 0; i < sourceMaterials.Length; i++)
            {
                var materials = sourceMaterials[i];
                var indices = resultIndices[i] = new int[materials.Length];

                for (var j = 0; j < materials.Length; j++)
                {
                    var material = materials[j];
                    var foundIndex = resultMaterials.IndexOf(material);
                    if (material.material != null && doNotMerges.Contains(material.material) || foundIndex == -1)
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

            public (string, float)[] BlendShapes()
            {
                if (_processor.Component.blendShapeMode == MergeSkinnedMesh.BlendShapeMode.RenameToAvoidConflict)
                {
                    var rendererPrefixes = BlendShapePrefixComputer.Create();
                    return _processor.SkinnedMeshRenderers
                        .SelectMany(renderer =>
                        {
                            var blendShapes = EditSkinnedMeshComponentUtil.GetBlendShapes(renderer);
                            var prefix = rendererPrefixes.GetPrefix(renderer.gameObject.name);
                            return blendShapes.Select(x => (prefix + x.name, x.weight));
                        })
                        .ToArray();
                }

                return _processor.SkinnedMeshRenderers
                    .SelectMany(EditSkinnedMeshComponentUtil.GetBlendShapes)
                    .Distinct(BlendShapeNameComparator.Instance)
                    .ToArray();
            }

            public Material?[] Materials(bool fast = true)
            {
                var sourceMaterials = _processor.SkinnedMeshRenderers.Select(EditSkinnedMeshComponentUtil.GetMaterials)
                    .Concat(_processor.BasicMeshRenderers.Select(x => x.sharedMaterials))
                    .Select(a => a.Select(b => (MeshTopology.Triangles, b)).ToArray())
                    .ToArray();

                return CreateMergedMaterialsAndSubMeshIndexMapping(sourceMaterials, 
                        _processor.Component.doNotMergeMaterials.GetAsSet())
                    .materials
                    .Select(x => x.material)
                    .ToArray();
            }

            private class BlendShapeNameComparator : IEqualityComparer<(string name, float weight)>
            {
                public static readonly BlendShapeNameComparator Instance = new();

                public bool Equals((string name, float weight) x, (string name, float weight) y)
                {
                    return x.name == y.name;
                }

                public int GetHashCode((string name, float weight) obj)
                {
                    return obj.name.GetHashCode();
                }
            }
        }
    }
}
