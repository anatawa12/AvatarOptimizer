using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;
using nadena.dev.ndmf;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = System.Diagnostics.Debug;

namespace Anatawa12.AvatarOptimizer.Processors
{
    public class MeshInfo2Context : IExtensionContext
    {
        internal MeshInfo2Holder? Holder { get; private set; }
        public void OnActivate(BuildContext context)
        {
            Holder = new MeshInfo2Holder(context.AvatarRootObject);
        }

        public void OnDeactivate(BuildContext context)
        {
            if (Holder == null) throw new InvalidOperationException("Not activated");
            // avoid Array index (n) is out of bounds (size=m) error
            // by assigning null to AnimatorController before changing blendShapes count
            // and assigning back after changing blendShapes count.
            // see https://github.com/anatawa12/AvatarOptimizer/issues/804
            using (new EvacuateAnimatorController(context))
            {
                Holder.SaveToMesh();
            }
            Holder.Dispose();
            Holder = null;
        }
    }

    internal struct EvacuateAnimatorController : IDisposable
    {
        private Dictionary<Animator, RuntimeAnimatorController> _controllers;

        public EvacuateAnimatorController(BuildContext context)
        {
            _controllers = context.AvatarRootObject.GetComponentsInChildren<Animator>()
                .ToDictionary(a => a, a => a.runtimeAnimatorController);

            foreach (var animator in _controllers.Keys)
                animator.runtimeAnimatorController = null;
        }

        public void Dispose()
        {
            foreach (var (animator, runtimeAnimatorController) in _controllers)
                animator.runtimeAnimatorController = runtimeAnimatorController;
        }
    }

    internal class MeshInfo2Holder : IDisposable
    {
        private readonly Dictionary<SkinnedMeshRenderer, MeshInfo2> _skinnedCache =
            new Dictionary<SkinnedMeshRenderer, MeshInfo2>();

        private readonly Dictionary<MeshRenderer, MeshInfo2> _basicCache =
            new Dictionary<MeshRenderer, MeshInfo2>();

        // Maps source mesh → list of SkinnedMeshRenderers that share it, for mesh-sharing in SaveToMesh
        private readonly Dictionary<Mesh, List<SkinnedMeshRenderer>> _sourceMeshRenderers =
            new Dictionary<Mesh, List<SkinnedMeshRenderer>>();

        public MeshInfo2Holder(GameObject rootObject)
        {
            var avatarTagComponent = rootObject.GetComponentInChildren<AvatarTagComponent>(true);
            if (avatarTagComponent == null) return;
            foreach (var renderer in rootObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Profiler.BeginSample($"GetMeshInfoFor");
                GetMeshInfoFor(renderer);
                Profiler.EndSample();
            }
        }

        public MeshInfo2 GetMeshInfoFor(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned)
                return GetMeshInfoFor(skinned);
            if (renderer is MeshRenderer basic)
                return GetMeshInfoFor(basic);
            throw new ArgumentException("Renderer must be SkinnedMeshRenderer or MeshRenderer", nameof(renderer));
        }

        public MeshInfo2? TryGetMeshInfoFor(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned && _skinnedCache.TryGetValue(skinned, out var cached))
                return cached;
            if (renderer is MeshRenderer basic && _basicCache.TryGetValue(basic, out cached))
                return cached;
            return null;
        }

        public MeshInfo2 GetMeshInfoFor(SkinnedMeshRenderer renderer)
        {
            if (_skinnedCache.TryGetValue(renderer, out var cached))
                return cached;

            var newInfo = new MeshInfo2(renderer);
            _skinnedCache[renderer] = newInfo;

            // Track source mesh → renderer for potential sharing in SaveToMesh
            var sourceMesh = newInfo.OriginalMesh;
            if (sourceMesh != null)
            {
                if (!_sourceMeshRenderers.TryGetValue(sourceMesh, out var list))
                    _sourceMeshRenderers[sourceMesh] = list = new List<SkinnedMeshRenderer>();
                list.Add(renderer);
            }

            return newInfo;
        }

        public MeshInfo2 GetMeshInfoFor(MeshRenderer renderer) =>
            _basicCache.TryGetValue(renderer, out var cached)
                ? cached
                : _basicCache[renderer] = new MeshInfo2(renderer);


        public void SaveToMesh()
        {
            // Track which SMRs have been processed to avoid double-processing
            var processedRenderers = new HashSet<SkinnedMeshRenderer>();

            // Process groups of SMRs sharing the same source mesh
            foreach (var (_, renderers) in _sourceMeshRenderers)
            {
                // Filter to only alive renderers present in the cache
                var aliveRenderers = renderers
                    .Where(r => r && _skinnedCache.ContainsKey(r))
                    .ToList();

                if (aliveRenderers.Count <= 1)
                {
                    // No sharing possible, will be processed in the single-renderer pass below
                    continue;
                }

                // Sub-group by structural equivalence: renderers that would produce the same mesh can share it.
                // Key: (vertex count, submesh count, blendshape (name, weight) list)
                // Two renderers in the same sub-group started from the same mesh and ended up with
                // structurally identical MeshInfo2, meaning they will produce identical mesh data.
                var subgroups = aliveRenderers
                    .GroupBy(r => new MeshStructureKey(_skinnedCache[r]))
                    .ToList();

                foreach (var subgroup in subgroups)
                {
                    var groupRenderers = subgroup.ToList();

                    if (groupRenderers.Count == 1)
                    {
                        // Only one renderer with this structure — process normally below
                        continue;
                    }

                    // Multiple renderers with structurally equivalent MeshInfo2 — share output mesh
                    var primaryRenderer = groupRenderers[0];
                    var primaryMeshInfo2 = _skinnedCache[primaryRenderer];

                    Profiler.BeginSample($"Save Skinned Mesh {primaryRenderer.name} (shared primary)");
                    var previousMesh = primaryRenderer.sharedMesh;
                    primaryMeshInfo2.WriteToSkinnedMeshRenderer(primaryRenderer);
                    var createdMesh = primaryRenderer.sharedMesh;
                    bool meshWasCreated = createdMesh != null && createdMesh != previousMesh;
                    Profiler.EndSample();

                    processedRenderers.Add(primaryRenderer);

                    for (int i = 1; i < groupRenderers.Count; i++)
                    {
                        var secondaryRenderer = groupRenderers[i];
                        var secondaryMeshInfo2 = _skinnedCache[secondaryRenderer];

                        Profiler.BeginSample($"Save Skinned Mesh {secondaryRenderer.name} (shared secondary)");
                        if (meshWasCreated)
                            secondaryRenderer.sharedMesh = createdMesh;
                        secondaryMeshInfo2.ApplyRendererChanges(secondaryRenderer);
                        Profiler.EndSample();

                        processedRenderers.Add(secondaryRenderer);
                    }
                }
            }

            // Process remaining SMRs (single or not part of a sharing group)
            foreach (var keyValuePair in _skinnedCache)
            {
                var targetRenderer = keyValuePair.Key;
                if (!targetRenderer) continue;
                if (processedRenderers.Contains(targetRenderer)) continue;

                Profiler.BeginSample($"Save Skinned Mesh {targetRenderer.name}");
                keyValuePair.Value.WriteToSkinnedMeshRenderer(targetRenderer);
                Profiler.EndSample();
            }

            foreach (var (renderer, meshInfo2) in _basicCache)
            {
                if (!renderer) continue;

                Profiler.BeginSample($"Save Basic Mesh {renderer.name}");
                meshInfo2.WriteToMeshRenderer(renderer);
                Profiler.EndSample();
            }
        }

        public void Dispose()
        {
            Utils.DisposeAll(_skinnedCache.Values);
            _skinnedCache.Clear();
        }

        /// <summary>
        /// Key used to group MeshInfo2 instances that would produce structurally identical meshes.
        /// Two MeshInfo2s with the same key can safely share the same output mesh.
        /// The key includes vertex count, submesh count, and blendshape (name, weight) pairs.
        /// Using exact blendshape weights ensures renderers that froze blendshapes at different
        /// weights (producing different baked vertices) are correctly placed in separate groups.
        ///
        /// Blendshape ordering: MeshInfo2.BlendShapes preserves the original source-mesh order;
        /// processors only remove entries (via RemoveAll, which is stable) and never reorder them.
        /// Therefore order-based comparison is correct and consistent across all renderers that
        /// share the same source mesh.
        /// </summary>
        private readonly struct MeshStructureKey : IEquatable<MeshStructureKey>
        {
            private readonly int _vertexCount;
            private readonly int _subMeshCount;
            private readonly (string name, float weight)[] _blendShapes;

            public MeshStructureKey(MeshInfo2 meshInfo2)
            {
                _vertexCount = meshInfo2.Vertices.Count;
                _subMeshCount = meshInfo2.SubMeshes.Count;
                _blendShapes = meshInfo2.BlendShapes.ToArray();
            }

            public bool Equals(MeshStructureKey other)
            {
                if (_vertexCount != other._vertexCount || _subMeshCount != other._subMeshCount) return false;
                if (_blendShapes.Length != other._blendShapes.Length) return false;
                for (int i = 0; i < _blendShapes.Length; i++)
                {
                    if (_blendShapes[i].name != other._blendShapes[i].name) return false;
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (_blendShapes[i].weight != other._blendShapes[i].weight) return false;
                }
                return true;
            }

            public override bool Equals(object? obj) => obj is MeshStructureKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = HashCode.Combine(_vertexCount, _subMeshCount, _blendShapes.Length);
                    // Blendshapes are always in source-mesh order (processors only remove, never reorder),
                    // so iterating in order gives a stable, consistent hash.
                    foreach (var (name, weight) in _blendShapes)
                        hash = HashCode.Combine(hash, name, weight);
                    return hash;
                }
            }
        }
    }
}
