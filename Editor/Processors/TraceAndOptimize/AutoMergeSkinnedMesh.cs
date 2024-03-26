using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using JetBrains.Annotations;
using nadena.dev.ndmf;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

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
                var activenessAnimationLocations = new HashSet<AnimationLocation>(
                    AnimationLocation.CollectActivenessAnimationLocations(context, meshInfo2.SourceRenderer));
                var key = new CategorizationKey(meshInfo2, activenessAnimationLocations);
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

            int index = 0;

            foreach (var (key, meshInfos) in categorizedMeshes)
            {
                if (key.ActivenessAnimationLocations.Count == 0)
                {
                    if (!state.SkipMergeStaticSkinnedMesh)
                    {
                        // if there's no activeness animation, we merge them at root
                        var newSkinnedMeshRenderer = CreateNewRenderer($"Merged Skinned Mesh Renderer {index++}",
                            context.AvatarRootTransform, key);
                        var newMeshInfo = context.GetMeshInfoFor(newSkinnedMeshRenderer);
                        var meshInfosArray = meshInfos.ToArray();

                        // TODO: merge materials
                        var (subMeshIndexMap, materials) = CreateSubMeshesNoMerge(meshInfosArray);

                        MergeSkinnedMeshProcessor.DoMerge(context, newMeshInfo, meshInfosArray, subMeshIndexMap,
                            materials);

                        // We process FindUnusedObjects after this pass so we wipe empty renderer object in that pass
                        MergeSkinnedMeshProcessor.RemoveOldRenderers(newMeshInfo, meshInfosArray,
                            removeEmptyRendererObject: false);
                    }
                }
                else
                {
                    // TODO: merge in this pass
                }
            }

            Profiler.EndSample();
        }

        private SkinnedMeshRenderer CreateNewRenderer(
            string name,
            Transform parent,
            CategorizationKey key
        )
        {
            var newRenderer = new GameObject(name);
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

        private (int[][], List<(MeshTopology, Material)>) CreateSubMeshesNoMerge(MeshInfo2[] meshInfos)
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

        private bool IsAnimatedForbidden(AnimationComponentInfo<PropertyInfo> component)
        {
            // any of object / pptr / material animation is forbidden
            if (component.GetAllObjectProperties().Any())
                return true;

            foreach (var (name, _) in component.GetAllFloatProperties())
            {
                // m_Enabled is allowed
                if (name == "m_Enabled") continue;
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
                HashSet<AnimationLocation> activenessAnimationLocations
            )
            {
                var renderer = (SkinnedMeshRenderer)meshInfo2.SourceRenderer;

                HasNormals = meshInfo2.HasNormals;
                ActivenessAnimationLocations = activenessAnimationLocations;

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