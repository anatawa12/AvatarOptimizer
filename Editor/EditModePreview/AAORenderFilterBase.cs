using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using nadena.dev.ndmf.preview;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.EditModePreview
{
    internal abstract class AAORenderFilterBase<T> : IRenderFilter
        where T : EditSkinnedMeshComponent
    {
        private readonly TogglablePreviewNode _toggleNode;
        public IEnumerable<TogglablePreviewNode> GetPreviewControlNodes() => new[] { _toggleNode };
        public bool IsEnabled(ComputeContext context) => context.Observe(_toggleNode.IsEnabled);

        protected AAORenderFilterBase(string name, string component)
        {
            _toggleNode = TogglablePreviewNode.Create(() => name, $"com.anatawa12.avatar-optimizer.{component}");
        }

        public ImmutableList<RenderGroup> GetTargetGroups(ComputeContext ctx)
        {
            // currently remove meshes are only supported
            var components = ctx.GetComponentsByType<T>();

            var componentsByRenderer = new Dictionary<Renderer, List<T>>();
            foreach (var component in components)
            {
                if (component.TryGetComponent<MergeSkinnedMesh>(out _))
                {
                    // the component applies to MergeSkinnedMesh, which is not supported for now
                    // TODO: rollup the remove operation to source renderers of MergeSkinnedMesh
                    continue;
                }

                Renderer renderer;
                if (component.TryGetComponent<SkinnedMeshRenderer>(out var skinned) && skinned.sharedMesh != null)
                    renderer = skinned;
                else if (component.TryGetComponent<MeshRenderer>(out var basic) &&
                         component.TryGetComponent<MeshFilter>(out var meshFilter) && meshFilter.sharedMesh != null)
                    renderer = basic;
                else
                    continue;
                if (!componentsByRenderer.TryGetValue(renderer, out var list))
                    componentsByRenderer.Add(renderer, list = new List<T>());

                list.Add(component);
            }

            return componentsByRenderer
                .Where(x => SupportsMultiple() ? x.Value.Count >= 1 : x.Value.Count == 1)
                .Select(pair => RenderGroup.For(pair.Key).WithData(pair.Value.ToArray()))
                .ToImmutableList();
        }

        public async Task<IRenderFilterNode?> Instantiate(RenderGroup group,
            IEnumerable<(Renderer, Renderer)> proxyPairs, ComputeContext context)
        {
            var pair = proxyPairs.Single();

            // we modify the mesh so we need to clone the mesh

            var components = group.GetData<T[]>();

            var node = CreateNode();

            await node.Process(pair.Item1, pair.Item2, components, context);

            return node;
        }

        protected abstract AAORenderFilterNodeBase<T> CreateNode();
        protected abstract bool SupportsMultiple();
    }

    internal abstract class AAORenderFilterNodeBase<T> : IRenderFilterNode
        where T : EditSkinnedMeshComponent
    {
        private Mesh? _duplicated;

        RenderAspects IRenderFilterNode.WhatChanged => RenderAspects.Mesh | RenderAspects.Shapes;

        protected abstract ValueTask<bool> Process(
            Renderer original,
            Renderer proxy,
            [NotNull] T[] components,
            Mesh duplicated,
            ComputeContext context);

        internal async ValueTask Process(
            Renderer original,
            Renderer proxy,
            [NotNull] T[] components,
            ComputeContext context)
        {
            UnityEngine.Profiling.Profiler.BeginSample($"RemoveMeshByBlendShapeRendererNode.Process({original.name})");

            Mesh duplicated;
            {
                if (proxy is SkinnedMeshRenderer skinned)
                {
                    duplicated = Object.Instantiate(skinned.sharedMesh);
                    duplicated.name = skinned.sharedMesh.name + " (AAO Generated)";
                }
                else if (proxy is MeshRenderer && proxy.TryGetComponent<MeshFilter>(out var meshFilter))
                {
                    duplicated = Object.Instantiate(meshFilter.sharedMesh);
                    duplicated.name = meshFilter.sharedMesh.name + " (AAO Generated)";
                }
                else
                {
                    UnityEngine.Profiling.Profiler.EndSample();
                    return;
                }
            }

            if (await Process(original, proxy, components, duplicated, context))
            {
                if (proxy is SkinnedMeshRenderer skinned)
                    skinned.sharedMesh = duplicated;
                else if (proxy is MeshRenderer && proxy.TryGetComponent<MeshFilter>(out var meshFilter))
                    meshFilter.sharedMesh = duplicated;

                _duplicated = duplicated;
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }

        void IRenderFilterNode.OnFrame(Renderer original, Renderer proxy)
        {
            if (_duplicated == null) return;
            if (proxy is SkinnedMeshRenderer skinnedMeshProxy)
                skinnedMeshProxy.sharedMesh = _duplicated;
            else if (proxy is MeshRenderer && proxy.TryGetComponent<MeshFilter>(out var meshFilter))
                meshFilter.sharedMesh = _duplicated;
        }

        void IDisposable.Dispose()
        {
            if (_duplicated != null)
            {
                Object.DestroyImmediate(_duplicated);
                _duplicated = null;
            }
        }
    }
}
