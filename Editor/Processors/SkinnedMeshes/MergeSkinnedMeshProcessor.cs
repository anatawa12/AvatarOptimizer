using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using JetBrains.Annotations;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

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
        
        private IEnumerable<MeshRenderer> StaticMeshRenderers =>
            Component.staticRenderersSet.GetAsList();

        public override EditSkinnedMeshProcessorOrder ProcessOrder => EditSkinnedMeshProcessorOrder.Generation;

        public override void Process(BuildContext context, MeshInfo2 target)
        {
            List<SkinnedMeshRenderer> skinnedMeshRenderers;
            List<MeshRenderer> staticMeshRenderers;
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
                staticMeshRenderers = StaticMeshRenderers.Where(x => RendererEnabled(x) == enabledSelf).ToList();
            }
            else
            {
                skinnedMeshRenderers = SkinnedMeshRenderers.ToList();
                staticMeshRenderers = StaticMeshRenderers.ToList();
            }

            Profiler.BeginSample("Merge PreserveBlendShapes");
            {
                var state = context.GetState<TraceAndOptimizes.TraceAndOptimizeState>();
                HashSet<string> thisPreserve = null;
                foreach (var skinnedRenderer in skinnedMeshRenderers)
                {
                    if (!state.PreserveBlendShapes.TryGetValue(skinnedRenderer, out var preserve)) continue;

                    if (thisPreserve == null && !state.PreserveBlendShapes.TryGetValue(Target, out thisPreserve))
                        state.PreserveBlendShapes.Add(Target, thisPreserve = new HashSet<string>());
                    thisPreserve.UnionWith(preserve);
                }
            }
            Profiler.EndSample();
            Profiler.BeginSample("Collect MeshInfos");
            var meshInfos = skinnedMeshRenderers.Select(context.GetMeshInfoFor)
                .Concat(staticMeshRenderers.Select(renderer => new MeshInfo2(renderer)))
                .ToArray();

            foreach (var meshInfo2 in meshInfos) meshInfo2.FlattenMultiPassRendering("Merge Skinned Mesh");
            foreach (var meshInfo2 in meshInfos) meshInfo2.MakeBoned();

            var sourceMaterials = meshInfos.Select(x => x.SubMeshes.Select(y => (y.Topology, y.SharedMaterial)).ToArray()).ToArray();
            Profiler.EndSample();

            Profiler.BeginSample("Material / Shader Parameter Animation Warnings");
            MaterialParameterAnimationWarnings(meshInfos, context);
            Profiler.EndSample();

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

            Profiler.BeginSample("Generate ActivenessWarning");
            ActivenessAnimationWarning(skinnedMeshRenderers.Concat<Renderer>(staticMeshRenderers), context);
            Profiler.EndSample();

            Profiler.BeginSample("Remove Unsupported Components");
            foreach (var renderer in skinnedMeshRenderers)
            {
                var removeZeroSizedPolygon = renderer.GetComponent<RemoveZeroSizedPolygon>();
                if (removeZeroSizedPolygon)
                {
                    BuildLog.LogWarning("MergeSkinnedMesh:warning:removeZeroSizedPolygonOnSources", removeZeroSizedPolygon);
                    DestroyTracker.DestroyImmediate(removeZeroSizedPolygon);
                }
                var cloth = renderer.GetComponent<Cloth>();
                if (cloth)
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

            Profiler.BeginSample("Merge Material Indices");
            var (subMeshIndexMap, materials) = CreateMergedMaterialsAndSubMeshIndexMapping(sourceMaterials);
            Profiler.EndSample();

            var sourceRootBone = target.RootBone;
            var updateBounds = sourceRootBone && target.Bounds == default;

            target.Clear();
            target.SubMeshes.Capacity = Math.Max(target.SubMeshes.Capacity, materials.Count);
            foreach (var material in materials)
                target.SubMeshes.Add(new SubMesh(material.material, material.topology));

            TexCoordStatus TexCoordStatusMax(TexCoordStatus x, TexCoordStatus y) =>
                (TexCoordStatus)Math.Max((int)x, (int)y);

            var newBoundMin = Vector3.positiveInfinity;
            var newBoundMax = Vector3.negativeInfinity;

            var mappings = new List<(string, string)>();

            for (var i = 0; i < meshInfos.Length; i++)
            {
                Profiler.BeginSample($"Process MeshInfo#{i}");
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
                    var targetSubMesh = target.SubMeshes[targetSubMeshIndex];
                    var sourceSubMesh = meshInfo.SubMeshes[j];
                    System.Diagnostics.Debug.Assert(targetSubMesh.Topology == sourceSubMesh.Topology);
                    targetSubMesh.Vertices.AddRange(sourceSubMesh.Vertices);
                    mappings.Add(($"m_Materials.Array.data[{j}]",
                        $"m_Materials.Array.data[{targetSubMeshIndex}]"));
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

                context.RecordMoveProperties(meshInfo.SourceRenderer, mappings.ToArray());
                
                // Avatars can have animation to hide source meshes.
                // Such a animation often intended to hide/show some accessories but
                // after we merge mesh, it affects to big merged mesh.
                // This often be a unexpected behavior so we invalidate changing m_Enabled
                // property for original mesh in animation.
                // This invalidation doesn't affect to m_Enabled property of merged mesh.
                context.RecordRemoveProperty(meshInfo.SourceRenderer, "m_Enabled");

                context.RecordMergeComponent(meshInfo.SourceRenderer, Target);

                target.RootBone = sourceRootBone;
                target.Bones.AddRange(meshInfo.Bones);

                target.HasColor |= meshInfo.HasColor;
                target.HasNormals |= meshInfo.HasNormals;
                target.HasTangent |= meshInfo.HasTangent;

                target.AssertInvariantContract($"processing meshInfo {Target.gameObject.name}");
                Profiler.EndSample();
            }

#if !UNITY_2021_2_OR_NEWER
            Profiler.BeginSample("ShiftIndex Check");
            // material slot #4 should not be animated to avoid Unity bug
            // https://issuetracker.unity3d.com/issues/material-is-applied-to-two-slots-when-applying-material-to-a-single-slot-while-recording-animation
            const int SubMeshIndexToShiftIfAnimated = 4;
            bool shouldShiftSubMeshIndex = CheckAnimateSubMeshIndex(context, meshInfos, subMeshIndexMap, SubMeshIndexToShiftIfAnimated);
            Profiler.EndSample();
#endif

            if (updateBounds && newBoundMin != Vector3.positiveInfinity && newBoundMax != Vector3.negativeInfinity)
            {
                target.Bounds.SetMinMax(newBoundMin, newBoundMax);
            }

            var boneTransforms = new HashSet<Transform>(target.Bones.Select(x => x.Transform));

            Profiler.BeginSample("Postprocess Source Renderers");
            foreach (var renderer in skinnedMeshRenderers)
            {
                var rendererGameObject = renderer.gameObject;
                DestroyTracker.DestroyImmediate(renderer);

                // process removeEmptyRendererObject
                if (!Component.removeEmptyRendererObject) continue;
                // no other components should be exist
                if (!rendererGameObject.GetComponents<Component>().All(x =>
                        x is AvatarTagComponent || x is Transform || x is SkinnedMeshRenderer)) continue;
                // no children is required
                if (rendererGameObject.transform.childCount != 0) continue;
                // the SkinnedMeshRenderer may also be used as bone. it's not good to remove
                if (boneTransforms.Contains(rendererGameObject.transform)) continue;
                DestroyTracker.DestroyImmediate(rendererGameObject);
            }

            foreach (var renderer in staticMeshRenderers)
            {
                DestroyTracker.DestroyImmediate(renderer.GetComponent<MeshFilter>());
                DestroyTracker.DestroyImmediate(renderer);
            }
            Profiler.EndSample();

#if !UNITY_2021_2_OR_NEWER
            if (shouldShiftSubMeshIndex)
            {
                Profiler.BeginSample("ShiftIndex");
                mappings.Clear();
                for (var i = SubMeshIndexToShiftIfAnimated; i < target.SubMeshes.Count; i++)
                {
                    mappings.Add(($"m_Materials.Array.data[{i}]", $"m_Materials.Array.data[{i + 1}]"));
                }

                context.RecordMoveProperties(target.SourceRenderer, mappings.ToArray());

                target.SubMeshes.Insert(SubMeshIndexToShiftIfAnimated, new SubMesh());

                target.AssertInvariantContract($"shifting meshInfo.SubMeshes {Target.gameObject.name}");
                Profiler.EndSample();
            }
#endif
        }

        private void MaterialParameterAnimationWarnings(MeshInfo2[] sourceRenderers, BuildContext context)
        {
            var properties = new Dictionary<string, List<(RootPropModNode<float>, MeshInfo2)>>();
            var materialByMeshInfo2 = new List<(MeshInfo2 meshInfo2, List<Material> materials)>();
            foreach (var meshInfo2 in sourceRenderers)
            {
                var component = context.GetAnimationComponent(meshInfo2.SourceRenderer);
                foreach (var (name, property) in component.GetAllFloatProperties())
                {
                    if (!name.StartsWith("material.", StringComparison.Ordinal)) continue;
                    var materialPropertyName = name.Substring("material.".Length);

                    if (!properties.TryGetValue(materialPropertyName, out var list))
                        properties.Add(materialPropertyName, list = new List<(RootPropModNode<float>, MeshInfo2)>());

                    list.Add((property, meshInfo2));
                }
                var materials = new List<Material>();
                for (var i = 0; i < meshInfo2.SubMeshes.Count; i++)
                {
                    if (component.TryGetObject($"m_Materials.Array.data[{i}]", out var objectNode))
                        materials.AddRange(objectNode.Value.PossibleValues?.OfType<Material>().Where(x => x) ??
                                           Enumerable.Empty<Material>());
                    if (meshInfo2.SubMeshes[i].SharedMaterial)
                        materials.Add(meshInfo2.SubMeshes[i].SharedMaterial);
                }
                materialByMeshInfo2.Add((meshInfo2, materials));
            }

            var animatedProperties = new List<string>();

            foreach (var (propertyName, animatingProperties) in properties)
            {
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
                    string.Join(",", animatedProperties), Component);
        }

        private void ActivenessAnimationWarning(IEnumerable<Renderer> renderers, BuildContext context)
        {
            // collect activeness animation for the merged object
            var animationLocationsForMerged = GetAnimationLocations(context, Target);

            var sources = new List<object>();

            foreach (var renderer in renderers)
            {
                var animationLocationsForSource = GetAnimationLocations(context, renderer);
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
                if (context.GetAnimationComponent(component).TryGetFloat("m_Enabled", out var p))
                    locations.UnionWith(AnimationLocation.CollectAnimationLocation(p));
            }
            foreach (var transform in component.transform.ParentEnumerable(context.AvatarRootTransform, includeMe: true))
                if (context.GetAnimationComponent(transform.gameObject).TryGetFloat("m_IsActive", out var p))
                    locations.UnionWith(AnimationLocation.CollectAnimationLocation(p));
            return locations;
        }

        private (int[][] mapping, List<(MeshTopology topology, Material material)> materials)
            CreateMergedMaterialsAndSubMeshIndexMapping(
                (MeshTopology topology, Material material)[][] sourceMaterials)
        {
            var doNotMerges = Component.doNotMergeMaterials.GetAsSet();
            var resultMaterials = new List<(MeshTopology, Material)>();
            var resultIndices = new int[sourceMaterials.Length][];

            for (var i = 0; i < sourceMaterials.Length; i++)
            {
                var materials = sourceMaterials[i];
                var indices = resultIndices[i] = new int[materials.Length];

                for (var j = 0; j < materials.Length; j++)
                {
                    var material = materials[j];
                    var foundIndex = resultMaterials.IndexOf(material);
                    if (doNotMerges.Contains(material.material) || foundIndex == -1)
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

#if !UNITY_2021_2_OR_NEWER
        private bool CheckAnimateSubMeshIndex(BuildContext context, MeshInfo2[] meshInfos, int[][] subMeshIndexMap, int targetSubMeshIndex)
        {
            var targetProperties = new HashSet<(Object, string)>(subMeshIndexMap
                .SelectMany((x, i) => x.Select((y, j) => (renderer: meshInfos[i].SourceRenderer, srcSubMeshIndex: j, dstSubMeshIndex: y)))
                .Where(x => x.dstSubMeshIndex == targetSubMeshIndex)
                .Select(x => (x.renderer as Object, $"m_Materials.Array.data[{x.srcSubMeshIndex}]")));
            foreach (var component in context.GetComponents<Component>())
            {
                if (component is Transform) continue;

                using (var serialized = new SerializedObject(component))
                {
                    foreach (var prop in serialized.ObjectReferenceProperties())
                    {
                        if (!(prop.objectReferenceValue is AnimatorController controller)) continue;
                        if (controller.animationClips
                            .SelectMany(x => AnimationUtility.GetObjectReferenceCurveBindings(x))
                            .Select(x => (AnimationUtility.GetAnimatedObject(component.gameObject, x), x.propertyName))
                            .Any(targetProperties.Contains))
                            return true;
                    }
                }
            }
            return false;
        }
#endif

        public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => new MeshInfoComputer(this);

        class MeshInfoComputer : IMeshInfoComputer
        {
            private readonly MergeSkinnedMeshProcessor _processor;

            public MeshInfoComputer(MergeSkinnedMeshProcessor processor) => _processor = processor;

            public (string, float)[] BlendShapes() =>
                _processor.SkinnedMeshRenderers
                    .SelectMany(EditSkinnedMeshComponentUtil.GetBlendShapes)
                    .Distinct(BlendShapeNameComparator.Instance)
                    .ToArray();

            public Material[] Materials(bool fast = true)
            {
                var sourceMaterials = _processor.SkinnedMeshRenderers.Select(EditSkinnedMeshComponentUtil.GetMaterials)
                    .Concat(_processor.StaticMeshRenderers.Select(x => x.sharedMaterials))
                    .Select(a => a.Select(b => (MeshTopology.Triangles, b)).ToArray())
                    .ToArray();

                return _processor.CreateMergedMaterialsAndSubMeshIndexMapping(sourceMaterials)
                    .materials
                    .Select(x => x.material)
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
